namespace LooseNotes.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Note> Notes { get; set; } = new List<Note>();
    public ICollection<Rating> Ratings { get; set; } = new List<Rating>();
}
