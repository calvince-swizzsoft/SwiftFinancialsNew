using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.TransactionsModule;
using Fedhaplus.DashboardApplication.Controllers;
using Fedhaplus.DashboardApplication.Helpers;
using Fedhaplus.Presentation.Infrastructure.Util;
using Infrastructure.Crosscutting.Framework.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Fedhaplus.DashboardApplication.Areas.Inventory.Controllers
{
    public class ItemDispatchController : MasterController
    {
        // GET: Inventory/ItemDispatch
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

            var pageCollectionInfo = await _channelService.FindSaleOrdersByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }

            else return this.DataTablesJson(items: new List<SaleOrderDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        // GET: Inventory/ItemDispatch/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: Inventory/ItemDispatch/Create
        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            return View();
        }

        // POST: Inventory/ItemDispatch/Create
        [HttpPost]
        public async Task<ActionResult> Create(SaleOrderBindingModel saleOrderBindingModel)
        {
            try
            {
                await ServeNavigationMenus();

                saleOrderBindingModel.ValidateAll();

                if (saleOrderBindingModel.HasErrors)
                {
                    TempData["Error"] = saleOrderBindingModel.ErrorMessages;

                    return View(saleOrderBindingModel);
                }

                var saleOrderDTO = await _channelService.AddNewSaleOrderAsync(saleOrderBindingModel.MapTo<SaleOrderDTO>(), GetServiceHeader());

                if (saleOrderDTO != null)
                {
                    var itemRegisterDTO = await _channelService.FindItemRegisterAsync(saleOrderDTO.ItemRegisterId, GetServiceHeader());

                    itemRegisterDTO.RecordStatus = (short)ItemRegisterStatus.Issued;

                    await _channelService.UpdateItemRegisterAsync(itemRegisterDTO, GetServiceHeader());

                    return RedirectToAction("Index");
                }

                return View(saleOrderBindingModel);
            }
            catch
            {
                return View();
            }
        }

        // GET: Inventory/ItemDispatch/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: Inventory/ItemDispatch/Edit/5
        [HttpPost]
        public ActionResult Edit(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add update logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: Inventory/ItemDispatch/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: Inventory/ItemDispatch/Delete/5
        [HttpPost]
        public ActionResult Delete(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }
    }
}