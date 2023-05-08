using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.Accounts.Controllers
{
    public class DefaultController : Controller
    {
        // GET: Accounts/Default
        public ActionResult Index()
        {
            return View();
        }
    }
}