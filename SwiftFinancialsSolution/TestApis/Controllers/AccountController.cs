//using Application.MainBoundedContext.DTO.AdministrationModule;
//using DistributedServices.MainBoundedContext.Identity;
//using Infrastructure.Crosscutting.Framework.Utils;
//using Microsoft.AspNet.Identity;
//using Microsoft.AspNet.Identity.EntityFramework;
//using Microsoft.AspNet.Identity.Owin;
//using Microsoft.Owin.Security.DataProtection;
//using System;
//using System.Configuration;
//using System.Threading.Tasks;
//using System.Web.Http;
//using TestApis.Identity;
//using TestApis.Models;

//namespace TestApis.Controllers
//{
//    [RoutePrefix("api/account")]
//    public class AccountController : ApiController
//    {
//        private MasterController _master = new MasterController();

//        private static readonly int PasswordExpiryPeriod =
//            Convert.ToInt32(ConfigurationManager.AppSettings["PasswordExpiryPeriod"]);

//        private ApplicationUserManager _userManager;
//        private ApplicationSignInManager _signInManager;

//        private ApplicationUserManager UserManager
//        {
//            get
//            {
//                if (_userManager == null)
//                    _userManager = CreateUserManager();
//                return _userManager;
//            }
//        }

//        private ApplicationSignInManager SignInManager
//        {
//            get
//            {
//                if (_signInManager == null)
//                    _signInManager = new ApplicationSignInManager(UserManager);
//                return _signInManager;
//            }
//        }

//        private ApplicationUserManager CreateUserManager()
//        {
//            var context = new ApplicationDbContext("AuthStore");
//            var userStore = new UserStore<ApplicationUser>(context);
//            var manager = new ApplicationUserManager(userStore);

//            var provider = new DpapiDataProtectionProvider("SwiftFinancials");
//            manager.UserTokenProvider = new DataProtectorTokenProvider<ApplicationUser>(provider.Create());

//            return manager;
//        }

//        [HttpPost]
//        [Route("login")]
//        [AllowAnonymous]
//        public async Task<IHttpActionResult> Login(LoginViewModel model)
//        {
//            if (!ModelState.IsValid)
//                return BadRequest("Invalid payload");

//            var user = await UserManager.FindByEmailAsync(model.Email);
//            if (user == null)
//                return Unauthorized();

//            if (user.LastPasswordChangedDate == null ||
//                user.LastPasswordChangedDate.Value.AddDays(PasswordExpiryPeriod) < DateTime.Now)
//            {
//                return Ok(new
//                {
//                    requiresPasswordChange = true,
//                    userId = user.Id,
//                    reason = "Password expired or never changed"
//                });
//            }

//            if (user.LockoutEnabled)
//                return Ok(new { lockedOut = true });

//            var result = await SignInManager.PasswordSignInAsync(user.UserName, model.Password, false, false);

//            //switch (result)
//            //{
//            //    case SignInStatus.Success:
//            //        return Ok(new { login = "success", user = new { user.Id, user.UserName, user.Email } });
//            //    case SignInStatus.RequiresVerification:
//            //        return Ok(new { requires2FA = true, userId = user.Id });
//            //    case SignInStatus.LockedOut:
//            //        return Ok(new { lockedOut = true });
//            //    default:
//            //        return Unauthorized();
//            //}
//            return Ok (new { login = "success", user = new { user} });
//        }

//        [HttpPost]
//        [Route("send-code")]
//        [AllowAnonymous]
//        public async Task<IHttpActionResult> Send2FACode(SendCodeViewModel model)
//        {
//            if (string.IsNullOrWhiteSpace(model.Email))
//                return BadRequest("Email required");

//            var user = await UserManager.FindByEmailAsync(model.Email);
//            if (user == null)
//                return NotFound();

//            var dto = new UserDTO
//            {
//                Id = user.Id,
//                FirstName = user.FirstName,
//                OtherNames = user.OtherNames,
//                Email = user.Email,
//                PhoneNumber = user.PhoneNumber,
//                BranchId = user.BranchId,
//                CustomerId = user.CustomerId,
//                Provider = model.SelectedProvider
//            };

//            var serviceHeader = _master.GetServiceHeader();
//            var result = await _master._channelService.VerifyMembershipAsync(dto, serviceHeader);

//            if (!result)
//                return BadRequest("Unable to send verification code");

//            return Ok(new { sent = true });
//        }

//        [HttpPost]
//        [Route("forgot-password")]
//        [AllowAnonymous]
//        public async Task<IHttpActionResult> ForgotPassword(ForgotPasswordViewModel model)
//        {
//            if (!ModelState.IsValid)
//                return BadRequest("Invalid payload");

//            var user = await UserManager.FindByEmailAsync(model.Email);
//            if (user == null)
//                return Ok(new { dispatched = true });

//            var dto = new UserDTO
//            {
//                Id = user.Id,
//                FirstName = user.FirstName,
//                OtherNames = user.OtherNames,
//                Email = user.Email,
//                PhoneNumber = user.PhoneNumber
//            };

//            var serviceHeader = _master.GetServiceHeader();
//            await _master._channelService.ResetMembershipPasswordAsync(dto, serviceHeader);

//            return Ok(new { dispatched = true });
//        }

//        [HttpPost]
//        [Route("force-change-password")]
//        [AllowAnonymous]
//        public async Task<IHttpActionResult> ForceChangePassword(ChangePasswordViewModel model)
//        {
//            if (string.IsNullOrWhiteSpace(model.Email))
//                return BadRequest("Email required");

//            var user = await UserManager.FindByEmailAsync(model.Email);
//            if (user == null)
//                return NotFound();

//            var result = await UserManager.ChangePasswordAsync(user.Id, model.OldPassword, model.NewPassword);
//            if (!result.Succeeded)
//                return BadRequest(string.Join(",", result.Errors));

//            user.LastPasswordChangedDate = DateTime.Now;
//            await UserManager.UpdateAsync(user);

//            return Ok(new { changed = true });
//        }
//    }
//}
