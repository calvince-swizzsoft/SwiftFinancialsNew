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
    public class ReceiveController : MasterController
    {

        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();
            int totalRecordCount = 0;

            ViewBag.recordStatus = GetRecordStatusSelectList(string.Empty);
            var pageCollectionInfo = await _channelService.FindFileRegistersByFilterInPageAsync("", 1, int.MaxValue, int.MaxValue, GetServiceHeader());

            var sortedData = pageCollectionInfo.PageCollection
                        .OrderByDescending(FileRegisterDTO => FileRegisterDTO.CreatedDate)
                        .ToList();

            totalRecordCount = sortedData.Count;
            ViewBag.fileregister = pageCollectionInfo;

            return View();
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
                // FileMovementId = fileMovement.Id,
                FileRegisterId = fileMovement.FileRegisterId,
                FileRegisterCustomerId = fileMovement.FileRegisterCustomerId,
                FileRegisterCustomerIndividualFirstName = fileMovement.FileRegisterCustomerIndividualFirstName,
                SourceDepartmentId = fileMovement.SourceDepartmentId,
                SourceDepartmentDescription = fileMovement.SourceDepartmentDescription,
                DestinationDepartmentId = fileMovement.DestinationDepartmentId,
                DestinationDepartmentDescription = fileMovement.DestinationDepartmentDescription,
                // DispatchDate = fileMovement.DispatchDate,
                //ExpectedReturnDate = fileMovement.ExpectedReturnDate,
                Remarks = fileMovement.Remarks
            };

            return View("Receive", receiveModel);
        }

        #region
        //[HttpPost]



        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<ActionResult> ReceiveFile(FileMovementHistoryDTO model)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        try
        //        {
        //            // Call service to update file movement as received
        //            var result = await _channelService.ReceiveDispatchedFileAsync(model.FileMovementId, GetServiceHeader());

        //            if (result)
        //            {
        //                TempData["SuccessMessage"] = "File received successfully!";
        //                return RedirectToAction("Index");
        //            }
        //            else
        //            {
        //                ModelState.AddModelError("", "Failed to receive file. Please try again.");
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            ModelState.AddModelError("", $"An error occurred: {ex.Message}");
        //        }
        //    }

        //    await ServeNavigationMenus();
        //    return View("Receive", model);
        //}

        //public async Task<ActionResult> FileHistory(Guid fileRegisterId)
        //{
        //    await ServeNavigationMenus();
        //    var history = await _channelService.FindFileMovementHistoryByFileRegisterIdAsync(fileRegisterId, GetServiceHeader());
        //    return PartialView("_FileHistory", history);
        //}

        //public async Task<ActionResult> GetDispatchedFiles(Guid customerId)
        //{
        //    var dispatchedFiles = await _channelService.FindDispatchedFilesByCustomerIdAsync(customerId, GetServiceHeader());
        //    return Json(dispatchedFiles.Select(f => new
        //    {
        //        f.Id,
        //        f.FileRegisterId,
        //        f.FileRegisterCustomerIndividualFirstName,
        //        f.SourceDepartmentDescription,
        //        f.DestinationDepartmentDescription,
        //        f.DispatchDate,
        //        f.ExpectedReturnDate,
        //        f.Remarks
        //    }), JsonRequestBehavior.AllowGet);
        //}
        #endregion
    }
}