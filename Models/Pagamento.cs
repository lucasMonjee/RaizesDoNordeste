using RaizesNordesteWeb.API.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static RaizesNordesteWeb.API.Models.Enums;

namespace RaizesNordesteWeb.API.Models
{
    public class Pagamento
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid PedidoId { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Valor { get; set; }

        public StatusPagamento Status { get; set; } = StatusPagamento.Pendente;

        // Nome do provedor externo (ex: "PagSeguro", "Stripe", "Cielo")
        [MaxLength(100)]
        public string Provedor { get; set; } = string.Empty;

        // JSON ou código retornado pelo serviço externo
        [MaxLength(1000)]
        public string? RespostaExterna { get; set; }

        public DateTime ProcessadoEm { get; set; } = DateTime.UtcNow;

        // Navegação
        [ForeignKey(nameof(PedidoId))]
        public Pedido Pedido { get; set; } = null!;
    }

    public class EstoqueUnidade
    {
        // Chave composta: UnidadeId + ProdutoId
        public Guid UnidadeId { get; set; }
        public Guid ProdutoId { get; set; }

        public int Quantidade { get; set; } = 0;

        // Quantidade mínima antes de disparar alerta
        public int AlertaMinimo { get; set; } = 5;

        // Navegação
        [ForeignKey(nameof(UnidadeId))]
        public Unidade Unidade { get; set; } = null!;

        [ForeignKey(nameof(ProdutoId))]
        public Produto Produto { get; set; } = null!;

        public bool AbaixoDoAlerta => Quantidade <= AlertaMinimo;
    }
}