using Application.MainBoundedContext.DTO.AdministrationModule;
using DistributedServices.MainBoundedContext.Identity;
using Infrastructure.Crosscutting.Framework.Utils;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.DataProtection;
using System;
using System.Configuration;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using SwiftFinancials.Web.Identity;
using SwiftFinancials.Web.Models;

namespace SwiftFinancials.Web.Controllers
{
    [Authorize]
    public class AccountController : MasterController
    {
        private readonly ApplicationSignInManager _signInManager;
        private readonly IAuthenticationManager _authenticationManager;
        private readonly DpapiDataProtectionProvider _provider;
        private static readonly int _passwordExpiryPeriod = Convert.ToInt32(ConfigurationManager.AppSettings["PasswordExpiryPeriod"]);

        public AccountController(ApplicationSignInManager signInManager,
            IAuthenticationManager authenticationManager, ServiceHeader serviceHeader)
        {
            Guard.ArgumentNotNull(signInManager, nameof(signInManager));
            Guard.ArgumentNotNull(authenticationManager, nameof(authenticationManager));

            _signInManager = signInManager;
            _authenticationManager = authenticationManager;

            _provider = new DpapiDataProtectionProvider("SwiftFinancials");
            _applicationUserManager.UserTokenProvider = new DataProtectorTokenProvider<ApplicationUser>(_provider.Create());
        }

        //
        // GET: /Account/Login
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        //
        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        {
            using (_applicationUserManager)
            {
                if (!ModelState.IsValid)
                    return View(model);

                ApplicationUser user = await _applicationUserManager.FindByEmailAsync(model.Email);

                if (user != null)
                {
                    if (user.LastPasswordChangedDate == null)
                    {
                        TempData["ApplicationUser"] = user;

                        return RedirectToAction("ForceChangePassword", "Account");
                    }
                    else if (user.LastPasswordChangedDate.Value.AddDays(_passwordExpiryPeriod) < DateTime.Now)
                    {
                        TempData["ApplicationUser"] = user;

                        return RedirectToAction("ForceChangePassword", "Account");
                    }
                    else if (user.LockoutEnabled)
                    {
                        return View("Lockout");
                    }
                    else
                    {
                        // This doesn't count login failures towards account lockout
                        // To enable password failures to trigger account lockout, change to shouldLockout: true
                        var result = await _signInManager.PasswordSignInAsync(userName: user.UserName, password: model.Password, isPersistent: false, shouldLockout: false);

                        switch (result)
                        {
                            case SignInStatus.Success:
                                //load navigation access rights
                                await LoadModuleAccessRights(user.UserName);
                                return RedirectToLocal(returnUrl);
                            case SignInStatus.LockedOut:
                                return View("Lockout");
                            case SignInStatus.RequiresVerification:
                                return RedirectToAction("SendCode", new { model.Email, ReturnUrl = returnUrl });
                            case SignInStatus.Failure:
                            default:
                                ModelState.AddModelError("", "Invalid login attempt.");
                                return View(model);
                        }
                    }
                }

                return View(model);
            }
        }

        //
        // GET: /Account/VerifyCode
        [AllowAnonymous]
        public async Task<ActionResult> VerifyCode(string provider, string username, string returnUrl)
        {
            // Require that the user has already logged in via username/password or external login
            if (!await _signInManager.HasBeenVerifiedAsync())
            {
                return View("Error");
            }

            return View(new VerifyCodeViewModel { Provider = provider, UserName = username, ReturnUrl = returnUrl });
        }

        //
        // POST: /Account/VerifyCode
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> VerifyCode(VerifyCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (!string.IsNullOrEmpty(Request["provider"]) && !string.IsNullOrEmpty(Request["username"]))
            {
                // The following code protects for brute force attacks against the two factor codes. 
                // If a user enters incorrect codes for a specified amount of time then the user account 
                // will be locked out for a specified amount of time. 
                // You can configure the account lockout settings in IdentityConfig
                var result = await _signInManager.TwoFactorSignInAsync(Request["provider"].ToString(), model.Code, isPersistent: false, rememberBrowser: false);

                switch (result)
                {
                    case SignInStatus.Success:
                        await LoadModuleAccessRights(username: Request["username"].ToString());
                        return RedirectToLocal(model.ReturnUrl);
                    case SignInStatus.LockedOut:
                        return View("Lockout");
                    case SignInStatus.Failure:
                    default:
                        ModelState.AddModelError("", "Invalid code.");
                        return View(model);
                }
            }

            return View(model);
        }

        //
        // GET: /Account/ConfirmEmail
        [AllowAnonymous]
        public async Task<ActionResult> ConfirmEmail(string userId, string code)
        {
            using (_applicationUserManager)
            {
                if (userId == null || code == null)
                {
                    return View("Error");
                }

                var result = await _applicationUserManager.ConfirmEmailAsync(userId, code);

                if (result.Succeeded)
                    await _applicationUserManager.SetLockoutEnabledAsync(userId, false);

                return View(result.Succeeded ? "ConfirmEmail" : "Error");
            }
        }

