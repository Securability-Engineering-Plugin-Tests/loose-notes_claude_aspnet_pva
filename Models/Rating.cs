namespace LooseNotes.Models;

public class Rating
{
    public int Id { get; set; }
    public int NoteId { get; set; }
    public int UserId { get; set; }
    public int Stars { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Note? Note { get; set; }
    public User? User { get; set; }
}
