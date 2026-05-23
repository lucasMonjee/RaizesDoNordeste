using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaizerNordesteWeb.API.Data;
using RaizesNordesteWeb.API.DTOs;
using RaizesNordesteWeb.API.Models;
using System.Security.Claims;
using static RaizesNordesteWeb.API.Models.Enums;
using static RaizesNordesteWeb.API.Models.Pedido;

namespace RaizesNordesteWeb.API.Controllers
{
    [ApiController]
    [Route("pedidos")]
    [Produces("application/json")]
    [Authorize]
    public class PedidosController : ControllerBase
    {
        private readonly AppDbContext _db;

        public PedidosController(AppDbContext db) => _db = db;

        /// <summary>
        /// Lista pedidos com filtros por canal, status e unidade.
        /// Clientes só veem os próprios pedidos; Gerentes e Admins veem todos.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(200)]
        [ProducesResponseType(typeof(ErroPadrao), 401)]
        public async Task<IActionResult> Listar(
            [FromQuery] CanalAtendimento? canalPedido,
            [FromQuery] StatusPedido? status,
            [FromQuery] Guid? unidadeId,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10)
        {
            if (page < 1) page = 1;
            if (limit < 1 || limit > 100) limit = 10;

            var query = _db.Pedidos
                .Include(p => p.Itens).ThenInclude(i => i.Produto)
                .Include(p => p.Cliente)
                .AsQueryable();

            // Clientes só enxergam seus próprios pedidos (LGPD — minimização de dados)
            var perfil = User.FindFirstValue(ClaimTypes.Role);
            if (perfil == PerfilUsuario.Cliente.ToString())
            {
                var usuarioId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var cliente = await _db.Clientes
                    .FirstOrDefaultAsync(c => _db.Usuarios
                        .Any(u => u.Id == usuarioId && u.ClienteId == c.id));

                if (cliente != null)
                    query = query.Where(p => p.ClienteId == cliente.id);
            }

            // Filtros da query (requisito de multicanalidade)
            if (canalPedido.HasValue)
                query = query.Where(p => p.CanalPedido == canalPedido.Value);

            if (status.HasValue)
                query = query.Where(p => p.Status == status.Value);

            if (unidadeId.HasValue)
                query = query.Where(p => p.UnidadeId == unidadeId.Value);

            var total = await query.CountAsync();
            var pedidos = await query
                .OrderByDescending(p => p.CriadoEm)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync();

            var resposta = pedidos.Select(MapearParaResponse).ToList();

            return Ok(new { total, page, limit, data = resposta });
        }

        /// <summary>Retorna um pedido específico pelo ID.</summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(PedidoResponse), 200)]
        [ProducesResponseType(typeof(ErroPadrao), 404)]
        public async Task<IActionResult> ObterPorId(Guid id)
        {
            var pedido = await _db.Pedidos
                .Include(p => p.Itens).ThenInclude(i => i.Produto)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null)
                return NotFound(ErroPadrao.Criar("PEDIDO_NAO_ENCONTRADO",
                    "Pedido não encontrado.", $"/pedidos/{id}"));

            return Ok(MapearParaResponse(pedido));
        }

