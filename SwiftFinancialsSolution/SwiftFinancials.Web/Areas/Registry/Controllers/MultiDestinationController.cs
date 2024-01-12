using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.BackOfficeModule;
using Application.MainBoundedContext.DTO.RegistryModule;
using SwiftFinancials.Presentation.Infrastructure.Models;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.Loaning.Controllers
{
    public class MultiDestinationController : MasterController
    {


        public async Task<ActionResult> Create(Guid? id)
        {
            await ServeNavigationMenus();

            ViewBag.WithdrawalNotificationCategorySelectList = GetWithdrawalNotificationCategorySelectList(string.Empty);

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            var customer = await _channelService.FindCustomerAsync(parseId, GetServiceHeader());

            WithdrawalNotificationDTO withdrawalNotificationDTO = new WithdrawalNotificationDTO();

            if (customer != null)
            {

                withdrawalNotificationDTO.CustomerId = customer.Id;
                withdrawalNotificationDTO.CustomerFullName = customer.FullName;
                withdrawalNotificationDTO.CustomerIndividualPayrollNumbers = customer.IndividualPayrollNumbers;
                withdrawalNotificationDTO.CustomerSerialNumber = customer.SerialNumber;
                withdrawalNotificationDTO.CustomerIndividualIdentityCardNumber = customer.IndividualIdentityCardNumber;
                withdrawalNotificationDTO.CustomerStationDescription = customer.StationDescription;
                withdrawalNotificationDTO.CustomerStationZoneDivisionEmployerDescription = customer.StationZoneDivisionEmployerDescription;
            }

            return View(withdrawalNotificationDTO);
        }
        [HttpPost]
        public async Task<ActionResult> Create(FileMovementHistoryDTO fileMovementHistoryDTO)
        {
            fileMovementHistoryDTO.ValidateAll();

            if (!fileMovementHistoryDTO.HasErrors)
            {
                await _channelService.MultiDestinationDispatchAsync(new System.Collections.ObjectModel.ObservableCollection<FileMovementHistoryDTO> { fileMovementHistoryDTO }, GetServiceHeader());

                ViewBag.IncomeAdjustmentTypeSelectList = GetIncomeAdjustmentTypeSelectList(fileMovementHistoryDTO.Carrier.ToString());


                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = fileMovementHistoryDTO.ErrorMessages;

                return View("index");
            }
        }


    }
}

