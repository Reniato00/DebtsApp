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

        /// <summary>Tipo de pago: "regular" (cotidiano) o "prepayment" (prepago).</summary>
        public string PaymentType { get; set; } = "regular";

        /// <summary>Indica si el pago se realizó a tiempo.</summary>
        public bool WasOnTime { get; set; } = true;

        /// <summary>Efecto del prepago: "reduceTerm" o "reducePayment" (solo si PaymentType = prepayment).</summary>
        public string? PrepaymentEffect { get; set; }
    }
}
