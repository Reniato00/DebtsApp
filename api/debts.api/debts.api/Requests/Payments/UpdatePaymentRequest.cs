using System.ComponentModel.DataAnnotations;

namespace debts.api.Requests.Payments
{
    /// <summary>
    /// Solicitud para actualizar el monto de un pago.
    /// </summary>
    public class UpdatePaymentRequest
    {
        /// <summary>Nuevo monto del pago.</summary>
        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }
    }
}
