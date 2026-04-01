using LooseNotes.Models;

namespace LooseNotes.Data;

public static class DbSeeder
{
    public static void Seed(ApplicationDbContext db)
    {
        if (db.Users.Any()) return;

        var admin = new User
        {
            Username = "admin",
            Email = "admin@loosenotes.local",
            Password = "admin123",
            IsAdmin = true,
            CreatedAt = DateTime.UtcNow
        };
        var alice = new User
        {
            Username = "alice",
            Email = "alice@example.com",
            Password = "password",
            IsAdmin = false,
            CreatedAt = DateTime.UtcNow
        };
        var bob = new User
        {
            Username = "bob",
            Email = "bob@example.com",
            Password = "password",
            IsAdmin = false,
            CreatedAt = DateTime.UtcNow
        };

        db.Users.AddRange(admin, alice, bob);
        db.SaveChanges();

        var note1 = new Note
        {
            Title = "Welcome to Loose Notes",
            Content = "This is the official welcome note. Feel free to create your own notes, share them with friends, and rate others' notes!",
            IsPublic = true,
            OwnerId = alice.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var note2 = new Note
        {
            Title = "My Private Thoughts",
            Content = "This is a private note only I can see.",
            IsPublic = false,
            OwnerId = alice.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var note3 = new Note
        {
            Title = "Bob's Public Tips",
            Content = "Here are some useful productivity tips: 1. Take breaks. 2. Stay hydrated. 3. Use Loose Notes!",
            IsPublic = true,
            OwnerId = bob.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Notes.AddRange(note1, note2, note3);
        db.SaveChanges();

        db.Ratings.AddRange(
            new Rating { NoteId = note1.Id, UserId = bob.Id, Stars = 5, Comment = "Great intro!", CreatedAt = DateTime.UtcNow },
            new Rating { NoteId = note3.Id, UserId = alice.Id, Stars = 4, Comment = "Helpful tips.", CreatedAt = DateTime.UtcNow }
        );
        db.SaveChanges();
    }
}
