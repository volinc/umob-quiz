using Microsoft.EntityFrameworkCore;
using UmobQuiz.Api.Domain.Entities;

namespace UmobQuiz.Api.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<GameSession> GameSessions => Set<GameSession>();
    public DbSet<Bike> Bikes => Set<Bike>();
    public DbSet<Station> Stations => Set<Station>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("postgis");

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.Username).HasColumnName("username").HasMaxLength(100);
            entity.Property(x => x.PasswordHash).HasColumnName("password_hash");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(x => x.Username).IsUnique();
        });

        modelBuilder.Entity<GameSession>(entity =>
        {
            entity.ToTable("game_sessions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.UserId).HasColumnName("user_id");
            entity.Property(x => x.StartTime).HasColumnName("start_time");
            entity.Property(x => x.EndTime).HasColumnName("end_time");
            entity.Property(x => x.Score).HasColumnName("score");
            entity.Property(x => x.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasMaxLength(20);
            entity.HasOne(x => x.User).WithMany(x => x.GameSessions).HasForeignKey(x => x.UserId);
        });

        modelBuilder.Entity<Bike>(entity =>
        {
            entity.ToTable("bikes");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.Provider).HasColumnName("provider").HasMaxLength(50);
            entity.Property(x => x.ExternalId).HasColumnName("external_id").HasMaxLength(100);
            entity.Property(x => x.Location)
                .HasColumnName("location")
                .HasColumnType("geography (point, 4326)");
            entity.Property(x => x.IsActive).HasColumnName("is_active");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(x => new { x.Provider, x.ExternalId }).IsUnique();
        });

        modelBuilder.Entity<Station>(entity =>
        {
            entity.ToTable("stations");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.Provider).HasColumnName("provider").HasMaxLength(50);
            entity.Property(x => x.ExternalId).HasColumnName("external_id").HasMaxLength(100);
            entity.Property(x => x.Location)
                .HasColumnName("location")
                .HasColumnType("geography (point, 4326)");
            entity.Property(x => x.Capacity).HasColumnName("capacity");
            entity.Property(x => x.NumBikesAvailable).HasColumnName("num_bikes_available");
            entity.Property(x => x.IsActive).HasColumnName("is_active");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(x => new { x.Provider, x.ExternalId }).IsUnique();
        });
    }
}
