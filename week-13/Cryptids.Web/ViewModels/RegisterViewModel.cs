using System.ComponentModel.DataAnnotations;

namespace Cryptids.Web.ViewModels;

// What the registration form posts. It is not the Member: the password
// arrives here once, gets hashed by Identity, and is never stored.
public class RegisterViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    [Display(Name = "Field name")]
    [StringLength(40, MinimumLength = 2)]
    public string FieldName { get; set; } = "";

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    [Compare(nameof(Password), ErrorMessage = "The two passwords don't match.")]
    public string ConfirmPassword { get; set; } = "";
}
