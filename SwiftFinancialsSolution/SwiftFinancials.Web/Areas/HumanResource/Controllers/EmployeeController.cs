using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.HumanResourcesModule;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.BackOfficeModule;
using Application.MainBoundedContext.DTO.RegistryModule;
using Infrastructure.Crosscutting.Framework.Utils;
using SwiftFinancials.Web.Areas.Registry.DocumentsModel;



namespace SwiftFinancials.Web.Areas.HumanResource.Controllers
{
    public class EmployeeController : MasterController
    {
        private readonly string _connectionString;
        public EmployeeController()
        {
            // Get connection string from Web.config
            _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;
        }

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

            bool sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc";
            var sortedColumns = jQueryDataTablesModel.GetSortedColumns().Select(s => s.PropertyName).ToList();

            int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;
            int pageSize = jQueryDataTablesModel.iDisplayLength;

            var pageCollectionInfo = await _channelService.FindEmployeesByFilterInPageAsync(
                jQueryDataTablesModel.sSearch,
                0,
                int.MaxValue,
                GetServiceHeader()
            );

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {

                var sortedData = pageCollectionInfo.PageCollection
                    .OrderByDescending(employeeDTO => employeeDTO.CreatedDate)
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
                items: new List<EmployeeDTO>(),
                totalRecords: totalRecordCount,
                totalDisplayRecords: searchRecordCount,
                sEcho: jQueryDataTablesModel.sEcho
                );
        }

