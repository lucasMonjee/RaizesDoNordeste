using System.ComponentModel.DataAnnotations;
using static RaizesNordesteWeb.API.Models.Enums;

namespace RaizesNordesteWeb.API.DTOs
{
    //REQUEST 
    public class CriarPedidoRequest
    {
        [Required(ErrorMessage = "O canalPedido é obrigatório.")]
        public CanalAtendimento CanalPedido { get; set; }

        [Required(ErrorMessage = "O clienteId é obrigatório.")]
        public Guid ClienteId { get; set; }

        [Required(ErrorMessage = "O unidadeId é obrigatório.")]
        public Guid UnidadeId { get; set; }

        [Required(ErrorMessage = "Informe ao menos um item no pedido.")]
        [MinLength(1, ErrorMessage = "Informe ao menos um item no pedido.")]
        public List<ItemPedidoRequest> Itens { get; set; } = new();

        [Required(ErrorMessage = "A formaPagamento é obrigatória.")]
        public FormaPagamento FormaPagamento { get; set; } = FormaPagamento.Mock;

        // Opcional: pontos a resgatar para desconto
        public int PontosResgatar { get; set; } = 0;
    }

    public class ItemPedidoRequest
    {
        [Required]
        public Guid ProdutoId { get; set; }

        [Range(1, 99, ErrorMessage = "A quantidade deve ser entre 1 e 99.")]
        public int Quantidade { get; set; }
    }

    public class AtualizarStatusRequest
    {
        [Required(ErrorMessage = "O status é obrigatório.")]
        public StatusPedido Status { get; set; }
    }

    //RESPONSE 

    public class PedidoResponse
    {
        public Guid PedidoId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string CanalPedido { get; set; } = string.Empty;
        public string FormaPagamento { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public decimal Desconto { get; set; }
        public List<ItemPedidoResponse> Itens { get; set; } = new();
        public DateTime CriadoEm { get; set; }
    }

    public class ItemPedidoResponse
    {
        public Guid ProdutoId { get; set; }
        public string NomeProduto { get; set; } = string.Empty;
        public int Quantidade { get; set; }
        public decimal PrecoUnitario { get; set; }
        public decimal Subtotal { get; set; }
    }
}
