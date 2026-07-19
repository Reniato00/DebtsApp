namespace persistence.Queries
{
    public static class PaymentQueries
    {
        public const string GetById = """
        SELECT *
        FROM Payments
        WHERE Id = @Id
        """;

        public const string GetByDebtId = """
        SELECT *
        FROM Payments
        WHERE DebtId = @DebtId
        ORDER BY PaymentDate DESC
        """;

        public const string CreatePayment = """
        INSERT INTO Payments
        (
            Id,
            DebtId,
            Amount,
            PaymentDate,
            PaymentType,
            WasOnTime,
            PrepaymentEffect,
            OriginalMonthlyPayment
        )
        VALUES
        (
            @Id,
            @DebtId,
            @Amount,
            @PaymentDate,
            @PaymentType,
            @WasOnTime,
            @PrepaymentEffect,
            @OriginalMonthlyPayment
        )
        """;

        public const string GetPaymentHistoryByUser = """
        SELECT p.*
        FROM Payments p
        INNER JOIN Debts d ON d.Id = p.DebtId
        WHERE d.UserId = @UserId
        ORDER BY p.PaymentDate DESC
        """;

        public const string ReduceDebtBalance = """
        UPDATE Debts
        SET CurrentBalance = CurrentBalance - @Amount
        WHERE Id = @DebtId
        """;

        public const string UpdatePayment = """
        UPDATE Payments
        SET Amount = @Amount
        WHERE Id = @Id
        """;

        public const string DeletePayment = """
        DELETE FROM Payments
        WHERE Id = @Id
        """;

        public const string DeleteByDebtIds = """
        DELETE FROM Payments
        WHERE DebtId IN @DebtIds
        """;

        public const string RevertDebtBalance = """
        UPDATE Debts
        SET CurrentBalance = CurrentBalance + @Amount
        WHERE Id = @DebtId
        """;

        public const string GetPaymentHistoryWithDebtName = """
        SELECT p.*, d.Name AS DebtName
        FROM Payments p
        INNER JOIN Debts d ON d.Id = p.DebtId
        WHERE d.UserId = @UserId
        ORDER BY p.PaymentDate DESC
        """;

        public const string GetPaymentHistoryWithDebtNamePaged = """
        SELECT p.*, d.Name AS DebtName
        FROM Payments p
        INNER JOIN Debts d ON d.Id = p.DebtId
        WHERE d.UserId = @UserId
        ORDER BY p.PaymentDate DESC
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
        """;

        public const string CountPaymentsByUser = """
        SELECT COUNT(*)
        FROM Payments p
        INNER JOIN Debts d ON d.Id = p.DebtId
        WHERE d.UserId = @UserId
        """;

        public const string GetTotalByDebt = """
        SELECT ISNULL(SUM(Amount), 0)
        FROM Payments
        WHERE DebtId = @DebtId
        """;

        public const string GetMonthlySpending = """
        SELECT
            YEAR(PaymentDate) AS [Year],
            MONTH(PaymentDate) AS [Month],
            SUM(Amount) AS Total
        FROM Payments p
        INNER JOIN Debts d ON d.Id = p.DebtId
        WHERE d.UserId = @UserId
        GROUP BY YEAR(PaymentDate), MONTH(PaymentDate)
        ORDER BY [Year] DESC, [Month] DESC
        """;

        public const string GetTotalPaymentsByUser = """
        SELECT ISNULL(SUM(p.Amount), 0)
        FROM Payments p
        INNER JOIN Debts d ON d.Id = p.DebtId
        WHERE d.UserId = @UserId
        """;
    }
}
