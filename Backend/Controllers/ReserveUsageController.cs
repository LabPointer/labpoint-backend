using Backend.Services;
using DTOs.Error;
using DTOs.Usage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

/// <summary>
/// Check-in, relatório de uso e check-out das reservas, por turno. As rotas <c>token/...</c> são públicas: o token do
/// link enviado por e-mail é a credencial (uso único, válido só na janela da etapa).
/// </summary>
[ApiController]
[Route("usage")]
public class ReserveUsageController(IReserveUsageService usageService) : ControllerBase
{
    /// <summary>
    /// Sessão aberta por um link do e-mail
    /// </summary>
    /// <remarks>Dados da reserva no turno, relatório preenchido até agora e a etapa (check-in ou check-out) do link.</remarks>
    [HttpGet("token/{token}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(UsageTokenResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByToken([FromRoute] string token) =>
        Ok(await usageService.GetByToken(token));

    /// <summary>
    /// Check-in pelo link do e-mail
    /// </summary>
    /// <remarks>Exige ao menos o primeiro horário do turno. <c>RepeatForAll</c> copia a primeira linha para todos.</remarks>
    [HttpPost("token/{token}/check-in")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PostTokenCheckIn([FromRoute] string token, [FromBody] UsageEntriesRequestDTO data)
    {
        await usageService.TokenCheckIn(token, data);
        return NoContent();
    }

    /// <summary>
    /// Check-out pelo link do e-mail
    /// </summary>
    /// <remarks>Todos os horários do turno precisam terminar com entrada e saída preenchidas.</remarks>
    [HttpPost("token/{token}/check-out")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PostTokenCheckOut([FromRoute] string token, [FromBody] UsageEntriesRequestDTO data)
    {
        await usageService.TokenCheckOut(token, data);
        return NoContent();
    }

    /// <summary>
    /// Minhas sessões de uso
    /// </summary>
    /// <remarks>Mais recentes primeiro. Retorna até <c>Limit + 1</c> itens: o extra indica que há próxima página.</remarks>
    [HttpGet("mine")]
    [Authorize]
    [ProducesResponseType(typeof(IEnumerable<UsageSessionResponseDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMine([FromQuery] UsageRequestDTO query) =>
        Ok(await usageService.GetMine(User, query));

    /// <summary>
    /// Check-in de uma sessão própria
    /// </summary>
    [HttpPost("{usageId}/check-in")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PostCheckIn([FromRoute] long usageId, [FromBody] UsageEntriesRequestDTO data)
    {
        await usageService.CheckIn(User, usageId, data);
        return NoContent();
    }

    /// <summary>
    /// Preencher o relatório de uma sessão própria
    /// </summary>
    /// <remarks>Só entre o check-in e o check-out. Grava apenas os horários enviados.</remarks>
    [HttpPut("{usageId}/entries")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PutEntries([FromRoute] long usageId, [FromBody] UsageEntriesRequestDTO data)
    {
        await usageService.SaveEntries(User, usageId, data);
        return NoContent();
    }

    /// <summary>
    /// Check-out de uma sessão própria
    /// </summary>
    [HttpPost("{usageId}/check-out")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PostCheckOut([FromRoute] long usageId, [FromBody] UsageEntriesRequestDTO data)
    {
        await usageService.CheckOut(User, usageId, data);
        return NoContent();
    }

    /// <summary>
    /// Sessões de uso de uma reserva de sala
    /// </summary>
    /// <remarks>Dono da reserva ou Admin/Root. Lista vazia quando ainda não há sessões.</remarks>
    [HttpGet("reserve/space/{spaceReserveId}")]
    [Authorize]
    [ProducesResponseType(typeof(IEnumerable<UsageSessionResponseDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBySpaceReserve([FromRoute] long spaceReserveId) =>
        Ok(await usageService.GetBySpaceReserve(User, spaceReserveId));

    /// <summary>
    /// Sessões de uso de uma reserva de recurso
    /// </summary>
    /// <remarks>Reserva vinculada devolve as sessões da sala. Dono da reserva ou Admin/Root.</remarks>
    [HttpGet("reserve/resource/{resourceReserveId}")]
    [Authorize]
    [ProducesResponseType(typeof(IEnumerable<UsageSessionResponseDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByResourceReserve([FromRoute] long resourceReserveId) =>
        Ok(await usageService.GetByResourceReserve(User, resourceReserveId));

    /// <summary>
    /// Admin: listar sessões de uso
    /// </summary>
    /// <remarks>Sessões dos outros usuários no período. Retorna até <c>Limit + 1</c> itens.</remarks>
    [HttpGet("admin")]
    [Authorize(Roles = "Admin,Root")]
    [ProducesResponseType(typeof(IEnumerable<UsageSessionResponseDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AdminGet([FromQuery] AdminUsageRequestDTO query) =>
        Ok(await usageService.AdminGet(User, query));

    /// <summary>
    /// Admin: editar o relatório de uma sessão
    /// </summary>
    /// <remarks>Qualquer sessão com check-in, inclusive já encerrada.</remarks>
    [HttpPut("admin/{usageId}/entries")]
    [Authorize(Roles = "Admin,Root")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AdminPutEntries([FromRoute] long usageId, [FromBody] UsageEntriesRequestDTO data)
    {
        await usageService.AdminSaveEntries(User, usageId, data);
        return NoContent();
    }

    /// <summary>
    /// Admin: fechar o check-out de uma sessão
    /// </summary>
    /// <remarks>Para sessões em uso ou com o prazo de check-out vencido. Registra quem fechou.</remarks>
    [HttpPost("admin/{usageId}/check-out")]
    [Authorize(Roles = "Admin,Root")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AdminPostCheckOut([FromRoute] long usageId, [FromBody] UsageEntriesRequestDTO data)
    {
        await usageService.AdminCheckOut(User, usageId, data);
        return NoContent();
    }
}
