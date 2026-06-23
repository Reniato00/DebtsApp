namespace application.Models
{
    public class PayoffStrategyResult
    {
        public List<PayoffStep> Snowball { get; set; } = new();
        public List<PayoffStep> Avalanche { get; set; } = new();
        public decimal TotalMonthlyPayment { get; set; }
        public int TotalDebts { get; set; }
    }

    public class PayoffStep
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal CurrentBalance { get; set; }
        public decimal InterestRate { get; set; }
        public decimal MonthlyPayment { get; set; }
        public int PayoffOrder { get; set; }
        public int EstimatedMonths { get; set; }
        public DateTime? EstimatedPayoffDate { get; set; }
    }
}
