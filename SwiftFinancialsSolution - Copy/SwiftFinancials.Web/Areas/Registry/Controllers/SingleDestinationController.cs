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

namespace SwiftFinancials.Web.Areas.Registry.Controllers
{
    public class SingleDestinationController : MasterController
    {

        public async Task<ActionResult> Search(Guid? id)
        {
            //string Remarks = "";
            await ServeNavigationMenus();
            ViewBag.recordStatus = GetRecordStatusSelectList(string.Empty);
            ViewBag.customerFilter = GetCustomerFilterSelectList(string.Empty);
            ViewBag.CustomerTypeSelectList = GetCustomerTypeSelectList(string.Empty);
            ViewBag.WithdrawalNotificationCategorySelectList = GetWithdrawalNotificationCategorySelectList(string.Empty);
            var customers = await _channelService.FindCustomersAsync(GetServiceHeader());
            ViewBag.customers = customers;
            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            var designation = await _channelService.FindDesignationAsync(parseId, GetServiceHeader());

            var history = await _channelService.FindFileRegisterAndLastDepartmentByCustomerIdAsync(customers.Last().Id, GetServiceHeader());
            ViewBag.history = history;
            SingleDestinationDispatchModel singleDestinationDispatchModel = new SingleDestinationDispatchModel();



            if (designation != null)
            {

                singleDestinationDispatchModel.DestinationDepartmentId = designation.Id;
                singleDestinationDispatchModel.SourceDepartment = designation.Description;



            }

            return View("Create", singleDestinationDispatchModel);
        }

        public async Task<ActionResult> ChartOfAccountLookUp(Guid? id)
        {
            //string Remarks = "";
            await ServeNavigationMenus();
            ViewBag.recordStatus = GetRecordStatusSelectList(string.Empty);
            ViewBag.customerFilter = GetCustomerFilterSelectList(string.Empty);
            ViewBag.CustomerTypeSelectList = GetCustomerTypeSelectList(string.Empty);
            ViewBag.WithdrawalNotificationCategorySelectList = GetWithdrawalNotificationCategorySelectList(string.Empty);
            var customers = await _channelService.FindCustomersAsync(GetServiceHeader());
            ViewBag.customers = customers;
            TempData["customers"] = customers;
            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            var designation = await _channelService.FindDepartmentAsync(parseId, GetServiceHeader());

            var history = await _channelService.FindFileRegisterAndLastDepartmentByCustomerIdAsync(customers.Last().Id, GetServiceHeader());
            ViewBag.history = history;
            SingleDestinationDispatchModel singleDestinationDispatchModel = new SingleDestinationDispatchModel();



            if (designation != null)
            {

                singleDestinationDispatchModel.DestinationDepartmentId = designation.Id;
                singleDestinationDispatchModel.SourceDepartment = designation.Description;




                return Json(new
                {
                    success = true,
                    data = new
                    {
                        ChartOfAccountId = singleDestinationDispatchModel.DestinationDepartmentId,
                        ChartOfAccountAccountName = singleDestinationDispatchModel.SourceDepartment
                    }
                });
            }
            return Json(new { success = false, message = "Product Not Found!" });
        }

        public async Task<ActionResult> Create(Guid? id)
        {
            await ServeNavigationMenus();

            ViewBag.WithdrawalNotificationCategorySelectList = GetWithdrawalNotificationCategorySelectList(string.Empty);
            var customers = await _channelService.FindCustomersAsync(GetServiceHeader());
            ViewBag.customers = customers;
            TempData["customers"] = customers;
            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            var customer = await _channelService.FindCustomerAsync(parseId, GetServiceHeader());

            FileMovementHistoryDTO fileMovementHistoryDTO = new FileMovementHistoryDTO();

            if (customer != null)
            {

                fileMovementHistoryDTO.FileRegisterCustomerId = customer.Id;
                fileMovementHistoryDTO.FileRegisterCustomerIndividualFirstName = customer.FullName;
                //fileMovementHistoryDTO. = customer.IndividualPayrollNumbers;
                //fileMovementHistoryDTO. = customer.SerialNumber;
                //fileMovementHistoryDTO.CustomerIndividualIdentityCardNumber = customer.IndividualIdentityCardNumber;
                //fileMovementHistoryDTO.CustomerStationDescription = customer.StationDescription;
                //fileMovementHistoryDTO.CustomerStationZoneDivisionEmployerDescription = customer.StationZoneDivisionEmployerDescription;
            }

            return View(fileMovementHistoryDTO);
        }
        [HttpPost]
        public async Task<ActionResult> Create(FileMovementHistoryDTO fileMovementHistoryDTO, string[] customerIds)
        {
            SingleDestinationDispatchModel singleDestinationDispatch = new SingleDestinationDispatchModel();
            singleDestinationDispatch.Carrier = fileMovementHistoryDTO.Carrier;
            singleDestinationDispatch.DestinationDepartmentId = fileMovementHistoryDTO.DestinationDepartmentId;
            singleDestinationDispatch.SourceDepartment = fileMovementHistoryDTO.SourceDepartmentId.ToString();


            singleDestinationDispatch.ValidateAll();

            if (!singleDestinationDispatch.HasErrors)
            {

                var selectedIds = customerIds.Select(Guid.Parse).ToList();
                foreach (var customer in selectedIds)
                {

                    await _channelService.SingleDestinationDispatchAsync(singleDestinationDispatch, new System.Collections.ObjectModel.ObservableCollection<FileRegisterDTO> { }, GetServiceHeader());

                    ViewBag.IncomeAdjustmentTypeSelectList = GetIncomeAdjustmentTypeSelectList(singleDestinationDispatch.Carrier.ToString());
                }
                return RedirectToAction("Index");
            }

            else
            {
                var errorMessages = singleDestinationDispatch.ErrorMessages;

                return View("index");
            }

        }


    }
}

