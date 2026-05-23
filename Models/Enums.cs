namespace RaizesNordesteWeb.API.Models
{
    public class Enums
    {
        public enum StatusPedido
        {
            Aguardando,
            EmPreparo,
            Pronto,
            Entregue,
            Cancelado
        }

        // Valores exigidos pelo roteiro: APP, TOTEM, BALCAO, PICKUP, WEB
        public enum CanalAtendimento
        {
            App,
            Totem,
            Balcao,
            PickUp,
            Web       
        }

        public enum StatusPagamento
        {
            Pendente,
            Aprovado,
            Recusado,
            Estornado
        }

        // Aqui são os Mocks para o pagamento
        public enum FormaPagamento
        {
            Mock,
            Pix,
            Cartao,
            Dinheiro
        }

        // Perfis de acesso — usados para autorização via roles no JWT
        public enum PerfilUsuario
        {
            Admin,
            Gerente,
            Atendente,
            Cozinha,
            Cliente
        }
    }
}
