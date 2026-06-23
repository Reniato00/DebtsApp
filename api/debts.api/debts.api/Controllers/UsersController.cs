using application.Services;
using debts.api.Requests.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace debts.api.Controllers
{
    /// <summary>
    /// Gestión de usuarios del sistema.
    /// </summary>
    [ApiController]
    [Route("api/user")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService userService;

        public UsersController(IUserService userService)
        {
            this.userService = userService;
        }

        /// <summary>
        /// Registra un nuevo usuario en el sistema.
        /// </summary>
        /// <param name="body">Datos del usuario a registrar.</param>
        /// <returns>El usuario creado con su Id asignado.</returns>
        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] CreateUserRequest body)
        {
            var user = await userService.CreateUserAsync(body.Name, body.Email);
            return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
        }

        /// <summary>
        /// Obtiene un usuario por su Id.
        /// </summary>
        /// <param name="id">Id del usuario.</param>
        /// <returns>El usuario si existe; 404 en caso contrario.</returns>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var user = await userService.GetByIdAsync(id);
            if (user == null) return NotFound();
            return Ok(user);
        }

        /// <summary>
        /// Actualiza los datos de un usuario existente.
        /// </summary>
        /// <param name="id">Id del usuario a actualizar.</param>
        /// <param name="body">Datos actualizados.</param>
        /// <returns>El usuario actualizado; 404 si no existe.</returns>
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest body)
        {
            var user = await userService.UpdateUserAsync(id, body.Name, body.Email);
            if (user == null) return NotFound();
            return Ok(user);
        }
    }
}
