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
    public class CashTransferController : MasterController
    {

        public async Task<ActionResult> Create(Guid? id)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            var employee = await _channelService.FindEmployeeAsync(parseId, GetServiceHeader());

           

            CashTransferRequestDTO cashTransferRequestDTO = new CashTransferRequestDTO();

            if (employee != null)
            {
                cashTransferRequestDTO.EmployeeId = employee.Id;
                cashTransferRequestDTO.EmployeeCustomerIndividualFirstName = employee.CustomerFullName;
                cashTransferRequestDTO.EmployeeCustomerIndividualLastName = employee.CustomerIndividualFirstName;
            }


            ViewBag.WithdrawalNotificationCategorySelectList = GetWithdrawalNotificationCategorySelectList(string.Empty);

            ViewBag.TellerTypeSelectList = GetCashTransferTransactionTypeSelectList(string.Empty);

            return View(cashTransferRequestDTO);
        }

        [HttpPost]
        public async Task<ActionResult> Create(CashTransferRequestDTO cashTransferRequestDTO)
        {
            if (ModelState.IsValid)
            {
                await _channelService.AddCashTransferRequestAsync(cashTransferRequestDTO, GetServiceHeader());

                return RedirectToAction("Create");
            }
            else
            {

                ViewBag.TellerTypeSelectList = GetCashTransferTransactionTypeSelectList(cashTransferRequestDTO.Status.ToString());

                return View(cashTransferRequestDTO);
            }
        }



        [HttpGet]
        public async Task<JsonResult> GetTellersAsync()
        {
            var tellersDTOs = await _channelService.FindTellersAsync(GetServiceHeader());

            return Json(tellersDTOs, JsonRequestBehavior.AllowGet);

        }
    }
}

