namespace BaseApi.Domain;

public enum DomainErrorKind { Validation, Conflict, Unauthorized, Forbidden, InvalidToken }

/// <summary>Error de negocio. El middleware de la API lo traduce a Problem Details.</summary>
public sealed class DomainException(DomainErrorKind kind, string message) : Exception(message)
{
    public DomainErrorKind Kind { get; } = kind;

    public static DomainException Validation(string msg) => new(DomainErrorKind.Validation, msg);
    public static DomainException EmailTaken() => new(DomainErrorKind.Conflict, "El correo ya está registrado");
    public static DomainException InvalidCredentials() => new(DomainErrorKind.Unauthorized, "Credenciales inválidas");
    public static DomainException Unauthorized() => new(DomainErrorKind.Unauthorized, "No autenticado");
    public static DomainException EmailNotVerified() => new(DomainErrorKind.Forbidden, "Debes verificar tu correo antes de iniciar sesión");
    public static DomainException InvalidToken() => new(DomainErrorKind.InvalidToken, "El token es inválido o expiró");
}
