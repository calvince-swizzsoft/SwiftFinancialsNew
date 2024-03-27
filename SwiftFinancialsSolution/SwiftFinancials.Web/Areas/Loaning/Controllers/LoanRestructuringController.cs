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

using System.Data.SqlClient;
using System.Data;

namespace SwiftFinancials.Web.Areas.Loaning.Controllers
{
    public class LoanRestructuringController : MasterController
    {
        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();

            return View();
        }


        [HttpPost]
        public async Task<JsonResult> Index(JQueryDataTablesModel jQueryDataTablesModel, string text, int pageIndex, int pageSize, LoanCaseDTO loancaseDTO)
        {
            int totalRecordCount = 0;

            int searchRecordCount = 0;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindCustomerAccountsByCustomerAccountTypeTargetProductIdInPageAsync(loancaseDTO.Id, pageIndex, pageSize, true, true, true, false, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<CustomerAccountDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }


        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var loanguarantorsDTO = await _channelService.FindLoanGuarantorAsync(id, GetServiceHeader());

            return View(loanguarantorsDTO);
        }


        public async Task<ActionResult> Create(Guid? Id, CustomerAccountDTO customerAccountDTO, LoanCaseDTO loanCaseDTO)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (Id == Guid.Empty || !Guid.TryParse(Id.ToString(), out parseId))
            {
                return View();
            }


            Guid loanCaseId = parseId;



            var loanCases = await _channelService.FindLoanCaseAsync(loanCaseDTO.Id, GetServiceHeader());

            var loanCaseByCustomerId = await _channelService.FindLastLoanCaseByCustomerIdAsync(customerAccountDTO.CustomerId, loanCaseDTO.LoanProductId, GetServiceHeader());


            var customerAccount = await _channelService.FindCustomerAccountAsync(parseId, true, true, true, true, GetServiceHeader());

            if (loanCases != null)
            {
                //var loanCaseID = loanCases.Id;
                //var FullAccountNumber = customerAccount.FullAccountNumber;

                customerAccountDTO.CustomerId = loanCases.CustomerId;
                customerAccountDTO.CustomerReference1 = loanCases.CustomerReference1;
                customerAccountDTO.CustomerIndividualFirstName = loanCases.CustomerFullName;
                customerAccountDTO.Remarks = loanCases.Remarks;
                customerAccountDTO.CustomerPersonalIdentificationNumber = loanCases.CustomerPersonalIdentificationNumber;
                customerAccountDTO.CustomerReference3 = loanCases.CustomerReference3;
                customerAccountDTO.CustomerReference2 = loanCases.CustomerReference2;
                customerAccountDTO.CustomerIndividualPayrollNumbers = loanCases.CustomerIndividualPayrollNumbers;
                //customerAccountDTO.FullAcctNumber = loanCases.CustomerIndividualPayrollNumbers;
            }


            //if (customerAccount != null)
            //{
            //    customerAccountDTO.CustomerId = customerAccount.CustomerId;
            //    customerAccountDTO.CustomerReference1 = customerAccount.CustomerReference1;
            //    customerAccountDTO.CustomerIndividualFirstName = customerAccount.CustomerFullName;
            //    customerAccountDTO.Remarks = customerAccount.Remarks;
            //    customerAccountDTO.CustomerPersonalIdentificationNumber = customerAccount.CustomerPersonalIdentificationNumber;
            //    customerAccountDTO.CustomerReference3 = customerAccount.CustomerReference3;
            //    customerAccountDTO.CustomerReference2 = customerAccount.CustomerReference2;
            //    customerAccountDTO.CustomerIndividualPayrollNumbers = customerAccount.CustomerIndividualPayrollNumbers;
            //}

            return View(customerAccountDTO);
        }



        public async Task<ActionResult> Search(Guid? id, LoanGuarantorDTO loanGuarantorDTO)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            var customer = await _channelService.FindCustomerAsync(parseId, GetServiceHeader());

            //var loanGuarantorsDTO = new LoanGuarantorDTO();


            if (Session["LoanCaseAmountApplied"] != null)
            {
                loanGuarantorDTO.LoanCaseAmountApplied = Convert.ToDecimal(Session["LoanCaseAmountApplied"].ToString());
            }
            if (Session["CustomerFullName"] != null)
            {
                loanGuarantorDTO.CustomerFullName = Session["CustomerFullName"].ToString();
            }
            if (Session["CustomerReference1"] != null)
            {
                loanGuarantorDTO.CustomerReference1 = Session["CustomerReference1"].ToString();
            }
            if (Session["CustomerReference2"] != null)
            {
                loanGuarantorDTO.CustomerReference2 = Session["CustomerReference2"].ToString();
            }
            if (Session["LoanCaseCaseNumber"] != null)
            {
                loanGuarantorDTO.LoanCaseCaseNumber = Convert.ToInt32(Session["LoanCaseCaseNumber"].ToString());
            }


            if (customer != null)
            {
                loanGuarantorDTO.CustomerId = customer.Id;
                loanGuarantorDTO.GuarantorCustomerFullName = customer.FullName;
                loanGuarantorDTO.GuarantorCustomerSerialNumber = customer.SerialNumber;
                loanGuarantorDTO.GuarantorEmployerDescription = customer.StationZoneDivisionEmployerDescription;
                loanGuarantorDTO.CustomerPersonalIdentificationNumber = customer.IdentificationNumber;
                loanGuarantorDTO.GuarantorStationDescription = customer.StationDescription;
                loanGuarantorDTO.GuarantorIndividualPayrollNumbers = customer.IndividualPayrollNumbers;

            }

            return View("Create", loanGuarantorDTO);
        }



        [HttpPost]
        public ActionResult LoaneeData(string LoanCaseCaseNumber, string CustomerFullName, string LoanCaseAmountApplied, string CustomerReference1,
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
            var loancases = await _channelService.FindLoanCasesAsync(GetServiceHeader());

            loanGuarantorDTO.ValidateAll();

            await ServeNavigationMenus();

            //var loanCases = await _channelService.FindLoanCaseAsync(parseId, GetServiceHeader());
            // var customer = await _channelService.FindCustomerAsync(parseId, GetServiceHeader());

            var loanCaseDTO = await _channelService.FindLoanCaseAsync((Guid)loanGuarantorDTO.LoanCaseId, GetServiceHeader());

            //var customerAccountDTO=await _channelService.findcustomeraccount

            //Guid sourceCustomerID = customer.Id;
            //Guid destinationProductID = customerAccount.CustomerAccountTypeTargetProductId;

            int moduleNavigationCode = 0;

            if (!loanGuarantorDTO.HasErrors)
            {
                //await _channelService.AttachLoanGuarantorsAsync(sourceCustomerID, destinationProductID, LoanGuarantorsDTO, moduleNavigationCode, GetServiceHeader());

                TempData["AlertMessage"] = "Loan guarantor added successfully.";

                return RedirectToAction("Index", "LoanRegistration");
            }
            else
            {
                var errorMessages = loanGuarantorDTO.ErrorMessages;
                return View(loanGuarantorDTO);
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