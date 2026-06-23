using System.Text;
using application.Services;
using Microsoft.AspNetCore.Mvc;

namespace debts.api.Controllers
{
    /// <summary>
    /// Exportación de datos en formatos descargables.
    /// </summary>
    [ApiController]
    [Route("api/data")]
    public class DataController : ControllerBase
    {
        private readonly IDebtService debtService;
        private readonly IPaymentService paymentService;

        public DataController(IDebtService debtService, IPaymentService paymentService)
        {
            this.debtService = debtService;
            this.paymentService = paymentService;
        }

        /// <summary>
        /// Exporta todas las deudas y pagos del usuario a un archivo CSV.
        /// </summary>
        /// <param name="userId">Id del usuario (header).</param>
        /// <returns>Archivo CSV con deudas y pagos.</returns>
        [HttpGet("export/csv")]
        public async Task<IActionResult> ExportCsv([FromHeader] Guid userId)
        {
            var debts = await debtService.GetAllDebtsAsync(userId);
            var payments = await paymentService.GetPaymentHistoryAsync(userId);

            var sb = new StringBuilder();

            sb.AppendLine("=== Debts ===");
            sb.AppendLine("Id,Name,OriginalAmount,CurrentBalance,InterestRate,MonthlyPayment,StartDate,DueDate");

            foreach (var debt in debts)
            {
                sb.AppendLine($"{debt.Id},{debt.Name},{debt.OriginalAmount},{debt.CurrentBalance},{debt.InterestRate},{debt.MonthlyPayment},{debt.StartDate:yyyy-MM-dd},{debt.DueDate:yyyy-MM-dd}");
            }

            sb.AppendLine();
            sb.AppendLine("=== Payments ===");
            sb.AppendLine("Id,DebtId,Amount,PaymentDate");

            foreach (var payment in payments)
            {
                sb.AppendLine($"{payment.Id},{payment.DebtId},{payment.Amount},{payment.PaymentDate:yyyy-MM-dd}");
            }

            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"debt-data-{DateTime.UtcNow:yyyyMMdd}.csv");
        }
    }
}
