namespace BaseApi.Application.Auth;

// DTOs del contrato. Los records ya traen constructor, igualdad y ToString.
public sealed record RegisterRequest(string Email, string Password, string Name);
public sealed record LoginRequest(string Email, string Password);
public sealed record RefreshRequest(string RefreshToken);
public sealed record TokenRequest(string Token);
public sealed record EmailRequest(string Email);
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public sealed record ResetPasswordRequest(string Token, string NewPassword);

public sealed record TokenPairResponse(string AccessToken, string RefreshToken, string TokenType, int ExpiresIn);
public sealed record UserResponse(Guid Id, string Email, string Name, bool EmailVerified, DateTimeOffset CreatedAt);
