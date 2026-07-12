using application.Models;
using application.Services;
using Microsoft.AspNetCore.Mvc;

namespace debts.api.Controllers
{
    /// <summary>
    /// Calculadoras financieras: estrategias de pago e interés diario.
    /// </summary>
    [ApiController]
    [Route("api/calculator")]
    public class CalculatorController : ControllerBase
    {
        private readonly ICalculatorService calculatorService;

        public CalculatorController(ICalculatorService calculatorService)
        {
            this.calculatorService = calculatorService;
        }

        /// <summary>
        /// Compara las estrategias Snowball (menor saldo) vs Avalanche (mayor interés) para liquidar deudas.
        /// </summary>
        /// <param name="userId">Id del usuario (header).</param>
        /// <returns>Orden de pago recomendado para cada estrategia.</returns>
        [HttpGet("strategy")]
        public async Task<IActionResult> GetPayoffStrategy(
            [FromHeader] Guid userId,
            [FromQuery] decimal monthlyPayment = 0)
        {
            var result = await calculatorService.GetPayoffStrategyAsync(userId, monthlyPayment);
            return Ok(result);
        }

        /// <summary>
        /// Simula el impacto de un pago extra en una deuda (reducir plazo vs reducir cuota).
        /// </summary>
        [HttpPost("prepayment-analysis")]
        public async Task<IActionResult> AnalyzePrepayment(
            [FromHeader] Guid userId,
            [FromBody] PrepaymentAnalysisRequest request)
        {
            var result = await calculatorService.AnalyzePrepaymentAsync(userId, request);
            return Ok(result);
        }

        /// <summary>
        /// Calcula el interés diario, mensual y anual de cada deuda.
        /// </summary>
        /// <param name="userId">Id del usuario (header).</param>
        /// <returns>Desglose de intereses por deuda y totales.</returns>
        [HttpGet("daily-interest")]
        public async Task<IActionResult> GetDailyInterest([FromHeader] Guid userId)
        {
            var result = await calculatorService.GetDailyInterestAsync(userId);
            return Ok(result);
        }
    }
}
