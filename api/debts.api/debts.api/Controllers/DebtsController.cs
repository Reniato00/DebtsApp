using application.Services;
using debts.api.Models;
using debts.api.Requests.Debts;
using domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace debts.api.Controllers
{
    /// <summary>
    /// Operaciones CRUD y utilidades para la gestión de deudas.
    /// </summary>
    [ApiController]
    [Route("api/debt")]
    public class DebtsController : ControllerBase
    {
        private readonly IDebtService debtService;
        public DebtsController(IDebtService debtService)
        {
            this.debtService = debtService;
        }

        /// <summary>
        /// Crea una nueva deuda para un usuario.
        /// </summary>
        /// <param name="body">Datos de la deuda a crear.</param>
        /// <returns>La deuda creada con su Id asignado.</returns>
        [HttpPost("create")]
        public async Task<IActionResult> CreateDebt([FromBody] CreateDebtRequest body)
        {
            var debt = new Debt
            {
                UserId = body.UserId,
                Name = body.Name,
                OriginalAmount = body.OriginalAmount,
                CurrentBalance = body.CurrentBalance,
                InterestRate = body.InterestRate,
                MonthlyPayment = body.MonthlyPayment,
                StartDate = body.StartDate,
                DueDate = body.DueDate
            };
            var created = await debtService.CreateDebtAsync(debt);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        /// <summary>
        /// Obtiene una deuda por su Id.
        /// </summary>
        /// <param name="id">Id de la deuda.</param>
        /// <param name="userId">Id del usuario (header).</param>
        /// <returns>La deuda si existe; 404 en caso contrario.</returns>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id, [FromHeader] Guid userId)
        {
            var debt = await debtService.GetByIdAsync(id, userId);
            if (debt == null) return NotFound();
            return Ok(debt);
        }

        /// <summary>
        /// Obtiene todas las deudas de un usuario.
        /// </summary>
        /// <param name="userId">Id del usuario (header).</param>
        /// <returns>Lista de deudas.</returns>
        [HttpGet("all")]
        public async Task<IActionResult> GetAllDebts([FromHeader] Guid userId)
        {
            var debts = await debtService.GetAllDebtsAsync(userId);
            return Ok(debts);
        }

        /// <summary>
        /// Obtiene todas las deudas de un usuario con paginación.
        /// </summary>
        /// <param name="userId">Id del usuario (header).</param>
        /// <param name="page">Número de página (default: 1).</param>
        /// <param name="pageSize">Elementos por página (default: 10).</param>
        /// <returns>Lista paginada de deudas con total de registros.</returns>
        [HttpGet("all/paged")]
        public async Task<IActionResult> GetAllDebtsPaged([FromHeader] Guid userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var (items, totalCount) = await debtService.GetAllDebtsPagedAsync(userId, page, pageSize);
            return Ok(new PagedResult<Debt>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            });
        }

        /// <summary>
        /// Actualiza una deuda existente.
        /// </summary>
        /// <param name="id">Id de la deuda a actualizar.</param>
        /// <param name="userId">Id del usuario (header).</param>
        /// <param name="body">Datos actualizados de la deuda.</param>
        /// <returns>La deuda actualizada; 404 si no existe.</returns>
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateDebt(Guid id, [FromHeader] Guid userId, [FromBody] UpdateDebtRequest body)
        {
            var debt = new Debt
            {
                Id = id,
                UserId = userId,
                Name = body.Name,
                OriginalAmount = body.OriginalAmount,
                CurrentBalance = body.CurrentBalance,
                InterestRate = body.InterestRate,
                MonthlyPayment = body.MonthlyPayment,
                StartDate = body.StartDate,
                DueDate = body.DueDate
            };
            var updated = await debtService.UpdateDebtAsync(debt);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        /// <summary>
        /// Elimina una deuda por su Id.
        /// </summary>
        /// <param name="id">Id de la deuda a eliminar.</param>
        /// <param name="userId">Id del usuario (header).</param>
        /// <returns>204 si se eliminó; 404 si no existe.</returns>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteDebt(Guid id, [FromHeader] Guid userId)
        {
            var deleted = await debtService.DeleteDebtAsync(id, userId);
            if (!deleted) return NotFound();
            return NoContent();
        }

        /// <summary>
        /// Liquida una deuda estableciendo su saldo actual a cero.
        /// </summary>
        /// <param name="id">Id de la deuda a liquidar.</param>
        /// <param name="userId">Id del usuario (header).</param>
        /// <returns>204 si se liquidó; 404 si no existe.</returns>
        [HttpPatch("{id:guid}/pay-off")]
        public async Task<IActionResult> PayOffDebt(Guid id, [FromHeader] Guid userId)
        {
            var paid = await debtService.PayOffDebtAsync(id, userId);
            if (!paid) return NotFound();
            return NoContent();
        }

        /// <summary>
        /// Busca deudas por nombre (búsqueda parcial).
        /// </summary>
        /// <param name="userId">Id del usuario (header).</param>
        /// <param name="q">Término de búsqueda.</param>
        /// <returns>Lista de deudas que coinciden con el término.</returns>
        [HttpGet("search")]
        public async Task<IActionResult> SearchDebts([FromHeader] Guid userId, [FromQuery] string q)
        {
            var debts = await debtService.SearchDebtsAsync(userId, q);
            return Ok(debts);
        }

        /// <summary>
        /// Obtiene las deudas vencidas (DueDate menor a la fecha actual).
        /// </summary>
        /// <param name="userId">Id del usuario (header).</param>
        /// <returns>Lista de deudas vencidas.</returns>
        [HttpGet("overdue")]
        public async Task<IActionResult> GetOverdueDebts([FromHeader] Guid userId)
        {
            var debts = await debtService.GetOverdueDebtsAsync(userId);
            return Ok(debts);
        }
    }
}