        //
        // GET: /Account/ForgotPassword
        [AllowAnonymous]
        public ActionResult ForgotPassword()
        {
            return View();
        }

        //
        // POST: /Account/ForgotPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ForgotPassword(ForgotPasswordViewModel forgotPasswordViewModel)
        {
            using (_applicationUserManager)
            {
                if (ModelState.IsValid)
                {
                    var user = await _applicationUserManager.FindByEmailAsync(forgotPasswordViewModel.Email);

                    if (user == null || !await _applicationUserManager.IsEmailConfirmedAsync(user.Id))
                    {
                        // Don't reveal that the user does not exist or is not confirmed
                        return View("ForgotPasswordConfirmation");
                    }

                    var userDTO = new UserDTO
                    {
                        Id = user.Id,
                        FirstName = user.FirstName,
                        OtherNames = user.OtherNames,
                        Email = user.Email,
                        PhoneNumber = user.PhoneNumber,
                        CompanyId = user.CompanyId,
                        CustomerId = user.CustomerId,
                    };

                    var result = await _channelService.ResetMembershipPasswordAsync(userDTO, GetServiceHeader());

                    if (!result)
                    {
                        return View("ForgotPasswordConfirmation");
                    }

                    return RedirectToAction("ForgotPasswordConfirmation", "Account");
                }

                return View(forgotPasswordViewModel);
            }
        }

        //
        // GET: /Account/ForgotPasswordConfirmation
        [AllowAnonymous]
        public ActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        //
        // GET: /Account/ResetPassword
        [AllowAnonymous]
        public ActionResult ResetPassword(string code)
        {
            return code == null ? View("Error") : View();
        }

        //
        // POST: /Account/ResetPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            using (_applicationUserManager)
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                ApplicationUser user = await _applicationUserManager.FindByEmailAsync(model.Email);

                if (user == null)
                {
                    // Don't reveal that the user does not exist
                    return RedirectToAction("ResetPasswordConfirmation", "Account");
                }

                var result = await _applicationUserManager.ResetPasswordAsync(user.Id, model.Code, model.Password);

                if (result.Succeeded)
                {
                    return RedirectToAction("ResetPasswordConfirmation", "Account");
                }

                AddErrors(result);

