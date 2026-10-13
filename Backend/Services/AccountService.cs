using System.Security.Claims;
using Backend.Handler;
using Data;
using DTOs.Account;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.Account;

namespace Backend.Services;

public interface IAccountService
{
    public Task<AccountResponseDTO> GetAccount(ClaimsPrincipal user);

    public Task EditAccount(ClaimsPrincipal user, AccountEditRequestDTO data);

    public Task<IEnumerable<AccountResponseDTO>> AdminGetUsers(ClaimsPrincipal user, AdminAccountRequestDTO query);

    public Task AdminEditAccount(ClaimsPrincipal user, AdminAccountEditRequestDTO data);
}

public class AccountService(AppDbContext dbCtx) : IAccountService
{
    public async Task<AccountResponseDTO> GetAccount(ClaimsPrincipal user)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)!.Value;

        var account = await dbCtx.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new AccountRow(u.Id, u.Registration, u.UserName, u.Email, u.LockoutEnd))
            .FirstOrDefaultAsync();

        if (account == null)
        {
            throw new UnauthorizedException("Usuário não identificado.", true);
        }

        var ids = new[] { account.Id };
        var roles = await AccountDirectory.LoadRoles(dbCtx, ids);
        var subjects = await AccountDirectory.LoadSubjects(dbCtx, ids);

        return AccountDirectory.ToDto(account, roles, subjects);
    }

    public async Task EditAccount(ClaimsPrincipal user, AccountEditRequestDTO data)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)!.Value;

        var account = await dbCtx.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (account == null)
        {
            throw new UnauthorizedException("Usuário não identificado.", true);
        }

        await ApplyUserName(account, data.Username);
        await dbCtx.SaveChangesAsync();
    }

    public async Task<IEnumerable<AccountResponseDTO>> AdminGetUsers(ClaimsPrincipal user, AdminAccountRequestDTO query)
    {
        var actorId = user.FindFirst(ClaimTypes.NameIdentifier)!.Value;

        var usersQuery = dbCtx.Users.AsNoTracking()
            .Where(u => u.Id != actorId && u.EmailConfirmed);

        // Busca por trecho (nome, matrícula ou e-mail), sem diferenciar maiúsculas de minúsculas.
        if (!string.IsNullOrWhiteSpace(query.SearchQuery))
        {
            var pattern = QueryHelpers.ContainsPattern(query.SearchQuery);
            usersQuery = usersQuery.Where(u =>
                EF.Functions.ILike(u.Registration, pattern)
                || (u.UserName != null && EF.Functions.ILike(u.UserName, pattern))
                || (u.Email != null && EF.Functions.ILike(u.Email, pattern)));
        }

        if (query.Role.HasValue)
        {
            var roleName = query.Role.Value.ToString();
            var roleId = await dbCtx.Roles
                .Where(r => r.Name == roleName)
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            usersQuery = roleId == null
                ? usersQuery.Where(_ => false)
                : usersQuery.Where(u => dbCtx.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == roleId));
        }

        if (query.Enabled.HasValue)
        {
            usersQuery = query.Enabled.Value
                ? usersQuery.Where(u => u.LockoutEnd == null)
                : usersQuery.Where(u => u.LockoutEnd != null);
        }

        if (query.SubjectId.HasValue)
        {
            var subjectId = query.SubjectId.Value;
            usersQuery = usersQuery.Where(u =>
                dbCtx.AccountSubjects.Any(accountSubject =>
                    accountSubject.FkAccountId == u.Id && accountSubject.FkSubjectId == subjectId));
        }

        // Ordenação estável: sem ela as páginas podem repetir ou pular usuários.
        var rows = await usersQuery
            .OrderBy(u => u.UserName)
            .ThenBy(u => u.Id)
            .Skip(query.Page * query.Limit)
            .Take(query.Limit + 1)
            .Select(u => new AccountRow(u.Id, u.Registration, u.UserName, u.Email, u.LockoutEnd))
            .ToListAsync();

        if (rows.Count == 0)
        {
            throw new ResourceNotFoundException("Nenhum usuário encontrado com os filtros informados.");
        }

        // Cargo e matéria de todos os usuários da página em lote (antes: 3 consultas por usuário).
        var ids = rows.Select(row => row.Id).ToList();
        var roles = await AccountDirectory.LoadRoles(dbCtx, ids);
        var subjects = await AccountDirectory.LoadSubjects(dbCtx, ids);

        return rows.Select(row => AccountDirectory.ToDto(row, roles, subjects)).ToList();
    }

    public async Task AdminEditAccount(ClaimsPrincipal user, AdminAccountEditRequestDTO data)
    {
        var actorId = user.FindFirst(ClaimTypes.NameIdentifier)!.Value;

        if (actorId == data.Id)
        {
            throw new BadRequestException("Não é possível editar a própria conta.");
        }

        var account = await dbCtx.Users.FirstOrDefaultAsync(u => u.Id == data.Id);
        if (account == null)
        {
            throw new ResourceNotFoundException("Usuário não encontrado.");
        }

        // O cargo vem do banco (e não do cookie): o cookie só é atualizado na próxima revalidação da sessão.
        var roles = await AccountDirectory.LoadRoles(dbCtx, [actorId, data.Id]);
        var actorRole = roles.GetValueOrDefault(actorId, EAccountRole.User);
        var targetRole = roles.GetValueOrDefault(data.Id, EAccountRole.User);

        if (actorRole < EAccountRole.Admin)
        {
            throw new ForbiddenException("Você não tem permissão para editar usuários.");
        }

        // Regra 1: somente o Owner altera cargos.
        var roleChanged = data.Role.HasValue && data.Role.Value != targetRole;
        if (roleChanged && actorRole != EAccountRole.Owner)
        {
            throw new ForbiddenException("Somente o proprietário (Owner) pode alterar o cargo de um usuário.");
        }

        // Regra 2: ativar/desativar a conta exige cargo estritamente superior ao do usuário alvo
        // (Admin só altera User; Owner altera User e Admin, nunca outro Owner).
        var lockChanged = data.LockAccount.HasValue && data.LockAccount.Value != (account.LockoutEnd != null);
        if (lockChanged && actorRole <= targetRole)
        {
            throw new ForbiddenException(
                "Você não pode ativar ou desativar a conta de um usuário com cargo igual ou superior ao seu.");
        }

        if (!string.IsNullOrEmpty(data.Username))
        {
            await ApplyUserName(account, data.Username);
        }

        if (!string.IsNullOrEmpty(data.Registration) && data.Registration != account.Registration)
        {
            if (data.Registration.Length > 16)
            {
                throw new BadRequestException("A matrícula deve ter no máximo 16 caracteres.");
            }

            var registrationInUse = await dbCtx.Users.AnyAsync(u => u.Id != data.Id && u.Registration == data.Registration);
            if (registrationInUse)
            {
                throw new ConflictException("Já existe um usuário com esta matrícula.");
            }

            account.Registration = data.Registration;
        }

        if (roleChanged)
        {
            var roleName = data.Role!.Value.ToString();
            var roleId = await dbCtx.Roles
                .Where(r => r.Name == roleName)
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            if (roleId == null)
            {
                throw new BadRequestException("Cargo nao existe.");
            }

            // Um cargo por usuário: remove os demais e garante o novo.
            var currentRoles = await dbCtx.UserRoles.Where(ur => ur.UserId == data.Id).ToListAsync();
            dbCtx.UserRoles.RemoveRange(currentRoles.Where(ur => ur.RoleId != roleId));

            if (currentRoles.All(ur => ur.RoleId != roleId))
            {
                dbCtx.UserRoles.Add(new IdentityUserRole<string> { UserId = data.Id, RoleId = roleId });
            }
        }

        if (lockChanged)
        {
            var locked = data.LockAccount!.Value;

            // O bloqueio é controlado só por LockoutEnd; LockoutEnabled precisa continuar true
            // (antes era gravado como false ao reativar, desligando o bloqueio por tentativas falhas de login).
            account.LockoutEnabled = true;
            account.LockoutEnd = locked ? DateTimeOffset.MaxValue : null;

            if (!locked)
            {
                account.AccessFailedCount = 0;
            }
        }

        if (data.SubjectId.HasValue)
        {
            var subjectExists = await dbCtx.Subjects.AnyAsync(subject => subject.Id == data.SubjectId.Value);
            if (!subjectExists)
            {
                throw new ResourceNotFoundException("Disciplina não encontrada.");
            }

            var accountSubjects = await dbCtx.AccountSubjects
                .Where(accountSubject => accountSubject.FkAccountId == data.Id)
                .OrderBy(accountSubject => accountSubject.Id)
                .ToListAsync();

            var accountSubject = accountSubjects.FirstOrDefault();

            if (accountSubject == null)
            {
                dbCtx.AccountSubjects.Add(new AccountSubjectModel
                {
                    FkAccountId = data.Id,
                    FkSubjectId = data.SubjectId.Value
                });
            }
            else
            {
                accountSubject.FkSubjectId = data.SubjectId.Value;

                if (accountSubjects.Count > 1)
                {
                    dbCtx.AccountSubjects.RemoveRange(accountSubjects.Skip(1));
                }
            }
        }

        // Troca de cargo ou bloqueio invalida as sessões abertas do usuário na próxima revalidação do cookie.
        if (roleChanged || lockChanged)
        {
            account.SecurityStamp = Guid.NewGuid().ToString();
        }

        await dbCtx.SaveChangesAsync();
    }

    /// <summary>Define o nome de usuário mantendo o valor normalizado em sincronia e checando duplicidade.</summary>
    private async Task ApplyUserName(AccountModel account, string username)
    {
        if (username == account.UserName)
        {
            return;
        }

        var normalized = username.ToUpperInvariant();
        var inUse = await dbCtx.Users.AnyAsync(u => u.Id != account.Id && u.NormalizedUserName == normalized);
        if (inUse)
        {
            throw new ConflictException("Já existe um usuário com este nome.");
        }

        account.UserName = username;
        account.NormalizedUserName = normalized;
    }
}
