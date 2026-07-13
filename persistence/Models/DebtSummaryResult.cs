namespace persistence.Models
{
    public class DebtSummaryResult
    {
        public int TotalDebts { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalMonthlyPayment { get; set; }
        public decimal AverageInterestRate { get; set; }
    }
}
