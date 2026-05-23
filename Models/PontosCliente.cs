using RaizesNordesteWeb.API.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaizesNordesteWeb.API.Models
{
    public class PontosCliente
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid ClienteId { get; set; }

        public int Saldo { get; set; } = 0;

        public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;

        // Navegação
        [ForeignKey(nameof(ClienteId))]
        public Cliente Cliente { get; set; } = null!;

        /// <summary>
        /// Verifica se há saldo suficiente e debita os pontos.
        /// Retorna false se saldo insuficiente.
        /// </summary>
        public bool Resgatar(int pontos)
        {
            if (Saldo < pontos) return false;
            Saldo -= pontos;
            AtualizadoEm = DateTime.UtcNow;
            return true;
        }

        public void Acumular(int pontos)
        {
            Saldo += pontos;
            AtualizadoEm = DateTime.UtcNow;
        }
    }
}