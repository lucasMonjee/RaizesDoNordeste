using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaizerNordesteWeb.API.Data;
using RaizesNordesteWeb.API.DTOs;
using RaizesNordesteWeb.API.Models;
using System.Security.Claims;
using static RaizesNordesteWeb.API.Models.Enums;

namespace RaizesNordesteWeb.API.Controllers
{
    [ApiController]
    [Route("pagamentos")]
    [Produces("application/json")]
    [Authorize]
    public class PagamentosController : ControllerBase
    {
        private readonly AppDbContext _db;

        public PagamentosController(AppDbContext db) => _db = db;

        /// <summary>
        /// Processa o pagamento de um pedido via serviço externo simulado (mock).
        /// Registra o resultado (aprovado ou recusado) e atualiza o status do pedido.
        /// Nota: nenhum pagamento real é realizado — apenas o fluxo é representado.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Gerente,Atendente,Cliente")]
        [ProducesResponseType(typeof(PagamentoResponse), 200)]
        [ProducesResponseType(typeof(ErroPadrao), 400)]
        [ProducesResponseType(typeof(ErroPadrao), 404)]
        [ProducesResponseType(typeof(ErroPadrao), 409)]
        public async Task<IActionResult> ProcessarMock([FromBody] ProcessarPagamentoRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var pedido = await _db.Pedidos
                .Include(p => p.Itens)
                .FirstOrDefaultAsync(p => p.Id == request.PedidoId);

            if (pedido == null)
                return NotFound(ErroPadrao.Criar("PEDIDO_NAO_ENCONTRADO",
                    "Pedido não encontrado.", "/pagamentos",
                    new() { new DetalheErro { Field = "pedidoId", Issue = "Pedido não existe." } }));

            // Verifica se já existe pagamento aprovado para este pedido
            var pagamentoExistente = await _db.Pagamentos
                .FirstOrDefaultAsync(p => p.PedidoId == request.PedidoId
                                       && p.Status == StatusPagamento.Aprovado);

            if (pagamentoExistente != null)
                return Conflict(ErroPadrao.Criar("PAGAMENTO_JA_APROVADO",
                    "Este pedido já possui um pagamento aprovado.",
                    "/pagamentos",
                    new() { new DetalheErro { Field = "pedidoId", Issue = "Pagamento já registrado." } }));

            // ── Simulação do gateway externo ────────────────────────────────────
            // Em produção, aqui seria feita a chamada HTTP ao provedor de pagamento.
            // Para fins acadêmicos, o parâmetro SimularAprovacao controla o resultado.
            var statusPagamento = request.SimularAprovacao
                ? StatusPagamento.Aprovado
                : StatusPagamento.Recusado;

            var respostaExterna = new
            {
                transactionId = Guid.NewGuid().ToString(),
                provider = "MockPay",
                status = request.SimularAprovacao ? "APPROVED" : "DECLINED",
                reason = request.SimularAprovacao ? null : "INSUFFICIENT_FUNDS",
                processedAt = DateTime.UtcNow.ToString("o")
            };
            // ────────────────────────────────────────────────────────────────────

            var pagamento = new Pagamento
            {
                PedidoId = pedido.Id,
                Valor = pedido.Total,
                Status = statusPagamento,
                Provedor = "MockPay",
                RespostaExterna = System.Text.Json.JsonSerializer.Serialize(respostaExterna),
                ProcessadoEm = DateTime.UtcNow
            };

            _db.Pagamentos.Add(pagamento);

            // Atualiza status do pedido conforme resultado do pagamento
            if (statusPagamento == StatusPagamento.Aprovado)
                pedido.Status = StatusPedido.EmPreparo;
            else
                pedido.Status = StatusPedido.Cancelado;

            // Auditoria do processamento de pagamento
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _db.AuditoriaLogs.Add(new AuditoriaLog
            {
                UsuarioId = Guid.TryParse(usuarioId, out var uid) ? uid : Guid.Empty,
                Acao = "PROCESSAR_PAGAMENTO_MOCK",
                Detalhe = $"Pedido {pedido.Id} | Status: {statusPagamento} | Provedor: MockPay"
            });

            // Acumula pontos de fidelidade se pagamento aprovado (1 ponto por R$ 1,00)
            if (statusPagamento == StatusPagamento.Aprovado)
            {
                var pontos = await _db.PontosClientes
                    .FirstOrDefaultAsync(p => p.ClienteId == pedido.ClienteId);

                if (pontos == null)
                {
                    pontos = new PontosCliente { ClienteId = pedido.ClienteId };
                    _db.PontosClientes.Add(pontos);
                }

                pontos.Acumular((int)Math.Floor(pedido.Total));
            }

            await _db.SaveChangesAsync();

            return Ok(new PagamentoResponse
            {
                PagamentoId = pagamento.Id,
                PedidoId = pagamento.PedidoId,
                Valor = pagamento.Valor,
                Status = pagamento.Status.ToString(),
                Provedor = pagamento.Provedor,
                RespostaExterna = respostaExterna,
                ProcessadoEm = pagamento.ProcessadoEm
            });
        }

        /// <summary>Consulta o pagamento de um pedido específico.</summary>
        [HttpGet("{pedidoId:guid}")]
        [ProducesResponseType(typeof(PagamentoResponse), 200)]
        [ProducesResponseType(typeof(ErroPadrao), 404)]
        public async Task<IActionResult> ConsultarPorPedido(Guid pedidoId)
        {
            var pagamento = await _db.Pagamentos
                .FirstOrDefaultAsync(p => p.PedidoId == pedidoId);

            if (pagamento == null)
                return NotFound(ErroPadrao.Criar("PAGAMENTO_NAO_ENCONTRADO",
                    "Pagamento não encontrado para este pedido.",
                    $"/pagamentos/{pedidoId}"));

            return Ok(new PagamentoResponse
            {
                PagamentoId = pagamento.Id,
                PedidoId = pagamento.PedidoId,
                Valor = pagamento.Valor,
                Status = pagamento.Status.ToString(),
                Provedor = pagamento.Provedor,
                ProcessadoEm = pagamento.ProcessadoEm
            });
        }
    }
}
