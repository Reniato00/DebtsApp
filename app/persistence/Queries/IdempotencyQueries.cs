namespace persistence.Queries
{
    public static class IdempotencyQueries
    {
        public const string GetByKey = """
        SELECT *
        FROM IdempotencyKeys
        WHERE [Key] = @Key
        """;

        public const string Insert = """
        INSERT INTO IdempotencyKeys
        (
            [Key],
            ResponseJson,
            StatusCode,
            CreatedAt,
            ExpiresAt
        )
        VALUES
        (
            @Key,
            @ResponseJson,
            @StatusCode,
            @CreatedAt,
            @ExpiresAt
        )
        """;

        public const string DeleteExpired = """
        DELETE FROM IdempotencyKeys
        WHERE ExpiresAt < GETUTCDATE()
        """;
    }
}
