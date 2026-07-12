namespace application.Models
{
    public class PayoffStrategyResult
    {
        public List<PayoffStep> Snowball { get; set; } = new();
        public List<PayoffStep> Avalanche { get; set; } = new();
        public int SnowballMonths { get; set; }
        public int AvalancheMonths { get; set; }
    }

    public class PayoffStep
    {
        public int Month { get; set; }
        public string DebtName { get; set; } = string.Empty;
        public decimal PaymentAmount { get; set; }
        public decimal RemainingBalance { get; set; }
    }
}
