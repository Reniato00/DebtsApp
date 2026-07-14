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
        DOM["domain\nEntities: Debt, Payment, User\nIdempotencyKey"]
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
    end

    subgraph Services["Services Layer"]
        DS["IDebtService"]
        PS["IPaymentService"]
        CS["ICalculatorService"]
        DBS["IDashboardService"]
        AS["IAuthService"]
    end

    subgraph Data["Data Access"]
        DR["DebtRepository"]
        PR["PaymentRepository"]
        UR["UserRepository"]
        IR["IdempotencyRepository"]
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

    DS --> DR
    PS --> PR
    DBS --> DR
    DBS --> PS
    AS --> UR

    DR --> DB
    PR --> DB
    UR --> DB
    IR --> DB
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
        LOGIN["Login.razor\nEmail+Password"]
        AS["AuthService\nRegisterAsync / LoginAsync\nPassword validation\nRate limiting (5→15min)"]
        CSP["CustomAuthStateProvider\nSignIn / SignOut\n20min session timeout"]
        PAGES["Pages\nAuthHelper.GetUserIdOrRedirect"]
    end

    LOGIN --> AS
    AS --> CSP
    CSP --> PAGES
```
