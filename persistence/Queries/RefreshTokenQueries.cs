namespace persistence.Queries
{
    public static class RefreshTokenQueries
    {
        public const string Create = """
        INSERT INTO RefreshTokens
        (
            Id,
            UserId,
            TokenHash,
            ExpiresAt,
            IsRevoked,
            CreatedAt,
            RevokedAt
        )
        VALUES
        (
            @Id,
            @UserId,
            @TokenHash,
            @ExpiresAt,
            @IsRevoked,
            @CreatedAt,
            @RevokedAt
        )
        """;

        public const string GetByToken = """
        SELECT *
        FROM RefreshTokens
        WHERE TokenHash = @TokenHash
        """;

        public const string Revoke = """
        UPDATE RefreshTokens
        SET IsRevoked = 1,
            RevokedAt = @RevokedAt
        WHERE Id = @Id
        """;

        public const string RevokeAllForUser = """
        UPDATE RefreshTokens
        SET IsRevoked = 1,
            RevokedAt = @RevokedAt
        WHERE UserId = @UserId AND IsRevoked = 0
        """;
    }
}
