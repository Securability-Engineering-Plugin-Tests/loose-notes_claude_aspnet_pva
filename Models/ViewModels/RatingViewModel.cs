using System.ComponentModel.DataAnnotations;

namespace LooseNotes.Models.ViewModels;

public class RatingViewModel
{
    [Required]
    public int NoteId { get; set; }

    [Required]
    [Range(1, 5)]
    public int Stars { get; set; }

    [StringLength(1000)]
    public string Comment { get; set; } = string.Empty;
}
