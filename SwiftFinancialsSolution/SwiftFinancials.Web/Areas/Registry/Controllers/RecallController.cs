using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.BackOfficeModule;
using Application.MainBoundedContext.DTO.RegistryModule;
using SwiftFinancials.Presentation.Infrastructure.Models;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.Registry.Controllers
{
    public class RecallController : MasterController
    {
        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();
            ViewBag.recordStatus = GetRecordStatusSelectList(string.Empty);
            ViewBag.customerFilter = GetCustomerFilterSelectList(string.Empty);
            ViewBag.CustomerTypeSelectList = GetCustomerTypeSelectList(string.Empty);

            return View();
        }

        [HttpPost]
        public async Task<JsonResult> Index(JQueryDataTablesModel jQueryDataTablesModel, int? recordStatus, int? customerFilter, int? customerType, string filterValue)
        {
            int totalRecordCount = 0;

            int searchRecordCount = 0;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindFileRegistersByFilterInPageAsync(jQueryDataTablesModel.sSearch, (int)customerFilter, int.MaxValue, int.MaxValue, GetServiceHeader());


            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {

                var sortedData = pageCollectionInfo.PageCollection
                    .OrderByDescending(loanCase => loanCase.CreatedDate)
                    .ToList();

                totalRecordCount = sortedData.Count;

                var paginatedData = sortedData
                    .Skip(jQueryDataTablesModel.iDisplayStart)
                    .Take(jQueryDataTablesModel.iDisplayLength)
                    .ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch)
                    ? sortedData.Count
                    : totalRecordCount;

                return this.DataTablesJson(
                    items: paginatedData,
                    totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount,
                    sEcho: jQueryDataTablesModel.sEcho
                );
            }

            else return this.DataTablesJson(
                 items: new List<WithdrawalNotificationDTO>(),
                 totalRecords: totalRecordCount,
                 totalDisplayRecords: searchRecordCount,
                 sEcho: jQueryDataTablesModel.sEcho
         );
        }

        public async Task<ActionResult> Search(Guid? id)
        {
            await ServeNavigationMenus();
            ViewBag.recordStatus = GetRecordStatusSelectList(string.Empty);
            ViewBag.customerFilter = GetCustomerFilterSelectList(string.Empty);
            ViewBag.CustomerTypeSelectList = GetCustomerTypeSelectList(string.Empty);

            var customers = await _channelService.FindCustomersAsync(GetServiceHeader());
            ViewBag.customers = customers;

            if (id == null || id == Guid.Empty)
            {
                return View();
            }

            FileMovementHistoryDTO fileMovementHistoryDTO = new FileMovementHistoryDTO();
            var fileMovement = fileMovementHistoryDTO;
            if (fileMovement == null)
            {
                return HttpNotFound();
            }

            var receiveModel = new FileMovementHistoryDTO
            {
                Id = fileMovement.Id,
                FileRegisterId = fileMovement.FileRegisterId,
                FileRegisterCustomerId = fileMovement.FileRegisterCustomerId,
                FileRegisterCustomerIndividualFirstName = fileMovement.FileRegisterCustomerIndividualFirstName,
                SourceDepartmentId = fileMovement.SourceDepartmentId,
                SourceDepartmentDescription = fileMovement.SourceDepartmentDescription,
                DestinationDepartmentId = fileMovement.DestinationDepartmentId,
                DestinationDepartmentDescription = fileMovement.DestinationDepartmentDescription,
                Remarks = fileMovement.Remarks
            };

            return View("Receive", receiveModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RecallFile(FileRegisterDTO request)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    ObservableCollection<FileRegisterDTO> fileRegisterDTOs = new ObservableCollection<FileRegisterDTO>();
                    fileRegisterDTOs.Add(request);
                    var currentStatus = await _channelService.RecallFilesAsync(fileRegisterDTOs, GetServiceHeader());



                    if (currentStatus==true)
                    {
                        TempData["SuccessMessage"] = "File recall request submitted successfully!";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        ModelState.AddModelError("", "Failed to initiate file recall. Please try again.");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"An error occurred: {ex.Message}");
                }
            }

            await ServeNavigationMenus();
            return View("Receive");
        }

        //public async Task<ActionResult> GetRecallEligibleFiles(Guid customerId)
        //{
        //    try
        //    {
        //        var dispatchedFiles = await _channelService.file(customerId, GetServiceHeader());

        //        var recallEligibleFiles = dispatchedFiles
        //            .Where(f => f.Status == "Dispatched")
        //            .Select(f => new
        //            {
        //                f.Id,
        //                f.FileRegisterId,
        //                CustomerName = f.FileRegisterCustomerIndividualFirstName + " " + f.FileRegisterCustomerIndividualLastName,
        //                f.SourceDepartmentDescription,
        //                f.DestinationDepartmentDescription,
        //                f.DispatchDate,
        //                f.ExpectedReturnDate,
        //                f.Remarks,
        //                IsRecallEligible = true
        //            });

        //        return Json(recallEligibleFiles, JsonRequestBehavior.AllowGet);
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
        //    }
        //}

        //public async Task<ActionResult> FileRecallHistory(Guid fileRegisterId)
        //{
        //    await ServeNavigationMenus();
        //    var history = await _channelService.FindFileRecallHistoryByFileRegisterIdAsync(fileRegisterId, GetServiceHeader());
        //    return PartialView("_FileRecallHistory", history);
        //}
    }

}