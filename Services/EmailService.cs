using System.Net.Mail;

namespace ControleAusencia.Services;

public interface IEmailService
{
    Task EnviarAlertaFaltasAsync(string destinatario, string nomeAluno, int totalFaltas);
}

public class EmailServiceSmtp : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailServiceSmtp> _logger;

    public EmailServiceSmtp(IConfiguration config, ILogger<EmailServiceSmtp> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task EnviarAlertaFaltasAsync(string destinatario, string nomeAluno, int totalFaltas)
    {
        var host = _config["Smtp:Host"] ?? throw new InvalidOperationException("Smtp:Host não configurado.");
        var port = int.Parse(_config["Smtp:Port"] ?? "587");
        var user = _config["Smtp:User"] ?? throw new InvalidOperationException("Smtp:User não configurado.");
        var pass = _config["Smtp:Password"] ?? throw new InvalidOperationException("Smtp:Password não configurado.");
        var from = _config["Smtp:From"] ?? user;

        using var client = new SmtpClient(host, port)
        {
            Credentials = new System.Net.NetworkCredential(user, pass),
            EnableSsl = true
        };

        var mensagem = new MailMessage(from, destinatario)
        {
            Subject = "Alerta: Limite de Faltas Atingido",
            Body = $"Prezado(a),\n\nO aluno {nomeAluno} atingiu {totalFaltas} faltas nos últimos 7 dias.\n\nSistema de Controle de Ausência Escolar.",
            IsBodyHtml = false
        };

        await client.SendMailAsync(mensagem);
        _logger.LogInformation("Email de alerta enviado para {Destinatario}.", destinatario);
    }
}
