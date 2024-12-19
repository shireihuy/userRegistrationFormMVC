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
            [Required(ErrorMessage = "���O�C��ID����͂��Ă��������B")]
            [Display(Name = "Login ID")]
            public string LoginId { get; set; }

            [Required(ErrorMessage = "�p�X���[�h����͂��Ă��������B")]
            [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_])[a-zA-Z\d\W_]{8,}$", ErrorMessage = "�p�X���[�h��8�����ȏ�ŁA�啶���A�������A�����A���ꕶ�����܂ޕK�v������܂��B")]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Display(Name = "���O�C�����ۑ�")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            //�����̊O��Cookie���N���A���āA���O�C���v���Z�X���N���[���ɂ��Ă�������
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            if (ModelState.IsValid)
            {
                _logger.LogInformation("���O�C�� ID: {LoginId} �Ń��O�C�������s���܂���", Input.LoginId);

                var user = await _userManager.FindByNameAsync(Input.LoginId);
                if (user == null)
                {
                    _logger.LogWarning("���O�C�� ID: {LoginId} �̃��[�U�[��������܂���", Input.LoginId);
                }
                else
                {
                    _logger.LogInformation("���O�C�� ID: {LoginId} �̃��[�U�[��������܂���", Input.LoginId);
                }

                if (user != null && await _userManager.CheckPasswordAsync(user, Input.Password))
                {
                    _logger.LogInformation("���O�C�� ID: {LoginId} �̃p�X���[�h �`�F�b�N�ɍ��i���܂���", Input.LoginId);

                    await _signInManager.SignInAsync(user, Input.RememberMe);
                    _logger.LogInformation("���[�U�[�̓��O�C�� ID: {LoginId} �Ń��O�C�����܂���", Input.LoginId);

                    return LocalRedirect(returnUrl);
                }

                _logger.LogWarning("���O�C�� ID {LoginId} �̃��O�C�����s�������ł�", Input.LoginId);
                ModelState.AddModelError(string.Empty, "�����ȃ��O�C�����s�ł��B");
                return Page();
            }

            _logger.LogError("Model state�� LoginId �ɑ΂��Ė����ł�: {LoginId}", Input.LoginId);
            return Page();
        }

    }
}
