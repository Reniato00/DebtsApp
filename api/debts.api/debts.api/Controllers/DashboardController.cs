using application.Services;
using debts.api.Responses;
using Microsoft.AspNetCore.Mvc;
using persistence.Models;

namespace debts.api.Controllers
{
    /// <summary>
    /// Reportes y métricas del dashboard financiero.
    /// </summary>
    [ApiController]
    [Route("api/dashboard")]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService dashboardService;
        private readonly IPaymentService paymentService;
        public DashboardController(IDashboardService dashboardService, IPaymentService paymentService)
        {
            this.dashboardService = dashboardService;
            this.paymentService = paymentService;
        }

        /// <summary>
        /// Obtiene la cantidad total de deudas de un usuario.
        /// </summary>
        /// <param name="userId">Id del usuario (header).</param>
        /// <returns>Objeto con el total de deudas.</returns>
        [HttpGet("debt-count")]
        public async Task<IActionResult> GetDebtCount([FromHeader] Guid userId)
        {
            var count = await dashboardService.GetDebtCountAsync(userId);
            var response = new DebtsCountResponse { TotalDebts = count };
            return Ok(response);
        }

        /// <summary>
        /// Obtiene el monto total adeudado (suma de saldos actuales).
        /// </summary>
        /// <param name="userId">Id del usuario (header).</param>
        /// <returns>Objeto con el monto total.</returns>
        [HttpGet("total-amount")]
        public async Task<IActionResult> GetTotalAmountDebtsAsync([FromHeader] Guid userId)
        {
            var totalAmount = await dashboardService.GetTotalAmountDebtsAsync(userId);
            var response = new
            {
                TotalAmount = totalAmount
            };
            return Ok(response);
        }

        /// <summary>
        /// Obtiene la suma total de pagos mensuales de todas las deudas.
        /// </summary>
        /// <param name="userId">Id del usuario (header).</param>
        /// <returns>Objeto con el total de pagos mensuales.</returns>
        [HttpGet("total-payment-amount")]
        public async Task<IActionResult> GetTotalAmountMonthlyPaymentAsync([FromHeader] Guid userId)
        {
            var total = await dashboardService.GetTotalAmountMonthlyPaymentAsync(userId);
            var response = new { TotalAmountMonthlyPayment = total };
            return Ok(response);
        }

        /// <summary>
        /// Obtiene un resumen completo de deudas: total, monto, pago mensual y tasa de interés promedio.
        /// </summary>
        /// <param name="userId">Id del usuario (header).</param>
        /// <returns>Resumen de deudas.</returns>
        [HttpGet("summary")]
        public async Task<IActionResult> GetDebtSummary([FromHeader] Guid userId)
        {
            var summary = await dashboardService.GetDebtSummaryAsync(userId);
            return Ok(summary);
        }

        /// <summary>
        /// Obtiene las deudas con pagos próximos a vencer (DueDate >= hoy), ordenadas por fecha ascendente.
        /// </summary>
        /// <param name="userId">Id del usuario (header).</param>
        /// <returns>Lista de deudas próximas a vencer.</returns>
        [HttpGet("upcoming-payments")]
        public async Task<IActionResult> GetUpcomingPayments([FromHeader] Guid userId)
        {
            var debts = await dashboardService.GetUpcomingPaymentsAsync(userId);
            var result = debts.Select(d => new UpcomingPaymentResponse
            {
                DebtId = d.Id,
                DebtName = d.Name,
                Amount = d.MonthlyPayment,
                DueDate = d.DueDate,
                DaysUntilDue = Math.Max(0, (int)(d.DueDate.Date - DateTime.UtcNow.Date).TotalDays)
            });
            return Ok(result);
        }

        /// <summary>
        /// Obtiene el historial completo de pagos realizados por el usuario.
        /// </summary>
        /// <param name="userId">Id del usuario (header).</param>
        /// <returns>Lista de pagos ordenados por fecha descendente.</returns>
        [HttpGet("payment-history")]
        public async Task<IActionResult> GetPaymentHistory([FromHeader] Guid userId)
        {
            var payments = await paymentService.GetPaymentHistoryWithDebtNameAsync(userId);
            return Ok(payments);
        }

        /// <summary>
        /// Obtiene el desglose de gastos mensuales en pagos de deudas.
        /// </summary>
        /// <param name="userId">Id del usuario (header).</param>
        /// <returns>Lista de gastos agrupados por año y mes.</returns>
        [HttpGet("monthly-spending")]
        public async Task<IActionResult> GetMonthlySpending([FromHeader] Guid userId)
        {
            var spending = await dashboardService.GetMonthlySpendingAsync(userId);
            return Ok(spending);
        }

        /// <summary>
        /// Proyecta el tiempo estimado para liquidar cada deuda según el pago mensual actual.
        /// </summary>
        /// <param name="userId">Id del usuario (header).</param>
        /// <returns>Lista de proyecciones por deuda.</returns>
        [HttpGet("debt-projection")]
        public async Task<IActionResult> GetDebtProjection([FromHeader] Guid userId)
        {
            var projection = await dashboardService.GetDebtProjectionAsync(userId);
            return Ok(projection);
        }

        /// <summary>
        /// Obtiene el costo total en intereses: monto original, saldo actual y tasa de interés promedio.
        /// </summary>
        /// <param name="userId">Id del usuario (header).</param>
        /// <returns>Objeto con datos de costo de intereses.</returns>
        [HttpGet("interest-cost")]
        public async Task<IActionResult> GetInterestCost([FromHeader] Guid userId)
        {
            var cost = await dashboardService.GetInterestCostAsync(userId);
            return Ok(cost);
        }

        /// <summary>
        /// Compara el total pagado vs el total pendiente, con porcentaje de avance.
        /// </summary>
        /// <param name="userId">Id del usuario (header).</param>
        /// <returns>Objeto con total pagado, pendiente y porcentaje.</returns>
        [HttpGet("paid-vs-pending")]
        public async Task<IActionResult> GetPaidVsPending([FromHeader] Guid userId)
        {
            var result = await dashboardService.GetPaidVsPendingAsync(userId);
            return Ok(result);
        }
    }
}
