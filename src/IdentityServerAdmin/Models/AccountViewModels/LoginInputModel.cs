using System.ComponentModel.DataAnnotations;

namespace IdentityServerAdmin.Models.AccountViewModels
{
    public class LoginInputModel
    {
        [Required]
        public string Username { get; set; }
        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
        public bool RememberLogin { get; set; }

        [Display(Name = "Remember me?")]
        public string ReturnUrl { get; set; }
    }
}
