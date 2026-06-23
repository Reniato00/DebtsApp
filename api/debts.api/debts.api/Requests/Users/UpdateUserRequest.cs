using System.ComponentModel.DataAnnotations;

namespace debts.api.Requests.Users
{
    /// <summary>
    /// Solicitud para actualizar un usuario existente.
    /// </summary>
    public class UpdateUserRequest
    {
        /// <summary>Nombre del usuario.</summary>
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>Correo electrónico del usuario.</summary>
        [Required, MaxLength(255)]
        public string Email { get; set; } = string.Empty;
    }
}
