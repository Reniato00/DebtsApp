using System.ComponentModel.DataAnnotations;

namespace debts.api.Requests.Payments
{
    /// <summary>
    /// Solicitud para registrar un nuevo pago.
    /// </summary>
    public class CreatePaymentRequest
    {
        /// <summary>Id de la deuda a la que se aplica el pago.</summary>
        [Required]
        public Guid DebtId { get; set; }

        /// <summary>Monto del pago.</summary>
        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }
    }
}
