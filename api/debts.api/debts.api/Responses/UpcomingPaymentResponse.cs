using System.Text.Json.Serialization;

namespace debts.api.Responses
{
    public class UpcomingPaymentResponse
    {
        [JsonPropertyName("debtId")]
        public required Guid DebtId { get; set; }

        [JsonPropertyName("debtName")]
        public required string DebtName { get; set; }

        [JsonPropertyName("amount")]
        public required decimal Amount { get; set; }

        [JsonPropertyName("dueDate")]
        public required DateTime DueDate { get; set; }

        [JsonPropertyName("daysUntilDue")]
        public required int DaysUntilDue { get; set; }
    }
}
