using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static RaizesNordesteWeb.API.Models.Pedido;

namespace RaizesNordesteWeb.API.Models
{
    public class Produto
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid CategoriaId { get; set; }

        [Required, MaxLength(150)]
        public string Nome { get; set; } = string.Empty;

        [Column(TypeName = "decimal(10,2)")]
        public decimal Preco { get; set; }

        public bool Disponivel { get; set; } = true;

        // Nesse caso seria o periodo junino por exemplo
        public bool Sazonal { get; set; } = false;

        [MaxLength(100)]
        public string? PeriodoDisponivel { get; set; }

        [ForeignKey(nameof(CategoriaId))]
        public Categoria Categoria { get; set; } = null!;

        public ICollection<ItemPedido> ItensPedido { get; set; } = new List<ItemPedido>();
        public ICollection<EstoqueUnidade> Estoques { get; set; } = new List<EstoqueUnidade>();
    }

    public class Categoria 
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();  

        [Required, MaxLength(100)]
        public string Nome { get; set; } = string.Empty;

        [MaxLength(300)]
        public string Descricao {get; set;} = string.Empty;

        public ICollection<Produto> Produtos { get; set; } = new List<Produto>();

    }

}
