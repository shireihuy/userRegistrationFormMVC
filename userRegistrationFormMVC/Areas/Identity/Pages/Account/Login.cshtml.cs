// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace userRegistrationFormMVC.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ILogger<LoginModel> _logger;
        private readonly UserManager<IdentityUser> _userManager;

        public LoginModel(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager, ILogger<LoginModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "ログインIDを入力してください。")]
            [Display(Name = "Login ID")]
            public string LoginId { get; set; }

            [Required(ErrorMessage = "パスワードを入力してください。")]
            [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_])[a-zA-Z\d\W_]{8,}$", ErrorMessage = "パスワードは8文字以上で、大文字、小文字、数字、特殊文字を含む必要があります。")]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Display(Name = "ログイン情報保存")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            //既存の外部Cookieをクリアして、ログインプロセスをクリーンにしてください
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            if (ModelState.IsValid)
            {
                _logger.LogInformation("ログイン ID: {LoginId} でログインを試行しました", Input.LoginId);

                var user = await _userManager.FindByNameAsync(Input.LoginId);
                if (user == null)
                {
                    _logger.LogWarning("ログイン ID: {LoginId} のユーザーが見つかりません", Input.LoginId);
                }
                else
                {
                    _logger.LogInformation("ログイン ID: {LoginId} のユーザーが見つかりました", Input.LoginId);
                }

                if (user != null && await _userManager.CheckPasswordAsync(user, Input.Password))
                {
                    _logger.LogInformation("ログイン ID: {LoginId} のパスワード チェックに合格しました", Input.LoginId);

                    await _signInManager.SignInAsync(user, Input.RememberMe);
                    _logger.LogInformation("ユーザーはログイン ID: {LoginId} でログインしました", Input.LoginId);

                    return LocalRedirect(returnUrl);
                }

                _logger.LogWarning("ログイン ID {LoginId} のログイン試行が無効です", Input.LoginId);
                ModelState.AddModelError(string.Empty, "無効なログイン試行です。");
                return Page();
            }

            _logger.LogError("Model stateが LoginId に対して無効です: {LoginId}", Input.LoginId);
            return Page();
        }

    }
}
