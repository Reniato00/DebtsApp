USE DebtManager_db
GO

-- =============================================
-- Users
-- =============================================
DECLARE @UserId1 UNIQUEIDENTIFIER = '11111111-1111-1111-1111-111111111111';
DECLARE @UserId2 UNIQUEIDENTIFIER = '22222222-2222-2222-2222-222222222222';

-- contraseña para ambos: 123456 (hash BCrypt)
INSERT INTO Users (Id, Name, Email, PasswordHash)
VALUES
    (@UserId1, 'Juan Pérez',    'juan@example.com',  '$2a$11$tYapW6CNCuXhwYubUQ8bpeeg1SaymTW/7qKyomZ8hMqStyqBJ2vya'),
    (@UserId2, 'María López',   'maria@example.com', '$2a$11$tYapW6CNCuXhwYubUQ8bpeeg1SaymTW/7qKyomZ8hMqStyqBJ2vya');
GO

-- =============================================
-- Debts
-- =============================================
DECLARE @UserId1 UNIQUEIDENTIFIER = '11111111-1111-1111-1111-111111111111';
DECLARE @UserId2 UNIQUEIDENTIFIER = '22222222-2222-2222-2222-222222222222';

-- Juan's debts
DECLARE @DebtAuto    UNIQUEIDENTIFIER = 'AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAA1';
DECLARE @DebtCard    UNIQUEIDENTIFIER = 'AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAA2';
DECLARE @DebtMortg   UNIQUEIDENTIFIER = 'AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAA3';

INSERT INTO Debts (Id, UserId, Name, OriginalAmount, CurrentBalance, InterestRate, MonthlyPayment, StartDate, DueDate)
VALUES
    (@DebtAuto,  @UserId1, 'Crédito Automotriz',  300000, 250000, 12.50,  6500,  '2025-01-01', '2031-01-01'),
    (@DebtCard,  @UserId1, 'Tarjeta de Crédito',   50000,  42000,  28.00,  8000,  '2025-03-15', '2026-03-15'),
    (@DebtMortg, @UserId1, 'Hipoteca',            1500000,1400000, 9.50,  18000,  '2024-06-01', '2044-06-01');

-- María's debts
DECLARE @DebtStudent UNIQUEIDENTIFIER = 'BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBB01';
DECLARE @DebtPersonal UNIQUEIDENTIFIER = 'BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBB02';

INSERT INTO Debts (Id, UserId, Name, OriginalAmount, CurrentBalance, InterestRate, MonthlyPayment, StartDate, DueDate)
VALUES
    (@DebtStudent, @UserId2, 'Préstamo Estudiantil', 120000, 85000, 6.80,  3200, '2023-09-01', '2030-09-01'),
    (@DebtPersonal,@UserId2, 'Préstamo Personal',    30000,  15000, 15.00, 2500, '2025-06-01', '2027-06-01');
GO

-- =============================================
-- Payments (con DebtId conocidos)
-- =============================================
DECLARE @DebtAuto    UNIQUEIDENTIFIER = 'AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAA1';
DECLARE @DebtCard    UNIQUEIDENTIFIER = 'AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAA2';
DECLARE @DebtMortg   UNIQUEIDENTIFIER = 'AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAA3';
DECLARE @DebtStudent UNIQUEIDENTIFIER = 'BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBB01';
DECLARE @DebtPersonal UNIQUEIDENTIFIER = 'BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBB02';

INSERT INTO Payments (Id, DebtId, Amount, PaymentDate)
VALUES
    -- Juan: 3 pagos al auto
    (NEWID(), @DebtAuto,  6500, '2025-02-01'),
    (NEWID(), @DebtAuto,  6500, '2025-03-01'),
    (NEWID(), @DebtAuto,  6500, '2025-04-01'),
    -- Juan: 2 pagos a la tarjeta
    (NEWID(), @DebtCard,  4000, '2025-04-01'),
    (NEWID(), @DebtCard,  4000, '2025-05-01'),
    -- Juan: 1 pago a la hipoteca
    (NEWID(), @DebtMortg, 18000, '2025-05-01'),
    -- María: 1 pago al estudio
    (NEWID(), @DebtStudent, 3200, '2025-04-01'),
    -- María: 2 pagos al personal
    (NEWID(), @DebtPersonal, 2500, '2025-05-01'),
    (NEWID(), @DebtPersonal, 2500, '2025-06-01');
GO

-- =============================================
-- Refresh Tokens (opcional — para pruebas)
-- =============================================
-- El token real es "dGVzdC1yZWZyZXNoLXRva2VuLXBhcmEtanVhbg=="
-- y se guarda hasheado con SHA256
DECLARE @UserId1 UNIQUEIDENTIFIER = '11111111-1111-1111-1111-111111111111';

INSERT INTO RefreshTokens (Id, UserId, TokenHash, ExpiresAt, IsRevoked, CreatedAt, RevokedAt)
VALUES
    (NEWID(), @UserId1, 'xqH7vTc3mR9kL2pW5yB8nE1sJ4uA0dF6gI3oQ8rT7yV9cX2bN5mP0kL3jH6gF1d',
     DATEADD(DAY, 7, GETDATE()), 0, GETDATE(), NULL);
GO
