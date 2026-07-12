namespace application.Models;

public class PrepaymentAnalysisRequest
{
    public Guid DebtId { get; set; }
    public decimal ExtraAmount { get; set; }
}

public class PrepaymentAnalysisResult
{
    public string DebtName { get; set; } = string.Empty;
    public decimal CurrentBalance { get; set; }
    public decimal ExtraAmount { get; set; }
    public decimal NewBalance { get; set; }
    public decimal CurrentMonthlyPayment { get; set; }
    public int CurrentRemainingMonths { get; set; }
    public decimal CurrentTotalInterest { get; set; }
    public PrepaymentScenario ReduceTerm { get; set; } = new();
    public PrepaymentScenario ReducePayment { get; set; } = new();
}

public class PrepaymentScenario
{
    public int RemainingMonths { get; set; }
    public decimal NewMonthlyPayment { get; set; }
    public decimal TotalInterest { get; set; }
    public decimal InterestSaved { get; set; }
    public int MonthsSaved { get; set; }
    public DateTime? PayoffDate { get; set; }
}
