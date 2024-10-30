using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.FrontOfficeModule;
using Infrastructure.Crosscutting.Framework.Utils;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System.Diagnostics;
using System.Web;
using System.IO;




namespace SwiftFinancials.Web.Areas.FrontOffice.Controllers
{
    public class AutomatedClearingController : MasterController
    {

        public async Task<ActionResult> Index(string status, DateTime? startDate, DateTime? endDate, string searchText, int pageIndex = 1, int pageSize = 10)
        {
            await ServeNavigationMenus();

            int parsedStatus = int.TryParse(status, out parsedStatus) ? parsedStatus : 0; // Default status if parsing fails

            // Get or initialize the service header
            var serviceHeader = GetServiceHeader();

            // Ensure serviceHeader is not null
            if (serviceHeader == null)
            {
                Debug.WriteLine("Service header is null");
                return new HttpStatusCodeResult(500, "Service header not found");
            }

            // Call the service with the serviceHeader
            var result = await _channelService.FindElectronicJournalsByFilterInPageAsync(
                parsedStatus,
                startDate ?? DateTime.MinValue,
                endDate ?? DateTime.MaxValue,
                searchText,
                pageIndex,
                pageSize,
                serviceHeader
            );

            return View(); // Assuming Items is the list of journals
        }

        [HttpPost]
        public async Task<JsonResult> UploadElectronicJournal(HttpPostedFileBase chequeImage)
        {
            if (chequeImage == null || chequeImage.ContentLength == 0)
            {
                return Json(new { success = false, message = "Please select a file to upload." });
            }

            string fileName = Path.GetFileName(chequeImage.FileName);

            try
            {
                // Call the parsing method
                ElectronicJournalDTO result = await _channelService.ParseElectronicJournalImportAsync(fileName, GetServiceHeader());

                // Assuming you need to return this data to the client
                return Json(new { success = true, chequeData = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }




        public async Task<ActionResult> Processing(string status, DateTime? startDate, DateTime? endDate, string searchText, int pageIndex = 1, int pageSize = 10)
        {
            await ServeNavigationMenus();


            int parsedStatus = int.TryParse(status, out parsedStatus) ? parsedStatus : 0; // Default status if parsing fails

            // Get or initialize the service header
            var serviceHeader = GetServiceHeader();

            // Ensure serviceHeader is not null
            if (serviceHeader == null)
            {
                Debug.WriteLine("Service header is null");
                return new HttpStatusCodeResult(500, "Service header not found");
            }

            // Call the service with the serviceHeader
            var result = await _channelService.FindElectronicJournalsByFilterInPageAsync(
                parsedStatus,
                startDate ?? DateTime.MinValue,
                endDate ?? DateTime.MaxValue,
                searchText,
                pageIndex,
                pageSize,
                serviceHeader
            );

            return View(); // Assuming Items is the list of journals
        }















    }
}