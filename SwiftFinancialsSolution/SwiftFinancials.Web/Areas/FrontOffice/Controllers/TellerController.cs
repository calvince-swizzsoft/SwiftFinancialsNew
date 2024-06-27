using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Infrastructure.Crosscutting.Framework.Utils;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.FrontOffice.Controllers
{
    public class TellerController : MasterController
    {

        // TellerController.cs


        public async Task<ActionResult> Index(TellerDTO tellerDTO)
        {
            await ServeNavigationMenus();
            await _channelService.FindTellersByTypeAsync(tellerDTO.Type, tellerDTO.Reference, true, GetServiceHeader());
            ViewBag.TellerTypeSelectList = GetTellerTypeSelectList(string.Empty);

            return View();
        }

        [HttpPost]
        public async Task<JsonResult> Index(JQueryDataTablesModel jQueryDataTablesModel, TellerDTO tellerDTO)
        {

            int totalRecordCount = 0;

            int searchRecordCount = 0;


            await _channelService.FindTellersByTypeAsync(tellerDTO.Type, tellerDTO.Reference, true, GetServiceHeader());

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            ViewBag.TellerTypeSelectList = GetTellerTypeSelectList(tellerDTO.Type.ToString());

            // var teller = tellerDTO.Type;

            int teller = tellerDTO.Type;

            var pageCollectionInfo = await _channelService.FindTellersByFilterInPageAsync(teller, jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, true, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(postingPeriod => postingPeriod.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }

            else return this.DataTablesJson(items: new List<TellerDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var tellerDTO = await _channelService.FindTellerAsync(id, true);

            return View(tellerDTO);
        }

        public async Task<ActionResult> Filter(JQueryDataTablesModel jQueryDataTablesModel, TellerDTO tellerDTO)
        {
            await ServeNavigationMenus();

            await _channelService.FindTellersByTypeAsync(tellerDTO.Type, tellerDTO.Reference, false, GetServiceHeader());

            return View("Index");
        }

        public async Task<ActionResult> Create(Guid? id)
        {
            await ServeNavigationMenus();
            Guid parseId;
            ViewBag.TellerTypeSelectList = GetTellerTypeSelectList(string.Empty);

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }
            //var employees = await _channelService.FindEmployeeAsync(parseId, GetServiceHeader());
            //bool includeBalances = false;
            //bool includeProductDescription = false;
            //bool includeInterestBalanceForLoanAccounts = false;
            //bool considerMaturityPeriodForInvestmentAccounts = false;
            var employee = await _channelService.FindEmployeeAsync(parseId, GetServiceHeader());

            TellerDTO tellerDTO = new TellerDTO();

            if (employee != null)
            {
                tellerDTO.EmployeeCustomerId = employee.CustomerId;
                tellerDTO.EmployeeId = employee.Id;
                tellerDTO.EmployeeCustomerIndividualFirstName = employee.CustomerIndividualFirstName;
                tellerDTO.FloatCustomerAccountId = employee.CustomerId;



            }
            return View(tellerDTO);
        }

        [HttpPost]
        public async Task<ActionResult> Create(TellerDTO tellerDTO)
        {

            tellerDTO.ValidateAll();

            if (!tellerDTO.HasErrors)
            {
                await _channelService.AddTellerAsync(tellerDTO, GetServiceHeader());
                ViewBag.TellerTypeSelectList = GetTellerTypeSelectList(tellerDTO.Type.ToString());
                TempData["SuccessMessage"] = "Create successful.";

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = tellerDTO.ErrorMessages;
                ViewBag.TellerTypeSelectList = GetTellerTypeSelectList(tellerDTO.Type.ToString());
                TempData["Error"] = "Failed successful.";
                return View("Create");
            }
        }
        public async Task<ActionResult> Find(Guid id ,TellerDTO teller)
        {
            await ServeNavigationMenus();
            Guid parseId;
            ViewBag.TellerTypeSelectList = GetTellerTypeSelectList(string.Empty);

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }
            //var employees = await _channelService.FindEmployeeAsync(parseId, GetServiceHeader());
            //bool includeBalances = false;
            //bool includeProductDescription = false;
            //bool includeInterestBalanceForLoanAccounts = false;
            //bool considerMaturityPeriodForInvestmentAccounts = false;
            var employee = await _channelService.FindEmployeeAsync(parseId, GetServiceHeader());

            TellerDTO tellerDTO = new TellerDTO();

            if (employee != null)
            {
                tellerDTO.EmployeeCustomerId = employee.CustomerId;
                tellerDTO.EmployeeId = employee.Id;
                tellerDTO.EmployeeCustomerIndividualFirstName = employee.CustomerIndividualFirstName;
                tellerDTO.FloatCustomerAccountId = employee.CustomerId;
            }
            Session["tell"] = teller;
            return View("Edit");


        }


            public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var teller = await _channelService.FindTellerAsync(id, true);
            ViewBag.TellerTypeSelectList = GetTellerTypeSelectList(string.Empty);

            Session["tell"] = teller;
            Guid parseId;
            ViewBag.TellerTypeSelectList = GetTellerTypeSelectList(string.Empty);

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }
            //var employees = await _channelService.FindEmployeeAsync(parseId, GetServiceHeader());
            //bool includeBalances = false;
            //bool includeProductDescription = false;
            //bool includeInterestBalanceForLoanAccounts = false;
            //bool considerMaturityPeriodForInvestmentAccounts = false;
            var employee = await _channelService.FindEmployeeAsync(parseId, GetServiceHeader());

            TellerDTO tellerDTO = new TellerDTO();

            if (employee != null)
            {
                tellerDTO.EmployeeCustomerId = employee.CustomerId;
                tellerDTO.EmployeeId = employee.Id;
                tellerDTO.EmployeeCustomerIndividualFirstName = employee.CustomerIndividualFirstName;
                tellerDTO.FloatCustomerAccountId = employee.CustomerId;



            }
            return View(teller);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(TellerDTO tellerBindingModel)
        {
            ViewBag.TellerTypeSelectList = GetTellerTypeSelectList(tellerBindingModel.Type.ToString());
            if (!tellerBindingModel.HasErrors)
            {

                await _channelService.UpdateTellerAsync(tellerBindingModel, GetServiceHeader());
                TempData["SuccessMessage"] = "Edit successful.";

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = tellerBindingModel.ErrorMessages;
                return View(tellerBindingModel);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetTellersAsync()
        {
            var tellersDTOs = await _channelService.FindTellersAsync(GetServiceHeader());

            return Json(tellersDTOs, JsonRequestBehavior.AllowGet);

        }
    }
}