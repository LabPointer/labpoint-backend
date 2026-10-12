using DTOs.Subject;
using Data;
using Microsoft.EntityFrameworkCore;
using Models;

namespace Backend.Services;

public static class QueryHelpers
{
    /// <summary>
    /// Padrão para ILIKE "contém": escapa %, _ e \ digitados pelo usuário.
    /// Substitui a busca full-text, que só casava palavras inteiras (não achava "joa" em "joão") e
    /// devolvia vazio quando qualquer campo concatenado era nulo.
    /// </summary>
    public static string ContainsPattern(string term)
    {
        var escaped = term.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
        return $"%{escaped}%";
    }
}

/// <summary>Linha escalar de uma conta (sem cargo/matéria), lida em uma única consulta.</summary>
public sealed record AccountRow(string Id, string Registration, string? UserName, string? Email, DateTimeOffset? LockoutEnd);

/// <summary>
/// Cargo e matéria de várias contas em lote (2 consultas no total, independente da quantidade de contas),
/// no lugar de 3 consultas por conta.
/// </summary>
public static class AccountDirectory
{
    public static EAccountRole ResolveRole(IEnumerable<string?> roleNames)
    {
        var names = roleNames.ToHashSet();
        if (names.Contains(nameof(EAccountRole.Owner))) return EAccountRole.Owner;
        if (names.Contains(nameof(EAccountRole.Admin))) return EAccountRole.Admin;
        return EAccountRole.User;
    }

    public static async Task<Dictionary<string, EAccountRole>> LoadRoles(AppDbContext dbCtx, IReadOnlyCollection<string> userIds)
    {
        if (userIds.Count == 0) return new Dictionary<string, EAccountRole>();

        var rows = await dbCtx.UserRoles.AsNoTracking()
            .Where(ur => userIds.Contains(ur.UserId))
            .Join(dbCtx.Roles, ur => ur.RoleId, role => role.Id, (ur, role) => new { ur.UserId, role.Name })
            .ToListAsync();

        return rows
            .GroupBy(row => row.UserId)
            .ToDictionary(group => group.Key, group => ResolveRole(group.Select(row => row.Name)));
    }

    public static async Task<Dictionary<string, SubjectResponseDTO>> LoadSubjects(AppDbContext dbCtx, IReadOnlyCollection<string> userIds)
    {
        if (userIds.Count == 0) return new Dictionary<string, SubjectResponseDTO>();

        var rows = await dbCtx.AccountSubjects.AsNoTracking()
            .Where(accountSubject => userIds.Contains(accountSubject.FkAccountId))
            .OrderBy(accountSubject => accountSubject.Id)
            .Select(accountSubject => new
            {
                accountSubject.FkAccountId,
                Subject = new SubjectResponseDTO(accountSubject.Subject.Id, accountSubject.Subject.Name, accountSubject.Subject.Enabled)
            })
            .ToListAsync();

        // uma matéria por conta: a primeira vinculada
        return rows
            .GroupBy(row => row.FkAccountId)
            .ToDictionary(group => group.Key, group => group.First().Subject);
    }

    public static DTOs.Account.AccountResponseDTO ToDto(
        AccountRow row,
        IReadOnlyDictionary<string, EAccountRole> roles,
        IReadOnlyDictionary<string, SubjectResponseDTO> subjects)
        => new(
            row.Id,
            row.Registration,
            row.UserName ?? string.Empty,
            row.Email ?? string.Empty,
            roles.GetValueOrDefault(row.Id, EAccountRole.User),
            subjects.GetValueOrDefault(row.Id),
            row.LockoutEnd == null);
}
