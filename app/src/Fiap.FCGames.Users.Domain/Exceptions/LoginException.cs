namespace Fiap.FCGames.Users.Domain.Exceptions;

public class LoginException : Exception
{
    public int StatusCode { get; }
    public LoginException(string message, int statusCode) : base(message)
        => StatusCode = statusCode;
}
