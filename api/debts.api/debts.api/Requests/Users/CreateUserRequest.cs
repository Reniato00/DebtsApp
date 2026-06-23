using System.ComponentModel.DataAnnotations;

namespace debts.api.Requests.Users
{
    /// <summary>
    /// Solicitud para registrar un nuevo usuario.
    /// </summary>
    public class CreateUserRequest
    {
        /// <summary>Nombre del usuario.</summary>
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>Correo electrónico del usuario.</summary>
        [Required, MaxLength(255)]
        public string Email { get; set; } = string.Empty;
    }
}
