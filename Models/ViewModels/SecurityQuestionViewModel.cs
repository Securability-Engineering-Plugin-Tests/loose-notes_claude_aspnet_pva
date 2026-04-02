using System.ComponentModel.DataAnnotations;

namespace LooseNotes.Models.ViewModels;

public class SecurityQuestionViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string Question { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Your Answer")]
    public string Answer { get; set; } = string.Empty;
}
