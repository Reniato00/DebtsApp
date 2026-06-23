namespace domain.Entities
{
    public class Payment
    {
        public Guid Id { get; set; }

        public Guid DebtId { get; set; }

        public decimal Amount { get; set; }

        public DateTime PaymentDate { get; set; }
    }
}
