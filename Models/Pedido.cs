using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static RaizesNordesteWeb.API.Models.Enums;

namespace RaizesNordesteWeb.API.Models
{
    public class Pedido
    {
        [Key]
        public Guid Id {  get; set; } = Guid.NewGuid();
        public Guid ClienteId { get; set; }
        public Guid UnidadeId { get; set; }

        public StatusPedido Status { get; set; } = StatusPedido.Aguardando;

        // canalPedido: campo obrigatório conforme requisito de multicanalidade (APP, TOTEM, BALCAO, PICKUP, WEB)
        public CanalAtendimento CanalPedido { get; set; } = CanalAtendimento.App;

        // Forma de pagamento solicitada pelo cliente na criação do pedido
        public FormaPagamento FormaPagamento { get; set; } = FormaPagamento.Mock;

        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

        // Desconto aplicado via resgate de pontos de fidelidade
        [Column(TypeName = "decimal(10,2)")]
        public decimal Desconto { get; set; } = 0;

        [ForeignKey(nameof(ClienteId))]
        public Cliente Cliente { get; set; } = null!;

        [ForeignKey(nameof(UnidadeId))]
        public Unidade Unidade { get; set; } = null!;

        public ICollection<ItemPedido> Itens { get; set; } = new List<ItemPedido>();
        public Pagamento Pagamento { get; set; }

        public decimal Total => Itens.Sum(i => i.PrecoUnit * i.Quantidade) - Desconto;

        public bool Cancelar() 
        {
            if (Status == StatusPedido.Entregue) return false;
            Status = StatusPedido.Cancelado;
            return true;
        }

        public class ItemPedido
        {
            public Guid Id {  get; set; } = Guid.NewGuid(); 
            public Guid PedidoId { get; set; }  
            public Guid ProdutoId { get; set; }

            [Range(1,99)]
            public int Quantidade { get; set; }

            //Aqui o preço é com base no anuncio, porem pode mudar futuramente
            [Column(TypeName = "decimal(10,2)")]
            public decimal PrecoUnit { get; set; }

            [ForeignKey(nameof(PedidoId))]
            public Pedido Pedido { get; set; } = null!;

            [ForeignKey(nameof(ProdutoId))]
            public Produto Produto { get; set; } = null!;


        }


    }
}
