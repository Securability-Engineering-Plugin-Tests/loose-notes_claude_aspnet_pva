using System.ComponentModel.DataAnnotations;

namespace LooseNotes.Models.ViewModels;

public class NoteViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    public bool IsPublic { get; set; }

    public IFormFile? Attachment { get; set; }
}
