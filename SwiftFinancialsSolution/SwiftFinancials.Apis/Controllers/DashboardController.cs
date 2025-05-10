using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using SwiftFinancials.Apis.Identity;
using VanguardFinancials.Presentation.Infrastructure.Services;

namespace SwiftFinancials.Apis.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationUserManager _userManager;
        private readonly IChannelService _channelService;

        public DashboardController(
            ApplicationUserManager userManager,
            IChannelService channelService)
        {
            _userManager = userManager;
            _channelService = channelService;
        }

        //
        // GET: /Dashboard/
        public ActionResult Index()
        {
            return View();
        }
    }
}