using System.ComponentModel.DataAnnotations;

namespace RaizesNordesteWeb.API.Models
{
    // Aqui serve como uma log de Registro interno 
    public class AuditoriaLog
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        // Id do usuário (atendente, gerente ou admin) que executou a ação
        public Guid UsuarioId { get; set; }

        [Required, MaxLength(100)]
        public string Acao { get; set; } = string.Empty;

        // Contexto adicional em formato livre "
        [MaxLength(1000)]
        public string? Detalhe { get; set; }

        public DateTime DataHora { get; set; } = DateTime.UtcNow;
    }
}