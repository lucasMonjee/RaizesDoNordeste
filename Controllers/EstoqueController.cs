using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaizerNordesteWeb.API.Data;
using RaizesNordesteWeb.API.DTOs;
using RaizesNordesteWeb.API.Models;
using System.Security.Claims;

namespace RaizesNordesteWeb.API.Controllers
{
    [ApiController]
    [Route("estoque")]
    [Produces("application/json")]
    [Authorize(Roles = "Admin,Gerente,Atendente")]
    public class EstoqueController : ControllerBase
    {
        private readonly AppDbContext _db;

        public EstoqueController(AppDbContext db) => _db = db;

        /// <summary>Consulta o estoque de produtos em uma unidade específica.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<EstoqueResponse>), 200)]
        [ProducesResponseType(typeof(ErroPadrao), 401)]
        [ProducesResponseType(typeof(ErroPadrao), 403)]
        [ProducesResponseType(typeof(ErroPadrao), 404)]
        public async Task<IActionResult> ConsultarPorUnidade(
            [FromQuery] Guid unidadeId,
            [FromQuery] bool? abaixoDoAlerta)
        {
            var unidadeExiste = await _db.Unidades.AnyAsync(u => u.Id == unidadeId);
            if (!unidadeExiste)
                return NotFound(ErroPadrao.Criar("UNIDADE_NAO_ENCONTRADA",
                    "Unidade não encontrada.", "/estoque"));

            var query = _db.EstoquesUnidade
                .Include(e => e.Produto)
                .Include(e => e.Unidade)
                .Where(e => e.UnidadeId == unidadeId);

            if (abaixoDoAlerta == true)
                query = query.Where(e => e.Quantidade <= e.AlertaMinimo);

            var estoques = await query
                .Select(e => new EstoqueResponse
                {
                    UnidadeId = e.UnidadeId,
                    NomeUnidade = e.Unidade.Nome,
                    ProdutoId = e.ProdutoId,
                    NomeProduto = e.Produto.Nome,
                    Quantidade = e.Quantidade,
                    AlertaMinimo = e.AlertaMinimo,
                    AbaixoDoAlerta = e.Quantidade <= e.AlertaMinimo
                })
                .ToListAsync();

            return Ok(estoques);
        }

        /// <summary>
        /// Movimenta o estoque de um produto em uma unidade.
        /// Quantidade positiva = entrada; negativa = saída.
        /// Restrito a Admin, Gerente e Atendente.
        /// </summary>
        [HttpPost("movimentar")]
        [ProducesResponseType(typeof(EstoqueResponse), 200)]
        [ProducesResponseType(typeof(ErroPadrao), 400)]
        [ProducesResponseType(typeof(ErroPadrao), 404)]
        [ProducesResponseType(typeof(ErroPadrao), 409)]
        public async Task<IActionResult> Movimentar([FromBody] MovimentarEstoqueRequest request)
        {
            if (!ModelState.IsValid)
            {
                var detalhes = ModelState
                    .Where(m => m.Value?.Errors.Count > 0)
                    .SelectMany(m => m.Value!.Errors.Select(e =>
                        new DetalheErro { Field = m.Key, Issue = e.ErrorMessage }))
                    .ToList();
                return BadRequest(ErroPadrao.Criar("VALIDACAO_INVALIDA",
                    "Dados inválidos.", "/estoque/movimentar", detalhes));
            }

            var unidade = await _db.Unidades.FindAsync(request.UnidadeId);
            if (unidade == null)
                return NotFound(ErroPadrao.Criar("UNIDADE_NAO_ENCONTRADA",
                    "Unidade não encontrada.", "/estoque/movimentar"));

            var produto = await _db.Produtos.FindAsync(request.ProdutoId);
            if (produto == null)
                return NotFound(ErroPadrao.Criar("PRODUTO_NAO_ENCONTRADO",
                    "Produto não encontrado.", "/estoque/movimentar"));

            var estoque = await _db.EstoquesUnidade
                .FirstOrDefaultAsync(e => e.UnidadeId == request.UnidadeId
                                       && e.ProdutoId == request.ProdutoId);

            if (estoque == null)
            {
                // Primeira movimentação desse produto nessa unidade
                if (request.Quantidade < 0)
                    return Conflict(ErroPadrao.Criar("ESTOQUE_INSUFICIENTE",
                        "Não há estoque cadastrado para este produto nesta unidade.",
                        "/estoque/movimentar"));

                estoque = new EstoqueUnidade
                {
                    UnidadeId = request.UnidadeId,
                    ProdutoId = request.ProdutoId,
                    Quantidade = request.Quantidade
                };
                _db.EstoquesUnidade.Add(estoque);
            }
            else
            {
                var novaQtd = estoque.Quantidade + request.Quantidade;
                if (novaQtd < 0)
                    return Conflict(ErroPadrao.Criar("ESTOQUE_INSUFICIENTE",
                        "Quantidade insuficiente em estoque.",
                        "/estoque/movimentar",
                        new() { new DetalheErro
                            { Field = "quantidade", Issue = $"Disponível: {estoque.Quantidade}" } }));

                estoque.Quantidade = novaQtd;
            }

            // Registra auditoria da movimentação de estoque
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _db.AuditoriaLogs.Add(new AuditoriaLog
            {
                UsuarioId = Guid.TryParse(usuarioId, out var uid) ? uid : Guid.Empty,
                Acao = "MOVIMENTACAO_ESTOQUE",
                Detalhe = $"Produto {produto.Nome} | Unidade {unidade.Nome} | Qtd: {request.Quantidade:+0;-0} | {request.Motivo}"
            });

            await _db.SaveChangesAsync();

            return Ok(new EstoqueResponse
            {
                UnidadeId = estoque.UnidadeId,
                NomeUnidade = unidade.Nome,
                ProdutoId = estoque.ProdutoId,
                NomeProduto = produto.Nome,
                Quantidade = estoque.Quantidade,
                AlertaMinimo = estoque.AlertaMinimo,
                AbaixoDoAlerta = estoque.Quantidade <= estoque.AlertaMinimo
            });
        }
    }
}
