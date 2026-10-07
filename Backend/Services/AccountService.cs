using System.Security.Claims;
using Backend.Handler;
using Data;
using DTOs.Account;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Models;

namespace Backend.Services;

public interface IAccountService
{
    public Task<AccountResponseDTO> GetAccount(ClaimsPrincipal user);

    public Task EditAccount(ClaimsPrincipal user, AccountEditRequestDTO data);

    public Task<IEnumerable<AccountResponseDTO>> AdminGetUsers(ClaimsPrincipal user, AdminAccountRequestDTO query);

    public Task AdminEditAccount(ClaimsPrincipal user, string accountId, AdminAccountEditRequestDTO data);
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
                    : EAccountRole.User
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
            .Take(query.Limit)
            .Skip(query.Page * query.Limit)
            .ToListAsync();

        if (accountsRes.Count == 0)
        {
            throw new ResourceNotFoundException("Nenhum usuário encontrado com os filtros informados.");
        }

        var accounts = accountsRes.Select(accountUser => new AccountResponseDTO(
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
                    : EAccountRole.User
            )
        );

        return accounts;
    }

    public async Task AdminEditAccount(ClaimsPrincipal user, string accountId, AdminAccountEditRequestDTO data)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)!.Value;

        if (userId == accountId)
        {
            throw new BadRequestException("Não é possível editar a própria conta.");
        }

        var account = await dbCtx.Users.FirstOrDefaultAsync(u => u.Id == accountId);
        if (account == null)
        {
            throw new ResourceNotFoundException("Usuário não encontrado.");
        }

        if (!string.IsNullOrEmpty(data.Username))
        {
            account.UserName = data.Username;
            account.NormalizedUserName = data.Username;
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
                throw new BadRequestException("Função inválida.");
            }

            var userRole = await dbCtx.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == accountId);
            if (userRole != null)
            {
                if (userRole.RoleId != role.Id)
                {
                    dbCtx.UserRoles.Remove(userRole);

                    await dbCtx.UserRoles.AddAsync(new IdentityUserRole<string>
                    {
                        UserId = accountId,
                        RoleId = role.Id
                    });
                }
            }
            else
            {
                await dbCtx.UserRoles.AddAsync(new IdentityUserRole<string>
                {
                    UserId = accountId,
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

        await dbCtx.SaveChangesAsync();
    }
}