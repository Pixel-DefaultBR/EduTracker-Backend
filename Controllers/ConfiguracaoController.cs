using ControleAusencia.Data;
using ControleAusencia.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ControleAusencia.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConfiguracaoController : ControllerBase
{
    private readonly AppDbContext _db;

    public ConfiguracaoController(AppDbContext db)
    {
        _db = db;
    }

    // GET api/configuracao
    [HttpGet]
    public async Task<IActionResult> Obter()
    {
        var config = await _db.Configuracoes.FirstOrDefaultAsync();
        return Ok(new { discordWebhookUrl = config?.DiscordWebhookUrl ?? "" });
    }

    // PUT api/configuracao/webhook
    // Body: { "url": "https://discord.com/api/webhooks/..." }
    [HttpPut("webhook")]
    public async Task<IActionResult> AtualizarWebhook([FromBody] WebhookRequest request)
    {
        var config = await _db.Configuracoes.FirstOrDefaultAsync();

        if (config is null)
        {
            config = new Configuracao { DiscordWebhookUrl = request.Url };
            _db.Configuracoes.Add(config);
        }
        else
        {
            config.DiscordWebhookUrl = request.Url;
        }

        await _db.SaveChangesAsync();
        return Ok(new { mensagem = "Webhook atualizado com sucesso." });
    }
}

public record WebhookRequest(string Url);
