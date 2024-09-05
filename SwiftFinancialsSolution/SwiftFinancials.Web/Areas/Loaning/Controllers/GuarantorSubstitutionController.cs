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
    public class GuarantorSubstitutionController : MasterController
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


                Session["customer"] = loanGuarantorDTO;

                JQueryDataTablesModel jQDtTble = new JQueryDataTablesModel
                {
                    sEcho = 1
                };

                await FindCustomerGuarantors(jQDtTble);
                await FindSavingsAccountsByProductCode(jQDtTble);
                await FindLoanAccountsByProductCode(jQDtTble);
                await FindInvestmentsAccountsByProductCode(jQDtTble);
            }

            return View("Create", loanGuarantorDTO);
        }



        [HttpPost]
        public async Task<ActionResult> FindCustomerGuarantors(JQueryDataTablesModel jQueryDataTablesModel)
        {
            Guid CustomerId = Guid.Empty;

            if (Session["CustomerId"] != null)
            {
                CustomerId = (Guid)Session["CustomerId"];
            }

            int totalRecordCount = 0;

            int searchRecordCount = 0;

            //var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindLoanGuarantorsByCustomerIdAndFilterInPageAsync(CustomerId, jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(guarantors => guarantors.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;


                List<LoanGuarantorDTO> myPageInfo = new List<LoanGuarantorDTO>();
                foreach (var items in pageCollectionInfo.PageCollection)
                {
                    myPageInfo.Add(items);
                }
                ViewBag.LoanGuarantors = myPageInfo;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<LoanGuarantorDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }


        /// <summary>
        ///         Savings Accounts
        /// </summary>
        /// <param name="jQueryDataTablesModel"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> FindSavingsAccountsByProductCode(JQueryDataTablesModel jQueryDataTablesModel)
        {
            int totalRecordCount = 0;

            int searchRecordCount = 0;

            //var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindCustomerAccountsByProductCodeAndFilterInPageAsync((int)ProductCode.Savings, jQueryDataTablesModel.sSearch,
                (int)CustomerFilter.FirstName, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, true, true, true, true, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(guarantors => guarantors.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;


                List<CustomerAccountDTO> myPageInfo = new List<CustomerAccountDTO>();
                foreach (var items in pageCollectionInfo.PageCollection)
                {
                    myPageInfo.Add(items);
                }
                ViewBag.SavingsAccounts = myPageInfo;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<CustomerAccountDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }


        /// <summary>
        ///             Loan Accounts
        /// </summary>
        /// <param name="jQueryDataTablesModel"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> FindLoanAccountsByProductCode(JQueryDataTablesModel jQueryDataTablesModel)
        {
            int totalRecordCount = 0;

            int searchRecordCount = 0;

            //var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindCustomerAccountsByProductCodeAndFilterInPageAsync((int)ProductCode.Loan, jQueryDataTablesModel.sSearch,
                (int)CustomerFilter.FirstName, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, true, true, true, true, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(guarantors => guarantors.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;


                List<CustomerAccountDTO> myPageInfo = new List<CustomerAccountDTO>();
                foreach (var items in pageCollectionInfo.PageCollection)
                {
                    myPageInfo.Add(items);
                }
                ViewBag.LoanAccounts = myPageInfo;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<CustomerAccountDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }


        /// <summary>
        ///             Investments Accounts
        /// </summary>
        /// <param name="jQueryDataTablesModel"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> FindInvestmentsAccountsByProductCode(JQueryDataTablesModel jQueryDataTablesModel)
        {
            int totalRecordCount = 0;

            int searchRecordCount = 0;

            //var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindCustomerAccountsByProductCodeAndFilterInPageAsync((int)ProductCode.Investment, jQueryDataTablesModel.sSearch,
                (int)CustomerFilter.FirstName, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, true, true, true, true, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(guarantors => guarantors.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;


                List<CustomerAccountDTO> myPageInfo = new List<CustomerAccountDTO>();
                foreach (var items in pageCollectionInfo.PageCollection)
                {
                    myPageInfo.Add(items);
                }
                ViewBag.InvestmentsAccounts = myPageInfo;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<CustomerAccountDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
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


            if (Session["customer"]!=null)
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


                Session["guarantor"] = loanGuarantorDTO;
            }

            return View("Create", loanGuarantorDTO);
        }







        public async Task<ActionResult> Create(Guid? id, LoanGuarantorDTO loanGuarantorDTO)
        {
            await ServeNavigationMenus();

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
