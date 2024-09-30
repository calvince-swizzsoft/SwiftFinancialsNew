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

            var loanproductId = myloanCases.LoanProductId;
            Session["LoanProductIdID"] = loanproductId;

            var sourceCustomerAccountId = myloanCases.CustomerId;
            Session["sourceCustomerAccountId"] = sourceCustomerAccountId;

            if (myloanCases != null)
            {
                loanGuarantorDTO.LoanCase = myloanCases;

                Session["loanCases"] = loanGuarantorDTO.LoanCase;

                Session["status"] = loanGuarantorDTO.LoanCase.Status;
            }

            var findLoanGuarantors = await _channelService.FindLoanGuarantorsByLoanCaseIdAsync(myloanCases.Id, GetServiceHeader());
            ViewBag.LoanGuarantors = findLoanGuarantors;

            Session["loanCaseId"] = myloanCases.Id;

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


            if (Session["loanCaseId"] != null)
            {
                Guid loanCaseId = (Guid)Session["loanCaseId"];

                var findLoanGuarantors = await _channelService.FindLoanGuarantorsByLoanCaseIdAsync(loanCaseId, GetServiceHeader());
                ViewBag.LoanGuarantors = findLoanGuarantors;
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
        public async Task<ActionResult> Add(Guid? id, LoanGuarantorDTO loanGuarantorDTO)
        {
            await ServeNavigationMenus();

            LoanGuarantorsDTO = TempData["LoanGuarantorsDTO"] as ObservableCollection<LoanGuarantorDTO>;

            double sumPercentages = 0;


            if (LoanGuarantorsDTO == null)
                LoanGuarantorsDTO = new ObservableCollection<LoanGuarantorDTO>();


            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                TempData["tPercentage"] = "Could not add charge split. Choose G/L Account.";

                await ServeNavigationMenus();

                return View("Create", loanGuarantorDTO);
            }


            //foreach (var GuarantorsDTO in loanGuarantorDTO.Guarantors)
            //{
            //    chargeSplitDTO.Id = parseId;
            //    chargeSplitDTO.Description = chargeSplitDTO.Description;
            //    chargeSplitDTO.ChartOfAccountId = commissionDTO.Id;
            //    chargeSplitDTO.ChartOfAccountAccountName = glAccount.ChartOfAccountAccountName;
            //    chargeSplitDTO.Percentage = chargeSplitDTO.Percentage;
            //    chargeSplitDTO.Leviable = chargeSplitDTO.Leviable;


            //    TempData["tPercentage"] = "";


            //    if (chargeSplitDTO.Description == null || chargeSplitDTO.ChartOfAccountAccountName == null || chargeSplitDTO.Percentage == 0)
            //    {
            //        TempData["tPercentage"] = "Could not add charge split. Make sure to provide all charge split details to proceed.";
            //    }
            //    else
            //    {
            //        ChargeSplitDTOs.Add(chargeSplitDTO);

            //        sumPercentages = ChargeSplitDTOs.Sum(cs => cs.Percentage);

            //        Session["chargeSplit"] = commissionDTO.chargeSplit;

            //        if (sumPercentages > 100)
            //        {
            //            TempData["tPercentage"] = "Failed to add \"" + chargeSplitDTO.Description.ToUpper() + "\". Total percentage exceeded 100%.";

            //            ChargeSplitDTOs.Remove(chargeSplitDTO);
            //        }
            //        else if (sumPercentages < 1)
            //        {
            //            TempData["tPercentage"] = "Total percentage must be at least greater than 1%.";
            //        }
            //    }
            //};


            ViewBag.ChargeSplitDTOs = ChargeSplitDTOs;

            ViewBag.totalPercentage = sumPercentages;

            Session["totalPercentage"] = sumPercentages;

            TempData["ChargeSplitDTOs"] = ChargeSplitDTOs;


            TempData["ChargeDTO"] = loanGuarantorDTO;

            return View("Create");
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

                loanGuarantorDTO.Customer = null;

                if (AddLoanGuarantor.ErrorMsgResult != null)
                {
                    await ServeNavigationMenus();

                    TempData["failedErrorMsg"] = AddLoanGuarantor.ErrorMsgResult;
                    Guid loanCaseId = (Guid)Session["loanCaseId"];

                    var findLoanGuarantors = await _channelService.FindLoanGuarantorsByLoanCaseIdAsync(loanCaseId, GetServiceHeader());
                    ViewBag.LoanGuarantors = findLoanGuarantors;
                    return View();
                }

                if (Session["loanCaseId"] != null)
                {
                    Guid loanCaseId = (Guid)Session["loanCaseId"];

                    var findLoanGuarantors = await _channelService.FindLoanGuarantorsByLoanCaseIdAsync(loanCaseId, GetServiceHeader());
                    ViewBag.LoanGuarantors = findLoanGuarantors;
                }

                TempData["AlertMessage"] = "Loan guarantor added successfully.";

                return View();
            }
            else
            {
                var errorMessages = loanGuarantorDTO.ErrorMessages;

                TempData["ErrorMsg"] = "Failed to attach Loan Guarantor.";

                return View(loanGuarantorDTO);
            }
        }


        [HttpPost]
        public async Task<ActionResult> Finish()
        {

            if (Session["loanCaseId"] != null || Session["LoanProductIdID"] != null || Session["sourceCustomerAccountId"] != null)
            {
                Guid loanCaseID = (Guid)Session["loanCaseId"];
                Guid loanProductId = (Guid)Session["LoanProductIdID"];
                Guid sourceCustomerAccountId = (Guid)Session["sourceCustomerAccountId"];


                var LoanGuarantors = await _channelService.FindLoanGuarantorsByLoanCaseIdAsync(loanCaseID, GetServiceHeader());

                ObservableCollection<LoanGuarantorDTO> loanGuarantors = new ObservableCollection<LoanGuarantorDTO>();

                foreach (var guarantors in LoanGuarantors)
                {
                    loanGuarantors.Add(guarantors);
                }

                await _channelService.AttachLoanGuarantorsAsync(sourceCustomerAccountId, loanProductId, loanGuarantors, 1234, GetServiceHeader());

                return RedirectToAction("Index", "LoanRegistration");
            }

            TempData["emptyIDs"] = "Sorry, something went wrong !";
            return View("Create");
        }


        [HttpPost]
        public async Task<ActionResult> removeGuarantors(Guid? id, LoanGuarantorDTO loanGuarantorDTO)
        {
            await ServeNavigationMenus();

            Guid loanCaseId = (Guid)Session["loanCaseId"];

            var findLoanGuarantors = await _channelService.FindLoanGuarantorsByLoanCaseIdAsync(loanCaseId, GetServiceHeader());
            ViewBag.LoanGuarantors = findLoanGuarantors;

            if (id == null || id == Guid.Empty)
            {
                return View("create", loanGuarantorDTO);
            }

            return View("create", loanGuarantorDTO);
        }


        // Guarantor Relieving
        public async Task<ActionResult> relieveguarantor(Guid id)
        {
            await ServeNavigationMenus();

            var loanCaseDTO = await _channelService.FindLoanCaseAsync(id, GetServiceHeader());

            var loanGuarantors = await _channelService.FindLoanGuarantorsByLoanCaseIdAsync(id, GetServiceHeader());

            ViewBag.LoanGuarantors = loanGuarantors;

            return View(loanCaseDTO);
        }
        [HttpPost]
        public async Task<ActionResult> relieveguarantor(Guid? id, LoanGuarantorDTO loanGuarantorDTO)
        {
            await ServeNavigationMenus();

            Guid loanCaseId = (Guid)Session["loanCaseId"];

            //await _channelService.RelieveLoanGuarantorsAsync(id, GetServiceHeader(null));

            if (id == null || id == Guid.Empty)
            {
                return View("create", loanGuarantorDTO);
            }

            return View("create", loanGuarantorDTO);
        }





        // Guarantor Management
        public async Task<ActionResult> guarantorManagement(Guid id)
        {
            await ServeNavigationMenus();

            var loanCaseDTO = await _channelService.FindLoanCaseAsync(id, GetServiceHeader());

            var loanGuarantors = await _channelService.FindLoanGuarantorsByLoanCaseIdAsync(id, GetServiceHeader());

            ViewBag.LoanGuarantors = loanGuarantors;

            return View(loanCaseDTO);
        }
        [HttpPost]
        public async Task<ActionResult> guarantorManagement(Guid? id, LoanGuarantorDTO loanGuarantorDTO)
        {
            await ServeNavigationMenus();

            Guid loanCaseId = (Guid)Session["loanCaseId"];

            //await _channelService.RelieveLoanGuarantorsAsync(id, GetServiceHeader(null));

            if (id == null || id == Guid.Empty)
            {
                return View("create", loanGuarantorDTO);
            }

            return View("create", loanGuarantorDTO);
        }




        // Guarantor Substitution
        public async Task<ActionResult> guarantorSubstitution(Guid id)
        {
            await ServeNavigationMenus();

            var loanCaseDTO = await _channelService.FindLoanCaseAsync(id, GetServiceHeader());

            var loanGuarantors = await _channelService.FindLoanGuarantorsByLoanCaseIdAsync(id, GetServiceHeader());

            ViewBag.LoanGuarantors = loanGuarantors;

            return View(loanCaseDTO);
        }
        [HttpPost]
        public async Task<ActionResult> guarantorSubstitution(Guid? id, LoanGuarantorDTO loanGuarantorDTO)
        {
            await ServeNavigationMenus();

            Guid loanCaseId = (Guid)Session["loanCaseId"];

            //await _channelService.RelieveLoanGuarantorsAsync(id, GetServiceHeader(null));

            if (id == null || id == Guid.Empty)
            {
                return View("create", loanGuarantorDTO);
            }

            return View("create", loanGuarantorDTO);
        }

    }
}