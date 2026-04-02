using System.ComponentModel.DataAnnotations;

namespace LooseNotes.Models.ViewModels;

public class RegisterViewModel
{
    [Required]
    [StringLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    [Required]
    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Security Question")]
    public string SecurityQuestion { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Security Answer")]
    public string SecurityAnswer { get; set; } = string.Empty;
}
