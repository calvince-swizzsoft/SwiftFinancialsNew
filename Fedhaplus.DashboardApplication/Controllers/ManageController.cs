using Application.MainBoundedContext.DTO.MessagingModule;
using DistributedServices.MainBoundedContext.Identity;
using Infrastructure.Crosscutting.Framework.Utils;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using RazorEngine;
using RazorEngine.Templating;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Fedhaplus.DashboardApplication.Configuration;
using Fedhaplus.DashboardApplication.Identity;
using Fedhaplus.DashboardApplication.Models;

namespace Fedhaplus.DashboardApplication.Controllers
{
    [Authorize]
    public class ManageController : MasterController
    {
        private ApplicationSignInManager _signInManager;

        public ManageController()
        {
        }

        public ManageController(ApplicationSignInManager signInManager)
        {
            SignInManager = signInManager;
        }

        public ApplicationSignInManager SignInManager
        {
            get
            {
                return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
            }
            private set
            {
                _signInManager = value;
            }
        }

        // GET: /Manage/Index
        public async Task<ActionResult> Index(ManageMessageId? message)
        {
            await ServeNavigationMenus();

            ViewBag.StatusMessage =
                message == ManageMessageId.ChangePasswordSuccess ? "Your password has been changed."
                : message == ManageMessageId.SetPasswordSuccess ? "Your password has been set."
                : message == ManageMessageId.SetTwoFactorSuccess ? "Your two-factor authentication provider has been set."
                : message == ManageMessageId.Error ? "An error has occurred."
                : message == ManageMessageId.AddPhoneSuccess ? "Your phone number was added."
                : message == ManageMessageId.RemovePhoneSuccess ? "Your phone number was removed."
                : "";

            var userId = User.Identity.GetUserId();
            var model = new IndexViewModel
            {
                HasPassword = HasPassword(),
                PhoneNumber = await _applicationUserManager.GetPhoneNumberAsync(userId),
                TwoFactor = await _applicationUserManager.GetTwoFactorEnabledAsync(userId),
                Logins = await _applicationUserManager.GetLoginsAsync(userId),
                BrowserRemembered = await AuthenticationManager.TwoFactorBrowserRememberedAsync(userId)
            };
            return View(model);
        }

        // POST: /Manage/RemoveLogin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RemoveLogin(string loginProvider, string providerKey)
        {
            ManageMessageId? message;
            var result = await _applicationUserManager.RemoveLoginAsync(User.Identity.GetUserId(), new UserLoginInfo(loginProvider, providerKey));
            if (result.Succeeded)
            {
                var user = await _applicationUserManager.FindByIdAsync(User.Identity.GetUserId());
                if (user != null)
                {
                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                }
                message = ManageMessageId.RemoveLoginSuccess;
            }
            else
            {
                message = ManageMessageId.Error;
            }
            return RedirectToAction("ManageLogins", new { Message = message });
        }

        // GET: /Manage/AddPhoneNumber
        public async Task<ActionResult> AddPhoneNumber()
        {
            await ServeNavigationMenus();

            return View();
        }

