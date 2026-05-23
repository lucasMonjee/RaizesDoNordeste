using RaizesNordesteWeb.API.Models;
using System.ComponentModel.DataAnnotations;

namespace RaizesNordesteWeb.API.Models
{
    public class Unidade
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, MaxLength(150)]
        public string Nome { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Cidade { get; set; } = string.Empty;

        [Required, MaxLength(2)]
        public string UF { get; set; } = string.Empty;

        // "completa" ou "reduzida" — unidades podem ter cozinhas diferentes
        [MaxLength(50)]
        public string TipoCozinha { get; set; } = "completa";

        public bool Ativa { get; set; } = true;

        // Navegação
        public ICollection<Pedido> Pedidos { get; set; } = new List<Pedido>();
        public ICollection<EstoqueUnidade> Estoques { get; set; } = new List<EstoqueUnidade>();
    }
}