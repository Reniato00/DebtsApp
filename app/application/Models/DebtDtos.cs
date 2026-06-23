namespace application.Models;

public class DebtDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = "";
    public decimal OriginalAmount { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal InterestRate { get; set; }
    public decimal MonthlyPayment { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime DueDate { get; set; }
}

public class CreateDebtRequest
{
    public string Name { get; set; } = "";
    public decimal OriginalAmount { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal InterestRate { get; set; }
    public decimal MonthlyPayment { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime DueDate { get; set; }
}

public class UpdateDebtRequest
{
    public string Name { get; set; } = "";
    public decimal OriginalAmount { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal InterestRate { get; set; }
    public decimal MonthlyPayment { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime DueDate { get; set; }
}
