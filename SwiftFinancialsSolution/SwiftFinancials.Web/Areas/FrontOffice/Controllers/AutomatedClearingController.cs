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
        public async Task<ActionResult> UploadCheques(HttpPostedFileBase chequeImage, string status, string startDate, string endDate, string searchText)
        {
            if (chequeImage != null && chequeImage.ContentLength > 0)
            {
                try
                {
                    // Logic for saving the file
                    string uploadDirectory = Server.MapPath("~/UploadedCheques");
                    if (!Directory.Exists(uploadDirectory))
                    {
                        Directory.CreateDirectory(uploadDirectory);
                    }

                    string uniqueFileName = Path.GetFileNameWithoutExtension(chequeImage.FileName) + "_" + Guid.NewGuid() + Path.GetExtension(chequeImage.FileName);
                    string path = Path.Combine(uploadDirectory, uniqueFileName);
                    chequeImage.SaveAs(path);

                    await Task.Run(() =>
                    {
                    });

                    var chequeData = new List<object>
            {
                new {
                    Status = status, // Use the status sent by the user
                    ProcessedEntries = 0, // Set to 0 or any default value since we're not processing
                    FileName = uniqueFileName,
                    FileSerialNumber = string.Empty, // Set to an appropriate default if needed
                    DateOfFileExchange = DateTime.Now.ToString("dd/MM/yyyy"), // Set current date
                    ClosedBy = string.Empty, // Set to an appropriate default if needed
                    ClosedDate = string.Empty, // Set to an appropriate default if needed
                    CreatedBy = string.Empty, // Set to an appropriate default if needed
                    CreatedDate = DateTime.Now.ToString("dd/MM/yyyy") // Set current date
                }
            };

                    // Return success and the data for table population
                    return Json(new { success = true, chequeData = chequeData });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "Error: " + ex.Message });
                }
            }

            return Json(new { success = false, message = "No file uploaded." });
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