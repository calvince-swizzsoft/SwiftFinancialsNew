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



        public async Task<ActionResult> CustomerLookUp(Guid? id, LoanCaseDTO loanCaseDTO)
        {
            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View("create");
            }

            var customer = await _channelService.FindCustomerAsync(parseId, GetServiceHeader());
            if (customer != null)
            {
                loanCaseDTO.CustomerId = customer.Id;
                loanCaseDTO.CustomerIndividualFirstName = customer.FullName;
                loanCaseDTO.CustomerNonIndividualDescription = customer.TypeDescription;
                loanCaseDTO.CustomerStationZoneDivisionEmployerDescription = customer.StationZoneDivisionEmployerDescription;
                loanCaseDTO.CustomerStation = customer.StationDescription;
                loanCaseDTO.CustomerPersonalIdentificationNumber = customer.PersonalIdentificationNumber;
                loanCaseDTO.CustomerReference1 = customer.Reference1;
                loanCaseDTO.CustomerReference2 = customer.Reference2;
                loanCaseDTO.CustomerReference3 = customer.Reference3;
                loanCaseDTO.Remarks = customer.Remarks;


                var productCodes = new int[] { (int)ProductCode.Savings, (int)ProductCode.Investment, (int)ProductCode.Loan };
                var findAccountsbyCode = await _channelService.FindCustomerAccountsByCustomerIdAndProductCodesAsync((Guid)loanCaseDTO.CustomerId, productCodes, true, true, true, false, GetServiceHeader());

                // savings Product
                var savingsAccounts = findAccountsbyCode.Where(account => account.CustomerAccountTypeProductCode == (int)ProductCode.Savings).ToList();

                // investments Product
                var investmentsAccounts = findAccountsbyCode.Where(account => account.CustomerAccountTypeProductCode == (int)ProductCode.Investment).ToList();

                // loans Product
                var loansAccounts = findAccountsbyCode.Where(account => account.CustomerAccountTypeProductCode == (int)ProductCode.Loan).ToList();

                // Loans Guaranteed
                var loansGuaranteed = await _channelService.FindLoanGuarantorsByCustomerIdAsync(parseId, GetServiceHeader());

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        CustomerId = loanCaseDTO.CustomerId,
                        CustomerIndividualFirstName = loanCaseDTO.CustomerIndividualFirstName,
                        CustomerNonIndividualDescription = loanCaseDTO.CustomerNonIndividualDescription,
                        CustomerStationZoneDivisionEmployerDescription = loanCaseDTO.CustomerStationZoneDivisionEmployerDescription,
                        CustomerStation = loanCaseDTO.CustomerStation,
                        CustomerPersonalIdentificationNumber = loanCaseDTO.CustomerPersonalIdentificationNumber,
                        CustomerReference1 = loanCaseDTO.CustomerReference1,
                        CustomerReference2 = loanCaseDTO.CustomerReference2,
                        CustomerReference3 = loanCaseDTO.CustomerReference3,
                        Remarks = loanCaseDTO.Remarks,

                        LoansGuaranteed = loansGuaranteed,
                        SavingsAccounts = savingsAccounts,
                        InvestmentsAccounts = investmentsAccounts,
                        LoansAccounts = loansAccounts
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
                return RedirectToAction("Index");
            }
            else
            {
                await ServeNavigationMenus();

                return View();
            }
        }
    }
}
