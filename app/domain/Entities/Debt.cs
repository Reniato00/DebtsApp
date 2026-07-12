namespace domain.Entities
{
    public class Debt
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }

        public string Name { get; set; } = string.Empty;

        public decimal OriginalAmount { get; set; }

        public decimal CurrentBalance { get; set; }

        public decimal InterestRate { get; set; }

        public decimal MonthlyPayment { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime DueDate { get; set; }
    }
}
