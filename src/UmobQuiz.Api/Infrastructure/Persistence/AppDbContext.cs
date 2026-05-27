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
            entity.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(x => x.Username).HasColumnName("username").HasMaxLength(100);
            entity.Property(x => x.PasswordHash).HasColumnName("password_hash");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            entity.HasIndex(x => x.Username).IsUnique();
        });

        modelBuilder.Entity<GameSession>(entity =>
        {
            entity.ToTable("game_sessions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(x => x.UserId).HasColumnName("user_id");
            entity.Property(x => x.StartTime).HasColumnName("start_time");
            entity.Property(x => x.EndTime).HasColumnName("end_time");
            entity.Property(x => x.Score).HasColumnName("score").HasDefaultValue(0);
            entity.Property(x => x.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(GameSessionStatus.Active);
            entity.HasIndex(x => x.UserId).HasDatabaseName("ix_game_sessions_user_id");
            entity.HasOne(x => x.User)
                .WithMany(x => x.GameSessions)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Bike>(entity =>
        {
            entity.ToTable("bikes");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(x => x.Provider).HasColumnName("provider").HasMaxLength(50);
            entity.Property(x => x.ExternalId).HasColumnName("external_id").HasMaxLength(100);
            entity.Property(x => x.Location)
                .HasColumnName("location")
                .HasColumnType("geography (point, 4326)");
            entity.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");
            entity.HasIndex(x => x.Location).HasMethod("gist").HasDatabaseName("ix_bikes_location");
            entity.HasIndex(x => new { x.Provider, x.IsActive }).HasDatabaseName("ix_bikes_provider_active");
            entity.HasIndex(x => new { x.Provider, x.ExternalId })
                .IsUnique()
                .HasDatabaseName("uq_bikes_provider_external");
        });

        modelBuilder.Entity<Station>(entity =>
        {
            entity.ToTable("stations");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(x => x.Provider).HasColumnName("provider").HasMaxLength(50);
            entity.Property(x => x.ExternalId).HasColumnName("external_id").HasMaxLength(100);
            entity.Property(x => x.Location)
                .HasColumnName("location")
                .HasColumnType("geography (point, 4326)");
            entity.Property(x => x.Capacity).HasColumnName("capacity");
            entity.Property(x => x.NumBikesAvailable).HasColumnName("num_bikes_available");
            entity.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");
            entity.HasIndex(x => x.Location).HasMethod("gist").HasDatabaseName("ix_stations_location");
            entity.HasIndex(x => new { x.Provider, x.IsActive }).HasDatabaseName("ix_stations_provider_active");
            entity.HasIndex(x => new { x.Provider, x.ExternalId })
                .IsUnique()
                .HasDatabaseName("uq_stations_provider_external");
        });
    }
}
