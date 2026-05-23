using System.ComponentModel.DataAnnotations;

namespace RaizesNordesteWeb.API.DTOs
{
    // ─── REQUEST ────────────────────────────────────────────────────────────────

    public class ProcessarPagamentoRequest
    {
        [Required]
        public Guid PedidoId { get; set; }

        /// <summary>
        /// Quando true, o mock simula aprovação; quando false, simula recusa.
        /// Permite testar os dois cenários de pagamento conforme o roteiro exige.
        /// </summary>
        public bool SimularAprovacao { get; set; } = true;
    }

    // ─── RESPONSE ───────────────────────────────────────────────────────────────

    public class PagamentoResponse
    {
        public Guid PagamentoId { get; set; }
        public Guid PedidoId { get; set; }
        public decimal Valor { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Provedor { get; set; } = string.Empty;

        /// <summary>Payload simulado retornado pelo "gateway" externo.</summary>
        public object? RespostaExterna { get; set; }

        public DateTime ProcessadoEm { get; set; }
    }
}
