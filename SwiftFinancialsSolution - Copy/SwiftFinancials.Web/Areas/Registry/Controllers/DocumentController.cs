using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.RegistryModule;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.Registry.Controllers
{
    public class DocumentController : MasterController
    {

        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public async Task<JsonResult> Index(JQueryDataTablesModel jQueryDataTablesModel)
        {
            int totalRecordCount = 0;

            int searchRecordCount = 0;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindCustomerDocumentsByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<DocumentController> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }




        [HttpGet]
        public async Task<ActionResult> GetCustomerDetails(Guid customerId)
        {
            try
            {
                var customer = await _channelService.FindCustomerAsync(customerId, GetServiceHeader());
                if (customer == null)
                {
                    return Json(new { success = false, message = "Customer not found." }, JsonRequestBehavior.AllowGet);
                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        CustomerId = customer.Id,
                        CustomerFullName = customer.FullName,
                        CustomerIndividualIdentityCardNumber = customer.IndividualIdentityCardNumber,
                        CustomerSerialNumber = customer.SerialNumber,
                        Customer = customer.SerialNumber,


                    }
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while fetching the customer details." }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult AssignText(string Remarks, string ZoneId)
        {
            Session["Remarks"] = Remarks;
            Session["ZoneId"] = ZoneId;

            return null;
        }


        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            // Fetch the specific Document by ID using the service
            var customerDocumentDTO = await _channelService.FindCustomerDocumentAsync(id, GetServiceHeader());

            // Check if the Document was found
            if (customerDocumentDTO == null)
            {
                return HttpNotFound();
            }

            // Pass the Document data to the Details view
            return View(customerDocumentDTO);
        }

        [HttpGet]
        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            ViewBag.CustomerDocumentTypeSelectList = GetCustomerDocumentTypeSelectList(string.Empty);

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(CustomerDocumentDTO customerDocumentDTO, HttpPostedFileBase file)
        {
            customerDocumentDTO.ValidateAll();

            if (file != null && file.ContentLength > 0)
            {
                string fileName = Path.GetFileName(file.FileName);
                customerDocumentDTO.FileName = fileName;
            }
            else if (string.IsNullOrWhiteSpace(customerDocumentDTO.FileName))
            {
                ModelState.AddModelError("File", "Please upload a valid file.");
            }

            if (!customerDocumentDTO.HasErrors)
            {
                try
                {
                    await _channelService.AddCustomerDocumentAsync(customerDocumentDTO, GetServiceHeader());
                    TempData["SuccessMessage"] = "Document created successfully!";
                    return RedirectToAction("Index");
                }
                catch (FaultException ex)
                {
                    ModelState.AddModelError("", "A service error occurred: " + ex.Message);
                    Console.WriteLine($"FaultException: {ex.Message}");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An unexpected error occurred. Please try again.");
                    Console.WriteLine($"Exception: {ex.Message}");
                }
            }

            ViewBag.CustomerDocumentTypeSelectList = GetCustomerDocumentTypeSelectList(string.Empty);
            return View(customerDocumentDTO);
        }






        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();
            ViewBag.CustomerDocumentTypeSelectList = GetCustomerDocumentTypeSelectList(string.Empty);
            var customerDocumentDTO = await _channelService.FindCustomerDocumentAsync(id, GetServiceHeader());

            return View(customerDocumentDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, CustomerDocumentDTO CustomerDocumentBindingModel)
        {
            if (ModelState.IsValid)
            {
                ViewBag.CustomerDocumentTypeSelectList = GetCustomerDocumentTypeSelectList(string.Empty);
                await _channelService.UpdateCustomerDocumentAsync(CustomerDocumentBindingModel, GetServiceHeader());

                TempData["SuccessMessage"] = "Document updated successfully!";

                return RedirectToAction("Index");
            }
            else
            {
                return View(CustomerDocumentBindingModel);
            }
        }


        [HttpGet]
        public async Task<JsonResult> GetDepartmentsAsync(Guid id)
        {
            var customerDocumentDTOs = await _channelService.FindCustomerDocumentAsync(id, GetServiceHeader());

            return Json(customerDocumentDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}