namespace services.Models
{
    public class InterestCostResult
    {
        public decimal TotalOriginalAmount { get; set; }
        public decimal TotalCurrentBalance { get; set; }
        public decimal AverageInterestRate { get; set; }
    }
}
