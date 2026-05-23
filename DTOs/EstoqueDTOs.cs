using System.ComponentModel.DataAnnotations;

namespace RaizesNordesteWeb.API.DTOs
{
    // REQUEST 

    public class MovimentarEstoqueRequest
    {
        [Required]
        public Guid UnidadeId { get; set; }

        [Required]
        public Guid ProdutoId { get; set; }

        /// <summary>Quantidade positiva = entrada; negativa = saída.</summary>
        [Required]
        [Range(-999, 999, ErrorMessage = "Quantidade deve ser entre -999 e 999.")]
        public int Quantidade { get; set; }

        [MaxLength(300)]
        public string? Motivo { get; set; }
    }

    //RESPONSE 

    public class EstoqueResponse
    {
        public Guid UnidadeId { get; set; }
        public string NomeUnidade { get; set; } = string.Empty;
        public Guid ProdutoId { get; set; }
        public string NomeProduto { get; set; } = string.Empty;
        public int Quantidade { get; set; }
        public int AlertaMinimo { get; set; }
        public bool AbaixoDoAlerta { get; set; }
    }
}