        // Get Documents ...........................
        private async Task<List<Document>> GetDocumentsAsync(Guid id)
        {
            var documents = new List<Document>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var query = "SELECT PassportPhoto, SignaturePhoto, IDCardFrontPhoto, IDCardBackPhoto FROM swiftFin_SpecimenCapture WHERE CustomerId = @CustomerId";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@CustomerId", id);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            documents.Add(new Document
                            {
                                PassportPhoto = reader.IsDBNull(0) ? null : (byte[])reader[0],
                                SignaturePhoto = reader.IsDBNull(1) ? null : (byte[])reader[1],
                                IDCardFrontPhoto = reader.IsDBNull(2) ? null : (byte[])reader[2],
                                IDCardBackPhoto = reader.IsDBNull(3) ? null : (byte[])reader[3]
                            });
                        }
                    }
                }
            }

            return documents;
        }


        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();
            ViewBag.BloodGroupSelectList = GetBloodGroupSelectList(string.Empty);

            // Find the employee using the provided ID
            var employeeDTO = await _channelService.FindEmployeeAsync(id, GetServiceHeader());

            if (employeeDTO != null)
            {
                var customerId = employeeDTO.CustomerId;

                // Retrieve associated documents
                var documents = await GetDocumentsAsync(customerId);

                if (documents.Any())
                {
                    var document = documents.First();

                    // Assign document data to TempData for use in the view
                    TempData["PassportPhoto"] = document.PassportPhoto;
                    TempData["SignaturePhoto"] = document.SignaturePhoto;
                    TempData["IDCardFrontPhoto"] = document.IDCardFrontPhoto;
                    TempData["IDCardBackPhoto"] = document.IDCardBackPhoto;

                    // Assign document data to employeeDTO properties
                    employeeDTO.PassportPhoto = document.PassportPhoto;
                    employeeDTO.SignaturePhoto = document.SignaturePhoto;
                    employeeDTO.IDCardFrontPhoto = document.IDCardFrontPhoto;
                    employeeDTO.IDCardBackPhoto = document.IDCardBackPhoto;
                }
                var individualParticular = await _channelService.FindCustomerAsync(customerId, GetServiceHeader());
                ViewBag.IndividualParticulars = individualParticular;

                // Retrieve referees for the customer
                var referees = await _channelService.FindRefereeCollectionByCustomerIdAsync(customerId, GetServiceHeader());

                // Assign referees to ViewBag or employeeDTO
                ViewBag.Referees = referees;

                // Retrive SavingProduct For the customer
                var savingProducts = await _channelService.FindSavingsProductAsync(customerId, GetServiceHeader());
                ViewBag.SavingsProducts = savingProducts;

                //Retrive LoadProducts for the Customer
                var loanAccount = await _channelService.FindLoanProductAsync(customerId, GetServiceHeader());
                ViewBag.LoanAccount = loanAccount;

                //Retrive Investment Accounts 
                var investmentAccounts = await _channelService.FindInvestmentProductAsync(customerId, GetServiceHeader());
                ViewBag.InvestmentAccount = investmentAccounts;

                

                // Return the view with the employee details
                return View(employeeDTO);
            }

            MessageBox.Show(
                "Operation Success: Employee not found.",
                "Employee ErrorPage",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information,
                MessageBoxDefaultButton.Button1,
                MessageBoxOptions.ServiceNotification
            );

            return RedirectToAction("ErrorPage"); // Replace with your error page action
        }




        public async Task<ActionResult> Create(Guid? id)
        {
            await ServeNavigationMenus();

            ViewBag.BloodGroupSelectList = GetBloodGroupSelectList(string.Empty);
            ViewBag.RecordStatusSelectList = GetRecordStatusSelectList(string.Empty);
            ViewBag.CustomerFilterSelectList = GetCustomerFilterSelectList(string.Empty);

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
            
                return View();
            }

            var customer = await _channelService.FindCustomerAsync(parseId, GetServiceHeader());

            EmployeeDTO employeeBindingModel = new EmployeeDTO();

            if (customer != null)
            {
                employeeBindingModel.CustomerId = customer.Id;
                employeeBindingModel.CustomerFullName = customer.FullName;
            }

            return View(employeeBindingModel);
        }

        [HttpPost]
        public async Task<ActionResult> Create(EmployeeDTO employeeBindingModel)
        {
            employeeBindingModel.ValidateAll();

            if (!employeeBindingModel.HasErrors)
            {
                await _channelService.AddEmployeeAsync(employeeBindingModel, GetServiceHeader());
                MessageBox.Show(
                                                             "Operation Success; Employee Added Successful",
                                                             "Success",
                                                             MessageBoxButtons.OK,
                                                             MessageBoxIcon.Information,
                                                             MessageBoxDefaultButton.Button1,
                                                             MessageBoxOptions.ServiceNotification
                                                         );

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = employeeBindingModel.ErrorMessages;
                ViewBag.BloodGroupSelectList = GetBloodGroupSelectList(string.Empty);
                ViewBag.RecordStatusSelectList = GetRecordStatusSelectList(string.Empty);
                ViewBag.CustomerFilterSelectList = GetCustomerFilterSelectList(string.Empty);
                return View(employeeBindingModel);
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();
            ViewBag.BloodGroupSelectList = GetBloodGroupSelectList(string.Empty);
            ViewBag.RecordStatusSelectList = GetRecordStatusSelectList(string.Empty);
            ViewBag.CustomerFilterSelectList = GetCustomerFilterSelectList(string.Empty);

            var employeeDTO = await _channelService.FindEmployeeAsync(id, GetServiceHeader());

            return View(employeeDTO);
        }





        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, EmployeeDTO employeeBindingModel)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateEmployeeAsync(employeeBindingModel, GetServiceHeader());
                MessageBox.Show(
                                                             "Operation Success: Employee Updated",
                                                             "Successs",
                                                             MessageBoxButtons.OK,
                                                             MessageBoxIcon.Information,
                                                             MessageBoxDefaultButton.Button1,
                                                             MessageBoxOptions.ServiceNotification
                                                         );

                return RedirectToAction("Index");
            }
            else
            {
                return View(employeeBindingModel);
            }
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
                        CustomerFullName = customer.FullName,
                        CustomerId = customer.Id,





                    }
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while fetching the customer details." }, JsonRequestBehavior.AllowGet);
            }
        }






        [HttpGet]
        public async Task<JsonResult> GetEmployeesAsync()
        {
            var employeesDTOs = await _channelService.FindEmployeesAsync(GetServiceHeader());

            return Json(employeesDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}