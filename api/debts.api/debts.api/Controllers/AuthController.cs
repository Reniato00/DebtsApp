using System.Security.Claims;
using application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace debts.api.Controllers
{
/// <summary>
/// Autenticación, healthcheck e información del usuario actual.
/// </summary>
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthService authService;

    public AuthController(IAuthService authService)
    {
        this.authService = authService;
    }

    /// <summary>
    /// Registra un nuevo usuario con email y contraseña.
    /// </summary>
    /// <param name="body">Datos de registro: email, name, password.</param>
    /// <returns>Access token, refresh token y tiempo de expiración.</returns>
    [AllowAnonymous]
    [HttpPost("api/auth/register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest body)
    {
        try
        {
            var (accessToken, refreshToken, expiresIn) =
                await authService.RegisterAsync(body.Email, body.Name, body.Password);
            return Ok(new
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = expiresIn
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { Message = ex.Message });
        }
    }

    /// <summary>
    /// Inicia sesión con email y contraseña.
    /// </summary>
    /// <param name="body">Credenciales: email y password.</param>
    /// <returns>Access token, refresh token y tiempo de expiración.</returns>
    [AllowAnonymous]
    [HttpPost("api/auth/login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest body)
    {
        try
        {
            var (accessToken, refreshToken, expiresIn) =
                await authService.LoginAsync(body.Email, body.Password);
            return Ok(new
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = expiresIn
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { Message = "Invalid email or password" });
        }
    }

    /// <summary>
    /// Renueva el access token usando un refresh token válido (rotación).
    /// </summary>
    /// <param name="body">Refresh token actual.</param>
    /// <returns>Nuevo par access + refresh token.</returns>
    [AllowAnonymous]
    [HttpPost("api/auth/refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest body)
    {
        try
        {
            var (accessToken, refreshToken, expiresIn) =
                await authService.RefreshTokenAsync(body.RefreshToken);
            return Ok(new
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = expiresIn
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { Message = "Invalid or expired refresh token" });
        }
    }

    /// <summary>
    /// Revoca todos los refresh tokens del usuario autenticado.
    /// </summary>
    [HttpPost("api/auth/logout")]
    public async Task<IActionResult> Logout()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
            return Unauthorized(new { Message = "User not authenticated" });

        await authService.LogoutAsync(Guid.Parse(userIdClaim));
        return Ok(new { Message = "Logged out successfully" });
    }

    /// <summary>
    /// Verifica que el servicio esté operativo.
    /// </summary>
    /// <returns>Estado y timestamp actual.</returns>
    [AllowAnonymous]
    [HttpGet("api/health")]
    public IActionResult HealthCheck()
    {
        return Ok(new
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Obtiene los datos del usuario autenticado a partir del token JWT.
    /// </summary>
    /// <returns>UserId y Name extraídos del token.</returns>
    [HttpGet("api/auth/me")]
    public IActionResult GetCurrentUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var name = User.FindFirstValue(ClaimTypes.Name);

        if (userId == null)
            return Unauthorized(new { Message = "User not authenticated" });

        return Ok(new
        {
            UserId = Guid.Parse(userId),
            Name = name ?? "Unknown"
        });
    }
}

/// <summary>
/// Solicitud de registro.
/// </summary>
public class RegisterRequest
{
    /// <summary>Correo electrónico.</summary>
    public string Email { get; set; } = "";
    /// <summary>Nombre del usuario.</summary>
    public string Name { get; set; } = "";
    /// <summary>Contraseña (mínimo 6 caracteres).</summary>
    public string Password { get; set; } = "";
}

/// <summary>
/// Solicitud de inicio de sesión.
/// </summary>
public class LoginRequest
{
    /// <summary>Correo electrónico.</summary>
    public string Email { get; set; } = "";
    /// <summary>Contraseña.</summary>
    public string Password { get; set; } = "";
}

/// <summary>
/// Solicitud de renovación de token.
/// </summary>
public class RefreshRequest
{
    /// <summary>Refresh token emitido en el login.</summary>
    public string RefreshToken { get; set; } = string.Empty;
}
}
