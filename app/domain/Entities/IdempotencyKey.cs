namespace domain.Entities
{
    public class IdempotencyKey
    {
        public string Key { get; set; } = string.Empty;
        public string ResponseJson { get; set; } = string.Empty;
        public int StatusCode { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
