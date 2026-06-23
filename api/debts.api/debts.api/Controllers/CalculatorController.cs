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
        public async Task<IActionResult> GetPayoffStrategy([FromHeader] Guid userId)
        {
            var result = await calculatorService.GetPayoffStrategyAsync(userId);
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