        // POST: /Manage/AddPhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddPhoneNumber(AddPhoneNumberViewModel addPhoneNumberViewModel)
        {
            if (!ModelState.IsValid)
            {
                return View(addPhoneNumberViewModel);
            }
            // Generate the token and send it
            var code = await _applicationUserManager.GenerateChangePhoneNumberTokenAsync(User.Identity.GetUserId(), addPhoneNumberViewModel.Number);

            ApplicationUser applicationUser = await _applicationUserManager.FindByIdAsync(User.Identity.GetUserId());
            
            var companyDTO = await _channelService.FindCompanyAsync((Guid)applicationUser.CompanyId, GetServiceHeader());

            var textAlertTemplatePath = Path.Combine(GetDashboardAppConfiguration().DashboardAppSettingsItems.TemplatesPath, string.Format("{0}_TextTemplate.cshtml", "PhoneNumberVerification"));

            if (System.IO.File.Exists(textAlertTemplatePath))
            {
                var template = System.IO.File.ReadAllText(textAlertTemplatePath);

                dynamic expando = new ExpandoObject();

                var model = expando as IDictionary<string, object>;

                model.Add("FirstName", applicationUser.FirstName);
                model.Add("Token", code);
                model.Add("CompanyDescription", companyDTO.Description);

                var textResult = Engine.Razor.RunCompile(template, string.Format("{0}_TextTemplate", "PhoneNumberVerification"), null, model);

                TextAlertDTO textAlertDTO = new TextAlertDTO
                {
                    TextMessageBody = textResult,
                    TextMessageRecipient = applicationUser.PhoneNumber,
                    TextMessageDLRStatus = (int)DLRStatus.Pending,
                    TextMessageOrigin = (int)MessageOrigin.Within,
                    TextMessagePriority = (int)QueuePriority.High,
                    TextMessageSecurityCritical = true
                };

                TextAlertDTO textAlertResult = await _channelService.AddTextAlertAsync(textAlertDTO, GetServiceHeader());
            }

            return RedirectToAction("VerifyPhoneNumber", new { PhoneNumber = addPhoneNumberViewModel.Number });
        }

        // POST: /Manage/EnableTwoFactorAuthentication
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EnableTwoFactorAuthentication()
        {
            await _applicationUserManager.SetTwoFactorEnabledAsync(User.Identity.GetUserId(), true);
            var user = await _applicationUserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user != null)
            {
                await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
            }
            return RedirectToAction("Index", "Manage");
        }

