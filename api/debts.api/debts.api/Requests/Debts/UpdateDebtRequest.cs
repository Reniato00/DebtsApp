using System.ComponentModel.DataAnnotations;

namespace debts.api.Requests.Debts
{
    /// <summary>
    /// Solicitud para actualizar una deuda existente.
    /// </summary>
    public class UpdateDebtRequest
    {
        /// <summary>Nombre o descripción de la deuda.</summary>
        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        /// <summary>Monto original de la deuda.</summary>
        [Range(0.01, double.MaxValue)]
        public decimal OriginalAmount { get; set; }

        /// <summary>Saldo actual pendiente.</summary>
        [Range(0, double.MaxValue)]
        public decimal CurrentBalance { get; set; }

        /// <summary>Tasa de interés anual (0-100).</summary>
        [Range(0, 100)]
        public decimal InterestRate { get; set; }

        /// <summary>Monto del pago mensual.</summary>
        [Range(0.01, double.MaxValue)]
        public decimal MonthlyPayment { get; set; }

        /// <summary>Fecha de inicio de la deuda.</summary>
        [Required]
        public DateTime StartDate { get; set; }

        /// <summary>Fecha de vencimiento.</summary>
        [Required]
        public DateTime DueDate { get; set; }
    }
}
