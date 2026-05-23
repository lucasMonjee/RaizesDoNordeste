using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaizerNordesteWeb.API.Data;
using RaizesNordesteWeb.API.DTOs;
using RaizesNordesteWeb.API.Models;

namespace RaizesNordesteWeb.API.Controllers
{
    [ApiController]
    [Route("produtos")]
    [Produces("application/json")]
    public class ProdutosController : ControllerBase
    {
        private readonly AppDbContext _db;

        public ProdutosController(AppDbContext db) => _db = db;

        /// <summary>
        /// Lista produtos com paginação. Filtrável por unidade, categoria e disponibilidade.
        /// Endpoint público — exibido no cardápio (App/Totem/Web).
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(200)]
        public async Task<IActionResult> Listar(
            [FromQuery] Guid? unidadeId,
            [FromQuery] Guid? categoriaId,
            [FromQuery] bool? disponivel,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10)
        {
            if (page < 1) page = 1;
            if (limit < 1 || limit > 100) limit = 10;

            var query = _db.Produtos.Include(p => p.Categoria).AsQueryable();

            if (categoriaId.HasValue)
                query = query.Where(p => p.CategoriaId == categoriaId.Value);

            if (disponivel.HasValue)
                query = query.Where(p => p.Disponivel == disponivel.Value);

            // Filtro por estoque em determinada unidade
            if (unidadeId.HasValue)
            {
                var idsComEstoque = _db.EstoquesUnidade
                    .Where(e => e.UnidadeId == unidadeId.Value && e.Quantidade > 0)
                    .Select(e => e.ProdutoId);
                query = query.Where(p => idsComEstoque.Contains(p.Id));
            }

            var total = await query.CountAsync();
            var produtos = await query
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(p => new
                {
                    p.Id,
                    p.Nome,
                    p.Preco,
                    p.Disponivel,
                    p.Sazonal,
                    p.PeriodoDisponivel,
                    Categoria = p.Categoria.Nome
                })
                .ToListAsync();

            return Ok(new
            {
                total,
                page,
                limit,
                data = produtos
            });
        }

        /// <summary>Retorna um produto pelo ID.</summary>
        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        [ProducesResponseType(200)]
        [ProducesResponseType(typeof(ErroPadrao), 404)]
        public async Task<IActionResult> ObterPorId(Guid id)
        {
            var produto = await _db.Produtos
                .Include(p => p.Categoria)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (produto == null)
                return NotFound(ErroPadrao.Criar("PRODUTO_NAO_ENCONTRADO",
                    "Produto não encontrado.", $"/produtos/{id}"));

            return Ok(new
            {
                produto.Id, produto.Nome, produto.Preco,
                produto.Disponivel, produto.Sazonal, produto.PeriodoDisponivel,
                Categoria = new { produto.Categoria.Id, produto.Categoria.Nome }
            });
        }

        /// <summary>Cria um novo produto no cardápio. Restrito a Admin e Gerente.</summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Gerente")]
        [ProducesResponseType(201)]
        [ProducesResponseType(typeof(ErroPadrao), 400)]
        [ProducesResponseType(typeof(ErroPadrao), 401)]
        [ProducesResponseType(typeof(ErroPadrao), 403)]
        [ProducesResponseType(typeof(ErroPadrao), 404)]
        public async Task<IActionResult> Criar([FromBody] Produto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var categoriaExiste = await _db.Categorias.AnyAsync(c => c.Id == dto.CategoriaId);
            if (!categoriaExiste)
                return NotFound(ErroPadrao.Criar("CATEGORIA_NAO_ENCONTRADA",
                    "Categoria informada não existe.", "/produtos",
                    new() { new DetalheErro { Field = "categoriaId", Issue = "Categoria não encontrada." } }));

            var produto = new Produto
            {
                Nome = dto.Nome,
                Preco = dto.Preco,
                CategoriaId = dto.CategoriaId,
                Disponivel = dto.Disponivel,
                Sazonal = dto.Sazonal,
                PeriodoDisponivel = dto.PeriodoDisponivel
            };

            _db.Produtos.Add(produto);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(ObterPorId), new { id = produto.Id },
                new { produto.Id, produto.Nome, produto.Preco, produto.Disponivel });
        }

        /// <summary>Atualiza dados de um produto. Restrito a Admin e Gerente.</summary>
        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin,Gerente")]
        [ProducesResponseType(200)]
        [ProducesResponseType(typeof(ErroPadrao), 404)]
        public async Task<IActionResult> Atualizar(Guid id, [FromBody] Produto dto)
        {
            var produto = await _db.Produtos.FindAsync(id);
            if (produto == null)
                return NotFound(ErroPadrao.Criar("PRODUTO_NAO_ENCONTRADO",
                    "Produto não encontrado.", $"/produtos/{id}"));

            produto.Nome = dto.Nome;
            produto.Preco = dto.Preco;
            produto.Disponivel = dto.Disponivel;
            produto.Sazonal = dto.Sazonal;
            produto.PeriodoDisponivel = dto.PeriodoDisponivel;

            await _db.SaveChangesAsync();
            return Ok(new { produto.Id, produto.Nome, produto.Preco, produto.Disponivel });
        }

        /// <summary>Lista todas as categorias disponíveis.</summary>
        [HttpGet("categorias")]
        [AllowAnonymous]
        [ProducesResponseType(200)]
        public async Task<IActionResult> ListarCategorias()
        {
            var categorias = await _db.Categorias
                .Select(c => new { c.Id, c.Nome, c.Descricao })
                .ToListAsync();

            return Ok(categorias);
        }
    }
}
