using SwiftFinancials.Web.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.Admin.Controllers
{
    public class SSRSReportSettingLoginController : MasterController
    {
        [HttpGet]
        public async Task<ActionResult> Login()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Login(string email, string password)
        {
            await ServeNavigationMenus();

            if (email == "info@stamlinetechnologies.com" && password == "Abc.2020!")
            {
                Session["UserEmail"] = email;
                return RedirectToAction("Create", "SSRSReportSetting", new { area = "Admin" });
            }
            else
            {
                ViewBag.ErrorMessage = "Access Denied!";
                return View();
            }
        }
    }
}