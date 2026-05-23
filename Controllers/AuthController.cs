using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RaizerNordesteWeb.API.Data;
using RaizesNordesteWeb.API.DTOs;
using RaizesNordesteWeb.API.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace RaizesNordesteWeb.API.Controllers
{
    [ApiController]
    [Route("auth")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;

        public AuthController(AppDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        ///Autentica um usuário e retorna o token JWT.
        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponse), 200)]
        [ProducesResponseType(typeof(ErroPadrao), 400)]
        [ProducesResponseType(typeof(ErroPadrao), 401)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
                return UnprocessableEntity(CriarErroValidacao("/auth/login"));

            var usuario = await _db.Usuarios
                .FirstOrDefaultAsync(u => u.Email == request.Email && u.Ativo);

            if (usuario == null || !BCrypt.Net.BCrypt.Verify(request.Senha, usuario.SenhaHash))
            {
                return Unauthorized(ErroPadrao.Criar(
                    "CREDENCIAIS_INVALIDAS",
                    "E-mail ou senha inválidos.",
                    "/auth/login"));
            }

            var token = GerarToken(usuario);
            var expHoras = int.Parse(_config["Jwt:ExpiracaoHoras"] ?? "8");

            return Ok(new AuthResponse
            {
                AccessToken = token,
                TokenType = "Bearer",
                ExpiresIn = expHoras * 3600,
                User = new UsuarioResponse
                {
                    Id = usuario.Id,
                    Nome = usuario.Nome,
                    Email = usuario.Email,
                    Perfil = usuario.Perfil.ToString()
                }
            });
        }

        /// Registra um novo usuário (cadastro público para clientes).
        [HttpPost("register")]
        [ProducesResponseType(typeof(UsuarioResponse), 201)]
        [ProducesResponseType(typeof(ErroPadrao), 400)]
        [ProducesResponseType(typeof(ErroPadrao), 409)]
        [ProducesResponseType(typeof(ErroPadrao), 422)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
                return UnprocessableEntity(CriarErroValidacao("/auth/register"));

            // Verifica se e-mail já está em uso
            var emailExistente = await _db.Usuarios.AnyAsync(u => u.Email == request.Email);
            if (emailExistente)
            {
                return Conflict(ErroPadrao.Criar(
                    "EMAIL_JA_CADASTRADO",
                    "Já existe uma conta com este e-mail.",
                    "/auth/register",
                    new() { new DetalheErro { Field = "email", Issue = "E-mail já utilizado." } }));
            }

            // Consentimento LGPD obrigatório para cadastro
            if (!request.ConsentimentoLGPD)
            {
                return BadRequest(ErroPadrao.Criar(
                    "CONSENTIMENTO_LGPD_NECESSARIO",
                    "É necessário aceitar os termos de uso e política de privacidade.",
                    "/auth/register",
                    new() { new DetalheErro { Field = "consentimentoLGPD", Issue = "Consentimento não fornecido." } }));
            }

            var usuario = new Usuario
            {
                Nome = request.Nome,
                Email = request.Email,
                // Senha armazenada como hash BCrypt (LGPD — dado sensível)
                SenhaHash = BCrypt.Net.BCrypt.HashPassword(request.Senha),
                Perfil = request.Perfil,
                ConsentimentoLGPD = true,
                DataConsentimento = DateTime.UtcNow
            };

            _db.Usuarios.Add(usuario);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(Login), new UsuarioResponse
            {
                Id = usuario.Id,
                Nome = usuario.Nome,
                Email = usuario.Email,
                Perfil = usuario.Perfil.ToString()
            });
        }

        //Helpers 

        private string GerarToken(Usuario usuario)
        {
            var secretKey = _config["Jwt:SecretKey"]!;
            var issuer = _config["Jwt:Issuer"];
            var audience = _config["Jwt:Audience"];
            var expHoras = int.Parse(_config["Jwt:ExpiracaoHoras"] ?? "8");

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Email, usuario.Email),
                new Claim(ClaimTypes.Name, usuario.Nome),
                new Claim(ClaimTypes.Role, usuario.Perfil.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(expHoras),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private ErroPadrao CriarErroValidacao(string path)
        {
            var detalhes = ModelState.Where(m => m.Value?.Errors.Count > 0).SelectMany(m => m.Value!.Errors.Select(e =>new DetalheErro { Field = m.Key, Issue = e.ErrorMessage })).ToList();

            return ErroPadrao.Criar("VALIDACAO_INVALIDA",
                "Um ou mais campos estão incorretos.", path, detalhes);
        }
    }
}
