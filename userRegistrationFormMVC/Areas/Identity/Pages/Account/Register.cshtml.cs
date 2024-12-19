// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace userRegistrationFormMVC.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IUserStore<IdentityUser> _userStore;
        private readonly IUserEmailStore<IdentityUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;
        public RegisterModel(
            UserManager<IdentityUser> userManager,
            IUserStore<IdentityUser> userStore,
            SignInManager<IdentityUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender
           )
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "���O�C��ID����͂��Ă��������B")]
            [Display(Name = "���O�C��ID")]
            public string LoginId { get; set; }

            [Required(ErrorMessage = "���[���A�h���X����͂��Ă��������B")]
            [EmailAddress(ErrorMessage = "���������[���A�h���X����͂��Ă��������B")]
            [Display(Name = "���[���A�h���X")]
            public string Email { get; set; } // ���[���A�h���X

            [Required(ErrorMessage = "�p�X���[�h����͂��Ă��������B")]
            [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_])[a-zA-Z\d\W_]{8,}$", ErrorMessage = "�p�X���[�h��8�����ȏ�ŁA�啶���A�������A�����A���ꕶ�����܂ޕK�v������܂��B")]
            [DataType(DataType.Password)]
            [Display(Name = "�p�X���[�h")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "�m�F�p�X���[�h")]
            [Required(ErrorMessage = "�m�F�p�X���[�h����͂��Ă��������B")]
            [Compare("Password", ErrorMessage = "�m�F�p�X���[�h�ƃp�X���[�h�͕s��v���܂����B")]
            public string ConfirmPassword { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                var user = CreateUser();

                //���O�C��ID�����[�U�[���Ƃ��Đݒ肷��
                await _userStore.SetUserNameAsync(user, Input.LoginId, CancellationToken.None);

                //���[����ݒ肷��
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

                var result = await _userManager.CreateAsync(user, Input.Password);
                if (result.Succeeded)
                {
                    _logger.LogInformation("���[�U�[�̓p�X���[�h���g�p���ĐV�����A�J�E���g���쐬���܂����B");

                    await _signInManager.SignInAsync(user, isPersistent: false);

                    return LocalRedirect(returnUrl);
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // �����܂ŗ����牽�������s�����̂Ńt�H�[�����ĕ\�����܂�
            return Page();
        }

        private IdentityUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<IdentityUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(IdentityUser)}'. " +
                    $"Ensure that '{nameof(IdentityUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
            }
        }

        private IUserEmailStore<IdentityUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<IdentityUser>)_userStore;
        }
    }
}
