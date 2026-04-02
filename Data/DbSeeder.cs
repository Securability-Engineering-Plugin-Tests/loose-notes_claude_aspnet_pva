using LooseNotes.Models;
using Microsoft.Extensions.Configuration;

namespace LooseNotes.Data;

public static class DbSeeder
{
    public static void Seed(ApplicationDbContext db, IConfiguration? configuration = null)
    {
        if (db.Users.Any()) return;

        User admin, alice, bob;

        if (configuration != null)
        {
            var seedUsers = configuration.GetSection("SeedUsers").Get<List<SeedUserConfig>>();
            if (seedUsers != null && seedUsers.Count >= 3)
            {
                admin = new User
                {
                    Username = seedUsers[0].Username,
                    Email = seedUsers[0].Email,
                    Password = seedUsers[0].Password,
                    IsAdmin = seedUsers[0].IsAdmin,
                    CreatedAt = DateTime.UtcNow,
                    SecurityQuestion = seedUsers[0].SecurityQuestion,
                    SecurityAnswer = seedUsers[0].SecurityAnswer
                };
                alice = new User
                {
                    Username = seedUsers[1].Username,
                    Email = seedUsers[1].Email,
                    Password = seedUsers[1].Password,
                    IsAdmin = seedUsers[1].IsAdmin,
                    CreatedAt = DateTime.UtcNow,
                    SecurityQuestion = seedUsers[1].SecurityQuestion,
                    SecurityAnswer = seedUsers[1].SecurityAnswer
                };
                bob = new User
                {
                    Username = seedUsers[2].Username,
                    Email = seedUsers[2].Email,
                    Password = seedUsers[2].Password,
                    IsAdmin = seedUsers[2].IsAdmin,
                    CreatedAt = DateTime.UtcNow,
                    SecurityQuestion = seedUsers[2].SecurityQuestion,
                    SecurityAnswer = seedUsers[2].SecurityAnswer
                };
            }
            else
            {
                (admin, alice, bob) = CreateDefaultUsers();
            }
        }
        else
        {
            (admin, alice, bob) = CreateDefaultUsers();
        }

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
            new Rating { NoteId = note1.Id, UserId = bob.Id, UserEmail = bob.Email, Stars = 5, Comment = "Great intro!", CreatedAt = DateTime.UtcNow },
            new Rating { NoteId = note3.Id, UserId = alice.Id, UserEmail = alice.Email, Stars = 4, Comment = "Helpful tips.", CreatedAt = DateTime.UtcNow }
        );
        db.SaveChanges();
    }

    private static (User admin, User alice, User bob) CreateDefaultUsers()
    {
        var admin = new User
        {
            Username = "admin",
            Email = "admin@loosenotes.local",
            Password = "YWRtaW4xMjM=",
            IsAdmin = true,
            CreatedAt = DateTime.UtcNow,
            SecurityQuestion = "What is the name of your first pet?",
            SecurityAnswer = "fluffy"
        };
        var alice = new User
        {
            Username = "alice",
            Email = "alice@example.com",
            Password = "cGFzc3dvcmQ=",
            IsAdmin = false,
            CreatedAt = DateTime.UtcNow,
            SecurityQuestion = "What city were you born in?",
            SecurityAnswer = "springfield"
        };
        var bob = new User
        {
            Username = "bob",
            Email = "bob@example.com",
            Password = "cGFzc3dvcmQ=",
            IsAdmin = false,
            CreatedAt = DateTime.UtcNow,
            SecurityQuestion = "What was the name of your elementary school?",
            SecurityAnswer = "lakeside"
        };
        return (admin, alice, bob);
    }

    private class SeedUserConfig
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
        public string SecurityQuestion { get; set; } = string.Empty;
        public string SecurityAnswer { get; set; } = string.Empty;
    }
}
