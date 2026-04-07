using ControleAusencia.Models;
using Microsoft.EntityFrameworkCore;

namespace ControleAusencia.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Aluno> Alunos => Set<Aluno>();
    public DbSet<RegistroFalta> RegistrosFaltas => Set<RegistroFalta>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Aluno>().HasData(
            new Aluno { Id = 1, Nome = "João Silva", Email = "joao@escola.com" },
            new Aluno { Id = 2, Nome = "Maria Souza", Email = "maria@escola.com" }
        );
    }
}
