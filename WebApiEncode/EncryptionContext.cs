using EncodeLibrary;
using Microsoft.EntityFrameworkCore;

namespace WebApiEncode
{
    public class EncryptionContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Text> Texts { get; set; }
        public DbSet<History> Histories { get; set; }

        public EncryptionContext()
        {
            Database.EnsureCreated();
        }

        public EncryptionContext(DbContextOptions<EncryptionContext> options) : base(options)
        {
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // Используем SQLite - база данных будет создана автоматически в папке с приложением
                optionsBuilder.UseSqlite("Data Source=WebApiDB.db");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Настройка связей для Text
            modelBuilder.Entity<Text>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Cascade);

            // Настройка связей для History
            modelBuilder.Entity<History>()
                .HasOne(h => h.User)
                .WithMany()
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Cascade);

            // Настройка ID для Text - по умолчанию автогенерация, но можно указать вручную
            modelBuilder.Entity<Text>()
                .Property(t => t.Id)
                .ValueGeneratedOnAdd();
        }
    }
}
