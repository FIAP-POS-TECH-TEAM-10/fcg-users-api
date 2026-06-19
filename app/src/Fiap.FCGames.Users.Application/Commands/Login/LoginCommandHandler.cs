using Fiap.FCGames.Users.Domain.Exceptions;
using Fiap.FCGames.Users.Domain.Interfaces;
using Fiap.FCGames.Users.Domain.Services;
using MediatR;

namespace Fiap.FCGames.Users.Application.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, UsuarioLogadoDto>
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IPasswordHasherService _passwordHasher;
    private readonly ITokenService _tokenService;

    public LoginCommandHandler(
        IUsuarioRepository usuarioRepository,
        IPasswordHasherService passwordHasher,
        ITokenService tokenService)
    {
        _usuarioRepository = usuarioRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<UsuarioLogadoDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var usuario = await _usuarioRepository.ObterPorEmailAsync(request.Email);

        if (usuario is null || !_passwordHasher.Verificar(request.Senha, usuario.SenhaHash))
            throw new LoginException("E-mail ou senha inválidos.", 401);

        var expiracao = DateTime.UtcNow.AddMinutes(30);
        var token = _tokenService.GerarToken(usuario.Id.Value, usuario.Email, usuario.TipoAcesso, expiracao);

        return new UsuarioLogadoDto
        {
            Email = usuario.Email,
            Token = token,
            Expiracao = expiracao
        };
    }
}
