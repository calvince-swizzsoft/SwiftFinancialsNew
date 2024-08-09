using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Threading.Tasks;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.FrontOfficeModule;
using Infrastructure.Crosscutting.Framework.Utils;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;


namespace SwiftFinancials.Web.Areas.FrontOffice.Controllers
{
    public class EndOfDayController : MasterController
    {
        // GET: FrontOffice/EndOfDay
        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();
            return View();
        }

    public async Task<ActionResult> Create(Guid? id)
    {


        await ServeNavigationMenus();


        Guid parseId;

        if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
        {
            return View();
        }

        return View();
    }

    [HttpPost]
    public  ActionResult Create()
    {


      return View();
    
    }

}

}