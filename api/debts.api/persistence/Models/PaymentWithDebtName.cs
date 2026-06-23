namespace persistence.Models
{
    public class PaymentWithDebtName
    {
        public Guid Id { get; set; }
        public Guid DebtId { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string DebtName { get; set; } = string.Empty;
    }
}
