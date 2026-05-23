using System.ComponentModel.DataAnnotations;
using static RaizesNordesteWeb.API.Models.Enums;

namespace RaizesNordesteWeb.API.Models
{
    /// Usuário do sistema — pode ser um cliente, atendente, cozinha, gerente ou admin.
    /// A senha nunca é armazenada em texto puro; somente o hash BCrypt é persistido (LGPD).
    public class Usuario
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, MaxLength(150)]
        public string Nome { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        // Armazenado como hash BCrypt 
        [Required]
        public string SenhaHash { get; set; } = string.Empty;

        public PerfilUsuario Perfil { get; set; } = PerfilUsuario.Cliente;

        public bool Ativo { get; set; } = true;

        public bool ConsentimentoLGPD { get; set; } = false;
        public DateTime? DataConsentimento { get; set; }

        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

        public Guid? ClienteId { get; set; }
        public Cliente? Cliente { get; set; }
    }
}
