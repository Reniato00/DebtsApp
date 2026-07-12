namespace application.Models;

public class UpcomingPaymentItem
{
    public Guid DebtId { get; set; }
    public string DebtName { get; set; } = "";
    public decimal Amount { get; set; }
    public DateTime DueDate { get; set; }
    public int DaysUntilDue { get; set; }
}

public class DebtInterestItem
{
    public string Name { get; set; } = "";
    public decimal TotalInterestPaid { get; set; }
    public decimal RemainingInterest { get; set; }
}
