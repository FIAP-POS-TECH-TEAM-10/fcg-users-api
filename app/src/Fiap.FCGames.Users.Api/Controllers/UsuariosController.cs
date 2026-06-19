using Fiap.FCGames.Users.Application.Commands.AtualizarUsuario;
using Fiap.FCGames.Users.Application.Commands.CriarUsuario;
using Fiap.FCGames.Users.Application.Commands.Login;
using Fiap.FCGames.Users.Application.Queries.BuscarUsuarioPorId;
using Fiap.FCGames.Users.Application.Queries.ListarUsuarios;
using Fiap.FCGames.Users.Api.Controllers.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fiap.FCGames.Users.Api.Controllers;

[Route("usuarios")]
public class UsuariosController : ApiControllerBase<UsuariosController>
{
    public UsuariosController(ISender sender, ILogger<UsuariosController> logger)
        : base(sender, logger) { }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> CriarAsync([FromBody] CriarUsuarioCommand command)
    {
        var result = await _sender.Send(command);
        return StatusCode(201, result);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> LoginAsync([FromBody] LoginCommand command)
    {
        var result = await _sender.Send(command);
        _logger.LogInformation("Login realizado para {Email}", command.Email);
        return Ok(result);
    }

    [HttpGet]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> ListarTodosAsync()
    {
        var result = await _sender.Send(new ListarUsuariosQuery());
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> BuscarPorIdAsync(Guid id)
    {
        var result = await _sender.Send(new BuscarUsuarioPorIdQuery(id));
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> AtualizarAsync(Guid id, [FromBody] AtualizarUsuarioCommand command)
    {
        var result = await _sender.Send(command with { Id = id });
        return Ok(result);
    }
}
