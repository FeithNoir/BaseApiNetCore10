using System.Text.RegularExpressions;

namespace BaseApi.Domain.Users;

/// <summary>Value object: correo normalizado (minúsculas, sin espacios) y con forma válida.</summary>
public sealed partial record Email
{
    public string Value { get; }

    private Email(string value) => Value = value;

    public static Email Parse(string? raw)
    {
        var v = (raw ?? "").Trim().ToLowerInvariant();
        if (v.Length > 254 || !Shape().IsMatch(v))
            throw DomainException.Validation("email: formato inválido");
        return new Email(v);
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"^[^@\s]+@[^@\s.][^@\s]*\.[^@\s]+$")]
    private static partial Regex Shape();
}
