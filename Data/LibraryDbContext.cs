using Microsoft.EntityFrameworkCore;
using LibraryAutomation.Models; // Modellerinin bulunduğu namespace
using System;

namespace LibraryAutomation.Data
{
    public class LibraryDbContext : DbContext
    {
        // Tabloların tanımlanması
        public DbSet<Book> Books { get; set; }
        public DbSet<Genre> Genres { get; set; }
        public DbSet<Author> Authors { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
              string connectionString = "server=127.0.0.1;port=3307;database=librarydb;user=root;password=;";

                optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // İlişkileri burada netleştirebilirsin (Opsiyonel ama önerilir)
            modelBuilder.Entity<Book>()
                .HasOne(b => b.Author)
                .WithMany()
                .HasForeignKey(b => b.AuthorId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Book>()
                .HasOne(b => b.Genre)
                .WithMany()
                .HasForeignKey(b => b.GenreId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}