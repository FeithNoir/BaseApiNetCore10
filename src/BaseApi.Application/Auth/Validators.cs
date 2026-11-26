using FluentValidation;

namespace BaseApi.Application.Auth;

// FluentValidation: reglas de forma (campos vacíos, largos). Las reglas de negocio viven en el dominio.

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage("es obligatorio")
            .EmailAddress().WithMessage("formato inválido").MaximumLength(254);
        RuleFor(x => x.Password).NotEmpty().WithMessage("es obligatorio")
            .Length(8, 72).WithMessage("debe tener entre 8 y 72 caracteres");
        RuleFor(x => x.Name).NotEmpty().WithMessage("es obligatorio")
            .Length(2, 100).WithMessage("debe tener entre 2 y 100 caracteres");
    }
}

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage("es obligatorio").EmailAddress().WithMessage("formato inválido");
        RuleFor(x => x.Password).NotEmpty().WithMessage("es obligatorio");
    }
}

public sealed class RefreshRequestValidator : AbstractValidator<RefreshRequest>
{
    public RefreshRequestValidator() => RuleFor(x => x.RefreshToken).NotEmpty().WithMessage("es obligatorio");
}

public sealed class TokenRequestValidator : AbstractValidator<TokenRequest>
{
    public TokenRequestValidator() => RuleFor(x => x.Token).NotEmpty().WithMessage("es obligatorio");
}

public sealed class EmailRequestValidator : AbstractValidator<EmailRequest>
{
    public EmailRequestValidator() =>
        RuleFor(x => x.Email).NotEmpty().WithMessage("es obligatorio").EmailAddress().WithMessage("formato inválido");
}

public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().WithMessage("es obligatorio");
        RuleFor(x => x.NewPassword).NotEmpty().WithMessage("es obligatorio")
            .Length(8, 72).WithMessage("debe tener entre 8 y 72 caracteres");
    }
}

public sealed class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.Token).NotEmpty().WithMessage("es obligatorio");
        RuleFor(x => x.NewPassword).NotEmpty().WithMessage("es obligatorio")
            .Length(8, 72).WithMessage("debe tener entre 8 y 72 caracteres");
    }
}
