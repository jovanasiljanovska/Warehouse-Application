using System.ComponentModel.DataAnnotations;

namespace Warehouse.Web.Models.Account
{
    public class LoginViewModel
    {
        [Required]
        public string EmailOrUserName { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }
}
