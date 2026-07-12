USE DebtManager_db
GO

-- =============================================
-- Datos de pago de ejemplo
-- Ejecutar DummieDebts.sql primero para crear
-- usuarios, deudas y obtener los DebtId.
-- =============================================

-- Si ya corriste DummieDebts.sql, los DebtId son:
--   Juan:  AAAA...A1 (Auto),    AAAA...A2 (Tarjeta),   AAAA...A3 (Hipoteca)
--   María: BBBB...01 (Estudio), BBBB...02 (Personal)
--
-- Puedes agregar pagos adicionales aquí si necesitas
-- más registros después del seed inicial.

PRINT 'Los pagos de ejemplo ya fueron insertados en DummieDebts.sql';
GO
