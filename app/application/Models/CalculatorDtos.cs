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
