using Fiap.FCGames.Users.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fiap.FCGames.Users.CrossCutting.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        var opts = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        if (exception is ValidationException ve)
        {
            _logger.LogWarning("Erro de validação: {Fields}", ve.Errors.Select(e => e.PropertyName));
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            var r = new { StatusCode = 400, Errors = ve.Errors.Select(e => new { Field = e.PropertyName, Message = e.ErrorMessage }) };
            return context.Response.WriteAsync(JsonSerializer.Serialize(r, opts));
        }

        if (exception is LoginException le)
        {
            _logger.LogWarning(le, "Erro de login");
            context.Response.StatusCode = le.StatusCode;
            return context.Response.WriteAsync(JsonSerializer.Serialize(new ErrorResponse { StatusCode = le.StatusCode, Message = le.Message }, opts));
        }

        if (exception is NotFoundException nfe)
        {
            _logger.LogWarning(nfe, "Recurso não encontrado");
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            return context.Response.WriteAsync(JsonSerializer.Serialize(new ErrorResponse { StatusCode = 404, Message = nfe.Message }, opts));
        }

        if (exception is BusinessException be)
        {
            _logger.LogWarning(be, "Erro de negócio");
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            return context.Response.WriteAsync(JsonSerializer.Serialize(new ErrorResponse { StatusCode = 400, Message = be.Message }, opts));
        }

        _logger.LogError(exception, "Erro não tratado: {Message}", exception.Message);
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        return context.Response.WriteAsync(JsonSerializer.Serialize(new ErrorResponse { StatusCode = 500, Message = "Ocorreu um erro interno. Tente novamente mais tarde." }, opts));
    }
}

public class ErrorResponse
{
    public int StatusCode { get; set; }
    public required string Message { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DetailedMessage { get; set; }
}
