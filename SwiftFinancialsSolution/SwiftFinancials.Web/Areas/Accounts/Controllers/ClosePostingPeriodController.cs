using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using SwiftFinancials.Presentation.Infrastructure.Util;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.Accounts.Controllers
{
    public class ClosePostingPeriodController : MasterController
    {
        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();


            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Index(JQueryDataTablesModel jQueryDataTablesModel, GeneralLedgerDTO generalLedgerDTO)
        {
            int totalRecordCount = 0;

            int searchRecordCount = 0;

            DateTime startDate = generalLedgerDTO.CreatedDate;

            DateTime endDate = startDate;
            int journalEntryFilter = 0;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindPostingPeriodsByFilterInPageAsync( jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, journalEntryFilter, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<PostingPeriodDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var generalLedgerDTO = await _channelService.FindGeneralLedgerAsync(id, GetServiceHeader());

            return View(generalLedgerDTO);
        }

        public async Task<ActionResult> Create(Guid? id)
        {
            await ServeNavigationMenus();
            ViewBag.SystemGeneralLedgerAccountCodeSelectList = GetSystemGeneralLedgerAccountCodeSelectList(string.Empty);
           
            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            var customer = await _channelService.FindPostingPeriodAsync(parseId, GetServiceHeader());

            PostingPeriodDTO customerAccountDTO = new PostingPeriodDTO();

            if (customer != null)
            {

                customerAccountDTO.Id = customer.Id;
                customerAccountDTO.Description = customer.Description;
                customerAccountDTO.CreatedDate = customer.CreatedDate;
                customerAccountDTO.DurationStartDate = customer.DurationStartDate;
                customerAccountDTO.DurationEndDate = customer.DurationEndDate;
                customerAccountDTO.CreatedBy = customer.CreatedBy;
                customerAccountDTO.ClosedBy = customer.ClosedBy;
            }           
            return View(customerAccountDTO);
        }

        [HttpPost]
        public async Task<ActionResult> Create(PostingPeriodDTO postingPeriodDTO)
        {
            //var closedDate = Request["closedDate"];
            //var endDate = Request["endDate"];
            //var startDate = Request["startDate"];

            //postingPeriodDTO.DurationStartDate = DateTime.ParseExact((Request["startDate"].ToString()), "dd/MM/yyyy", CultureInfo.InvariantCulture);

            //postingPeriodDTO.DurationEndDate = DateTime.ParseExact((Request["endDate"].ToString()), "dd/MM/yyyy", CultureInfo.InvariantCulture);

            //postingPeriodDTO.ClosedDate = DateTime.ParseExact((Request["closedDate"].ToString()), "dd/MM/yyyy", CultureInfo.InvariantCulture);
            postingPeriodDTO.ClosedDate = DateTime.Now;
            int moduleNavigationItemCode = 0;
            postingPeriodDTO.ValidateAll();

            if (!postingPeriodDTO.HasErrors)
            {
              var k=await _channelService.ClosePostingPeriodAsync(postingPeriodDTO, moduleNavigationItemCode, GetServiceHeader());
              //  var f =await _channelService.UpdatePostingPeriodAsync(postingPeriodDTO, GetServiceHeader());
                // ViewBag.SystemGeneralLedgerAccountCodeSelectList = GetSystemGeneralLedgerAccountCodeSelectList(postingPeriodDTO.ToString());
                TempData["SuccessMessage"] = "posting period closed successful.";
                await ServeNavigationMenus();
                return RedirectToAction("Create");
            }
            else
            {
                var errorMessages = postingPeriodDTO.ErrorMessages;

                return View(postingPeriodDTO);
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var postingPeriodDTO = await _channelService.FindGeneralLedgerAsync(id, GetServiceHeader());

            return View(postingPeriodDTO.MapTo<GeneralLedgerDTO>());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, GeneralLedgerDTO generalLedgerDTO)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateGeneralLedgerAsync(generalLedgerDTO.MapTo<GeneralLedgerDTO>(), GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                return View(generalLedgerDTO);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetPostingPeriodsAsync()
        {
            var postingPeriodsDTOs = await _channelService.FindPostingPeriodsAsync(GetServiceHeader());

            return Json(postingPeriodsDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}