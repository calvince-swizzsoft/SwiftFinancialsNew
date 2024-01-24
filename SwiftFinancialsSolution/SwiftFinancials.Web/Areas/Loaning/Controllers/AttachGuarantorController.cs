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
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.Loaning.Controllers
{
    public class AttachGuarantorController : MasterController
    {

        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public async Task<JsonResult> Index(JQueryDataTablesModel jQueryDataTablesModel, string text, int pageIndex, int pageSize)
        {
            int totalRecordCount = 0;

            int searchRecordCount = 0;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindLoanGuarantorsByFilterInPageAsync(text, pageIndex, pageSize, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<LoanGuarantorDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var loanguarantorsDTO = await _channelService.FindLoanGuarantorAsync(id, GetServiceHeader());

            return View(loanguarantorsDTO);
        }

        public async Task<ActionResult> Create(Guid? id)
        {
            await ServeNavigationMenus();


           
            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            //var loanCase = await _channelService.FindLoanCaseAsync(parseId, GetServiceHeader());
            var loanCases = await _channelService.FindLoanCaseAsync(parseId, GetServiceHeader());
            var customer = await _channelService.FindCustomerAsync(parseId, GetServiceHeader());

            var loanGuarantorsDTO = new LoanGuarantorDTO();

            if (loanCases != null)
            {

                loanGuarantorsDTO.LoaneeCustomerId = loanCases.CustomerId;
                loanGuarantorsDTO.LoanCaseCaseNumber = loanCases.CaseNumber;
                loanGuarantorsDTO.CustomerFullName = loanCases.CustomerFullName;
                loanGuarantorsDTO.LoanCaseAmountApplied = loanCases.AmountApplied;
                loanGuarantorsDTO.CustomerReference1 = loanCases.CustomerReference1;
                loanGuarantorsDTO.CustomerReference2 = loanCases.CustomerReference2;
                loanGuarantorsDTO.CustomerReference3 = loanCases.CustomerReference3;
                loanGuarantorsDTO.LoanCaseLoanPurposeDescription = loanCases.LoanPurposeDescription;
                loanGuarantorsDTO.LoanProductDescription = loanCases.LoanProductDescription;
                loanGuarantorsDTO.StationDescription = loanCases.CustomerStationZoneDivisionEmployerDescription;
                loanGuarantorsDTO.EmployerDescription = loanCases.EmployerName;


                //if (customer != null)
                //{
                //    loanGuarantorsDTO.CustomerId = customer.Id;
                //    loanGuarantorsDTO.CustomerIndividualPayrollNumbers = customer.IndividualPayrollNumbers;
                //    //loanGuarantorsDTO.CommittedShares = customer.
                //}
            }


            if (customer != null)
            {
                loanGuarantorsDTO.CustomerId = customer.Id;
                loanGuarantorsDTO.CustomerIndividualPayrollNumbers = customer.IndividualPayrollNumbers;
                //loanGuarantorsDTO.CommittedShares = customer.com
            }


            return View(loanGuarantorsDTO);
        }


        [HttpPost]
        public async Task<ActionResult> Create(LoanGuarantorDTO loanGuarantorDTO)
        {
            loanGuarantorDTO.ValidateAll();
            Guid sourceCustomerAccountId = loanGuarantorDTO.Id;
            Guid destinationLoanProductId = loanGuarantorDTO.Id;
            int navigatmoduleNavigationItemCode = 0;
           


            if (!loanGuarantorDTO.HasErrors)
            {
                await _channelService.AttachLoanGuarantorsAsync(sourceCustomerAccountId, destinationLoanProductId,  LoanGuarantorDTOs, navigatmoduleNavigationItemCode, GetServiceHeader());

                ViewBag.IncomeAdjustmentTypeSelectList = GetIncomeAdjustmentTypeSelectList(loanGuarantorDTO.Status.ToString());


                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = loanGuarantorDTO.ErrorMessages;

                return View("index");
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(LoanCaseDTO loanCaseDTO)
        {
            await ServeNavigationMenus();
            return View();
        }
    }
}