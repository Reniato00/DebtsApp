namespace services.Models
{
    public class PaidVsPendingResult
    {
        public decimal TotalPaid { get; set; }
        public decimal TotalPending { get; set; }
        public decimal PaidPercentage { get; set; }
    }
}
