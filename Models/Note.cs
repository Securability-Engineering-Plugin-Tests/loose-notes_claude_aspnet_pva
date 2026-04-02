namespace LooseNotes.Models;

public class Note
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsPublic { get; set; }
    public int OwnerId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? Category { get; set; }

    public User? Owner { get; set; }
    public ICollection<Rating> Ratings { get; set; } = new List<Rating>();
    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
    public ICollection<ShareToken> ShareTokens { get; set; } = new List<ShareToken>();

    public double AverageRating => Ratings.Count > 0 ? Ratings.Average(r => r.Stars) : 0;
}
