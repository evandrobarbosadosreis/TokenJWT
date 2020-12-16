using Microsoft.EntityFrameworkCore;
using webapi.Models;
using webapi.Services;

namespace webapi.Data
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        { }

        public DbSet<Usuario> Usuarios { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Usuario>().HasData(
                new Usuario
                {
                    Id     = 1,
                    Nome   = "Evandro",
                    Email  = "evandro@gmail.com",
                    Senha  = HashService.GerarHash("abc123"),
                    DataExpiracao = null,
                    RefreshToken  = null
                },
                new Usuario
                {
                    Id     = 2,
                    Nome   = "José",
                    Email  = "jose@gmail.com",
                    Senha  = HashService.GerarHash("abc456"),
                    DataExpiracao = null,
                    RefreshToken  = null
                },
                new Usuario
                {
                    Id     = 3,
                    Nome   = "Carlos",
                    Email  = "carlos@gmail.com",
                    Senha  = HashService.GerarHash("abc789"),
                    DataExpiracao = null,
                    RefreshToken  = null
                }
            );
            base.OnModelCreating(modelBuilder);
        }
    }
}