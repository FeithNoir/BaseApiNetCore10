namespace BaseApi.Application.Auth;

public sealed class AuthSettings
{
    public string AppUrl { get; set; } = "http://localhost:4200";
    public int RefreshTtlDays { get; set; } = 7;
}
