using LooseNotes.Models;
using Microsoft.EntityFrameworkCore;

namespace LooseNotes.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Note> Notes => Set<Note>();
    public DbSet<Rating> Ratings => Set<Rating>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<ShareToken> ShareTokens => Set<ShareToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Note>()
            .HasOne(n => n.Owner)
            .WithMany(u => u.Notes)
            .HasForeignKey(n => n.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Rating>()
            .HasOne(r => r.Note)
            .WithMany(n => n.Ratings)
            .HasForeignKey(r => r.NoteId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Rating>()
            .HasOne(r => r.User)
            .WithMany(u => u.Ratings)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Attachment>()
            .HasOne(a => a.Note)
            .WithMany(n => n.Attachments)
            .HasForeignKey(a => a.NoteId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ShareToken>()
            .HasOne(s => s.Note)
            .WithMany(n => n.ShareTokens)
            .HasForeignKey(s => s.NoteId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
