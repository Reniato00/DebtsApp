namespace persistence.Queries
{
    public static class DebtQueries
    {
        public const string GetAll = """
        SELECT *
        FROM Debts
        WHERE UserId = @UserId
        """;

        public const string GetById = """
        SELECT *
        FROM Debts
        WHERE Id = @Id AND UserId = @UserId
        """;

        public const string Count = """
        SELECT COUNT(*)
        FROM Debts
        WHERE UserId = @UserId
        """;

        public const string CreateDebt = """
        INSERT INTO Debts
        (
            Id,
            UserId,
            Name,
            OriginalAmount,
            CurrentBalance,
            InterestRate,
            MonthlyPayment,
            StartDate,
            DueDate
        )
        VALUES
        (
            @Id,
            @UserId,
            @Name,
            @OriginalAmount,
            @CurrentBalance,
            @InterestRate,
            @MonthlyPayment,
            @StartDate,
            @DueDate
        )
        """;

        public const string GetByIdAfterInsert = """
        SELECT *
        FROM Debts
        WHERE Id = @Id
        """;

        public const string UpdateDebt = """
        UPDATE Debts
        SET Name = @Name,
            OriginalAmount = @OriginalAmount,
            CurrentBalance = @CurrentBalance,
            InterestRate = @InterestRate,
            MonthlyPayment = @MonthlyPayment,
            StartDate = @StartDate,
            DueDate = @DueDate
        WHERE Id = @Id AND UserId = @UserId
        """;

        public const string DeleteDebt = """
        DELETE FROM Debts
        WHERE Id = @Id AND UserId = @UserId
        """;

        public const string DeleteByUserId = """
        DELETE FROM Debts
        WHERE UserId = @UserId
        """;

        public const string TotalAmountDebts = """
        SELECT SUM(CurrentBalance) AS TotalAmount
        FROM Debts
        WHERE UserId = @UserId
        """;

        public const string TotalAmountMonthlyPayment = """
        SELECT SUM(MonthlyPayment) AS TotalAmountMonthlyPayment
        FROM Debts
        WHERE UserId = @UserId
        """;

        public const string DebtSummary = """
        SELECT
            COUNT(*) AS TotalDebts,
            ISNULL(SUM(CurrentBalance), 0) AS TotalAmount,
            ISNULL(SUM(MonthlyPayment), 0) AS TotalMonthlyPayment,
            ISNULL(AVG(InterestRate), 0) AS AverageInterestRate
        FROM Debts
        WHERE UserId = @UserId
        """;

        public const string UpcomingPayments = """
        SELECT *
        FROM Debts
        WHERE UserId = @UserId AND DueDate >= GETDATE()
        ORDER BY DueDate ASC
        """;

        public const string PayOffDebt = """
        UPDATE Debts
        SET CurrentBalance = 0
        WHERE Id = @Id AND UserId = @UserId
        """;

        public const string SearchDebts = """
        SELECT *
        FROM Debts
        WHERE UserId = @UserId AND Name LIKE @Query
        ORDER BY DueDate ASC
        """;

        public const string OverdueDebts = """
        SELECT *
        FROM Debts
        WHERE UserId = @UserId AND DueDate < GETDATE()
        ORDER BY DueDate ASC
        """;

        public const string GetAllPaged = """
        SELECT *
        FROM Debts
        WHERE UserId = @UserId
        ORDER BY DueDate ASC
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
        """;
    }
}
