using System.ComponentModel.DataAnnotations;
using static RaizesNordesteWeb.API.Models.Enums;

namespace RaizesNordesteWeb.API.DTOs
{
    // REQUEST 

    public class LoginRequest
    {
        [Required(ErrorMessage = "O e-mail é obrigatório.")]
        [EmailAddress(ErrorMessage = "Formato de e-mail inválido.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "A senha é obrigatória.")]
        [MinLength(6, ErrorMessage = "A senha deve ter pelo menos 6 caracteres.")]
        public string Senha { get; set; } = string.Empty;
    }

    public class RegisterRequest
    {
        [Required(ErrorMessage = "O nome é obrigatório.")]
        [MaxLength(150)]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "O e-mail é obrigatório.")]
        [EmailAddress(ErrorMessage = "Formato de e-mail inválido.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "A senha é obrigatória.")]
        [MinLength(6, ErrorMessage = "A senha deve ter pelo menos 6 caracteres.")]
        public string Senha { get; set; } = string.Empty;

        // Por padrão, novos cadastros recebem perfil Cliente
        public PerfilUsuario Perfil { get; set; } = PerfilUsuario.Cliente;

        // Consentimento LGPD obrigatório — o usuário deve aceitar explicitamente
        [Range(typeof(bool), "true", "true",
            ErrorMessage = "É necessário aceitar os termos de uso e política de privacidade (LGPD).")]
        public bool ConsentimentoLGPD { get; set; } = false;
    }

    // ─── RESPONSE 

    public class AuthResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string TokenType { get; set; } = "Bearer";
        public int ExpiresIn { get; set; }       // segundos
        public UsuarioResponse User { get; set; } = null!;
    }

    public class UsuarioResponse
    {
        public Guid Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Perfil { get; set; } = string.Empty;
    }
}
