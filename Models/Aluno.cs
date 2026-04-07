namespace ControleAusencia.Models;

public class Aluno
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public ICollection<RegistroFalta> RegistrosFaltas { get; set; } = new List<RegistroFalta>();
}
