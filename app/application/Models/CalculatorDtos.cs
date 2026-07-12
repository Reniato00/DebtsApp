namespace application.Models;

public class PayoffStepDto
{
    public int Month { get; set; }
    public string DebtName { get; set; } = "";
    public decimal PaymentAmount { get; set; }
    public decimal RemainingBalance { get; set; }
}

public class PayoffStrategyResultDto
{
    public List<PayoffStepDto> Snowball { get; set; } = new();
    public List<PayoffStepDto> Avalanche { get; set; } = new();
    public int SnowballMonths { get; set; }
    public int AvalancheMonths { get; set; }
}

public class DailyInterestItemDto
{
    public string DebtName { get; set; } = "";
    public decimal CurrentBalance { get; set; }
    public decimal InterestRate { get; set; }
    public decimal DailyInterest { get; set; }
}

public class DailyInterestResultDto
{
    public List<DailyInterestItemDto> Items { get; set; } = new();
    public decimal TotalDailyInterest { get; set; }
}

public class PrepaymentAnalysisRequestDto
{
    public Guid DebtId { get; set; }
    public decimal ExtraAmount { get; set; }
}

public class PrepaymentAnalysisResultDto
{
    public string DebtName { get; set; } = "";
    public decimal CurrentBalance { get; set; }
    public decimal ExtraAmount { get; set; }
    public decimal NewBalance { get; set; }
    public decimal CurrentMonthlyPayment { get; set; }
    public int CurrentRemainingMonths { get; set; }
    public decimal CurrentTotalInterest { get; set; }
    public PrepaymentScenarioDto ReduceTerm { get; set; } = new();
    public PrepaymentScenarioDto ReducePayment { get; set; } = new();
}

public class PrepaymentScenarioDto
{
    public int RemainingMonths { get; set; }
    public decimal NewMonthlyPayment { get; set; }
    public decimal TotalInterest { get; set; }
    public decimal InterestSaved { get; set; }
    public int MonthsSaved { get; set; }
    public DateTime? PayoffDate { get; set; }
}
