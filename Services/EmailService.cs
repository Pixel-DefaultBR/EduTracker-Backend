using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ControleAusencia.Services;

public interface IEmailService
{
    Task EnviarAlertaFaltasAsync(string destinatario, string nomeAluno, int totalFaltas);
}

public class EmailServiceResend : IEmailService
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<EmailServiceResend> _logger;

    public EmailServiceResend(IConfiguration config, IHttpClientFactory httpClientFactory, ILogger<EmailServiceResend> logger)
    {
        _config = config;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task EnviarAlertaFaltasAsync(string destinatario, string nomeAluno, int totalFaltas)
    {
        var apiKey = _config["Resend:ApiKey"] ?? throw new InvalidOperationException("Resend:ApiKey não configurado.");
        var from = _config["Resend:From"] ?? throw new InvalidOperationException("Resend:From não configurado.");

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var payload = new
        {
            from,
            to = new[] { destinatario },
            subject = "Alerta: Limite de Faltas Atingido",
            html = $"""
                <p>Prezado(a),</p>
                <p>O aluno <strong>{nomeAluno}</strong> atingiu <strong>{totalFaltas} faltas</strong> nos últimos 7 dias.</p>
                <p>Sistema de Controle de Ausência Escolar.</p>
                """
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("https://api.resend.com/emails", content);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            _logger.LogError("Falha ao enviar email via Resend. Status: {Status}. Detalhes: {Body}", response.StatusCode, body);
        }
        else
        {
            _logger.LogInformation("Email de alerta enviado via Resend para {Destinatario}.", destinatario);
        }
    }
}
