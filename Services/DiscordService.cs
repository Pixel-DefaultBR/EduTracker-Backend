using ControleAusencia.Data;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace ControleAusencia.Services;

public class DiscordService
{
    private readonly AppDbContext _db;
    private readonly ILogger<DiscordService> _logger;
    private static readonly HttpClient _http = new();

    public DiscordService(AppDbContext db, ILogger<DiscordService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task EnviarAlertaAsync(string nomeAluno, int totalFaltas)
    {
        var config = await _db.Configuracoes.FirstOrDefaultAsync();

        if (config is null || string.IsNullOrWhiteSpace(config.DiscordWebhookUrl))
        {
            _logger.LogWarning("Webhook do Discord não configurado. Alerta não enviado.");
            return;
        }

        var payload = new
        {
            content = $"⚠️ **Alerta de Faltas** | O aluno **{nomeAluno}** atingiu **{totalFaltas} faltas** nos últimos 7 dias."
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _http.PostAsync(config.DiscordWebhookUrl, content);

        if (!response.IsSuccessStatusCode)
            _logger.LogError("Falha ao enviar mensagem para o Discord. Status: {Status}", response.StatusCode);
        else
            _logger.LogInformation("Alerta enviado ao Discord para o aluno {Aluno}.", nomeAluno);
    }
}
