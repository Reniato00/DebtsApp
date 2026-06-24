namespace application.Models;

public class DebtSummaryDto
{
    public int TotalDebts { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalMonthlyPayment { get; set; }
    public decimal AverageInterestRate { get; set; }
}

public class UpcomingPaymentDto
{
    public Guid DebtId { get; set; }
    public string DebtName { get; set; } = "";
    public decimal Amount { get; set; }
    public DateTime DueDate { get; set; }
    public int DaysUntilDue { get; set; }
}

public class MonthlySpendingDto
{
    public string Month { get; set; } = "";
    public decimal Total { get; set; }
}

public class DebtProjectionDto
{
    public string Month { get; set; } = "";
    public decimal RemainingBalance { get; set; }
}

public class InterestCostDto
{
    public string DebtName { get; set; } = "";
    public decimal TotalInterestPaid { get; set; }
    public decimal RemainingInterest { get; set; }
}

public class PaidVsPendingDto
{
    public decimal TotalPaid { get; set; }
    public decimal TotalPending { get; set; }
}
