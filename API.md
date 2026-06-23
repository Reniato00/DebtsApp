# Debt Management API

API REST para gestión de deudas personales multi-usuario con autenticación JWT, CRUD de deudas/pagos, dashboard financiero y calculadoras.

- **Stack:** .NET 8, Dapper, SQL Server, JWT Bearer, BCrypt
- **Base URL:** `https://localhost:7024`
- **Auth:** JWT Bearer + Refresh Token rotation

---

## Autenticación

Todas las rutas requieren `Authorization: Bearer <token>` **excepto** las marcadas como `[AllowAnonymous]`.

### Headers adicionales

| Header | Tipo | Requerido | Descripción |
|--------|------|-----------|-------------|
| `userId` | `Guid` | En rutas protegidas | ID del usuario autenticado (lo envía el frontend automáticamente) |

---

## Endpoints

### 🔓 Health Check `[AllowAnonymous]`

| Método | Ruta | Descripción |
|--------|------|-------------|
| `GET` | `/api/health` | Verifica que el servicio esté operativo |

**Response 200:**
```json
{ "status": "Healthy", "timestamp": "2026-06-23T17:47:28Z" }
```

---

### 🔓 Auth `[AllowAnonymous]`

| Método | Ruta | Descripción |
|--------|------|-------------|
| `POST` | `/api/auth/register` | Registrar nuevo usuario |
| `POST` | `/api/auth/login` | Iniciar sesión |
| `POST` | `/api/auth/refresh` | Renovar tokens (rotación) |
| `POST` | `/api/auth/me` | Obtener usuario del token (requiere auth) |
| `POST` | `/api/auth/logout` | Revocar todos los refresh tokens (requiere auth) |

**POST `/api/auth/register`**

```json
{ "email": "user@example.com", "name": "Nombre", "password": "123456" }
```

**POST `/api/auth/login`**

```json
{ "email": "user@example.com", "password": "123456" }
```

**POST `/api/auth/refresh`**

```json
{ "refreshToken": "..." }
```

**Response 200 (register, login, refresh):**
```json
{ "accessToken": "eyJ...", "refreshToken": "BVo2...", "expiresIn": 3600 }
```

**Response 200 (`/api/auth/me`):**
```json
{ "userId": "11111111-1111-1111-1111-111111111111", "name": "Juan Pérez" }
```

**Response 200 (`/api/auth/logout`):**
```json
{ "message": "Logged out successfully" }
```

---

### 👤 Users

| Método | Ruta | Descripción |
|--------|------|-------------|
| `POST` | `/api/user/register` | `[AllowAnonymous]` Crear usuario |
| `GET` | `/api/user/{id}` | Obtener usuario por ID |
| `PUT` | `/api/user/{id}` | Actualizar usuario |

**POST `/api/user/register`**
```json
{ "name": "Nombre", "email": "user@example.com" }
```

**PUT `/api/user/{id}`**
```json
{ "name": "NuevoNombre", "email": "nuevo@example.com" }
```

---

### 💳 Debts

| Método | Ruta | Descripción |
|--------|------|-------------|
| `POST` | `/api/debt/create` | Crear deuda |
| `GET` | `/api/debt/{id}` | Obtener deuda por ID |
| `GET` | `/api/debt/all` | Todas las deudas del usuario |
| `GET` | `/api/debt/all/paged?page=1&pageSize=10` | Deudas paginadas |
| `PUT` | `/api/debt/{id}` | Actualizar deuda |
| `DELETE` | `/api/debt/{id}` | Eliminar deuda |
| `PATCH` | `/api/debt/{id}/pay-off` | Liquidar deuda (CurrentBalance → 0) |
| `GET` | `/api/debt/search?q=texto` | Buscar deudas por nombre |
| `GET` | `/api/debt/overdue` | Deudas vencidas |

**POST `/api/debt/create`**
```json
{
  "name": "Auto",
  "originalAmount": 500000.00,
  "currentBalance": 450000.00,
  "interestRate": 12.5,
  "monthlyPayment": 15000.00,
  "startDate": "2024-01-15",
  "dueDate": "2026-12-15"
}
```

**PUT `/api/debt/{id}`** (mismos campos que create, todos opcionales excepto name)

---

### 💰 Payments