        // POST: /Manage/DisableTwoFactorAuthentication
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DisableTwoFactorAuthentication()
        {
            await _applicationUserManager.SetTwoFactorEnabledAsync(User.Identity.GetUserId(), false);
            var user = await _applicationUserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user != null)
            {
                await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
            }
            return RedirectToAction("Index", "Manage");
        }

        // GET: /Manage/VerifyPhoneNumber
        public async Task<ActionResult> VerifyPhoneNumber(string phoneNumber)
        {
            await ServeNavigationMenus();

            var code = await _applicationUserManager.GenerateChangePhoneNumberTokenAsync(User.Identity.GetUserId(), phoneNumber);
            // Send an SMS through the SMS provider to verify the phone number
            return phoneNumber == null ? View("Error") : View(new VerifyPhoneNumberViewModel { PhoneNumber = phoneNumber });
        }

        // POST: /Manage/VerifyPhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> VerifyPhoneNumber(VerifyPhoneNumberViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var result = await _applicationUserManager.ChangePhoneNumberAsync(User.Identity.GetUserId(), model.PhoneNumber, model.Code);
            if (result.Succeeded)
            {
                var user = await _applicationUserManager.FindByIdAsync(User.Identity.GetUserId());

                if (user != null)
                {
                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                }
                return RedirectToAction("Index", new { Message = ManageMessageId.AddPhoneSuccess });
            }
            // If we got this far, something failed, redisplay form
            ModelState.AddModelError("", "Failed to verify phone");

            return View(model);
        }

        // POST: /Manage/RemovePhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RemovePhoneNumber()
        {
            var result = await _applicationUserManager.SetPhoneNumberAsync(User.Identity.GetUserId(), null);
            if (!result.Succeeded)
            {
                return RedirectToAction("Index", new { Message = ManageMessageId.Error });
            }
            var user = await _applicationUserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user != null)
            {
                await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
            }
            return RedirectToAction("Index", new { Message = ManageMessageId.RemovePhoneSuccess });
        }

        // GET: /Manage/ChangePassword
        public async Task<ActionResult> ChangePassword()
        {
            await ServeNavigationMenus();

            return View();
        }

        // POST: /Manage/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var result = await _applicationUserManager.ChangePasswordAsync(User.Identity.GetUserId(), model.OldPassword, model.NewPassword);
            if (result.Succeeded)
            {
                var user = await _applicationUserManager.FindByIdAsync(User.Identity.GetUserId());
                if (user != null)
                {
                    user.LastPasswordChangedDate = DateTime.Now;
                    await _applicationUserManager.UpdateAsync(user);

                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                }
                return RedirectToAction("Index", new { Message = ManageMessageId.ChangePasswordSuccess });
            }

            AddErrors(result);

            return View(model);
        }

        // GET: /Manage/SetPassword
        public async Task<ActionResult> SetPassword()
        {
            await ServeNavigationMenus();

            return View();
        }

        // POST: /Manage/SetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SetPassword(SetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _applicationUserManager.AddPasswordAsync(User.Identity.GetUserId(), model.NewPassword);
                if (result.Succeeded)
                {
                    var user = await _applicationUserManager.FindByIdAsync(User.Identity.GetUserId());
                    if (user != null)
                    {
                        user.LastPasswordChangedDate = DateTime.Now;
                        await _applicationUserManager.UpdateAsync(user);

                        await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                    }
                    return RedirectToAction("Index", new { Message = ManageMessageId.SetPasswordSuccess });
                }
                AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        // GET: /Manage/ManageLogins
        public async Task<ActionResult> ManageLogins(ManageMessageId? message)
        {
            await ServeNavigationMenus();

            ViewBag.StatusMessage =
                message == ManageMessageId.RemoveLoginSuccess ? "The external login was removed."
                : message == ManageMessageId.Error ? "An error has occurred."
                : "";
            var user = await _applicationUserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user == null)
            {
                return View("Error");
            }
            var userLogins = await _applicationUserManager.GetLoginsAsync(User.Identity.GetUserId());
            var otherLogins = AuthenticationManager.GetExternalAuthenticationTypes().Where(auth => userLogins.All(ul => auth.AuthenticationType != ul.LoginProvider)).ToList();
            ViewBag.ShowRemoveButton = user.PasswordHash != null || userLogins.Count > 1;
            return View(new ManageLoginsViewModel
            {
                CurrentLogins = userLogins,
                OtherLogins = otherLogins
            });
        }

        // POST: /Manage/LinkLogin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LinkLogin(string provider)
        {
            // Request a redirect to the external login provider to link a login for the current user
            return new AccountController.ChallengeResult(provider, Url.Action("LinkLoginCallback", "Manage"), User.Identity.GetUserId());
        }

        // GET: /Manage/LinkLoginCallback
        public async Task<ActionResult> LinkLoginCallback()
        {
            var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync(XsrfKey, User.Identity.GetUserId());
            if (loginInfo == null)
            {
                return RedirectToAction("ManageLogins", new { Message = ManageMessageId.Error });
            }
            var result = await _applicationUserManager.AddLoginAsync(User.Identity.GetUserId(), loginInfo.Login);
            return result.Succeeded ? RedirectToAction("ManageLogins") : RedirectToAction("ManageLogins", new { Message = ManageMessageId.Error });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _applicationUserManager != null)
            {
                _applicationUserManager.Dispose();
                _applicationUserManager = null;
            }

            base.Dispose(disposing);
        }

        #region Helpers
        // Used for XSRF protection when adding external logins
        private const string XsrfKey = "XsrfId";

        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.GetOwinContext().Authentication;
            }
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        private bool HasPassword()
        {
            var user = _applicationUserManager.FindById(User.Identity.GetUserId());
            if (user != null)
            {
                return user.PasswordHash != null;
            }
            return false;
        }

        private bool HasPhoneNumber()
        {
            var user = _applicationUserManager.FindById(User.Identity.GetUserId());
            if (user != null)
            {
                return user.PhoneNumber != null;
            }
            return false;
        }

        public enum ManageMessageId
        {
            AddPhoneSuccess,
            ChangePasswordSuccess,
            SetTwoFactorSuccess,
            SetPasswordSuccess,
            RemoveLoginSuccess,
            RemovePhoneSuccess,
            Error
        }

        #endregion
    }
}