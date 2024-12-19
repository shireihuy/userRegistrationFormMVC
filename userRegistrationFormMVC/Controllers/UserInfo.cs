using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace userRegistrationFormMVC.Controllers
{
    public class UserInfo
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "氏名を入力してください。")]
        public string Name { get; set; } // 氏名

        [Required(ErrorMessage = "氏名(カナ)を入力してください。")]
        [RegularExpression(@"^[ァ-ヶーｦ-ﾟ]+(\s[ァ-ヶーｦ-ﾟ]+)*$", ErrorMessage = "氏名(カナ)はカタカナで入力してください。")]
        public string Kana { get; set; } // 氏名(カナ)

        [Required(ErrorMessage = "性別を選択してください。")]
        public string Gender { get; set; } // 性別

        [Required(ErrorMessage = "生年月日を入力してください。")]
        [DataType(DataType.Date)]
        public DateTime Birthdate { get; set; } // 生年月日

        [Required(ErrorMessage = "電話番号を入力してください。")]
        [RegularExpression(@"^\d{10,11}$", ErrorMessage = "電話番号は10〜11桁の数字で入力してください。ハイフン（-）は含めないでください。\r\n")]
        public string PhoneNumber { get; set; } // 電話番号

        [Required(ErrorMessage = "メールアドレスを入力してください。")]
        [EmailAddress(ErrorMessage = "正しいメールアドレスを入力してください。")]
        public string Email { get; set; } // メールアドレス

        [Required(ErrorMessage = "ログインIDを入力してください。")]
        public string LoginId { get; set; } // ログインID

        [Required(ErrorMessage = "パスワードを入力してください。")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)[a-zA-Z\d\W_]{8,}$", ErrorMessage = "パスワードは8文字以上で、大文字、小文字、数字を含む必要があります。")]
        public string Password { get; set; } // パスワード

        [NotMapped] //このプロパティはデータベースに保存されません
        [Required(ErrorMessage = "確認パスワードを入力してください。")]
        [Compare("Password", ErrorMessage = "確認パスワードとパスワードは不一致しました。")]
        public string ConfirmPassword { get; set; }

        public UserInfo()
        {

        }
    }
}