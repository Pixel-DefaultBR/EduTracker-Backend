namespace ControleAusencia.Services;

public interface IEmailService
{
    Task EnviarAlertaFaltasAsync(string destinatario, string nomeAluno, int totalFaltas);
}

/// <summary>
/// Implementação de mock: imprime o e-mail no console.
/// Para produção, substitua pelo SmtpClient ou um provider como SendGrid.
/// </summary>
public class EmailServiceMock : IEmailService
{
    private readonly ILogger<EmailServiceMock> _logger;

    public EmailServiceMock(ILogger<EmailServiceMock> logger)
    {
        _logger = logger;
    }

    public Task EnviarAlertaFaltasAsync(string destinatario, string nomeAluno, int totalFaltas)
    {
        _logger.LogWarning(
            "[E-MAIL SIMULADO] Para: {Destinatario} | Assunto: Alerta de Faltas | " +
            "Mensagem: O aluno {NomeAluno} atingiu {TotalFaltas} faltas nos últimos 7 dias.",
            destinatario, nomeAluno, totalFaltas);

        Console.WriteLine("======================================");
        Console.WriteLine($"  [E-MAIL SIMULADO]");
        Console.WriteLine($"  Para: {destinatario}");
        Console.WriteLine($"  Assunto: Alerta de Faltas Excessivas");
        Console.WriteLine($"  Mensagem: O aluno {nomeAluno} atingiu {totalFaltas} faltas nos últimos 7 dias.");
        Console.WriteLine("======================================");

        return Task.CompletedTask;
    }
}

/// <summary>
/// Implementação real via SMTP. Configure as variáveis de ambiente antes de usar:
/// SMTP_HOST, SMTP_PORT, SMTP_USER, SMTP_PASSWORD, SMTP_FROM
/// </summary>
public class EmailServiceSmtp : IEmailService
{
    private readonly IConfiguration _config;

    public EmailServiceSmtp(IConfiguration config)
    {
        _config = config;
    }

    public async Task EnviarAlertaFaltasAsync(string destinatario, string nomeAluno, int totalFaltas)
    {
        var host = _config["Smtp:Host"] ?? throw new InvalidOperationException("Smtp:Host não configurado.");
        var port = int.Parse(_config["Smtp:Port"] ?? "587");
        var user = _config["Smtp:User"] ?? string.Empty;
        var pass = _config["Smtp:Password"] ?? string.Empty;
        var from = _config["Smtp:From"] ?? user;

        using var client = new System.Net.Mail.SmtpClient(host, port)
        {
            Credentials = new System.Net.NetworkCredential(user, pass),
            EnableSsl = true
        };

        var mensagem = new System.Net.Mail.MailMessage(from, destinatario)
        {
            Subject = "Alerta: Limite de Faltas Atingido",
            Body = $"Prezado(a),\n\nO aluno {nomeAluno} atingiu {totalFaltas} faltas nos últimos 7 dias.\n\nSistema de Controle de Ausência Escolar."
        };

        await client.SendMailAsync(mensagem);
    }
}
