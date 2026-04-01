namespace LooseNotes.Models;

public class Attachment
{
    public int Id { get; set; }
    public int NoteId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StoredPath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public Note? Note { get; set; }
}