        /// <summary>
        /// Cria um novo pedido validando estoque e calculando o total.
        /// O campo canalPedido é obrigatório (multicanalidade).
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Gerente,Atendente,Cliente")]
        [ProducesResponseType(typeof(PedidoResponse), 201)]
        [ProducesResponseType(typeof(ErroPadrao), 400)]
        [ProducesResponseType(typeof(ErroPadrao), 404)]
        [ProducesResponseType(typeof(ErroPadrao), 409)]
        [ProducesResponseType(typeof(ErroPadrao), 422)]
        public async Task<IActionResult> Criar([FromBody] CriarPedidoRequest request)
        {
            if (!ModelState.IsValid)
                return UnprocessableEntity(CriarErroValidacao("/pedidos"));

            // Verifica se cliente existe
            var cliente = await _db.Clientes.FindAsync(request.ClienteId);
            if (cliente == null)
                return NotFound(ErroPadrao.Criar("CLIENTE_NAO_ENCONTRADO",
                    "Cliente não encontrado.", "/pedidos",
                    new() { new DetalheErro { Field = "clienteId", Issue = "Cliente não existe." } }));

            // Verifica se unidade existe e está ativa
            var unidade = await _db.Unidades.FindAsync(request.UnidadeId);
            if (unidade == null || !unidade.Ativa)
                return NotFound(ErroPadrao.Criar("UNIDADE_NAO_ENCONTRADA",
                    "Unidade não encontrada ou inativa.", "/pedidos",
                    new() { new DetalheErro { Field = "unidadeId", Issue = "Unidade não disponível." } }));

            // Valida produtos e estoque de cada item
            var itens = new List<ItemPedido>();
            var errosEstoque = new List<DetalheErro>();

            for (int i = 0; i < request.Itens.Count; i++)
            {
                var itemReq = request.Itens[i];

                var produto = await _db.Produtos.FindAsync(itemReq.ProdutoId);
                if (produto == null || !produto.Disponivel)
                {
                    return NotFound(ErroPadrao.Criar("PRODUTO_NAO_ENCONTRADO",
                        "Um ou mais produtos não existem ou estão indisponíveis.", "/pedidos",
                        new() { new DetalheErro
                            { Field = $"itens[{i}].produtoId", Issue = "Produto não encontrado ou indisponível." } }));
                }

                var estoque = await _db.EstoquesUnidade
                    .FirstOrDefaultAsync(e => e.UnidadeId == request.UnidadeId
                                           && e.ProdutoId == itemReq.ProdutoId);

                if (estoque == null || estoque.Quantidade < itemReq.Quantidade)
                {
                    errosEstoque.Add(new DetalheErro
                    {
                        Field = $"itens[{i}].quantidade",
                        Issue = $"Disponível: {estoque?.Quantidade ?? 0}"
                    });
                }
                else
                {
                    itens.Add(new ItemPedido
                    {
                        ProdutoId = produto.Id,
                        Quantidade = itemReq.Quantidade,
                        PrecoUnit = produto.Preco
                    });
                }
            }

            // Retorna 409 se qualquer item tiver estoque insuficiente
            if (errosEstoque.Any())
                return Conflict(ErroPadrao.Criar("ESTOQUE_INSUFICIENTE",
                    "Não há quantidade suficiente para um ou mais itens.", "/pedidos", errosEstoque));

            // Calcula desconto por resgate de pontos de fidelidade
            decimal desconto = 0;
            if (request.PontosResgatar > 0)
            {
                var pontos = await _db.PontosClientes
                    .FirstOrDefaultAsync(p => p.ClienteId == cliente.id);

                if (pontos != null && pontos.Resgatar(request.PontosResgatar))
                {
                    // Regra: 1 ponto = R$ 0,10 de desconto
                    desconto = request.PontosResgatar * 0.10m;
                }
            }

            // Cria o pedido
            var pedido = new Pedido
            {
                ClienteId = cliente.id,
                UnidadeId = unidade.Id,
                CanalPedido = request.CanalPedido,
                FormaPagamento = request.FormaPagamento,
                Desconto = desconto,
                Itens = itens
            };

            _db.Pedidos.Add(pedido);

            // Desconta do estoque de cada item
            foreach (var item in itens)
            {
                var estoque = await _db.EstoquesUnidade
                    .FirstAsync(e => e.UnidadeId == unidade.Id && e.ProdutoId == item.ProdutoId);
                estoque.Quantidade -= item.Quantidade;
            }

            // Auditoria de criação de pedido
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _db.AuditoriaLogs.Add(new AuditoriaLog
            {
                UsuarioId = Guid.TryParse(usuarioId, out var uid) ? uid : Guid.Empty,
                Acao = "CRIAR_PEDIDO",
                Detalhe = $"Pedido {pedido.Id} | Canal: {pedido.CanalPedido} | Unidade: {unidade.Nome}"
            });

            await _db.SaveChangesAsync();

            // Recarrega para retornar os dados completos
            await _db.Entry(pedido).Collection(p => p.Itens).Query()
                .Include(i => i.Produto).LoadAsync();

            return CreatedAtAction(nameof(ObterPorId), new { id = pedido.Id },
                MapearParaResponse(pedido));
        }

