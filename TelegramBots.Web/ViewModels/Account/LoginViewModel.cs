using System.ComponentModel.DataAnnotations;

namespace TelegramBots.Web.ViewModels.Account
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "وارد کردن {0} الزامی است")]
        [Display(Name = "ایمیل")]
        [EmailAddress(ErrorMessage = "لطفا یک ایمیل معتبر وارد کنید")]
        [MaxLength(50, ErrorMessage = "{0} باید حداکثر {1} کاراکتر باشد")]
        public string UserEmail { get; set; }

        [Required(ErrorMessage = "وارد کردن {0} الزامی است")]
        [Display(Name = "رمزعبور")]
        [DataType(DataType.Password)]
        [MaxLength(30, ErrorMessage = "{0} باید حداکثر {1} کاراکتر باشد")]
        public string Password { get; set; }

        [Display(Name = "مرا به خاطر بسپار")]
        public bool RememberMe { get; set; }
    }
}