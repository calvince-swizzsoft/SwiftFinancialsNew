using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Windows.Forms;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.BackOfficeModule;
using Application.MainBoundedContext.DTO.RegistryModule;
using Infrastructure.Crosscutting.Framework.Utils;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.Loaning.Controllers
{
    public class GuarantorSubstitutionController : MasterController
    {
        string connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;

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

            var pageCollectionInfo = await _channelService.FindLoanCasesByFilterInPageAsync(jQueryDataTablesModel.sSearch, 10, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, true, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(LoanCase => LoanCase.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<LoanCaseDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }


        //// Get Savings Account
        //private async Task<List<SelectListItem>> GetSavingsAccountAsync()
        //{
        //    await ServeNavigationMenus();

        //    var savingsAccount = new List<SelectListItem>();

        //    using (SqlConnection conn = new SqlConnection(connectionString))
        //    {
        //        await conn.OpenAsync();
        //        var query = "SELECT CategoryID, CategoryName FROM ReportCategories Order By CategoryName ASC";
        //        using (SqlCommand cmd = new SqlCommand(query, conn))
        //        {
        //            using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
        //            {
        //                while (await reader.ReadAsync())
        //                {
        //                    savingsAccount.Add(new CustomerAccountDTO
        //                    {
        //                        Value = reader["CategoryID"].ToString(),
        //                        Text = reader["CategoryName"].ToString()
        //                    });
        //                }
        //            }
        //        }
        //    }

        //    return categories;
        //}


        //// Get Investments Account
        //private async Task<List<SelectListItem>> GetInvestmentsAsync()
        //{
        //    await ServeNavigationMenus();

        //    var categories = new List<SelectListItem>();

        //    using (SqlConnection conn = new SqlConnection(connectionString))
        //    {
        //        await conn.OpenAsync();
        //        var query = "SELECT CategoryID, CategoryName FROM ReportCategories Order By CategoryName ASC";
        //        using (SqlCommand cmd = new SqlCommand(query, conn))
        //        {
        //            using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
        //            {
        //                while (await reader.ReadAsync())
        //                {
        //                    categories.Add(new SelectListItem
        //                    {
        //                        Value = reader["CategoryID"].ToString(),
        //                        Text = reader["CategoryName"].ToString()
        //                    });
        //                }
        //            }
        //        }
        //    }

        //    return categories;
        //}



        //// Get Loans Account
        //private async Task<List<SelectListItem>> GetCategoriesAsync()
        //{
        //    await ServeNavigationMenus();

        //    var categories = new List<SelectListItem>();

        //    using (SqlConnection conn = new SqlConnection(connectionString))
        //    {
        //        await conn.OpenAsync();
        //        var query = "SELECT CategoryID, CategoryName FROM ReportCategories Order By CategoryName ASC";
        //        using (SqlCommand cmd = new SqlCommand(query, conn))
        //        {
        //            using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
        //            {
        //                while (await reader.ReadAsync())
        //                {
        //                    categories.Add(new SelectListItem
        //                    {
        //                        Value = reader["CategoryID"].ToString(),
        //                        Text = reader["CategoryName"].ToString()
        //                    });
        //                }
        //            }
        //        }
        //    }

        //    return categories;
        //}


        public async Task<ActionResult> CustomerLookUp(Guid? id, LoanGuarantorDTO loanGuarantorDTO)
        {
            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View("create");
            }

            Session["CustomerId"] = parseId;


            if (Session["guarantor"] != null)
            {
                loanGuarantorDTO = Session["guarantor"] as LoanGuarantorDTO;
            }


            var customer = await _channelService.FindCustomerAsync(parseId, GetServiceHeader());
            if (customer != null)
            {
                loanGuarantorDTO.LoaneeCustomerId = customer.Id;
                loanGuarantorDTO.LoaneeCustomerIndividualFirstName = customer.IndividualSalutationDescription + " " + customer.IndividualFirstName + " " + customer.IndividualLastName;
                loanGuarantorDTO.CustomerNonIndividualDescription = customer.TypeDescription;
                loanGuarantorDTO.EmployerDescription = customer.StationZoneDivisionEmployerDescription;
                loanGuarantorDTO.EmployerId = customer.StationZoneDivisionEmployerId;
                loanGuarantorDTO.StationDescription = customer.StationDescription;
                loanGuarantorDTO.StationId = customer.StationId;
                loanGuarantorDTO.CustomerPersonalIdentificationNumber = customer.PersonalIdentificationNumber;
                loanGuarantorDTO.CustomerReference1 = customer.Reference1;
                loanGuarantorDTO.CustomerReference2 = customer.Reference2;
                loanGuarantorDTO.CustomerReference3 = customer.Reference3;


                var productCodes = new int[] { (int)ProductCode.Savings, (int)ProductCode.Investment, (int)ProductCode.Loan };
                var findAccountsbyCode = await _channelService.FindCustomerAccountsByCustomerIdAndProductCodesAsync((Guid)loanGuarantorDTO.LoaneeCustomerId, productCodes, true, true, true, false, GetServiceHeader());

                // savings Product
                var savingsAccounts = findAccountsbyCode.Where(account => account.CustomerAccountTypeProductCode == (int)ProductCode.Savings).ToList();

                // investments Product
                var investmentsAccounts = findAccountsbyCode.Where(account => account.CustomerAccountTypeProductCode == (int)ProductCode.Investment).ToList();

                // loans Product
                var loansAccounts = findAccountsbyCode.Where(account => account.CustomerAccountTypeProductCode == (int)ProductCode.Loan).ToList();


                // Get Loan

                // Loan Applications
                //var loanApplications = await LoanApplications();

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        LoaneeCustomerId = loanGuarantorDTO.LoaneeCustomerId,
                        LoaneeCustomerIndividualFirstName = loanGuarantorDTO.LoaneeCustomerIndividualFirstName,
                        CustomerNonIndividualDescription = loanGuarantorDTO.CustomerNonIndividualDescription,
                        EmployerDescription = loanGuarantorDTO.EmployerDescription,
                        EmployerId = loanGuarantorDTO.EmployerId,
                        StationDescription = loanGuarantorDTO.StationDescription,
                        StationId = loanGuarantorDTO.StationId,
                        CustomerPersonalIdentificationNumber = loanGuarantorDTO.CustomerPersonalIdentificationNumber,
                        CustomerReference1 = loanGuarantorDTO.CustomerReference1,
                        CustomerReference2 = loanGuarantorDTO.CustomerReference2,
                        CustomerReference3 = loanGuarantorDTO.CustomerReference3,
                        SavingsAccounts = savingsAccounts,
                        InvestmentsAccounts = investmentsAccounts,
                        LoansAccounts = loansAccounts
                    }
                });
            }

            MessageBox.Show(Form.ActiveForm, "Customer not found!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
            return Json(new { success = false, message = string.Empty });
        }


        [HttpPost]
        public async Task<ActionResult> LoanApplications(Guid? loaneeId)
        {

            //await _channelService.FindLoanGuarantorsByLoaneeCustomerIdAndLoanProductIdAsync();

            return RedirectToAction("Create");
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var dataCapture = await _channelService.FindDataAttachmentPeriodAsync(id, GetServiceHeader());

            return View(dataCapture);
        }





        public async Task<ActionResult> GuarantorLookUp(Guid? id, LoanGuarantorDTO loanGuarantorDTO)
        {
            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View("create");
            }


            if (Session["customer"] != null)
            {
                loanGuarantorDTO = Session["customer"] as LoanGuarantorDTO;
            }

            var customer = await _channelService.FindCustomerAsync(parseId, GetServiceHeader());
            if (customer != null)
            {
                loanGuarantorDTO.GuarantorId = customer.Id;
                loanGuarantorDTO.GuarantorFullName = customer.IndividualSalutationDescription + " " + customer.IndividualFirstName + " " + customer.IndividualLastName;
                loanGuarantorDTO.GuarantorTypeDescription = customer.TypeDescription;
                loanGuarantorDTO.GuarantorEmployerDescription = customer.StationZoneDivisionEmployerDescription;
                loanGuarantorDTO.GuarantorEmployerId = customer.StationZoneDivisionEmployerId;
                loanGuarantorDTO.GuarantorStationDescription = customer.StationDescription;
                loanGuarantorDTO.GuarantorStationId = customer.StationId;
                loanGuarantorDTO.GuarantorIdentificationNumber = customer.PersonalIdentificationNumber;
                loanGuarantorDTO.GuarantorRef1 = customer.Reference1;
                loanGuarantorDTO.GuarantorRef2 = customer.Reference2;
                loanGuarantorDTO.GuarantorRef3 = customer.Reference3;

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        GuarantorId = loanGuarantorDTO.GuarantorId,
                        GuarantorFullName = loanGuarantorDTO.GuarantorFullName,
                        GuarantorTypeDescription = loanGuarantorDTO.GuarantorTypeDescription,
                        GuarantorEmployerDescription = loanGuarantorDTO.GuarantorEmployerDescription,
                        GuarantorEmployerId = loanGuarantorDTO.GuarantorEmployerId,
                        GuarantorStationDescription = loanGuarantorDTO.GuarantorStationDescription,
                        GuarantorStationId = loanGuarantorDTO.GuarantorStationId,
                        GuarantorIdentificationNumber = loanGuarantorDTO.GuarantorIdentificationNumber,
                        GuarantorRef1 = loanGuarantorDTO.GuarantorRef1,
                        GuarantorRef2 = loanGuarantorDTO.GuarantorRef2,
                        GuarantorRef3 = loanGuarantorDTO.GuarantorRef3
                    }
                });
            }

            MessageBox.Show(Form.ActiveForm, "Customer not found!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
            return Json(new { success = false, message = string.Empty });
        }



        public async Task<ActionResult> Create(Guid? id, LoanGuarantorDTO loanGuarantorDTO)
        {
            await ServeNavigationMenus();

            ViewBag.CustomerFilter = GetCustomerFilterSelectList(string.Empty);
            ViewBag.RecordStatus = GetRecordStatusSelectList(string.Empty);
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(LoanGuarantorDTO loanGuarantorDTO)
        {

            loanGuarantorDTO.ValidateAll();

            if (!loanGuarantorDTO.HasErrors)
            {
                //await _channelService.AttachLoanGuarantorsAsync()

                TempData["message"] = "Successfully created Data Period";

                return RedirectToAction("Index");
            }
            else
            {
                //var errorMessages = dataPeriodDTO.ErrorMessages.ToString();

                //TempData["BugdetBalance"] = errorMessages;

                TempData["messageError"] = "Could not create Data Period";

                //ViewBag.MonthSelectList = GetMonthsAsync(dataPeriodDTO.MonthDescription);

                await ServeNavigationMenus();

                return View();
            }
        }
    }
}
