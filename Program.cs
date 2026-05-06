using ControleAusencia.Data;
using ControleAusencia.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Railway injeta DATABASE_URL no formato postgres://user:pass@host:port/db
// Convertemos para o formato aceito pelo Npgsql
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
string connectionString;

if (!string.IsNullOrEmpty(databaseUrl))
{
    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':');
    connectionString = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";
}
else
{
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
}

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

// Serviços
builder.Services.AddHttpClient();
builder.Services.AddScoped<FaltaService>();
builder.Services.AddScoped<DiscordService>();
builder.Services.AddScoped<IEmailService, EmailServiceResend>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS — aceita qualquer origem (Railway gera URLs dinâmicas)
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// Railway define a porta via variável PORT
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var app = builder.Build();

// Aplica migrations automaticamente com retry (aguarda o Postgres subir)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

    var retries = 10;
    while (retries > 0)
    {
        try
        {
            db.Database.Migrate();
            logger.LogInformation("Migrations aplicadas com sucesso.");
            break;
        }
        catch (Exception ex)
        {
            retries--;
            logger.LogWarning("Banco não disponível. Tentativas restantes: {Retries}. Erro: {Msg}", retries, ex.Message);
            if (retries == 0) throw;
            Thread.Sleep(3000);
        }
    }
}

app.UseCors("FrontendPolicy");

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthorization();
app.MapControllers();

app.Run();
