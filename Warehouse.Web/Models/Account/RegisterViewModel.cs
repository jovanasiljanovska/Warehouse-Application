using System.ComponentModel.DataAnnotations;

namespace Warehouse.Web.Models.Account
{
    public class RegisterViewModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string UserName { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        public string? FirstName { get; set; }
        public string? LastName { get; set; }

        // for suppliers
        public string? CompanyName { get; set; }

        [Required]
        public string Role { get; set; } = "Customer"; // default
    }
}
