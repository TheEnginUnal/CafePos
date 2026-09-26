using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using CafePos.Models.Entities;
namespace CafePos.Data
{
    public class AppDbContext : DbContext
    {
        // Tablolarımızı (DbSet) tanımlıyoruz
        public DbSet<Category> Categories { get; set; }
        public DbSet<ProductType> ProductTypes { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<Table> Tables { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<TicketItem> TicketItems { get; set; }
        public DbSet<TicketItemTag> TicketItemTags { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<AppUser> AppUsers { get; set; }
        public DbSet<EndOfDaySummary> EndOfDaySummaries { get; set; }



        // Veri tabanı bağlantı ayarını burada yapıyoruz
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Eğer daha önceden ayarlanmamışsa SQLite bağlantısını kur
            if (!optionsBuilder.IsConfigured)
            {
                // Uygulamanın çalıştığı dizinde 'SuflorPos2.db' adında bir dosya oluşturacak
                optionsBuilder.UseSqlite("Data Source=SuflorPos2.db");
            }
        }

        // İsteğe bağlı: İlişkilerin (Foreign Keys) ve kısıtlamaların özel kurallarını burada belirleyebiliriz (Fluent API)
        public DbSet<TicketActionLog> TicketActionLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ... Eski kısıtlamaların (Cascade vb.) burada duruyor ...

            // TicketActionLog tablosundaki finansal alanlar için hassasiyet
            modelBuilder.Entity<TicketActionLog>()
                .Property(l => l.OriginalValue).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<TicketActionLog>()
                .Property(l => l.AdjustmentValue).HasColumnType("decimal(18,2)");

            modelBuilder.Entity<TicketItem>()
                .Property(ti => ti.FinalPrice).HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Ticket>()
                .Property(t => t.RoundingAmount).HasColumnType("decimal(18,2)");

            // Cascade silme hatalarını engellemek için ActionLog kısıtlamaları
            modelBuilder.Entity<TicketActionLog>()
                .HasOne(l => l.Ticket)
                .WithMany(t => t.ActionLogs)
                .HasForeignKey(l => l.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TicketActionLog>()
                .HasOne(l => l.AppUser)
                .WithMany()
                .HasForeignKey(l => l.AppUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.RoundingUser)
                .WithMany()
                .HasForeignKey(t => t.RoundingUserId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}