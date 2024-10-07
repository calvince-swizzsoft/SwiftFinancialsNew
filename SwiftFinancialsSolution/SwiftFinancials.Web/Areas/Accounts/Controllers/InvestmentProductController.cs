using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.Accounts.Controllers
{
    public class InvestmentProductController : MasterController
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

            var pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;

            var pageCollectionInfo = await _channelService.FindInvestmentProductsByFilterInPageAsync(jQueryDataTablesModel.sSearch, pageIndex, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<InvestmentProductDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var investmentProductDTO = await _channelService.FindInvestmentProductAsync(id, GetServiceHeader());

            return View(investmentProductDTO);
        }


        public  async Task<ActionResult>Parent(InvestmentProductDTO investmentProductDTO, Guid? id)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }


            var parentGL = await _channelService.FindChartOfAccountAsync(parseId, GetServiceHeader());

            if(parentGL!=null)
            {
                investmentProductDTO.ParentId = parentGL.ParentId;
                investmentProductDTO.ParentChartOfAccountNameDescription = parentGL.ParentAccountName;
            }


            return View("Create", investmentProductDTO);
        }


        public async Task<ActionResult> Create(Guid? id, InvestmentProductDTO investmentProductDTO)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            var parentGL = await _channelService.FindChartOfAccountAsync(parseId, GetServiceHeader());

            if (parentGL != null)
            {
                investmentProductDTO.ParentId = parentGL.ParentId;
                investmentProductDTO.ChartOfAccountAccountName = parentGL.ParentAccountName;
            }

            ViewBag.RecoveryPrioritySelectList = GetRecoveryPrioritySelectList(string.Empty);

            return View(investmentProductDTO);
        }

        [HttpPost]
        public async Task<ActionResult> Create(InvestmentProductDTO investmentProductDTO)
        {
            investmentProductDTO.ValidateAll();

            if (!investmentProductDTO.HasErrors)
            {
                await _channelService.AddInvestmentProductAsync(investmentProductDTO, GetServiceHeader());

                TempData["AlertMessage"] = "Investment Product created successfully";

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = investmentProductDTO.ErrorMessages;

                TempData["Error"] = "Failed to create Investments Product";

                ViewBag.RecoveryPrioritySelectList = GetRecoveryPrioritySelectList(investmentProductDTO.Priority.ToString());
                return View(investmentProductDTO);
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();
            ViewBag.RecoveryPrioritySelectList = GetRecoveryPrioritySelectList(string.Empty);
            var investmentProductDTO = await _channelService.FindInvestmentProductAsync(id, GetServiceHeader());

            return View(investmentProductDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, InvestmentProductDTO investmentProductBindingModel)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateInvestmentProductAsync(investmentProductBindingModel, GetServiceHeader());

                TempData["Edit"] = "Edited Invetsments Product successfully";


                ViewBag.RecoveryPrioritySelectList = GetRecoveryPrioritySelectList(investmentProductBindingModel.Priority.ToString());
                return RedirectToAction("Index");
            }
            else
            {
                return View(investmentProductBindingModel);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetInvestmentProductsAsync()
        {
            var investmentProductDTOs = await _channelService.FindInvestmentProductsAsync(GetServiceHeader());

            return Json(investmentProductDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}
