using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using SwiftFinancials.Presentation.Infrastructure.Util;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.Accounts.Controllers
{
    public class TiersController : MasterController
    {
        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            ViewBag.chargeType = GetChargeTypeSelectList(string.Empty);


            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(CommissionDTO commissionDTO)
        {
            commissionDTO.ValidateAll();

            if (!commissionDTO.ErrorMessages.Any())
            {
                await _channelService.AddCommissionAsync(commissionDTO.MapTo<CommissionDTO>(), GetServiceHeader());

               

                return RedirectToAction("Index");
            }
            else
            {

                ViewBag.chargeType = GetChargeTypeSelectList(commissionDTO.ToString());

                return View(commissionDTO);
            }
        }
    }
}