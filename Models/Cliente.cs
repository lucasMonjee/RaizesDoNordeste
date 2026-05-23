using System.ComponentModel.DataAnnotations;

namespace RaizesNordesteWeb.API.Models
{
    public class Cliente
    {
        [Key]
        public Guid id { get; set; } = Guid.NewGuid();

        [Required, MaxLength(150)]
        public string name { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        //Aqui estou armazenando o cpf já com hash nunca o texto puro
        [MaxLength(64)]
        public string? HashCpf { get; set; }
        public bool ConsentimentoLGPD { get; set; } = false;
        public DateTime CraidoEm { get; set; } = DateTime.UtcNow;

        //Aqui eu tenho os pontos dos clientes e um ICollection de pedidos 
        public PontosCliente? Pontos {  get; set; }
        public ICollection<Pedido> Pedidos { get; set; } = new List<Pedido>();

    }
}
