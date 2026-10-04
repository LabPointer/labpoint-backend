using System.Security.Claims;
using Backend.Handler;
using Data;
using Microsoft.EntityFrameworkCore;
using Models;

namespace Backend.Services;

public interface IAccountService
{
    public Task<AccountModel> GetAccount(ClaimsPrincipal principal);
}

public class AccountService(AppDbContext dbCtx) : IAccountService
{
    public async Task<AccountModel> GetAccount(ClaimsPrincipal principal)
    {
        string userId = principal.FindFirst(ClaimTypes.NameIdentifier)!.Value;

        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedException("Usuário não identificado.", true);
        }

        var user = await dbCtx.Users.FirstAsync(u => u.Id == userId);
        if (user == null)
        {
            throw new UnauthorizedException("Usuário não identificado.", true);
        }

        return user;
    }
}