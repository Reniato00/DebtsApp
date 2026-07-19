namespace persistence.Queries
{
    public static class TermsAcceptanceQueries
    {
        public const string Create = """
        INSERT INTO TermsAcceptance
        (
            Id,
            UserId,
            Version,
            AcceptedAt
        )
        VALUES
        (
            @Id,
            @UserId,
            @Version,
            @AcceptedAt
        )
        """;

        public const string GetByUserId = """
        SELECT *
        FROM TermsAcceptance
        WHERE UserId = @UserId
        ORDER BY AcceptedAt DESC
        """;

        public const string DeleteByUserId = """
        DELETE FROM TermsAcceptance
        WHERE UserId = @UserId
        """;
    }
}
