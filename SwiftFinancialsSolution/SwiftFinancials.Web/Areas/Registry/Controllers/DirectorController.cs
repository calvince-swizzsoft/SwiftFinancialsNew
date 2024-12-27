using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.RegistryModule;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.Registry.Controllers
{
    public class DirectorController : MasterController
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

            var pageCollectionInfo = await _channelService.FindDirectorsByFilterInPageAsync(jQueryDataTablesModel.sSearch, 0, int.MaxValue, GetServiceHeader());
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

            return this.DataTablesJson(
                items: new List<DirectorDTO>(),
                totalRecords: totalRecordCount,
                totalDisplayRecords: searchRecordCount,
                sEcho: jQueryDataTablesModel.sEcho
        );
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
                        DivisionId = customer.StationZoneDivisionId,
                        DivisionDescription = customer.StationZoneDivisionDescription,
                        
                    }
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while fetching the customer details." }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult AssignText(string Remarks, string DivisionId)
        {
            Session["Remarks"] = Remarks;
            Session["DivisionId"] = DivisionId;

            return null;
        }


        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            // Fetch the specific director by ID using the service
            var directorDTO = await _channelService.FindDirectorAsync(id, GetServiceHeader());

            // Check if the director was found
            if (directorDTO == null)
            {
                return HttpNotFound();
            }

            // Pass the director data to the Details view
            return View(directorDTO);
        }

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(DirectorDTO directorDTO)
        {
            directorDTO.ValidateAll();

            if (!directorDTO.HasErrors)
            {
                await _channelService.AddDirectorAsync(directorDTO, GetServiceHeader());
                System.Windows.Forms.MessageBox.Show(
                         "Director " + directorDTO.CustomerFullName + " Added successfully.",
                         "Success",
                         System.Windows.Forms.MessageBoxButtons.OK,
                         System.Windows.Forms.MessageBoxIcon.Information,
                         System.Windows.Forms.MessageBoxDefaultButton.Button1,
                         System.Windows.Forms.MessageBoxOptions.ServiceNotification
                     );
                TempData["SuccessMessage"] = "Director created successfully!";

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = directorDTO.ErrorMessages;

                return View(directorDTO);
            }
        }


        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var directorDTO = await _channelService.FindDirectorAsync(id, GetServiceHeader());

            return View(directorDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, DirectorDTO BindingModelBase)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateDirectorAsync(BindingModelBase, GetServiceHeader());

                TempData["SuccessMessage"] = "Director updated successfully!";

                return RedirectToAction("Index");
            }
            else
            {
                return View(BindingModelBase);
            }
        }

        [HttpGet]
        public async Task<ActionResult> GetDivisionDetails(Guid DivisonId)
        {
            try
            {
                var Division = await _channelService.FindCustomerAccountAsync(DivisonId, includeBalances: true);
                if (Division == null)
                {
                    return Json(new { success = false, message = "Division not found." }, JsonRequestBehavior.AllowGet);
                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        DivisionId = Division.Id,
                        //customerAccountFullAccountNumber = customerAccount.FullAccountNumber,
                        //availableBalance = customerAccount.AvailableBalance,
                        //accountId = customerAccount.CustomerId,
                        //DebitCustomerAccountCustomerAccountTypeProductCode = customerAccount.CustomerAccountTypeProductCode,
                        //DebitCustomerAccountCustomerAccountTypeTargetProductId = customerAccount.CustomerAccountTypeTargetProductId,
                    }
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while fetching the customer account details." }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetDepartmentsAsync(Guid id)
        {
            var directorsDTOs = await _channelService.FindDirectorAsync(id, GetServiceHeader());

            return Json(directorsDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}