namespace domain.Entities
{
    public class TermsAcceptance
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Version { get; set; } = "1.0";
        public DateTime AcceptedAt { get; set; }
    }
}
