using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.FrontOfficeModule;
using Infrastructure.Crosscutting.Framework.Utils;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.FrontOffice.Controllers
{
    public class TransfersController : MasterController
    {
        // GET: FrontOffice/Transfers
        public async Task<ActionResult> Index()
        {

            await ServeNavigationMenus();
            return View();
        }

        public async Task<ActionResult> Create(Guid? id)
        {
            await ServeNavigationMenus();



            return View();
        }


        [HttpPost]
        public async Task<ActionResult> Create(ExternalChequeDTO externalChequeDTO)
        {
            externalChequeDTO.ValidateAll();

            if (!externalChequeDTO.HasErrors)
            {
                await _channelService.AddExternalChequeAsync(externalChequeDTO, GetServiceHeader());

                ViewBag.CustomerTypeSelectList = GetCustomerTypeSelectList(externalChequeDTO.CustomerAccountCustomerType.ToString());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = externalChequeDTO.ErrorMessages;

                return View(externalChequeDTO);
            }
        }

    }
}