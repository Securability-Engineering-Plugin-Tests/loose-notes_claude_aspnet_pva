using System.ComponentModel.DataAnnotations;

namespace LooseNotes.Models.ViewModels;

public class ProfileViewModel
{
    [Required]
    [StringLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string? CurrentPassword { get; set; }
    public string? NewPassword { get; set; }
    public string? ConfirmNewPassword { get; set; }
}
