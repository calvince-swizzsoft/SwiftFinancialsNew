using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.BackOfficeModule;
using Application.MainBoundedContext.DTO.RegistryModule;
using Infrastructure.Crosscutting.Framework.Utils;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.Loaning.Controllers
{
    public class GuarantorAttachmentController : MasterController
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


        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var dataCapture = await _channelService.FindDataAttachmentPeriodAsync(id, GetServiceHeader());

            return View(dataCapture);
        }

        public async Task<ActionResult> CustomerAccountLookUp(Guid? id, LoanGuarantorDTO loanGuarantorDTO)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View("create");
            }

            var accounts = await _channelService.FindCustomerAccountAsync(parseId, true, true, true, true, GetServiceHeader());

            if (accounts != null)
            {
                loanGuarantorDTO.CustomerAccountFullAccountNumber = accounts.FullAccountNumber;
                loanGuarantorDTO.CustomerAccountAccountId = accounts.Id;
                loanGuarantorDTO.CustomerAccountAccountStatusDescription = accounts.StatusDescription;
                loanGuarantorDTO.CustomerAccountAccountRemarks = accounts.Remarks;
                loanGuarantorDTO.BookBalance = accounts.BookBalance;
                loanGuarantorDTO.CustomerId = accounts.CustomerId;
                loanGuarantorDTO.CustomerIndividualFirstName = accounts.CustomerFullName;
                loanGuarantorDTO.CustomerAccounntCustomerTypeDescription = accounts.CustomerTypeDescription;
                loanGuarantorDTO.CustomerIndividualPayrollNumbers = accounts.CustomerIndividualPayrollNumbers;
                loanGuarantorDTO.CustomerPersonalIdentificationNumber = accounts.CustomerPersonalIdentificationNumber;
                loanGuarantorDTO.CustomerReference1 = accounts.CustomerReference1;
                loanGuarantorDTO.CustomerReference2 = accounts.CustomerReference2;
                loanGuarantorDTO.CustomerReference3 = accounts.CustomerReference3;


                // Find Guarantors

                var Guarantors = await _channelService.FindLoanGuarantorsByCustomerIdAsync(accounts.CustomerId, GetServiceHeader());
                if (Guarantors != null)
                {
                    ViewBag.LoanGuarantors = Guarantors;

                    List<LoanGuarantorDTO> Cases = new List<LoanGuarantorDTO>();
                    foreach (var gC in Guarantors)
                    {
                        Cases.Add(gC);
                    }
                }
                else
                {
                    TempData["NullGuarantors"] = "No guarantors attached for the specified customer";
                }
            }

            return View("Create", loanGuarantorDTO);
        }



        public async Task<ActionResult> Create(Guid? id, LoanGuarantorDTO loanGuarantorDTO)
        {
            await ServeNavigationMenus();

            ViewBag.MonthSelectList = GetMonthsAsync(string.Empty);

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

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
