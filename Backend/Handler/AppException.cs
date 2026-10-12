namespace Backend.Handler;

public abstract class AppException(string message, int statusCode, bool logout = false)
    : System.Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public bool Logout { get; } = logout;
}

public class ResourceNotFoundException(string message) : AppException(message, StatusCodes.Status404NotFound);
public class BadRequestException(string message) : AppException(message, StatusCodes.Status400BadRequest);
public class ConflictException(string message) : AppException(message, StatusCodes.Status409Conflict);
public class ForbiddenException(string message, bool logout = false) : AppException(message, StatusCodes.Status403Forbidden, logout);
public class UnauthorizedException(string message, bool logout = false) : AppException(message, StatusCodes.Status401Unauthorized, logout);
public class InternalServerException(string message) : AppException(message, StatusCodes.Status500InternalServerError);