using Core;
using Core.Service;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Util;
using static System.Net.Mime.MediaTypeNames;

namespace EventoWeb.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<UsuarioIdentity> _signInManager;
        private readonly UserManager<UsuarioIdentity> _userManager;
        private readonly ILogger<LoginModel> _logger;
   
        public LoginModel(SignInManager<UsuarioIdentity> signInManager, UserManager<UsuarioIdentity> userManager, ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string ErrorMessage { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required(ErrorMessage = "O campo E-mail ou CPF é obrigatório.")]
            [Display(Name = "E-mail ou CPF", Prompt = "Digite seu CPF ou E-mail")]
			public string EmailOrCpf { get; set; }

			/// <summary>
			///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
			///     directly from your code. This API may change or be removed in future releases.
			/// </summary>
			[Required(ErrorMessage = "O campo Senha é obrigatório.")]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Display(Name = "Lembre-me")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ReturnUrl = returnUrl;
        }

        public string mensagemPadrao(bool isEmail)
        {
            if (isEmail)
                return "E-mail ou senha inválidos.";
            else
                return "CPF ou senha inválidos.";
        }
        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                List<UsuarioIdentity> usuariosEncontrados = new List<UsuarioIdentity>();
                UsuarioIdentity user = null;
                
                string emailParaLogin = Input.EmailOrCpf.ToLower().Trim();
                bool contemLetra = emailParaLogin.Any(char.IsLetter);
                String msmPadrao = mensagemPadrao(contemLetra);

                string cpfSemFormatacao = Util.Methods.RemoveNaoNumericos(Input.EmailOrCpf);
                if (!String.IsNullOrEmpty(cpfSemFormatacao) && cpfSemFormatacao.All(char.IsDigit)&&!contemLetra)
                {
                    user = await _userManager.Users.SingleOrDefaultAsync(u => u.UserName == cpfSemFormatacao);  
                    if (user == null)
                    {
                        ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.EmailOrCpf)}", "CPF ou senha inválidos.");
                        ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.Password)}", "CPF ou senha inválidos.");
                        return Page();
                    }
                }
                else
                {

                    var usuariosComMesmoEmail = await _userManager.Users
                    .Where(u => u.Email.ToLower() == emailParaLogin)
                    .ToListAsync();

                    if (usuariosComMesmoEmail.Count > 1)
                    {
                        ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.EmailOrCpf)}", "Houve um problema ao acessar com este e-mail. Por favor, faça login utilizando o seu CPF.");
                        return Page();
                    }

                    user = usuariosComMesmoEmail.FirstOrDefault();
                    if (user == null)
                    {
                        ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.EmailOrCpf)}", "E-mail ou senha inválidos.");
                        ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.Password)}", "E-mail ou senha inválidos.");

                        return Page();
                    }
                }

                    var result = await _signInManager.PasswordSignInAsync(user.UserName, Input.Password, Input.RememberMe, lockoutOnFailure: false);
                    if (result.Succeeded)
                    {
                        _logger.LogInformation("Usuário logado.");
                        return LocalRedirect(returnUrl);
                    }
                    if (result.RequiresTwoFactor)
                    {
                        return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                    }
                    if (result.IsLockedOut)
                    {
                        _logger.LogWarning("Conta de usuário bloqueada.");
                        return RedirectToPage("./Lockout");
                    }
                    else
                    {
                        ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.EmailOrCpf)}", msmPadrao);
                        ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.Password)}", msmPadrao);
                        return Page();
                    }

            }
            else{
                ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.EmailOrCpf)}", "Os dados estão inválidos");
                ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.Password)}", "Os dados estão inválidos");
                return Page();
            }

            
        }

    }
}
