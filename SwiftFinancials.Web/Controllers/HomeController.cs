using Infrastructure.Crosscutting.Framework.Utils;
using System.Threading.Tasks;
using System.Web.Mvc;
using SwiftFinancials.Web.Attributes;
using SwiftFinancials.Web.Identity;

namespace SwiftFinancials.Web.Controllers
{
    [Authorize]
    [CustomErrorHandling]
    public class HomeController : MasterController
    {
        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();

            //clear tempdata that may have been used during forced password change
            if (TempData["ApplicationUser"] != null)
                TempData.Remove("ApplicationUser");

            return RedirectToAction("IndexPage");
        }

        public async Task<ActionResult> IndexPage()
        {
            //admin

            await ServeNavigationMenus();

            return View();
        }

        public async Task<ActionResult> StartPage()
        {
            //Company

            await ServeNavigationMenus();

            return View();
        }

        public async Task<ActionResult> HomePage()
        {
            //customer

           // ViewBag.Saccos = await _channelService.FindCustomerSubscriptionsByCustomerIdAsync(User.Identity.GetCustomerId(), GetServiceHeader());

            return View();
        }

        public async Task<ActionResult> Error()
        {
            await ServeNavigationMenus();

            return View("~/Views/Shared/Error.cshtml");
        }

        [AllowAnonymous]
        public ActionResult UnhandledError()
        {
            return View("~/Views/Shared/UnhandledError.cshtml");
        }

        //[HttpPost]
        //public async Task<ActionResult> RedirectTo(string NavigationItemCode)
        //{
        //    int code = 0;

        //    if (!string.IsNullOrWhiteSpace(NavigationItemCode))
        //    {
        //        if (int.TryParse(NavigationItemCode, out code))
        //        {
        //            var NavigationItem = await _channelService.FindNavigationItemByCodeAsync(code, GetServiceHeader());

        //            return RedirectToAction(NavigationItem.ActionName, NavigationItem.ControllerName, new { Area = NavigationItem.AreaName });
        //        }
        //        else
        //        {
        //            return RedirectToAction(Request.UrlReferrer.ToString());
        //        }
        //    }
        //    else
        //    {
        //        return RedirectToAction(Request.UrlReferrer.ToString());
        //    }
        //}
    }
}