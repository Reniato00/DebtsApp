namespace application.Models
{
    public class DailyInterestResult
    {
        public List<DailyInterestItem> Items { get; set; } = new();
        public decimal TotalDaily { get; set; }
        public decimal TotalMonthly { get; set; }
        public decimal TotalYearly { get; set; }
    }

    public class DailyInterestItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal CurrentBalance { get; set; }
        public decimal InterestRate { get; set; }
        public decimal DailyInterest { get; set; }
        public decimal MonthlyInterest { get; set; }
        public decimal YearlyInterest { get; set; }
    }
}
