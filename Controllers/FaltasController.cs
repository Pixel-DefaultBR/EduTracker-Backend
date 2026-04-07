using ControleAusencia.Services;
using Microsoft.AspNetCore.Mvc;

namespace ControleAusencia.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FaltasController : ControllerBase
{
    private readonly FaltaService _faltaService;

    public FaltasController(FaltaService faltaService)
    {
        _faltaService = faltaService;
    }

    // GET api/faltas/alunos
    [HttpGet("alunos")]
    public async Task<IActionResult> ListarAlunos()
    {
        var alunos = await _faltaService.ListarAlunosAsync();
        return Ok(alunos);
    }

    // POST api/faltas/alunos
    // Body: { "nome": "João", "email": "joao@escola.com" }
    [HttpPost("alunos")]
    public async Task<IActionResult> CriarAluno([FromBody] CriarAlunoRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nome) || string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { mensagem = "Nome e e-mail são obrigatórios." });

        var aluno = await _faltaService.CriarAlunoAsync(request.Nome, request.Email);
        return CreatedAtAction(nameof(ObterResumo), new { id = aluno.Id }, aluno);
    }

    // GET api/faltas/alunos/{id}/resumo
    [HttpGet("alunos/{id}/resumo")]
    public async Task<IActionResult> ObterResumo(int id)
    {
        try
        {
            var resumo = await _faltaService.ObterResumoAlunoAsync(id);
            return Ok(resumo);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensagem = ex.Message });
        }
    }

    // POST api/faltas
    // Body: { "alunoId": 1, "data": "2026-04-07", "quantidadeFaltas": 3 }
    [HttpPost]
    public async Task<IActionResult> RegistrarFalta([FromBody] RegistrarFaltaRequest request)
    {
        if (request.QuantidadeFaltas <= 0)
            return BadRequest(new { mensagem = "A quantidade de faltas deve ser maior que zero." });

        try
        {
            var data = DateOnly.Parse(request.Data);
            var registro = await _faltaService.RegistrarFaltaAsync(request.AlunoId, data, request.QuantidadeFaltas);
            return CreatedAtAction(nameof(ObterResumo), new { id = request.AlunoId }, registro);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensagem = ex.Message });
        }
        catch (FormatException)
        {
            return BadRequest(new { mensagem = "Formato de data inválido. Use YYYY-MM-DD." });
        }
    }
}

public record RegistrarFaltaRequest(int AlunoId, string Data, int QuantidadeFaltas);
public record CriarAlunoRequest(string Nome, string Email);
