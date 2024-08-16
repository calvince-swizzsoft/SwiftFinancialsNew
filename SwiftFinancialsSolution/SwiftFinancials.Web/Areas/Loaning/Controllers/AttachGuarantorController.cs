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
    public class AttachGuarantorController : MasterController
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


        public async Task<ActionResult> Create(Guid? id, LoanGuarantorDTO loanGuarantorDTO)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            var myloanCases = await _channelService.FindLoanCaseAsync(parseId, GetServiceHeader());

            if (myloanCases != null)
            {
                loanGuarantorDTO.LoanCase = myloanCases;

                Session["loanCases"] = loanGuarantorDTO.LoanCase;

                Session["status"] = loanGuarantorDTO.LoanCase.Status;
            }

            return View(loanGuarantorDTO);
        }



        public async Task<ActionResult> Search(Guid? id, LoanGuarantorDTO loanGuarantorDTO)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }


            if (Session["loanCases"] != null)
            {
                loanGuarantorDTO.LoanCase = Session["loanCases"] as LoanCaseDTO;
            }


            var customer = await _channelService.FindCustomerAsync(parseId, GetServiceHeader());

            if (customer != null)
            {
                loanGuarantorDTO.Customer = customer;

                Session["Customer"] = loanGuarantorDTO.Customer;
            }

            return View("Create", loanGuarantorDTO);
        }


        [HttpPost]
        public async Task<ActionResult> Create(LoanGuarantorDTO loanGuarantorDTO)
        {
            var customerDTO = await _channelService.FindCustomerAsync(loanGuarantorDTO.Customer.Id, GetServiceHeader());

            var loanCaseDTO = await _channelService.FindLoanCaseAsync(loanGuarantorDTO.LoanCase.Id, GetServiceHeader());

            // cheat
            loanGuarantorDTO.CustomerId = customerDTO.Id;
            loanGuarantorDTO.LoaneeCustomerId = loanCaseDTO.CustomerId;
            loanGuarantorDTO.LoanProductId = loanCaseDTO.LoanProductId;
            loanGuarantorDTO.TotalShares = 10000;
            loanGuarantorDTO.CommittedShares = 10000;
            loanGuarantorDTO.AmountPledged = 10000;
            loanGuarantorDTO.AppraisalFactor = 1;
            loanGuarantorDTO.LoanCaseId = loanCaseDTO.Id;
            loanGuarantorDTO.LoanCaseBranchId = loanCaseDTO.BranchId;


            loanGuarantorDTO.ValidateAll();

            await ServeNavigationMenus();

            if (!loanGuarantorDTO.HasErrors)
            {
                var status = Convert.ToInt32(Session["status"].ToString());
                if (status != 48826)
                {
                    TempData["status"] = "You can only attach guarantor for loans that are registered !";
                    return View();
                }

                var AddLoanGuarantor = await _channelService.AddLoanGuarantorAsync(loanGuarantorDTO, GetServiceHeader());

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




        // Add 
        [HttpPost]
        public async Task<ActionResult> Add(Guid? id, LoanGuarantorDTO loanGuarantorDTO)
        {
            await ServeNavigationMenus();

            LoanGuarantorDTOs = TempData["LoanGuarantorDTOs"] as ObservableCollection<LoanGuarantorDTO>;


            if (LoanGuarantorDTOs == null)
                LoanGuarantorDTOs = new ObservableCollection<LoanGuarantorDTO>();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                TempData["LoanGuarantorsError"] = "Could not add charge split. Choose G/L Account.";

                await ServeNavigationMenus();

                return View("Create", loanGuarantorDTO);
            }


            foreach (var loanGuarantorsGuarantors in loanGuarantorDTO.LoanGuarantors)
            {

                TempData["tPercentage"] = "";

                LoanGuarantorDTOs.Add(loanGuarantorsGuarantors);
            };

            ViewBag.Guarantors = ChargeSplitDTOs;

            TempData["ChargeSplitDTOs"] = ChargeSplitDTOs;

            return View("Create");
        }



        // Remove
        [HttpPost]
        public async Task<ActionResult> removeChargeSplit(Guid? id, CommissionDTO commissionDTO)
        {
            commissionDTO = TempData["ChargeDTO"] as CommissionDTO;

            commissionDTO.chargeSplit = Session["chargeSplit"] as ObservableCollection<CommissionSplitDTO>;

            var percentageOnRemove = commissionDTO.chargeSplit[0].Percentage;
            var currentPercentage = Convert.ToDouble(Session["totalPercentage"].ToString());

            double cPercentage = Convert.ToDouble(currentPercentage);
            double rPercentage = Convert.ToDouble(percentageOnRemove);

            double newpercentage = cPercentage - rPercentage;

            ViewBag.totalPercentage = newpercentage;

            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                await ServeNavigationMenus();

                return View("Create");
            }

            ChargeSplitDTOs = TempData["ChargeSplitDTOs"] as ObservableCollection<CommissionSplitDTO>;

            var glAccount = commissionDTO.chargeSplits;

            if (ChargeSplitDTOs == null)
                ChargeSplitDTOs = new ObservableCollection<CommissionSplitDTO>();

            foreach (var chargeSplitDTO in commissionDTO.chargeSplit)
            {
                chargeSplitDTO.Id = parseId;
                chargeSplitDTO.Description = chargeSplitDTO.Description;
                chargeSplitDTO.ChartOfAccountId = commissionDTO.Id;
                chargeSplitDTO.ChartOfAccountAccountName = chargeSplitDTO.ChartOfAccountAccountName;
                chargeSplitDTO.Percentage = chargeSplitDTO.Percentage;
                chargeSplitDTO.Leviable = chargeSplitDTO.Leviable;

                ChargeSplitDTOs.Remove(chargeSplitDTO);
            };

            TempData["ChargeSplitDTOs"] = ChargeSplitDTOs;

            TempData["ChargeDTO"] = commissionDTO;

            TempData["tPercentage"] = "";

            ViewBag.ChargeSplitDTOs = ChargeSplitDTOs;

            return View("Create", commissionDTO);
        }

    }
}