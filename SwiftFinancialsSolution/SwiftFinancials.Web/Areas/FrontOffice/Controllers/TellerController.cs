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

            var pageCollectionInfo = await _channelService.FindTellersByFilterInPageAsync((int)TellerType.Employee, jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, true, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

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

        public async Task<ActionResult> Create(Guid? id)
        {
            await ServeNavigationMenus();
            Guid parseId;
            ViewBag.TellerTypeSelectList = GetTellerTypeSelectList(string.Empty);
            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            var customer = await _channelService.FindEmployeeAsync(parseId, GetServiceHeader());

            TellerDTO employeeBindingModel = new TellerDTO();

            if (customer != null)
            {
                employeeBindingModel.EmployeeId = customer.Id;
                employeeBindingModel.EmployeeCustomerIndividualFirstName = customer.CustomerFullName;
            }

            return View(employeeBindingModel);
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

                return View(tellerDTO);
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var tellerDTO = await _channelService.FindTellerAsync(id, true);

            return View(tellerDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, TellerDTO tellerBindingModel)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateTellerAsync(tellerBindingModel, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
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