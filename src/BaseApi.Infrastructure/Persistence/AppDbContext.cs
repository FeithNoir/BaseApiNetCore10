using BaseApi.Domain.Tokens;
using BaseApi.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BaseApi.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<OneTimeToken> OneTimeTokens => Set<OneTimeToken>();

    protected override void OnModelCreating(ModelBuilder b) =>
        b.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}

// Fluent API: el mapeo vive aquí y no ensucia las entidades con atributos.

internal sealed class UserConfig : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> e)
    {
        e.ToTable("users");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id");
        e.Property(x => x.Email).HasColumnName("email").HasMaxLength(254).IsRequired()
            .HasConversion(v => v.Value, v => Email.Parse(v)); // value object <-> varchar
        e.HasIndex(x => x.Email).IsUnique();
        e.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        e.Property(x => x.PasswordHash).HasColumnName("password_hash").IsRequired();
        e.Property(x => x.EmailVerifiedAt).HasColumnName("email_verified_at");
        e.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        e.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
    }
}

internal sealed class RefreshTokenConfig : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> e)
    {
        e.ToTable("refresh_tokens");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id");
        e.Property(x => x.UserId).HasColumnName("user_id");
        e.Property(x => x.TokenHash).HasColumnName("token_hash").HasColumnType("char(64)").IsRequired();
        e.HasIndex(x => x.TokenHash).IsUnique();
        e.Property(x => x.ExpiresAt).HasColumnName("expires_at");
        e.Property(x => x.RevokedAt).HasColumnName("revoked_at");
        e.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        e.HasIndex(x => x.UserId).HasDatabaseName("ix_refresh_tokens_user");
        e.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class OneTimeTokenConfig : IEntityTypeConfiguration<OneTimeToken>
{
    public void Configure(EntityTypeBuilder<OneTimeToken> e)
    {
        e.ToTable("one_time_tokens", t =>
            t.HasCheckConstraint("ck_one_time_tokens_purpose", "purpose IN ('verify_email', 'reset_password')"));
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id");
        e.Property(x => x.UserId).HasColumnName("user_id");
        e.Property(x => x.Purpose).HasColumnName("purpose").HasMaxLength(20)
            .HasConversion(
                v => v == TokenPurpose.VerifyEmail ? "verify_email" : "reset_password",
                v => v == "verify_email" ? TokenPurpose.VerifyEmail : TokenPurpose.ResetPassword);
        e.Property(x => x.TokenHash).HasColumnName("token_hash").HasColumnType("char(64)").IsRequired();
        e.HasIndex(x => x.TokenHash).IsUnique();
        e.Property(x => x.ExpiresAt).HasColumnName("expires_at");
        e.Property(x => x.UsedAt).HasColumnName("used_at");
        e.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        e.HasIndex(x => new { x.UserId, x.Purpose }).HasDatabaseName("ix_one_time_tokens_user");
        e.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
