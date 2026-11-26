using System.Net;
using System.Reflection;
using BaseApi.Domain.Ports;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace BaseApi.Infrastructure.Mail;

public sealed class SmtpOptions
{
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 1025;
    public string From { get; set; } = "Base API <no-reply@baseapi.local>";
}

/// <summary>Envía por SMTP con MailKit. En local apunta a Mailpit (sin TLS ni usuario).</summary>
internal sealed class MailKitEmailSender(IOptions<SmtpOptions> options) : IEmailSender
{
    private readonly SmtpOptions _o = options.Value;

    public async Task SendAsync(EmailMessage m, CancellationToken ct)
    {
        var msg = new MimeMessage();
        msg.From.Add(MailboxAddress.Parse(_o.From));
        msg.To.Add(MailboxAddress.Parse(m.To));
        msg.Subject = m.Subject;
        msg.Body = new BodyBuilder { HtmlBody = m.Html, TextBody = m.Text }.ToMessageBody();

        using var client = new SmtpClient();
        // Auto: usa STARTTLS si el servidor lo ofrece. En producción exige TLS (SslOnConnect o StartTls).
        await client.ConnectAsync(_o.Host, _o.Port, SecureSocketOptions.Auto, ct);
        await client.SendAsync(new SendRequest(msg), ct); // MailKit 5: el envío recibe un SendRequest
        await client.DisconnectAsync(true, ct);
    }
}

/// <summary>
/// Plantillas como recursos incrustados (Mail/Templates/*.html y *.txt) con marcas {{clave}}.
/// En el HTML cada valor se escapa con WebUtility.HtmlEncode.
/// </summary>
internal sealed class EmbeddedEmailRenderer : IEmailRenderer
{
    private static readonly Assembly Asm = typeof(EmbeddedEmailRenderer).Assembly;

    public EmailMessage Render(string template, string to, string subject, IReadOnlyDictionary<string, string> data)
    {
        var html = Fill(Load($"{template}.html"), data, WebUtility.HtmlEncode);
        var text = Fill(Load($"{template}.txt"), data, v => v).Trim();
        return new EmailMessage(to, subject, html, text);
    }

    private static string Load(string file)
    {
        var name = $"BaseApi.Infrastructure.Mail.Templates.{file}";
        using var stream = Asm.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"No encuentro la plantilla {name}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static string Fill(string tpl, IReadOnlyDictionary<string, string> data, Func<string, string> encode) =>
        data.Aggregate(tpl, (acc, kv) => acc.Replace("{{" + kv.Key + "}}", encode(kv.Value)));
}
