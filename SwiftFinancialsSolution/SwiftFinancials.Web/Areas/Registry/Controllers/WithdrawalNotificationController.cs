using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.Services;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.RegistryModule;
using Infrastructure.Crosscutting.Framework.Utils;
using SwiftFinancials.Presentation.Infrastructure.Util;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.Registry.Controllers
{
    public class WithdrawalNotificationController : MasterController
    {
        public ObservableCollection<WithdrawalNotificationDTO> WithdrawalNotificationDTOs;
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

            var pageCollectionInfo = await _channelService.FindWithdrawalNotificationsByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(creditBatch => creditBatch.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<WithdrawalNotificationDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var withdrawalNotificationDTO = await _channelService.FindWithdrawalNotificationAsync(id, GetServiceHeader());

            return View(withdrawalNotificationDTO);
        }

        public async Task<ActionResult> Create(Guid? id)
        {
            await ServeNavigationMenus();

            ViewBag.WithdrawalNotificationCategorySelectList = GetWithdrawalNotificationCategorySelectList(string.Empty);

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }
            // await _channelService.FindCustomerAccountsByCustomerIdAndProductCodesAsync(parseId,int[], false, false, false, false, GetServiceHeader());
            var customer = await _channelService.FindCustomerAsync(parseId, GetServiceHeader());

            WithdrawalNotificationDTO withdrawalNotificationDTO = new WithdrawalNotificationDTO();

            if (customer != null)
            {

                withdrawalNotificationDTO.CustomerId = customer.Id;
                withdrawalNotificationDTO.CustomerFullName = customer.FullName;
                withdrawalNotificationDTO.CustomerIndividualPayrollNumbers = customer.IndividualPayrollNumbers;
                withdrawalNotificationDTO.CustomerSerialNumber = customer.SerialNumber;
                withdrawalNotificationDTO.CustomerIndividualIdentityCardNumber = customer.IndividualIdentityCardNumber;
                withdrawalNotificationDTO.CustomerStationDescription = customer.StationDescription;
                withdrawalNotificationDTO.CustomerStationZoneDivisionEmployerDescription = customer.StationZoneDivisionEmployerDescription;
            }
            //var k = await _channelService.FindCustomerAccountsByCustomerIdAndCustomerAccountTypeTargetProductIdAsync();
            return View(withdrawalNotificationDTO);
        }


        [HttpPost]
        public async Task<ActionResult> Create(WithdrawalNotificationDTO withdrawalNotificationDTO)
        {
            withdrawalNotificationDTO.ValidateAll();

            if (!withdrawalNotificationDTO.HasErrors)
            {
                var result = await _channelService.AddWithdrawalNotificationAsync(withdrawalNotificationDTO, GetServiceHeader());
                if (result.ErrorMessageResult != null)
                {
                    TempData["ErrorMsg"] = result.ErrorMessageResult;
                    await ServeNavigationMenus();
                    ViewBag.WithdrawalNotificationCategorySelectList = GetWithdrawalNotificationCategorySelectList(withdrawalNotificationDTO.Category.ToString());

                    return View();
                }
                TempData["SuccessMessage"] = "Member " + withdrawalNotificationDTO.CustomerFullName + " has an Awaiting Approval withdrawal Notification";
                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = withdrawalNotificationDTO.ErrorMessages;
                ViewBag.WithdrawalNotificationCategorySelectList = GetWithdrawalNotificationCategorySelectList(withdrawalNotificationDTO.Category.ToString());
                return View(withdrawalNotificationDTO);
            }
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


            WithdrawalNotificationDTO withdrawalNotificationDTO = new WithdrawalNotificationDTO();

            //WithdrawalNotificationDTOs = TempData["WithdrawalNotificationDTOs"] as ObservableCollection<WithdrawalNotificationDTO>;

            if (customer != null)
            {

                withdrawalNotificationDTO.CustomerId = customer.Id;
                withdrawalNotificationDTO.CustomerFullName = customer.FullName;
                withdrawalNotificationDTO.CustomerIndividualPayrollNumbers = customer.IndividualPayrollNumbers;
                withdrawalNotificationDTO.CustomerSerialNumber = customer.SerialNumber;
                withdrawalNotificationDTO.CustomerIndividualIdentityCardNumber = customer.IndividualIdentityCardNumber;
                withdrawalNotificationDTO.CustomerStationDescription = customer.StationDescription;
                withdrawalNotificationDTO.CustomerStationZoneDivisionEmployerDescription = customer.StationZoneDivisionEmployerDescription;
                //Session["Test"] =Request.Form["h"] + "";
                //string mimi = Session["Test"].ToString();
                Session["withdrawalNotificationDTO"] = withdrawalNotificationDTO;
                if (Session["Remarks"] != null)
                {
                    withdrawalNotificationDTO.Remarks = Session["Remarks"].ToString();
                }
                if (Session["BranchDescription"] != null)
                {
                    withdrawalNotificationDTO.BranchDescription = Session["BranchDescription"].ToString();
                }
                //
            }

            //TempData["WithdrawalNotificationDTOs"] = withdrawalNotificationDTO;
            return View("Create", withdrawalNotificationDTO);
        }



        public async Task<ActionResult> Search2(Guid? id)
        {
            //string Remarks = "";
            await ServeNavigationMenus();

            ViewBag.WithdrawalNotificationCategorySelectList = GetWithdrawalNotificationCategorySelectList(string.Empty);

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            var customer = await _channelService.FindInsuranceCompanyAsync(parseId, GetServiceHeader());


            WithdrawalNotificationDTO withdrawalNotificationDTO = new WithdrawalNotificationDTO();

            //WithdrawalNotificationDTOs = TempData["WithdrawalNotificationDTOs"] as ObservableCollection<WithdrawalNotificationDTO>;

            if (customer != null)
            {

                withdrawalNotificationDTO.CustomerId = customer.Id;
                withdrawalNotificationDTO.CustomerFullName = customer.Description;               
                //Session["Test"] =Request.Form["h"] + "";
                //string mimi = Session["Test"].ToString();
                Session["withdrawalNotificationDTO"] = withdrawalNotificationDTO;
                if (Session["Remarks"] != null)
                {
                    withdrawalNotificationDTO.Remarks = Session["Remarks"].ToString();
                }
                if (Session["BranchDescription"] != null)
                {
                    withdrawalNotificationDTO.BranchDescription = Session["BranchDescription"].ToString();
                }
                //
            }

            //TempData["WithdrawalNotificationDTOs"] = withdrawalNotificationDTO;
            return View("Create", withdrawalNotificationDTO);
        }



        [HttpPost]
        public ActionResult AssignText(string Remarks, string BranchDescription)
        {
            Session["Remarks"] = Remarks;
            Session["BranchDescription"] = BranchDescription;

            return null;
        }

        public async Task<ActionResult> Edit(Guid id)
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


            WithdrawalNotificationDTO withdrawalNotificationDTO = new WithdrawalNotificationDTO();

            //WithdrawalNotificationDTOs = TempData["WithdrawalNotificationDTOs"] as ObservableCollection<WithdrawalNotificationDTO>;

            if (customer != null)
            {

                withdrawalNotificationDTO.CustomerId = customer.Id;
                withdrawalNotificationDTO.CustomerFullName = customer.FullName;
                withdrawalNotificationDTO.CustomerIndividualPayrollNumbers = customer.IndividualPayrollNumbers;
                withdrawalNotificationDTO.CustomerSerialNumber = customer.SerialNumber;
                withdrawalNotificationDTO.CustomerIndividualIdentityCardNumber = customer.IndividualIdentityCardNumber;
                withdrawalNotificationDTO.CustomerStationDescription = customer.StationDescription;
                withdrawalNotificationDTO.CustomerStationZoneDivisionEmployerDescription = customer.StationZoneDivisionEmployerDescription;
                //Session["Test"] =Request.Form["h"] + "";
                //string mimi = Session["Test"].ToString();
                Session["withdrawalNotificationDTO"] = withdrawalNotificationDTO;
                if (Session["Remarks"] != null)
                {
                    withdrawalNotificationDTO.Remarks = Session["Remarks"].ToString();
                }
                if (Session["BranchDescription"] != null)
                {
                    withdrawalNotificationDTO.BranchDescription = Session["BranchDescription"].ToString();
                }
                //
            }

            //TempData["WithdrawalNotificationDTOs"] = withdrawalNotificationDTO;
            return View(withdrawalNotificationDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, WithdrawalNotificationDTO withdrawalNotificationDTO)
        {
            if (!withdrawalNotificationDTO.HasErrors)
            {
                await _channelService.UpdateWithdrawalNotificationAsync(withdrawalNotificationDTO, GetServiceHeader());
                ViewBag.WithdrawalNotificationCategorySelectList = GetWithdrawalNotificationCategorySelectList(withdrawalNotificationDTO.Category.ToString());
                return RedirectToAction("Index");
            }
            else
            {
                ViewBag.WithdrawalNotificationCategorySelectList = GetWithdrawalNotificationCategorySelectList(withdrawalNotificationDTO.Category.ToString());
                return View(withdrawalNotificationDTO);
            }
        }





        public async Task<ActionResult> Verify(Guid id)
        {
            await ServeNavigationMenus();

            ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(string.Empty);
            ViewBag.AuditOption = GetmembershipWithdrawalAuditOptionSelectList(string.Empty);

            var creditBatchDTO = await _channelService.FindWithdrawalNotificationAsync(id, GetServiceHeader());

            ViewBag.WithdrawalNotificationCategorySelectList = GetWithdrawalNotificationCategorySelectList(string.Empty);
            ViewBag.WithdrawalNotificationstatusSelectList = GetwithdrawalNotificationStatusSelectList(string.Empty);

            return View(creditBatchDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Verify(WithdrawalNotificationDTO withdrawalNotificationDTO)
        {
            withdrawalNotificationDTO.ValidateAll();
            int membershipWithdrawalApprovalOption = withdrawalNotificationDTO.Verify;

            if (!withdrawalNotificationDTO.HasErrors)
            {

                await _channelService.AuditWithdrawalNotificationAsync(withdrawalNotificationDTO, membershipWithdrawalApprovalOption, GetServiceHeader());

                TempData["SuccessMessage"] = "Member " + withdrawalNotificationDTO.CustomerFullName + "   withdrawal Notification has an been Verified";

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = withdrawalNotificationDTO.ErrorMessages;

                ViewBag.AuditOption = GetmembershipWithdrawalAuditOptionSelectList(withdrawalNotificationDTO.Status.ToString());
                ViewBag.WithdrawalNotificationstatusSelectList = GetwithdrawalNotificationStatusSelectList(withdrawalNotificationDTO.Status.ToString());
                ViewBag.WithdrawalNotificationCategorySelectList = GetWithdrawalNotificationCategorySelectList(withdrawalNotificationDTO.Category.ToString());
                return View(withdrawalNotificationDTO);
            }
        }

        public async Task<ActionResult> Approval(Guid id)
        {
            await ServeNavigationMenus();
            ViewBag.AuditOption = GetmembershipWithdrawalApprovalOptionSelectList(string.Empty);
            ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(string.Empty);
            var creditBatchDTO = await _channelService.FindWithdrawalNotificationAsync(id, GetServiceHeader());
            ViewBag.WithdrawalNotificationstatusSelectList = GetwithdrawalNotificationStatusSelectList(string.Empty);
            ViewBag.WithdrawalNotificationCategorySelectList = GetWithdrawalNotificationCategorySelectList(string.Empty);
            return View(creditBatchDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Approval(WithdrawalNotificationDTO withdrawalNotificationDTO)
        {

            withdrawalNotificationDTO.ValidateAll();
            int membershipWithdrawalApprovalOption = withdrawalNotificationDTO.Verify;
            if (!withdrawalNotificationDTO.HasErrors)
            {
                await _channelService.ApproveWithdrawalNotificationAsync(withdrawalNotificationDTO, membershipWithdrawalApprovalOption, GetServiceHeader());
                TempData["SuccessMessage"] = "Member " + withdrawalNotificationDTO.CustomerFullName + "   withdrawal Notification has an been Approved";

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = withdrawalNotificationDTO.ErrorMessages;
                ViewBag.WithdrawalNotificationstatusSelectList = GetwithdrawalNotificationStatusSelectList(withdrawalNotificationDTO.Status.ToString());
                ViewBag.AuditOption = GetmembershipWithdrawalApprovalOptionSelectList(withdrawalNotificationDTO.Status.ToString());
                ViewBag.WithdrawalNotificationCategorySelectList = GetWithdrawalNotificationCategorySelectList(withdrawalNotificationDTO.Category.ToString());
                return View(withdrawalNotificationDTO);
            }
        }

        public async Task<ActionResult> Settlement(Guid id)
        {
            await ServeNavigationMenus();

            ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(string.Empty);


            var creditBatchDTO = await _channelService.FindWithdrawalNotificationAsync(id, GetServiceHeader());

            ViewBag.WithdrawalNotificationCategorySelectList = GetWithdrawalNotificationCategorySelectList(string.Empty);
            return View(creditBatchDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Settlement(WithdrawalNotificationDTO withdrawalNotificationDTO)
        {
            withdrawalNotificationDTO.ValidateAll();

            if (!withdrawalNotificationDTO.HasErrors)
            {
                await _channelService.SettleWithdrawalNotificationAsync(withdrawalNotificationDTO, (int)MembershipWithdrawalSettlementOption.Settle, 1, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = withdrawalNotificationDTO.ErrorMessages;
                ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(withdrawalNotificationDTO.SettlementType.ToString());
                ViewBag.WithdrawalNotificationCategorySelectList = GetWithdrawalNotificationCategorySelectList(withdrawalNotificationDTO.Category.ToString());
                return View(withdrawalNotificationDTO);
            }
        }


        public async Task<ActionResult> DeathClaim(Guid id)
        {
            await ServeNavigationMenus();

            ViewBag.WithdrawalNotificationCategorySelectList = GetWithdrawalNotificationCategorySelectList(string.Empty);
            var creditBatchDTO = await _channelService.FindWithdrawalNotificationAsync(id, GetServiceHeader());

            ViewBag.WithdrawalNotificationCategorySelectList = GetWithdrawalNotificationCategorySelectList(string.Empty);
           

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }
            // await _channelService.FindCustomerAccountsByCustomerIdAndProductCodesAsync(parseId,int[], false, false, false, false, GetServiceHeader());
            var customer = await _channelService.FindCustomerAsync(parseId, GetServiceHeader());

            WithdrawalNotificationDTO withdrawalNotificationDTO = new WithdrawalNotificationDTO();

            if (customer != null)
            {

                withdrawalNotificationDTO.CustomerId = customer.Id;
                withdrawalNotificationDTO.CustomerFullName = customer.FullName;
                withdrawalNotificationDTO.CustomerIndividualPayrollNumbers = customer.IndividualPayrollNumbers;
                withdrawalNotificationDTO.CustomerSerialNumber = customer.SerialNumber;
                withdrawalNotificationDTO.CustomerIndividualIdentityCardNumber = customer.IndividualIdentityCardNumber;
                withdrawalNotificationDTO.CustomerStationDescription = customer.StationDescription;
                withdrawalNotificationDTO.CustomerStationZoneDivisionEmployerDescription = customer.StationZoneDivisionEmployerDescription;
            }
            //var k = await _channelService.FindCustomerAccountsByCustomerIdAndCustomerAccountTypeTargetProductIdAsync();
            return View(creditBatchDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeathClaim(WithdrawalNotificationDTO withdrawalNotificationDTO, ObservableCollection<WithdrawalSettlementDTO> withdrawalSettlementDTOs, InsuranceCompanyDTO insuranceCompanyDTO)
        {
            //var mandatoryInvestmentProducts = new ObservableCollection<WithdrawalSettlementDTO>();
            var k =await _channelService.FindWithdrawalSettlementsByWithdrawalNotificationIdAsync(withdrawalNotificationDTO.Id, false, GetServiceHeader());
            k = withdrawalSettlementDTOs;
            withdrawalNotificationDTO.ValidateAll();

            if (!withdrawalNotificationDTO.HasErrors)
            {
                await _channelService.ProcessDeathSettlementsAsync(withdrawalNotificationDTO, withdrawalSettlementDTOs, insuranceCompanyDTO,1, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = withdrawalNotificationDTO.ErrorMessages;
                ViewBag.WithdrawalNotificationCategorySelectList = GetWithdrawalNotificationCategorySelectList(withdrawalNotificationDTO.Category.ToString());
                return View(withdrawalNotificationDTO);
            }
        }

        /* [HttpGet]
         public async Task<JsonResult> GetWithdrawalNotificationsAsync()
         {
             var withdrawalNotificationDTOs = await _channelService.FindWithdrawalNotificationsAsync(false, true, GetServiceHeader());

             return Json(withdrawalNotificationDTOs, JsonRequestBehavior.AllowGet);
         }*/
    }
}