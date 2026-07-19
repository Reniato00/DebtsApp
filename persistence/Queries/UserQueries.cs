namespace persistence.Queries
{
    public static class UserQueries
    {
        public const string GetById = """
        SELECT *
        FROM Users
        WHERE Id = @Id
        """;

        public const string GetByEmail = """
        SELECT *
        FROM Users
        WHERE Email = @Email
        """;

        public const string CreateUser = """
        INSERT INTO Users
        (
            Id,
            Name,
            Email,
            PasswordHash
        )
        VALUES
        (
            @Id,
            @Name,
            @Email,
            @PasswordHash
        )
        """;

        public const string UpdateUser = """
        UPDATE Users
        SET Name = @Name,
            Email = @Email,
            PasswordHash = @PasswordHash
        WHERE Id = @Id
        """;

        public const string DeleteById = """
        DELETE FROM Users
        WHERE Id = @Id
        """;
    }
}