| Método | Ruta | Descripción |
|--------|------|-------------|
| `POST` | `/api/payment` | Crear pago (reduce saldo de la deuda) |
| `GET` | `/api/payment/{id}` | Obtener pago por ID |
| `GET` | `/api/payment/debt/{debtId}` | Pagos de una deuda |
| `PUT` | `/api/payment/{id}` | Actualizar monto del pago (ajusta saldo) |
| `DELETE` | `/api/payment/{id}` | Eliminar pago (revierte saldo) |
| `GET` | `/api/payment/history` | Historial completo del usuario |
| `GET` | `/api/payment/history/paged?page=1&pageSize=10` | Historial paginado |
| `GET` | `/api/payment/debt/{debtId}/total` | Total pagado por deuda |

**POST `/api/payment`**
```json
{ "debtId": "guid", "amount": 15000.00 }
```

**PUT `/api/payment/{id}`**
```json
{ "amount": 20000.00 }
```

---

### 📊 Dashboard

| Método | Ruta | Descripción |
|--------|------|-------------|
| `GET` | `/api/dashboard/debt-count` | Cantidad de deudas |
| `GET` | `/api/dashboard/total-amount` | Saldo total |
| `GET` | `/api/dashboard/total-payment-amount` | Pago mensual total |
| `GET` | `/api/dashboard/summary` | Resumen completo |
| `GET` | `/api/dashboard/upcoming-payments` | Próximos pagos |
| `GET` | `/api/dashboard/payment-history` | Historial de pagos |
| `GET` | `/api/dashboard/monthly-spending` | Gastos por mes |
| `GET` | `/api/dashboard/debt-projection` | Proyección de pagos |
| `GET` | `/api/dashboard/interest-cost` | Costo de intereses |
| `GET` | `/api/dashboard/paid-vs-pending` | Pagado vs Pendiente |

**GET `/api/dashboard/summary`**
```json
{
  "totalDebts": 3,
  "totalAmount": 1692000.00,
  "totalMonthlyPayment": 32500.00,
  "averageInterestRate": 16.67
}
```

**GET `/api/dashboard/upcoming-payments`**
```json
[
  { "debtId": "guid", "debtName": "Auto", "amount": 15000.00, "dueDate": "2026-07-15", "daysUntilDue": 22 },
  { "debtId": "guid", "debtName": "TC",      "amount": 10000.00, "dueDate": "2026-07-20", "daysUntilDue": 27 }
]
```

**GET `/api/dashboard/payment-history`** → array de pagos

**GET `/api/dashboard/paid-vs-pending`**
```json
{ "totalPaid": 150000.00, "totalPending": 1542000.00, "paidPercentage": 8.87 }
```

**GET `/api/dashboard/interest-cost`**
```json
{ "totalOriginalAmount": 2100000.00, "totalCurrentBalance": 1692000.00, "averageInterestRate": 16.67 }
```

**GET `/api/dashboard/monthly-spending`**
```json
[
  { "year": 2026, "month": 5, "total": 30000.00 },
  { "year": 2026, "month": 6, "total": 32500.00 }
]
```

**GET `/api/dashboard/debt-projection`**
```json
[
  { "id": "guid", "name": "Auto", "currentBalance": 350000.00, "monthlyPayment": 15000.00, "monthsToPayOff": 24, "estimatedPayOffDate": "2028-06-23" }
]
```

---

### 🧮 Calculator

| Método | Ruta | Descripción |
|--------|------|-------------|
| `GET` | `/api/calculator/strategy` | Estrategia Snowball vs Avalanche |
| `GET` | `/api/calculator/daily-interest` | Interés diario/mensual/anual por deuda |

**GET `/api/calculator/strategy`**
```json
{
  "totalMonthlyPayment": 32500.00,
  "totalDebts": 3,
  "snowball": [
    { "id": "guid", "name": "TC", "currentBalance": 120000.00, "interestRate": 28.0, "monthlyPayment": 10000.00, "payoffOrder": 1, "estimatedMonths": 13, "estimatedPayoffDate": "2027-07-23" },
    { "id": "guid", "name": "Auto", "currentBalance": 350000.00, "interestRate": 10.0, "monthlyPayment": 15000.00, "payoffOrder": 2, "estimatedMonths": 27, "estimatedPayoffDate": "2028-09-23" }
  ],
  "avalanche": [
    { "id": "guid", "name": "TC", "currentBalance": 120000.00, "interestRate": 28.0, "monthlyPayment": 10000.00, "payoffOrder": 1, "estimatedMonths": 13, "estimatedPayoffDate": "2027-07-23" }
  ]
}
```

