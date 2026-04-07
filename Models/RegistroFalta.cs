namespace ControleAusencia.Models;

public class RegistroFalta
{
    public int Id { get; set; }
    public int AlunoId { get; set; }
    public Aluno Aluno { get; set; } = null!;
    public DateOnly Data { get; set; }
    public int QuantidadeFaltas { get; set; }
}
