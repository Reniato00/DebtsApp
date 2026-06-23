namespace application.Models
{
    public class DebtProjectionItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal CurrentBalance { get; set; }
        public decimal MonthlyPayment { get; set; }
        public decimal MonthsToPayOff { get; set; }
        public DateTime? EstimatedPayOffDate { get; set; }
    }
}
