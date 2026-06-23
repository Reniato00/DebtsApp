using application.Services;
using debts.api.Models;
using debts.api.Requests.Payments;
using Microsoft.AspNetCore.Mvc;
using persistence.Models;

namespace debts.api.Controllers
{
    /// <summary>
    /// Operaciones CRUD y consultas para pagos asociados a deudas.
    /// </summary>
    [ApiController]
    [Route("api/payment")]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService paymentService;

        public PaymentsController(IPaymentService paymentService)
        {
            this.paymentService = paymentService;
        }

        /// <summary>
        /// Registra un pago y reduce el saldo actual de la deuda correspondiente.
        /// </summary>
        /// <param name="body">Datos del pago (DebtId y Amount).</param>
        /// <returns>El pago creado con su Id asignado.</returns>
        [HttpPost]
        public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentRequest body)
        {
            var payment = await paymentService.CreatePaymentAsync(body.DebtId, body.Amount);
            return CreatedAtAction(nameof(GetById), new { id = payment.Id }, payment);
        }

        /// <summary>
        /// Obtiene un pago por su Id.
        /// </summary>
        /// <param name="id">Id del pago.</param>
        /// <returns>El pago si existe; 404 en caso contrario.</returns>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var payment = await paymentService.GetByIdAsync(id);
            if (payment == null) return NotFound();
            return Ok(payment);
        }

        /// <summary>
        /// Obtiene todos los pagos de una deuda específica.
        /// </summary>
        /// <param name="debtId">Id de la deuda.</param>
        /// <returns>Lista de pagos ordenados por fecha descendente.</returns>
        [HttpGet("debt/{debtId:guid}")]
        public async Task<IActionResult> GetByDebtId(Guid debtId)
        {
            var payments = await paymentService.GetByDebtIdAsync(debtId);
            return Ok(payments);
        }

        /// <summary>
        /// Actualiza el monto de un pago y ajusta el saldo de la deuda asociada.
        /// </summary>
        /// <param name="id">Id del pago a actualizar.</param>
        /// <param name="body">Nuevo monto del pago.</param>
        /// <returns>204 si se actualizó; 404 si no existe.</returns>
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdatePayment(Guid id, [FromBody] UpdatePaymentRequest body)
        {
            var updated = await paymentService.UpdatePaymentAsync(id, body.Amount);
            if (!updated) return NotFound();
            return NoContent();
        }

        /// <summary>
        /// Elimina un pago y revierte el saldo de la deuda asociada.
        /// </summary>
        /// <param name="id">Id del pago a eliminar.</param>
        /// <returns>204 si se eliminó; 404 si no existe.</returns>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeletePayment(Guid id)
        {
            var deleted = await paymentService.DeletePaymentAsync(id);
            if (!deleted) return NotFound();
            return NoContent();
        }

        /// <summary>
        /// Obtiene el historial de pagos de un usuario con el nombre de la deuda asociada.
        /// </summary>
        /// <param name="userId">Id del usuario (header).</param>
        /// <returns>Lista de pagos con nombre de deuda.</returns>
        [HttpGet("history")]
        public async Task<IActionResult> GetHistory([FromHeader] Guid userId)
        {
            var payments = await paymentService.GetPaymentHistoryWithDebtNameAsync(userId);
            return Ok(payments);
        }

        /// <summary>
        /// Obtiene el historial de pagos paginado con el nombre de la deuda asociada.
        /// </summary>
        /// <param name="userId">Id del usuario (header).</param>
        /// <param name="page">Número de página (default: 1).</param>
        /// <param name="pageSize">Elementos por página (default: 10).</param>
        /// <returns>Lista paginada de pagos con total de registros.</returns>
        [HttpGet("history/paged")]
        public async Task<IActionResult> GetHistoryPaged([FromHeader] Guid userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var (items, totalCount) = await paymentService.GetPaymentHistoryWithDebtNamePagedAsync(userId, page, pageSize);
            return Ok(new PagedResult<PaymentWithDebtName>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            });
        }

        /// <summary>
        /// Obtiene el total pagado para una deuda específica.
        /// </summary>
        /// <param name="debtId">Id de la deuda.</param>
        /// <returns>Objeto con el total pagado.</returns>
        [HttpGet("debt/{debtId:guid}/total")]
        public async Task<IActionResult> GetTotalByDebt(Guid debtId)
        {
            var total = await paymentService.GetTotalByDebtAsync(debtId);
            return Ok(new { TotalPaid = total });
        }
    }
}
