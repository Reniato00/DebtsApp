using System.Text.Json.Serialization;

namespace debts.api.Responses
{
    public class DebtsCountResponse
    {
        [JsonPropertyName("totalDebts")]
        public required int TotalDebts { get; set; }
    }
}