        /// <summary>
        /// Atualiza o status do pedido (cozinha → pronto → entregue / cancelado).
        /// Restrito a Admin, Gerente, Atendente e Cozinha.
        /// </summary>
        [HttpPatch("{id:guid}/status")]
        [Authorize(Roles = "Admin,Gerente,Atendente,Cozinha")]
        [ProducesResponseType(200)]
        [ProducesResponseType(typeof(ErroPadrao), 404)]
        [ProducesResponseType(typeof(ErroPadrao), 409)]
        public async Task<IActionResult> AtualizarStatus(Guid id, [FromBody] AtualizarStatusRequest request)
        {
            var pedido = await _db.Pedidos.FindAsync(id);
            if (pedido == null)
                return NotFound(ErroPadrao.Criar("PEDIDO_NAO_ENCONTRADO",
                    "Pedido não encontrado.", $"/pedidos/{id}/status"));

            // Regra: pedido entregue não pode ser alterado
            if (pedido.Status == StatusPedido.Entregue)
                return Conflict(ErroPadrao.Criar("STATUS_INVALIDO",
                    "Pedidos já entregues não podem ter o status alterado.",
                    $"/pedidos/{id}/status",
                    new() { new DetalheErro { Field = "status", Issue = "Status atual: Entregue." } }));

            var statusAnterior = pedido.Status;
            pedido.Status = request.Status;

            // Auditoria de mudança de status
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _db.AuditoriaLogs.Add(new AuditoriaLog
            {
                UsuarioId = Guid.TryParse(usuarioId, out var uid) ? uid : Guid.Empty,
                Acao = "ATUALIZAR_STATUS_PEDIDO",
                Detalhe = $"Pedido {id} | {statusAnterior} → {request.Status}"
            });

            await _db.SaveChangesAsync();

            return Ok(new { pedido.Id, Status = pedido.Status.ToString(), pedido.CriadoEm });
        }

        /// <summary>Cancela um pedido (somente se não foi entregue).</summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin,Gerente,Cliente")]
        [ProducesResponseType(204)]
        [ProducesResponseType(typeof(ErroPadrao), 404)]
        [ProducesResponseType(typeof(ErroPadrao), 409)]
        public async Task<IActionResult> Cancelar(Guid id)
        {
            var pedido = await _db.Pedidos
                .Include(p => p.Itens)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null)
                return NotFound(ErroPadrao.Criar("PEDIDO_NAO_ENCONTRADO",
                    "Pedido não encontrado.", $"/pedidos/{id}"));

            if (!pedido.Cancelar())
                return Conflict(ErroPadrao.Criar("CANCELAMENTO_INVALIDO",
                    "Pedidos já entregues não podem ser cancelados.",
                    $"/pedidos/{id}",
                    new() { new DetalheErro { Field = "status", Issue = "Status: Entregue — cancelamento não permitido." } }));

            // Devolve itens ao estoque ao cancelar
            foreach (var item in pedido.Itens)
            {
                var estoque = await _db.EstoquesUnidade
                    .FirstOrDefaultAsync(e => e.UnidadeId == pedido.UnidadeId
                                           && e.ProdutoId == item.ProdutoId);
                if (estoque != null)
                    estoque.Quantidade += item.Quantidade;
            }

            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _db.AuditoriaLogs.Add(new AuditoriaLog
            {
                UsuarioId = Guid.TryParse(usuarioId, out var uid) ? uid : Guid.Empty,
                Acao = "CANCELAR_PEDIDO",
                Detalhe = $"Pedido {id} cancelado."
            });

            await _db.SaveChangesAsync();
            return NoContent();
        }

        // ─── Helper ─────────────────────────────────────────────────────────────

        private static PedidoResponse MapearParaResponse(Pedido pedido) => new()
        {
            PedidoId = pedido.Id,
            Status = pedido.Status.ToString(),
            CanalPedido = pedido.CanalPedido.ToString(),
            FormaPagamento = pedido.FormaPagamento.ToString(),
            Total = pedido.Total,
            Desconto = pedido.Desconto,
            CriadoEm = pedido.CriadoEm,
            Itens = pedido.Itens.Select(i => new ItemPedidoResponse
            {
                ProdutoId = i.ProdutoId,
                NomeProduto = i.Produto?.Nome ?? string.Empty,
                Quantidade = i.Quantidade,
                PrecoUnitario = i.PrecoUnit,
                Subtotal = i.PrecoUnit * i.Quantidade
            }).ToList()
        };

        private ErroPadrao CriarErroValidacao(string path)
        {
            var detalhes = ModelState
                .Where(m => m.Value?.Errors.Count > 0)
                .SelectMany(m => m.Value!.Errors.Select(e =>
                    new DetalheErro { Field = m.Key, Issue = e.ErrorMessage }))
                .ToList();

            return ErroPadrao.Criar("VALIDACAO_INVALIDA",
                "Um ou mais campos estão incorretos.", path, detalhes);
        }
    }
}
