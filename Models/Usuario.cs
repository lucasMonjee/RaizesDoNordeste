using System.ComponentModel.DataAnnotations;
using static RaizesNordesteWeb.API.Models.Enums;

namespace RaizesNordesteWeb.API.Models
{
    /// <summary>
    /// Usuário do sistema — pode ser um cliente, atendente, cozinha, gerente ou admin.
    /// A senha nunca é armazenada em texto puro; somente o hash BCrypt é persistido (LGPD).
    /// </summary>
    public class Usuario
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, MaxLength(150)]
        public string Nome { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        // Armazenado como hash BCrypt — nunca exposto em responses (LGPD)
        [Required]
        public string SenhaHash { get; set; } = string.Empty;

        public PerfilUsuario Perfil { get; set; } = PerfilUsuario.Cliente;

        public bool Ativo { get; set; } = true;

        // Consentimento LGPD: registra se o usuário autorizou uso dos dados
        public bool ConsentimentoLGPD { get; set; } = false;
        public DateTime? DataConsentimento { get; set; }

        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

        // Vínculo opcional com Cliente (quando o perfil é Cliente)
        public Guid? ClienteId { get; set; }
        public Cliente? Cliente { get; set; }
    }
}
