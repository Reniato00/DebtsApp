# Reglas
- Siempre preguntar antes de cambiar la lógica de negocio
- No agregar comentarios al código a menos que se solicite
- Mantener el código en español (UI) e inglés (código) según convención existente
- Cada vez que se modifique el código, revisar los diagramas Mermaid en este archivo y actualizarlos si los cambios afectan la arquitectura, dependencias, flujos o componentes representados

# Arquitectura del proyecto

```mermaid
flowchart TD
    subgraph Capas["Capas del proyecto"]
        direction TB
        DOM["domain\nEntities: Debt, Payment, User\nIdempotencyKey, TermsAcceptance"]
        PER["persistence\nDapper + SQL Server\nRepositorios, Queries"]
        SRV["services\nLógica de negocio\nServicios + Models"]
        UI["application\nBlazor Server UI\nComponentes, Auth"]
    end

    PER --> DOM
    SRV --> DOM
    SRV --> PER
    UI --> SRV
    UI --> PER
    UI --> DOM
```

```mermaid
flowchart TD
    subgraph Pages["Pages"]
        Dashboard["/dashboard"]
        Debts["/debts"]
        DebtDetail["/debt/{DebtId:guid}"]
        Calculator["/calculator"]
        Pagos["/pagos"]
        Login["/login"]
        Terms["/terms"]
        Profile["/profile"]
    end

    subgraph Layout["Layout"]
        MainLayout
        NavMenu
        BlankLayout
    end

    subgraph Shared["Shared Components"]
        LoadingSpinner
        AlertMessage
        ModalDialog
        SubmitButton
        Pagination
        EmptyState
        StatsGrid
        UpcomingPaymentsCard
        RecentPaymentsCard
        PaidVsPendingCard
        InterestCostCard
    end

    subgraph Helpers["Helpers"]
        FormattingHelpers
        AuthHelper
    end

    subgraph Auth["Auth"]
        CustomAuthStateProvider
    end

    MainLayout --> NavMenu
    MainLayout --> Pages
    BlankLayout --> Login
    Pages --> Shared
    Pages --> Helpers
    Pages --> Auth
    NavMenu --> Helpers
    NavMenu --> Auth
```

```mermaid
flowchart LR
    subgraph UI["Blazor Pages"]
        DASH["Dashboard"]
        DEBTS["Debts"]
        DETAIL["DebtDetail"]
        CALC["Calculator"]
        PAGOS["Pagos"]
        PROFILE["Profile"]
    end

    subgraph Services["Services Layer"]
        DS["IDebtService"]
        PS["IPaymentService"]
        CS["ICalculatorService"]
        DBS["IDashboardService"]
        AS["IAuthService\nRegister / Login / Logout\nDeleteAccount"]
    end

    subgraph Data["Data Access"]
        DR["DebtRepository"]
        PR["PaymentRepository"]
        UR["UserRepository"]
        IR["IdempotencyRepository"]
        TR["TermsAcceptanceRepository"]
        RR["RefreshTokenRepository"]
    end

    subgraph DB[("SQL Server\nDebtManager_db")]
    end

    DASH --> DBS
    DASH --> PS
    DEBTS --> DS
    DEBTS --> PS
    DETAIL --> DS
    DETAIL --> PS
    DETAIL --> CS
    CALC --> CS
    CALC --> DS
    PAGOS --> DS
    PAGOS --> PS
    Login --> AS
    PROFILE --> AS

    DS --> DR
    PS --> PR
    DBS --> DR
    DBS --> PS
    AS --> UR
    AS --> TR
    AS --> DR
    AS --> PR
    AS --> RR

    DR --> DB
    PR --> DB
    UR --> DB
    IR --> DB
    TR --> DB
    RR --> DB
```

```mermaid
sequenceDiagram
    actor User
    participant Browser
    participant Blazor as Blazor Server
    participant Service as Services
    participant DB as SQL Server

    User->>Browser: Navega a /debts
    Browser->>Blazor: Solicitud HTTP
    Blazor->>Blazor: AuthHelper.GetUserIdOrRedirect
    alt No autenticado
        Blazor->>Browser: Redirect /login
    end
    Blazor->>Service: DebtService.GetAllDebtsPagedAsync
    Service->>DB: Dapper query
    DB-->>Service: Lista de deudas
    Service-->>Blazor: List<Debt>
    Blazor-->>Browser: Renderiza tabla + Pagination
```

```mermaid
flowchart TD
    subgraph AuthFlow["Auth Flow"]
        LOGIN["Login.razor\nEmail+Password\n+ Acepta Términos"]
        PROFILE["Profile.razor\nEliminar cuenta\nModalDialog confirmación"]
        AS["AuthService\nRegisterAsync / LoginAsync / DeleteAccountAsync\nPassword validation\nRate limiting (5→15min)\nGuarda TermsAcceptance en BD\nElimina Payments, Debts,\nTermsAcceptance, RefreshTokens, User"]
        CSP["CustomAuthStateProvider\nSignIn / SignOut\n20min session timeout"]
        PAGES["Pages\nAuthHelper.GetUserIdOrRedirect"]
        DB[("SQL Server\nUsers, Debts, Payments,\nTermsAcceptance, RefreshTokens")]
    end

    LOGIN --> AS
    PROFILE --> AS
    AS --> DB
    AS --> CSP
    CSP --> PAGES
```
