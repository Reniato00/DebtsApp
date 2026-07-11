namespace domain.Entities
{
    public class Payment
    {
        public Guid Id { get; set; }
        public Guid DebtId { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string PaymentType { get; set; } = "regular";
        public bool WasOnTime { get; set; } = true;
        public string? PrepaymentEffect { get; set; }
        public decimal? OriginalMonthlyPayment { get; set; }
    }
}
