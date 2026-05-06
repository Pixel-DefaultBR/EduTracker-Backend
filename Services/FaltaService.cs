using ControleAusencia.Data;
using ControleAusencia.Models;
using Microsoft.EntityFrameworkCore;

namespace ControleAusencia.Services;

public class FaltaService
{
    private const int LimiteFaltasSemana = 7;

    private readonly AppDbContext _db;
    private readonly IEmailService _emailService;
    private readonly ILogger<FaltaService> _logger;

    public FaltaService(AppDbContext db, IEmailService emailService, ILogger<FaltaService> logger)
    {
        _db = db;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<RegistroFalta> RegistrarFaltaAsync(int alunoId, DateOnly data, int quantidade)
    {
        var aluno = await _db.Alunos.FindAsync(alunoId)
            ?? throw new KeyNotFoundException($"Aluno com Id {alunoId} não encontrado.");

        var registro = new RegistroFalta
        {
            AlunoId = alunoId,
            Data = data,
            QuantidadeFaltas = quantidade
        };

        _db.RegistrosFaltas.Add(registro);
        await _db.SaveChangesAsync();

        var totalUltimos7Dias = await CalcularFaltasUltimos7DiasAsync(alunoId, data);

        _logger.LogInformation(
            "Aluno {AlunoId} tem {Total} faltas nos últimos 7 dias (limite: {Limite}).",
            alunoId, totalUltimos7Dias, LimiteFaltasSemana);

        if (totalUltimos7Dias >= LimiteFaltasSemana)
        {
            try
            {
                await _emailService.EnviarAlertaFaltasAsync(aluno.Email, aluno.Nome, totalUltimos7Dias);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao enviar email de alerta para o aluno {AlunoId}.", alunoId);
            }
        }

        return registro;
    }

    private async Task<int> CalcularFaltasUltimos7DiasAsync(int alunoId, DateOnly dataReferencia)
    {
        var inicio = dataReferencia.AddDays(-6); // inclui o dia atual + 6 anteriores = 7 dias

        return await _db.RegistrosFaltas
            .Where(r => r.AlunoId == alunoId && r.Data >= inicio && r.Data <= dataReferencia)
            .SumAsync(r => r.QuantidadeFaltas);
    }

    public async Task<Aluno> CriarAlunoAsync(string nome, string email)
    {
        var aluno = new Aluno { Nome = nome, Email = email };
        _db.Alunos.Add(aluno);
        await _db.SaveChangesAsync();
        return aluno;
    }

    public async Task<List<Aluno>> ListarAlunosAsync()
    {
        return await _db.Alunos.ToListAsync();
    }

    public async Task DeletarAlunoAsync(int alunoId)
    {
        var aluno = await _db.Alunos.FindAsync(alunoId)
            ?? throw new KeyNotFoundException($"Aluno com Id {alunoId} não encontrado.");

        _db.Alunos.Remove(aluno);
        await _db.SaveChangesAsync();
    }

    public async Task<object> ObterResumoAlunoAsync(int alunoId)
    {
        var aluno = await _db.Alunos.FindAsync(alunoId)
            ?? throw new KeyNotFoundException($"Aluno com Id {alunoId} não encontrado.");

        var hoje = DateOnly.FromDateTime(DateTime.Today);
        var totalSemana = await CalcularFaltasUltimos7DiasAsync(alunoId, hoje);

        var registros = await _db.RegistrosFaltas
            .Where(r => r.AlunoId == alunoId)
            .OrderByDescending(r => r.Data)
            .Select(r => new { r.Data, r.QuantidadeFaltas })
            .ToListAsync();

        return new
        {
            aluno.Id,
            aluno.Nome,
            aluno.Email,
            TotalFaltasUltimos7Dias = totalSemana,
            LimiteExcedido = totalSemana >= LimiteFaltasSemana,
            Registros = registros
        };
    }
}