                return View();
            }
        }

        //
        // GET: /Account/ResetPasswordConfirmation
        [AllowAnonymous]
        public ActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        //
        // POST: /Account/ExternalLogin
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ExternalLogin(string provider, string returnUrl)
        {
            // Request a redirect to the external login provider
            return new ChallengeResult(provider, Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl }));
        }

        //
        // GET: /Account/SendCode
        [AllowAnonymous]
        public async Task<ActionResult> SendCode(string email, string returnUrl)
        {
            ViewBag.UserId = await _signInManager.GetVerifiedUserIdAsync();

            if (ViewBag.UserId == null)
                return View("Error");

            var model = new SendCodeViewModel()
            {
                ReturnUrl = returnUrl,
                Providers = GetTwoFactorProviders(string.Empty)
            };

            return View(model);
        }

        //
        // POST: /Account/SendCode
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SendCode(SendCodeViewModel sendCodeViewModel)
        {
            using (_applicationUserManager)
            {
                if (!ModelState.IsValid)
                    return View();

                sendCodeViewModel.Provider = GetEnumDescription((TwoFactorProviders)sendCodeViewModel.SelectedProvider);

                ApplicationUser applicationUser = new ApplicationUser();

                if (!string.IsNullOrEmpty(Request["userId"]))
                {
                    if (!Guid.TryParse(Request["userId"], out Guid userId))
                    {
                        TempData["Error"] = "Unable to complete your sign in request.Please try again";

                        return RedirectToAction("Login");
                    }

                    applicationUser = await _applicationUserManager.FindByIdAsync(userId.ToString());

                    var userDTO = new UserDTO
                    {
                        Id = applicationUser.Id,
                        FirstName = applicationUser.FirstName,
                        OtherNames = applicationUser.OtherNames,
                        Email = applicationUser.Email,
                        PhoneNumber = applicationUser.PhoneNumber,
                        CompanyId = applicationUser.CompanyId,
                        CustomerId = applicationUser.CustomerId,
                        Provider = sendCodeViewModel.SelectedProvider
                    };

                    var result = await _channelService.VerifyMembershipAsync(userDTO, GetServiceHeader());

                    if (!result)
                    {
                        ViewBag.Error = "Unable to send verification code";

                        return View();
                    }
                }

                return RedirectToAction("VerifyCode", new { sendCodeViewModel.Provider, applicationUser.UserName, sendCodeViewModel.ReturnUrl });
            }
        }

        //
        // GET: /Account/ExternalLoginCallback
        [AllowAnonymous]
        public async Task<ActionResult> ExternalLoginCallback(string returnUrl)
        {
            var loginInfo = await _authenticationManager.GetExternalLoginInfoAsync();
            if (loginInfo == null)
            {
                return RedirectToAction("Login");
            }

            // Sign in the user with this external login provider if the user already has a login
            var result = await _signInManager.ExternalSignInAsync(loginInfo, isPersistent: false);
            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(returnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.RequiresVerification:
                    return RedirectToAction("SendCode", new { ReturnUrl = returnUrl });
                case SignInStatus.Failure:
                default:
                    // If the user does not have an account, then prompt the user to create an account
                    ViewBag.ReturnUrl = returnUrl;
                    ViewBag.LoginProvider = loginInfo.Login.LoginProvider;
                    return View("ExternalLoginConfirmation", new ExternalLoginConfirmationViewModel { Email = loginInfo.Email });
            }
        }

        //
        // POST: /Account/ExternalLoginConfirmation
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, string returnUrl)
        {
            using (_applicationUserManager)
            {
                if (User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Index", "Manage");
                }

                if (ModelState.IsValid)
                {
                    // Get the information about the user from the external login provider
                    var info = await _authenticationManager.GetExternalLoginInfoAsync();
                    if (info == null)
                    {
                        return View("ExternalLoginFailure");
                    }
                    var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
                    var result = await _applicationUserManager.CreateAsync(user);
                    if (result.Succeeded)
                    {
                        result = await _applicationUserManager.AddLoginAsync(user.Id, info.Login);
                        if (result.Succeeded)
                        {
                            await _signInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                            return RedirectToLocal(returnUrl);
                        }
                    }
                    AddErrors(result);
                }

                ViewBag.ReturnUrl = returnUrl;
                return View(model);
            }
        }

        [HttpPost]
        public ActionResult LogOff()
        {
            _authenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);

            return RedirectToAction("Login");
        }

        //
        // GET: /Account/ExternalLoginFailure
        [AllowAnonymous]
        public ActionResult ExternalLoginFailure()
        {
            return View();
        }

        public ActionResult Lock()
        {
            ViewBag.Name = User.Identity.Name;

            _authenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);

            return View("~/Views/Shared/Lockout.cshtml");
        }

        [AllowAnonymous]
        public ActionResult Lockout()
        {
            return View();
        }

        // GET: /Manage/ForceChangePassword
        [AllowAnonymous]
        public ActionResult ForceChangePassword()
        {
            return View();
        }

        // POST: /Manage/ForceChangePassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ForceChangePassword(ChangePasswordViewModel model)
        {
            using (_applicationUserManager)
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                if (TempData["ApplicationUser"] != null)
                {
                    var userModel = TempData["ApplicationUser"] as ApplicationUser;

                    TempData.Keep("ApplicationUser");

                    var result = await _applicationUserManager.ChangePasswordAsync(userModel.Id, model.OldPassword, model.NewPassword);

                    if (result.Succeeded)
                    {
                        var user = await _applicationUserManager.FindByIdAsync(userModel.Id);

                        if (user != null)
                        {
                            user.LastPasswordChangedDate = DateTime.Now;

                            var identityResult = await _applicationUserManager.UpdateAsync(user);

                            if (identityResult.Succeeded)
                            {
                                var signInStatus = await _signInManager.PasswordSignInAsync(userName: userModel.UserName, password: model.NewPassword, isPersistent: false, shouldLockout: false);

                                switch (signInStatus)
                                {
                                    case SignInStatus.Success:
                                        //load navigation access rights
                                        await LoadModuleAccessRights(user.UserName);
                                        return RedirectToAction("Index", "Home");
                                    case SignInStatus.LockedOut:
                                        return View("Lockout");
                                    case SignInStatus.RequiresVerification:
                                        return RedirectToAction("SendCode", new { userModel.Email });
                                    case SignInStatus.Failure:
                                    default:
                                        //something happened and we could not automatically sign-in user
                                        //hence dump user to login screen.
                                        return RedirectToAction("Login");
                                }
                            }
                        }

                        return RedirectToAction("Login");
                    }

                    AddErrors(result);

                    return View(model);
                }
                else
                {
                    ModelState.AddModelError("", "Change password failed.");

                    return View(model);
                }
            }
        }

        #region Helpers

        // Used for XSRF protection when adding external logins
        private const string XsrfKey = "XsrfId";

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        internal class ChallengeResult : HttpUnauthorizedResult
        {
            public ChallengeResult(string provider, string redirectUri)
                : this(provider, redirectUri, null)
            {
            }

            public ChallengeResult(string provider, string redirectUri, string userId)
            {
                LoginProvider = provider;
                RedirectUri = redirectUri;
                UserId = userId;
            }

            public string LoginProvider { get; set; }
            public string RedirectUri { get; set; }
            public string UserId { get; set; }

            public override void ExecuteResult(ControllerContext context)
            {
                var properties = new AuthenticationProperties { RedirectUri = RedirectUri };
                if (UserId != null)
                {
                    properties.Dictionary[XsrfKey] = UserId;
                }
                context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
            }
        }

        #endregion
    }
}