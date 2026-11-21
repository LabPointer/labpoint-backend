using Backend.Handler;
using Data;
using Microsoft.EntityFrameworkCore;
using Models.Reserve;

namespace Backend.Services;

/// <summary>
/// Apoio à exclusão permanente feita pela coordenação. Algumas FKs (sessão de uso → reserva e recurso vinculado →
/// reserva de sala) não têm cascata no banco, então as reservas são apagadas aqui, na ordem que as FKs exigem:
/// sessões de uso (os itens caem em cascata) → reservas de recurso vinculadas → reservas.
/// Os chamadores devem rodar tudo dentro de uma transação.
/// </summary>
internal static class PermanentDeletion
{
    /// <summary>Confere se o texto digitado na confirmação é exatamente o nome do item.</summary>
    public static void EnsureConfirmed(string expectedName, string? typedName)
    {
        if (!string.Equals(expectedName.Trim(), typedName?.Trim(), StringComparison.Ordinal))
            throw new BadRequestException("O nome digitado não confere. Digite o nome exatamente como aparece para confirmar.");
    }

    public static async Task DeleteSpaceReserves(AppDbContext dbCtx, IQueryable<SpaceReserveModel> reserves)
    {
        var ids = reserves.Select(r => r.Id);

        await dbCtx.ReserveUsages
            .Where(u => u.FkSpaceReserveId != null && ids.Contains(u.FkSpaceReserveId.Value))
            .ExecuteDeleteAsync();

        await DeleteResourceReserves(
            dbCtx,
            dbCtx.ResourceReserves.Where(rr => rr.FkSpaceReserveId != null && ids.Contains(rr.FkSpaceReserveId.Value)));

        await reserves.ExecuteDeleteAsync();
    }

    public static async Task DeleteResourceReserves(AppDbContext dbCtx, IQueryable<ResourceReserveModel> reserves)
    {
        var ids = reserves.Select(r => r.Id);

        await dbCtx.ReserveUsages
            .Where(u => u.FkResourceReserveId != null && ids.Contains(u.FkResourceReserveId.Value))
            .ExecuteDeleteAsync();

        await reserves.ExecuteDeleteAsync();
    }
}
