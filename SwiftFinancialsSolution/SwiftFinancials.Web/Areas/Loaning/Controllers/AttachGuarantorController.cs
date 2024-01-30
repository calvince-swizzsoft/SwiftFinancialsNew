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

        public string customer_FullName { get; set; }
        public int Loan_CaseNumber { get; set; }
        public decimal Loan_CaseAmountApplied { get; set; }
        public string customer_Reference1 { get; set; }
        public string customer_Reference2 { get; set; }


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


            }

            if (customer != null)
            {
                if (loanGuarantorsDTO.CustomerId == loanGuarantorsDTO.LoaneeCustomerId)
                {
                    TempData["AlertMessage"] = "Loanee cannot be attached as guarantor";

                    return View(loanGuarantorsDTO);
                }
                else
                {
                    loanGuarantorsDTO.CustomerId = customer.Id;
                    loanGuarantorsDTO.GuarantorCustomerFullName = customer.FullName;
                    loanGuarantorsDTO.GuarantorCustomerSerialNumber = customer.SerialNumber;
                    loanGuarantorsDTO.GuarantorEmployerDescription = customer.StationZoneDivisionEmployerDescription;
                    loanGuarantorsDTO.CustomerPersonalIdentificationNumber = customer.IdentificationNumber;
                    loanGuarantorsDTO.GuarantorStationDescription = customer.StationDescription;
                    loanGuarantorsDTO.GuarantorIndividualPayrollNumbers = customer.IndividualPayrollNumbers;
                }

            }
            return View(loanGuarantorsDTO);
        }



        public async Task<ActionResult> Search(Guid? id)
        {
            //string Remarks = "";
            await ServeNavigationMenus();

            ViewBag.WithdrawalNotificationCategorySelectList = GetWithdrawalNotificationCategorySelectList(string.Empty);

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            var customer = await _channelService.FindCustomerAsync(parseId, GetServiceHeader());

            var loanGuarantorsDTO = new LoanGuarantorDTO();

            var loanCases = await _channelService.FindLoanCaseAsync(parseId, GetServiceHeader());

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


                if (Session["LoanCaseAmountApplied"] != null)
                {
                    loanGuarantorsDTO.LoanCaseCaseNumber = Convert.ToInt32(Session["LoancaseNumber"].ToString());
                }
                if (Session["CustomerFullName"] != null)
                {
                    loanGuarantorsDTO.CustomerFullName = Session["CustomerFullName"].ToString();
                }
                if (Session["LoanCaseAmountApplied"] != null)
                {
                    loanGuarantorsDTO.LoanCaseAmountApplied = Convert.ToDecimal(Session["LoanCaseAmountApplied"].ToString());
                }
                if (Session["CustomerReference1"] != null)
                {
                    loanGuarantorsDTO.CustomerReference1 = Session["CustomerReference1"].ToString();
                }
                if (Session["CustomerReference2"] != null)
                {
                    loanGuarantorsDTO.CustomerReference2 = Session["CustomerReference2"].ToString();
                }




            }

            
            if (customer != null)
            {
                if (loanGuarantorsDTO.CustomerId == loanGuarantorsDTO.LoaneeCustomerId)
                {
                    TempData["AlertMessage"] = "Loanee cannot be attached as guarantor";

                    return View(loanGuarantorsDTO);
                }
                else
                {
                    loanGuarantorsDTO.CustomerId = customer.Id;
                    loanGuarantorsDTO.GuarantorCustomerFullName = customer.FullName;
                    loanGuarantorsDTO.GuarantorCustomerSerialNumber = customer.SerialNumber;
                    loanGuarantorsDTO.GuarantorEmployerDescription = customer.StationZoneDivisionEmployerDescription;
                    loanGuarantorsDTO.CustomerPersonalIdentificationNumber = customer.IdentificationNumber;
                    loanGuarantorsDTO.GuarantorStationDescription = customer.StationDescription;
                    loanGuarantorsDTO.GuarantorIndividualPayrollNumbers = customer.IndividualPayrollNumbers;

                }
               
            }


            //TempData["WithdrawalNotificationDTOs"] = withdrawalNotificationDTO;
            return View("Create", loanGuarantorsDTO);
        }



        [HttpPost]
        public ActionResult LoaneeData(int LoanCaseCaseNumber, string CustomerFullName, decimal LoanCaseAmountApplied, string CustomerReference1,
            string CustomerReference2)
        {
            Session["LoanCaseCaseNumber"] = LoanCaseCaseNumber;
            Session["CustomerFullName"] = CustomerFullName;
            Session["LoanCaseAmountApplied"] = LoanCaseAmountApplied;
            Session["CustomerReference1"] = CustomerReference1;
            Session["CustomerReference2"] = CustomerReference2;

            return null;
        }



        [HttpPost]
        public async Task<ActionResult> Create(LoanGuarantorDTO loanGuarantorDTO)
        {
            loanGuarantorDTO.ValidateAll();

            if (!loanGuarantorDTO.HasErrors)
            {
                await _channelService.AddLoanGuarantorAsync(loanGuarantorDTO, GetServiceHeader());

                TempData["AlertMessage"] = "Loan guarantor added successfully.";

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = loanGuarantorDTO.ErrorMessages;

                //ViewBag.WithdrawalNotificationCategorySelectList = GetWithdrawalNotificationCategorySelectList(loanGuarantorDTO.Category.ToString());

                return View(loanGuarantorDTO);
            }
        }


        //[HttpPost]
        //public async Task<ActionResult> Create(LoanGuarantorDTO loanGuarantorDTO)
        //{
        //    loanGuarantorDTO.ValidateAll();
        //    Guid sourceCustomerAccountId = loanGuarantorDTO.Id;
        //    Guid destinationLoanProductId = loanGuarantorDTO.Id;
        //    int navigatmoduleNavigationItemCode = 0;



        //    if (!loanGuarantorDTO.HasErrors)
        //    {
        //        await _channelService.AttachLoanGuarantorsAsync(sourceCustomerAccountId, destinationLoanProductId,  LoanGuarantorDTOs, navigatmoduleNavigationItemCode, GetServiceHeader());

        //        ViewBag.IncomeAdjustmentTypeSelectList = GetIncomeAdjustmentTypeSelectList(loanGuarantorDTO.Status.ToString());


        //        return RedirectToAction("Index");
        //    }
        //    else
        //    {
        //        var errorMessages = loanGuarantorDTO.ErrorMessages;

        //        return View("index");
        //    }
        //}

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