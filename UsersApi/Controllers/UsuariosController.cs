using Fiap.Users.Application.Commands.AtualizarUsuario;
using Fiap.Users.Application.Commands.CriarUsuario;
using Fiap.Users.Application.Commands.Login;
using Fiap.Users.Application.Queries.BuscarUsuarioPorId;
using Fiap.Users.Application.Queries.ListarUsuarios;
using Fiap.Users.Domain.Aggregates;
using Fiap.UsersApi.Domain.Exceptions;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using UsersApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsuariosController: ApiControllerBase<UsuariosController>
{
    private readonly IPublishEndpoint _publishEndpoint;
    public UsuariosController(ISender sender, ILogger<UsuariosController> logger, IPublishEndpoint publishEndpoint) : base(sender, logger)
    {
        _publishEndpoint = publishEndpoint;
    }

    [HttpPost]
   // [Authorize]
    public async Task<IActionResult> CriarAsync([FromBody] CriarUsuarioCommand command)
    {
        var result = await _sender.Send(command);
        return StatusCode(201, result);
    }

    [HttpPut]
    //[Authorize(Policy = AuthConstants.AdminPolicy)]
    public async Task<IActionResult> AtualizarAsync([FromBody] AtualizarUsuarioCommand command)
    {
        var result = await _sender.Send(command);
        return Ok(result);
    }

    [HttpGet]
    //[Authorize(Policy = AuthConstants.AdminPolicy)]
    public async Task<IActionResult> ListarTodos()
    {
        var result = await _sender.Send(new ListarUsuariosQuery());

        if (result == null)
            return NoContent();
        return Ok(result);
    }

    [HttpGet("{id}")]
    //[Authorize(Policy = AuthConstants.AdminPolicy)]
    public async Task<IActionResult> BuscarPorId(Guid id)
    {
        var result = await _sender.Send(new BuscarUsuarioPorIdQuery(id));

        if (result == null)
            return NotFound();
        return Ok(result);
    }

    [HttpPost("/login")]
    public async Task<IActionResult> LoginAsync([FromBody] LoginCommand command)
    {
        try
        {
            var result = await _sender.Send(command);
            if(result != null) 
                await _publishEndpoint.Publish(new UsuarioLogadoEvent(command.Usuario, result.Token, result.LoginExpiracao));
            _logger.LogInformation("Login realizado com sucesso para usuario {Usuario}", command.Usuario);
            return Ok(result);
        }
        catch (LoginException ex)
        {
            _logger.LogError(ex, "Erro ao realizar login para usuario {Usuario}", command.Usuario);
            return StatusCode(ex.StatusCode, ex.Message);
        }
    }
}
