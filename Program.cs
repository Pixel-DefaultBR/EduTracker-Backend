using ControleAusencia.Data;
using ControleAusencia.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Banco de dados In-Memory (troque por SQLite com UseSqlite se preferir)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("ControleAusenciaDb"));

// Serviços
builder.Services.AddScoped<FaltaService>();
builder.Services.AddScoped<IEmailService, EmailServiceMock>(); // troque por EmailServiceSmtp para produção

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS para o frontend React (localhost:5173 é o padrão do Vite)
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

// Seed do banco In-Memory
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("FrontendPolicy");
app.UseAuthorization();
app.MapControllers();

app.Run();
