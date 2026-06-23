namespace application.Models;

public class PaymentDto
{
    public Guid Id { get; set; }
    public Guid DebtId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
}

public class CreatePaymentRequest
{
    public Guid DebtId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
}

public class UpdatePaymentRequest
{
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
}

public class PaymentHistoryDto
{
    public Guid Id { get; set; }
    public Guid DebtId { get; set; }
    public string DebtName { get; set; } = "";
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
}
