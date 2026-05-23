using Microsoft.EntityFrameworkCore;
using RaizesNordesteWeb.API.Models;
using static RaizesNordesteWeb.API.Models.Pedido;

namespace RaizerNordesteWeb.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // DbSets — uma tabela para cada entidade
        public DbSet<Usuario> Usuarios => Set<Usuario>();
        public DbSet<Cliente> Clientes => Set<Cliente>();
        public DbSet<PontosCliente> PontosClientes => Set<PontosCliente>();
        public DbSet<Unidade> Unidades => Set<Unidade>();
        public DbSet<Categoria> Categorias => Set<Categoria>();
        public DbSet<Produto> Produtos => Set<Produto>();
        public DbSet<Pedido> Pedidos => Set<Pedido>();
        public DbSet<ItemPedido> ItensPedido => Set<ItemPedido>();
        public DbSet<Pagamento> Pagamentos => Set<Pagamento>();
        public DbSet<EstoqueUnidade> EstoquesUnidade => Set<EstoqueUnidade>();
        public DbSet<AuditoriaLog> AuditoriaLogs => Set<AuditoriaLog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- Usuario ---
            modelBuilder.Entity<Usuario>(e =>
            {
                e.HasIndex(u => u.Email).IsUnique();
                e.Property(u => u.Perfil).HasConversion<string>();
                // Vínculo opcional com Cliente (perfil = Cliente)
                e.HasOne(u => u.Cliente)
                 .WithMany()
                 .HasForeignKey(u => u.ClienteId)
                 .IsRequired(false)
                 .OnDelete(DeleteBehavior.SetNull);
            });

            // --- Cliente ---
            modelBuilder.Entity<Cliente>(e =>
            {
                e.HasIndex(c => c.Email).IsUnique();
                e.Property(c => c.ConsentimentoLGPD).HasDefaultValue(false);
            });

            // --- PontosCliente ---
            // Relação 1:1 com Cliente
            modelBuilder.Entity<PontosCliente>(e =>
            {
                e.HasOne(p => p.Cliente)
                 .WithOne(c => c.Pontos)
                 .HasForeignKey<PontosCliente>(p => p.ClienteId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // --- Pedido ---
            modelBuilder.Entity<Pedido>(e =>
            {
                e.Property(p => p.Status)
                 .HasConversion<string>(); // salva como texto no banco

                e.Property(p => p.CanalPedido)
                 .HasConversion<string>();

                e.Property(p => p.FormaPagamento)
                 .HasConversion<string>();

                // Cliente deletado não apaga pedidos (segurança auditoria)
                e.HasOne(p => p.Cliente)
                 .WithMany(c => c.Pedidos)
                 .HasForeignKey(p => p.ClienteId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(p => p.Unidade)
                 .WithMany(u => u.Pedidos)
                 .HasForeignKey(p => p.UnidadeId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // --- ItemPedido ---
            modelBuilder.Entity<ItemPedido>(e =>
            {
                e.HasOne(i => i.Pedido)
                 .WithMany(p => p.Itens)
                 .HasForeignKey(i => i.PedidoId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(i => i.Produto)
                 .WithMany(p => p.ItensPedido)
                 .HasForeignKey(i => i.ProdutoId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // --- Pagamento ---
            modelBuilder.Entity<Pagamento>(e =>
            {
                e.Property(p => p.Status)
                 .HasConversion<string>();

                e.HasOne(p => p.Pedido)
                 .WithOne(p => p.Pagamento)
                 .HasForeignKey<Pagamento>(p => p.PedidoId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // --- Produto ---
            modelBuilder.Entity<Produto>(e =>
            {
                e.HasOne(p => p.Categoria)
                 .WithMany(c => c.Produtos)
                 .HasForeignKey(p => p.CategoriaId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // --- EstoqueUnidade — chave composta ---
            modelBuilder.Entity<EstoqueUnidade>(e =>
            {
                e.HasKey(es => new { es.UnidadeId, es.ProdutoId });

                e.HasOne(es => es.Unidade)
                 .WithMany(u => u.Estoques)
                 .HasForeignKey(es => es.UnidadeId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(es => es.Produto)
                 .WithMany(p => p.Estoques)
                 .HasForeignKey(es => es.ProdutoId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // --- AuditoriaLog — somente inserção, nunca deletar ---
            modelBuilder.Entity<AuditoriaLog>(e =>
            {
                e.ToTable("AuditoriaLogs");
                e.HasIndex(a => a.DataHora);
                e.HasIndex(a => a.UsuarioId);
            });
        }
    }
}