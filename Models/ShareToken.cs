namespace LooseNotes.Models;

public class ShareToken
{
    public int Id { get; set; }
    public int NoteId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }

    public Note? Note { get; set; }
}