**GET `/api/calculator/daily-interest`**
```json
{
  "items": [
    { "id": "guid", "name": "Auto", "currentBalance": 350000.00, "interestRate": 10.0, "dailyInterest": 95.89, "monthlyInterest": 2916.67, "yearlyInterest": 35000.00 }
  ],
  "totalDaily": 95.89,
  "totalMonthly": 2916.67,
  "totalYearly": 35000.00
}
```

---

### 📁 Data Export

| Método | Ruta | Descripción |
|--------|------|-------------|
| `GET` | `/api/data/export/csv` | Exportar deudas+pagos como CSV |

---

## Modelos de datos

### User
| Campo | Tipo | Descripción |
|-------|------|-------------|
| Id | `Guid` | PK |
| Name | `string` | Nombre |
| Email | `string` | Email único |
| PasswordHash | `string` | Hash BCrypt |

### Debt
| Campo | Tipo | Descripción |
|-------|------|-------------|
| Id | `Guid` | PK |
| UserId | `Guid` | FK → Users |
| Name | `string` | Nombre |
| OriginalAmount | `decimal(18,2)` | Monto original |
| CurrentBalance | `decimal(18,2)` | Saldo actual |
| InterestRate | `decimal(5,2)` | Tasa de interés % |
| MonthlyPayment | `decimal(18,2)` | Pago mensual |
| StartDate | `datetime` | Fecha inicio |
| DueDate | `datetime` | Fecha vencimiento |

### Payment
| Campo | Tipo | Descripción |
|-------|------|-------------|
| Id | `Guid` | PK |
| DebtId | `Guid` | FK → Debts |
| Amount | `decimal(18,2)` | Monto |
| PaymentDate | `datetime` | Fecha del pago |

### RefreshToken
| Campo | Tipo | Descripción |
|-------|------|-------------|
| Id | `Guid` | PK |
| UserId | `Guid` | FK → Users |
| TokenHash | `string` | SHA256 del token |
| ExpiresAt | `datetime` | Expiración |
| IsRevoked | `bit` | Revocado |
| CreatedAt | `datetime` | Creación |
| RevokedAt | `datetime` | Fecha revocación |

---

## Códigos de error

| Código | Significado |
|--------|-------------|
| `400` | Argumento inválido (userId vacío, validación) |
| `401` | No autenticado / token inválido |
| `404` | Recurso no encontrado |
| `409` | Conflicto (email duplicado) |
| `429` | Rate limit superado |
| `500` | Error interno del servidor |

**Formato errores:**
```json
{ "statusCode": 400, "message": "UserId is required", "timestamp": "..." }
```

---

## Configuración

| Variable | Default | Descripción |
|----------|---------|-------------|
| `ConnectionStrings:DefaultConnection` | `Server=localhost\SQLEXPRESS;Database=DebtManager_db;Trusted_Connection=True;TrustServerCertificate=True;` | Conexión SQL Server |
| `Jwt:Secret` | `YourSuperSecretKeyThatIsAtLeast32CharactersLong!` | Clave JWT (min 32 chars) |
| `Jwt:Issuer` | `DebtsApi` | Emisor JWT |
| `Jwt:Audience` | `DebtsApp` | Audiencia JWT |
| `Jwt:ExpirationMinutes` | `60` | Expiración del access token |
| `Jwt:RefreshTokenExpirationDays` | `7` | Expiración del refresh token |
| `RateLimiting:PermitLimit` | `100` | Máximo de requests por ventana |
| `RateLimiting:WindowSeconds` | `60` | Ventana de rate limiting |
| `Cors:AllowedOrigins` | `localhost:3000,5173,4200,7285,5268` | Orígenes CORS permitidos |

---

## Pipeline (orden de middleware)

1. **ExceptionMiddleware** — captura errores globales
2. **Security Headers** — X-Content-Type-Options, X-Frame-Options, etc.
3. **Swagger** (solo Development)
4. **HttpsRedirection**
5. **CORS**
6. **Authentication** (JWT Bearer)
7. **RateLimiter**
8. **Authorization**
9. **MapControllers**

---

## Flujo de Refresh Token

1. El refresh token se genera como 64 bytes aleatorios (Base64)
2. Se hashea con SHA256 y se almacena en BD
3. El token crudo se devuelve al cliente
4. Al refrescar, el servidor:
   - Hashea el token entrante
   - Busca el hash en BD
   - Valida que no esté revocado ni expirado
   - **Revoca** el token anterior (rotación)
   - Genera un **nuevo par** de tokens
5. Logout revoca **todos** los refresh tokens del usuario
