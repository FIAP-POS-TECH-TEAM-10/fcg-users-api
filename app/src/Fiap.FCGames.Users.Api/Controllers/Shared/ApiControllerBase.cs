using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Fiap.FCGames.Users.Api.Controllers.Shared;

[ApiController]
public abstract class ApiControllerBase<T> : ControllerBase where T : class
{
    protected readonly ISender _sender;
    protected readonly ILogger<T> _logger;

    protected ApiControllerBase(ISender sender, ILogger<T> logger)
    {
        _sender = sender;
        _logger = logger;
    }
}
