using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.TransactionsModule;
using Fedhaplus.DashboardApplication.Controllers;
using Fedhaplus.DashboardApplication.Helpers;
using Fedhaplus.Presentation.Infrastructure.Util;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Fedhaplus.DashboardApplication.Areas.Transactions.Controllers
{
    public class SaleOrderController : MasterController
    {
        // GET: Transactions/SaleOrder
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

        public async Task<JsonResult> SaleOrderItemList(JQueryDataTablesModel jQueryDataTablesModel)
        {
            int totalRecordCount = 0;

            int searchRecordCount = 0;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var SaleOrderDTO = TempData["SaleOrderDTO"] as SaleOrderDTO;

            var SaleOrderItemDTOs = await _channelService.FindSaleOrderItemsBySaleOrderIdAsync(SaleOrderDTO.Id, GetServiceHeader());

            if (SaleOrderItemDTOs != null && SaleOrderItemDTOs.Any())
            {
                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? SaleOrderItemDTOs.Count : totalRecordCount;

                return this.DataTablesJson(items: SaleOrderItemDTOs, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }

            else return this.DataTablesJson(items: new List<SaleOrderItemDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }


        // GET: Transactions/SaleOrder/Create
        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            ViewBag.SaleOrderItems = null;

            TempData["SaleOrderItemBindingModel"] = null;

            ViewBag.TransactionPaymentMethods = GetTransactionPaymentMethods(string.Empty);

            ViewBag.TotalAmount = 0m;

            return View();
        }

        // POST: Transactions/SaleOrder/Create
        [HttpPost]
        public async Task<ActionResult> AddAsync(SaleOrderItemBindingModel saleOrderItemBindingModel)
        {
            await ServeNavigationMenus();

            SaleOrderItems = TempData["SaleOrderItemBindingModel"] as ObservableCollection<SaleOrderItemBindingModel>;

            if (SaleOrderItems == null)
                SaleOrderItems = new ObservableCollection<SaleOrderItemBindingModel>();

            SaleOrderItems.Add(saleOrderItemBindingModel);

            TempData["SaleOrderItemBindingModel"] = SaleOrderItems;
         
            ViewBag.SaleOrderItems = SaleOrderItems;

            ViewBag.TransactionPaymentMethods = GetTransactionPaymentMethods(string.Empty);

            return View("Create", saleOrderItemBindingModel);
        }

        // POST: Transactions/SaleOrder/Create
        [HttpPost]
        public async Task<ActionResult> Create(SaleOrderItemBindingModel saleOrderItemBindingModel)
        {
            await ServeNavigationMenus();

            ViewBag.SaleOrderItems = null;

            saleOrderItemBindingModel.ValidateAll();

            if (saleOrderItemBindingModel.HasErrors)
            {
                TempData["Error"] = saleOrderItemBindingModel.ErrorMessages;

                return View(saleOrderItemBindingModel);
            }

            var saleOrderBindingModel = new SaleOrderBindingModel
            {
                CustomerId = saleOrderItemBindingModel.SaleOrderCustomerId
            };

            var saleOrderDTO = TempData["SaleOrderDTO"] as SaleOrderDTO;

            if (TempData["SaleOrderDTO"] != null)
                saleOrderDTO = await _channelService.FindSaleOrderAsync(saleOrderDTO.Id, GetServiceHeader());

            if (saleOrderDTO == null)
            {
                saleOrderDTO = await _channelService.AddNewSaleOrderAsync(saleOrderBindingModel.MapTo<SaleOrderDTO>(), GetServiceHeader());
            }

            if (saleOrderDTO != null)
            {
                TempData["SaleOrderDTO"] = saleOrderDTO;

                ObservableCollection<SaleOrderItemDTO> SaleOrderItemDTOs = new ObservableCollection<SaleOrderItemDTO>();

                SaleOrderItemDTOs.Add(saleOrderItemBindingModel.MapTo<SaleOrderItemDTO>());

                await _channelService.UpdateSaleOrderItemsBySaleOrderIdAsync(saleOrderDTO.Id, SaleOrderItemDTOs, GetServiceHeader());

                ViewBag.SaleOrderItems = await _channelService.FindSaleOrderItemsBySaleOrderIdAsync(saleOrderDTO.Id, GetServiceHeader());

                ViewBag.TransactionPaymentMethods = GetTransactionPaymentMethods(string.Empty);

                return View(saleOrderItemBindingModel);
            }

            return View(saleOrderItemBindingModel);
        }

        // POST: Transactions/SaleOrder/Post
        public async Task<ActionResult> PostSaleOrder(SaleOrderItemBindingModel saleOrderItemBindingModel)
        {
            await ServeNavigationMenus();

            SaleOrderItems = TempData["SaleOrderItemBindingModel"] as ObservableCollection<SaleOrderItemBindingModel>;

            var saleOrderDTO = new SaleOrderDTO
            {
                CustomerId = saleOrderItemBindingModel.SaleOrderCustomerId,
                Posted = true
            };

            if (saleOrderDTO != null)
                saleOrderDTO = await _channelService.AddNewSaleOrderAsync(saleOrderDTO, GetServiceHeader());

            if (saleOrderDTO != null)
            {
                foreach (var item in SaleOrderItems)
                {
                    if (SaleOrderItemDTOs == null)
                        SaleOrderItemDTOs = new ObservableCollection<SaleOrderItemDTO>();

                    SaleOrderItemDTOs.Add(item.MapTo<SaleOrderItemDTO>());
                }

                await _channelService.UpdateSaleOrderItemsBySaleOrderIdAsync(saleOrderDTO.Id, SaleOrderItemDTOs, GetServiceHeader());

                ViewBag.SaleOrderItems = null;

                SaleOrderItemDTOs = null;

                return RedirectToAction("Index");
            }

            return View("Create");
        }


        // GET: Transactions/SaleOrder/Edit/5
        public async Task<ActionResult> Edit(int id)
        {
            await ServeNavigationMenus();

            return View();
        }

        // POST: Transactions/SaleOrder/Edit/5
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

        // GET: Transactions/SaleOrder/Delete/5
        public async Task<ActionResult> Delete(int id)
        {
            await ServeNavigationMenus();

            return View();
        }

        // POST: Transactions/SaleOrder/Delete/5
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
