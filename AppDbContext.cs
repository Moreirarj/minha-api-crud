using Microsoft.EntityFrameworkCore;

namespace MinhaApiCrud
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite("Data Source=minhaapi.db");
            }
        }

        // 🔥🔥🔥 ADICIONE ESTE MÉTODO PARA CONFIGURAÇÕES DO MODELO
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuração da entidade User
            modelBuilder.Entity<User>(entity =>
            {
                // 🔥 Chave primária
                entity.HasKey(e => e.Id);

                // 🔥 Configurações do campo Name
                entity.Property(e => e.Name)
                    .IsRequired() // Torna obrigatório
                    .HasMaxLength(100) // Define tamanho máximo
                    .HasColumnType("TEXT"); // Tipo específico do SQLite

                // 🔥 Configurações do campo Email
                entity.Property(e => e.Email)
                    .IsRequired() // Torna obrigatório
                    .HasMaxLength(150) // Define tamanho máximo
                    .HasColumnType("TEXT"); // Tipo específico do SQLite

                // 🔥 Índice único para Email (evita duplicatas)
                entity.HasIndex(e => e.Email)
                    .IsUnique();

                // 🔥 Configurações do campo CreatedAt
                entity.Property(e => e.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("datetime('now')"); // Valor padrão no SQLite

                // 🔥 NOVO - Campo UpdatedAt (se você adicionar na classe User)
                // entity.Property(e => e.UpdatedAt)
                //     .IsRequired(false); // Opcional se você adicionar depois
            });

            // 🔥🔥🔥 ADICIONE ESTE COMENTÁRIO PARA FUTURAS ENTIDADES
            /*
            // Exemplo para quando adicionar novas entidades:
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.Name);
            });
            */
        }

        // 🔥🔥🔥 ADICIONE ESTE MÉTODO PARA ATUALIZAR TIMESTAMPS AUTOMATICAMENTE
        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return base.SaveChangesAsync(cancellationToken);
        }

        // 🔥🔥🔥 MÉTODO PARA ATUALIZAR DATAS AUTOMATICAMENTE
        private void UpdateTimestamps()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.Entity is User && 
                           (e.State == EntityState.Added || e.State == EntityState.Modified));

            foreach (var entry in entries)
            {
                var user = (User)entry.Entity;
                
                if (entry.State == EntityState.Added)
                {
                    user.CreatedAt = DateTime.UtcNow;
                }
                
                // 🔥 NOVO - Se você adicionar UpdatedAt na classe User depois:
                // user.UpdatedAt = DateTime.UtcNow;
            }
        }

        // 🔥🔥🔥 MÉTODO UTILITÁRIO PARA HEALTH CHECKS
        public async Task<bool> CanConnectAsync()
        {
            try
            {
                return await Database.CanConnectAsync();
            }
            catch
            {
                return false;
            }
        }

        // 🔥🔥🔥 MÉTODO PARA OBTER ESTATÍSTICAS DO BANCO (OPCIONAL)
        public async Task<object> GetDatabaseStats()
        {
            var userCount = await Users.CountAsync();
            var lastUser = await Users.OrderByDescending(u => u.Id).FirstOrDefaultAsync();

            return new
            {
                TotalUsers = userCount,
                LastUserId = lastUser?.Id,
                LastUserCreatedAt = lastUser?.CreatedAt,
                DatabasePath = Database.GetDbConnection().DataSource,
                DatabaseProvider = Database.ProviderName
            };
        }
    }
}