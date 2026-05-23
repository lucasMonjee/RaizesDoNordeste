using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaizerNordesteWeb.API.Data;
using RaizesNordesteWeb.API.DTOs;

namespace RaizesNordesteWeb.API.Controllers
{
    [ApiController]
    [Route("fidelidade")]
    [Produces("application/json")]
    [Authorize]
    public class FidelidadeController : ControllerBase
    {
        private readonly AppDbContext _db;

        public FidelidadeController(AppDbContext db) => _db = db;

        /// <summary>Consulta o saldo de pontos de fidelidade de um cliente.</summary>
        [HttpGet("{clienteId:guid}")]
        [Authorize(Roles = "Admin,Gerente,Cliente")]
        [ProducesResponseType(200)]
        [ProducesResponseType(typeof(ErroPadrao), 404)]
        public async Task<IActionResult> ConsultarSaldo(Guid clienteId)
        {
            var cliente = await _db.Clientes.FindAsync(clienteId);
            if (cliente == null)
                return NotFound(ErroPadrao.Criar("CLIENTE_NAO_ENCONTRADO",
                    "Cliente não encontrado.", $"/fidelidade/{clienteId}"));

            var pontos = await _db.PontosClientes
                .FirstOrDefaultAsync(p => p.ClienteId == clienteId);

            return Ok(new
            {
                clienteId,
                saldoPontos = pontos?.Saldo ?? 0,
                equivalenteReais = (pontos?.Saldo ?? 0) * 0.10m,
                atualizadoEm = pontos?.AtualizadoEm
            });
        }

        /// <summary>
        /// Consulta o histórico de pedidos do cliente (rastreamento de ganho e uso de pontos).
        /// </summary>
        [HttpGet("{clienteId:guid}/historico")]
        [Authorize(Roles = "Admin,Gerente,Cliente")]
        [ProducesResponseType(200)]
        [ProducesResponseType(typeof(ErroPadrao), 404)]
        public async Task<IActionResult> Historico(Guid clienteId,
            [FromQuery] int page = 1, [FromQuery] int limit = 10)
        {
            if (page < 1) page = 1;
            if (limit < 1 || limit > 100) limit = 10;

            var cliente = await _db.Clientes.FindAsync(clienteId);
            if (cliente == null)
                return NotFound(ErroPadrao.Criar("CLIENTE_NAO_ENCONTRADO",
                    "Cliente não encontrado.", $"/fidelidade/{clienteId}/historico"));

            var pedidos = await _db.Pedidos
                .Include(p => p.Pagamento)
                .Where(p => p.ClienteId == clienteId)
                .OrderByDescending(p => p.CriadoEm)
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(p => new
                {
                    pedidoId = p.Id,
                    total = p.Total,
                    status = p.Status.ToString(),
                    canalPedido = p.CanalPedido.ToString(),
                    desconto = p.Desconto,
                    // Pontos ganhos: 1 ponto por R$ 1,00 gasto (apenas pedidos aprovados)
                    pontosGanhos = p.Pagamento != null && p.Pagamento.Status == Models.Enums.StatusPagamento.Aprovado
                        ? (int)Math.Floor(p.Total)
                        : 0,
                    criadoEm = p.CriadoEm
                })
                .ToListAsync();

            return Ok(new { clienteId, page, limit, historico = pedidos });
        }
    }
}
