using System.Security.Claims;
using Backend.Handler;
using Data;
using DTOs.Account;
using DTOs.Subject;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Models;

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

        var account = await dbCtx.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (account == null)
        {
            throw new UnauthorizedException("Usuário não identificado.", true);
        }

        var accountSubject = await dbCtx.AccountSubjects
            .Include(accountSubject => accountSubject.Subject)
            .FirstOrDefaultAsync(accountSubject => accountSubject.FkAccountId == account.Id);
        
        var subjectDto = accountSubject?.Subject == null
            ? null
            : new SubjectResponseDTO(
                accountSubject.Subject.Id,
                accountSubject.Subject.Name,
                accountSubject.Subject.Enabled
            );
        var accountUserDto = new AccountResponseDTO(
            account.Id,
            account.Registration,
            account.UserName,
            account.Email,
            dbCtx.UserRoles
                .Join(
                    dbCtx.Roles,
                    userRole => userRole.RoleId,
                    role => role.Id,
                    (userRole, role) => new { userRole.UserId, role.Name })
                .Any(role => role.UserId == account.Id && role.Name == nameof(EAccountRole.Owner))
                ? EAccountRole.Owner
                : dbCtx.UserRoles
                    .Join(
                        dbCtx.Roles,
                        userRole => userRole.RoleId,
                        role => role.Id,
                        (userRole, role) => new { userRole.UserId, role.Name })
                    .Any(role => role.UserId == account.Id && role.Name == nameof(EAccountRole.Admin))
                    ? EAccountRole.Admin
                    : EAccountRole.User,
            subjectDto,
            account.LockoutEnd == null
        );

        return accountUserDto;
    }

    public async Task EditAccount(ClaimsPrincipal user, AccountEditRequestDTO data)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)!.Value;

        var account = await dbCtx.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (account == null)
        {
            throw new UnauthorizedException("Usuário não identificado.", true);
        }

        account.UserName = data.Username;
        await dbCtx.SaveChangesAsync();
    }

    public async Task<IEnumerable<AccountResponseDTO>> AdminGetUsers(ClaimsPrincipal user, AdminAccountRequestDTO query)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)!.Value;

        var usersQuery = dbCtx.Users.AsQueryable();

        usersQuery = usersQuery.Where(u => u.Id != userId);
        
        usersQuery = usersQuery.Where(u => u.EmailConfirmed == true);

        if (!string.IsNullOrEmpty(query.SearchQuery))
        {
            usersQuery = usersQuery.Where(s => 
                EF.Functions.ToTsVector("portuguese", s.Registration+ " " + s.UserName + " " + s.Email)
                    .Matches(EF.Functions.WebSearchToTsQuery("portuguese", query.SearchQuery))
            );
        }

        if (query.Role.HasValue)
        {
            var roleName = query.Role.Value.ToString();
            usersQuery = usersQuery.Where(u =>
                dbCtx.UserRoles
                    .Join(
                        dbCtx.Roles,
                        userRole => userRole.RoleId,
                        role => role.Id,
                        (userRole, role) => new { userRole.UserId, role.Name })
                    .Any(role => role.UserId == u.Id && role.Name == roleName)
            );
        }

        var accountsRes = await usersQuery
            .Take(query.Limit + 1)
            .Skip(query.Page * query.Limit)
            .ToListAsync();

        if (accountsRes.Count == 0)
        {
            throw new ResourceNotFoundException("Nenhum usuário encontrado com os filtros informados.");
        }

        var accounts = accountsRes.Select(accountUser =>
        {
            var accountSubject = dbCtx.AccountSubjects
                .Include(accountSubject => accountSubject.Subject)
                .FirstOrDefault(accountSubject => accountSubject.FkAccountId == accountUser.Id);

            var subjectDto = accountSubject?.Subject == null
                ? null
                : new SubjectResponseDTO(
                    accountSubject.Subject.Id,
                    accountSubject.Subject.Name,
                    accountSubject.Subject.Enabled
                );

            return new AccountResponseDTO(
                accountUser.Id,
                accountUser.Registration,
                accountUser.UserName,
                accountUser.Email,
                dbCtx.UserRoles
                    .Join(
                        dbCtx.Roles,
                        userRole => userRole.RoleId,
                        role => role.Id,
                        (userRole, role) => new { userRole.UserId, role.Name })
                    .Any(role => role.UserId == accountUser.Id && role.Name == nameof(EAccountRole.Owner))
                    ? EAccountRole.Owner
                    : dbCtx.UserRoles
                        .Join(
                            dbCtx.Roles,
                            userRole => userRole.RoleId,
                            role => role.Id,
                            (userRole, role) => new { userRole.UserId, role.Name })
                        .Any(role => role.UserId == accountUser.Id && role.Name == nameof(EAccountRole.Admin))
                        ? EAccountRole.Admin
                        : EAccountRole.User,
                subjectDto,
                accountUser.LockoutEnd == null
            );
        });

        return accounts;
    }

    public async Task AdminEditAccount(ClaimsPrincipal user, AdminAccountEditRequestDTO data)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)!.Value;

        if (userId == data.Id)
        {
            throw new BadRequestException("Não é possível editar a própria conta.");
        }

        var account = await dbCtx.Users.FirstOrDefaultAsync(u => u.Id == data.Id);
        if (account == null)
        {
            throw new ResourceNotFoundException("Usuário não encontrado.");
        }

        if (!string.IsNullOrEmpty(data.Username))
        {
            account.UserName = data.Username;
            account.NormalizedUserName = data.Username.ToUpper();
        }

        if (!string.IsNullOrEmpty(data.Registration))
        {
            account.Registration = data.Registration;
        }

        if (data.Role.HasValue)
        {
            var roleName = data.Role.Value.ToString();
            var role = await dbCtx.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
            if (role == null)
            {
                throw new BadRequestException("Cargo nao existe.");
            }

            var userRole = await dbCtx.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == data.Id);
            if (userRole != null)
            {
                if (userRole.RoleId != role.Id)
                {
                    dbCtx.UserRoles.Remove(userRole);

                    await dbCtx.UserRoles.AddAsync(new IdentityUserRole<string>
                    {
                        UserId = data.Id,
                        RoleId = role.Id
                    });
                }
            }
            else
            {
                await dbCtx.UserRoles.AddAsync(new IdentityUserRole<string>
                {
                    UserId = data.Id,
                    RoleId = role.Id
                });
            }
        }

        if (data.LockAccount.HasValue)
        {
            account.LockoutEnabled = data.LockAccount.Value;

            if (data.LockAccount.Value)
            {
                account.LockoutEnd = DateTimeOffset.MaxValue;
            }
            else
            {
                account.LockoutEnd = null;
            }
        }
        
        if (data.subjectId.HasValue)
        {
            var subjectExists = await dbCtx.Subjects
                .AnyAsync(subject => subject.Id == data.subjectId.Value);

            if (!subjectExists)
            {
                throw new ResourceNotFoundException("Disciplina não encontrada.");
            }

            var accountSubjects = await dbCtx.AccountSubjects
                .Where(accountSubject => accountSubject.FkAccountId == data.Id)
                .ToListAsync();

            var accountSubject = accountSubjects.FirstOrDefault();

            if (accountSubject == null)
            {
                await dbCtx.AccountSubjects.AddAsync(new AccountSubjectModel
                {
                    FkAccountId = data.Id,
                    FkSubjectId = data.subjectId.Value
                });
            }
            else
            {
                accountSubject.FkSubjectId = data.subjectId.Value;

                var duplicatedAccountSubjects = accountSubjects
                    .Where(duplicatedAccountSubject => duplicatedAccountSubject.Id != accountSubject.Id)
                    .ToList();

                if (duplicatedAccountSubjects.Count > 0)
                {
                    dbCtx.AccountSubjects.RemoveRange(duplicatedAccountSubjects);
                }
            }
        }

        await dbCtx.SaveChangesAsync();
    }
}