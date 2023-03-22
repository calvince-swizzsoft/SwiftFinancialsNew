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
    public class PurchaseOrderController : MasterController
    {
        // GET: Transactions/PurchaseOrder
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

            var pageCollectionInfo = await _channelService.FindPurchaseOrdersByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }

            else return this.DataTablesJson(items: new List<PurchaseOrderDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<JsonResult> PurchaseOrderItemList(JQueryDataTablesModel jQueryDataTablesModel)
        {
            int totalRecordCount = 0;

            int searchRecordCount = 0;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var purchaseOrderDTO = TempData["PurchaseOrderDTO"] as PurchaseOrderDTO;

            var purchaseOrderItemDTOs = await _channelService.FindPurchaseOrderItemsByPurchaseOrderIdAsync(purchaseOrderDTO.Id, GetServiceHeader());

            if (purchaseOrderItemDTOs != null && purchaseOrderItemDTOs.Any())
            {
                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? purchaseOrderItemDTOs.Count : totalRecordCount;

                return this.DataTablesJson(items: purchaseOrderItemDTOs, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }

            else return this.DataTablesJson(items: new List<PurchaseOrderItemDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        // GET: Transactions/PurchaseOrder/Create
        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            ViewBag.PurchaseOrderItems = null;

            TempData["PurchaseOrderItemBindingModel"] = null;

            ViewBag.TransactionPaymentMethods = GetTransactionPaymentMethods(string.Empty);

            ViewBag.TotalAmount = 0m;

            return View();
        }

        // POST: Transactions/PurchaseOrder/Create
        [HttpPost]
        public async Task<ActionResult> AddAsync(PurchaseOrderItemBindingModel purchaseOrderItemBindingModel)
        {
            await ServeNavigationMenus();

            PurchaseOrderItems = TempData["PurchaseOrderItemBindingModel"] as ObservableCollection<PurchaseOrderItemBindingModel>;

            if (PurchaseOrderItems == null)
                PurchaseOrderItems = new ObservableCollection<PurchaseOrderItemBindingModel>();

            PurchaseOrderItems.Add(purchaseOrderItemBindingModel);

            TempData["PurchaseOrderItemBindingModel"] = PurchaseOrderItems;
           
            ViewBag.PurchaseOrderItems = PurchaseOrderItems;

            ViewBag.TransactionPaymentMethods = GetTransactionPaymentMethods(string.Empty);

            return View("Create", purchaseOrderItemBindingModel);
        }

        [HttpPost]
        public async Task<ActionResult> Create(PurchaseOrderItemBindingModel purchaseOrderItemBindingModel)
        {
            await ServeNavigationMenus();

            ViewBag.PurchaseOrderItems = null;

            purchaseOrderItemBindingModel.ValidateAll();

            if (purchaseOrderItemBindingModel.HasErrors)
            {
                TempData["Error"] = purchaseOrderItemBindingModel.ErrorMessages;

                return View(purchaseOrderItemBindingModel);
            }

            var purchaseOrderBindingModel = new PurchaseOrderBindingModel
            {
                //CustomerId = purchaseOrderItemBindingModel.PurchaseOrderCustomerId
            };

            var purchaseOrderDTO = TempData["PurchaseOrderDTO"] as PurchaseOrderDTO;

            if (TempData["PurchaseOrderDTO"] != null)
                purchaseOrderDTO = await _channelService.FindPurchaseOrderAsync(purchaseOrderDTO.Id, GetServiceHeader());

            if (purchaseOrderDTO == null)
            {
                purchaseOrderDTO = await _channelService.AddNewPurchaseOrderAsync(purchaseOrderBindingModel.MapTo<PurchaseOrderDTO>(), GetServiceHeader());
            }

            if (purchaseOrderDTO != null)
            {
                TempData["PurchaseOrderDTO"] = purchaseOrderDTO;

                ObservableCollection<PurchaseOrderItemDTO> purchaseOrderItemDTOs = new ObservableCollection<PurchaseOrderItemDTO>();

                purchaseOrderItemDTOs.Add(purchaseOrderItemBindingModel.MapTo<PurchaseOrderItemDTO>());

                await _channelService.UpdatePurchaseOrderItemsByPurchaseOrderIdAsync(purchaseOrderDTO.Id, purchaseOrderItemDTOs, GetServiceHeader());

                ViewBag.PurchaseOrderItems = await _channelService.FindPurchaseOrderItemsByPurchaseOrderIdAsync(purchaseOrderDTO.Id, GetServiceHeader());

                ViewBag.TransactionPaymentMethods = GetTransactionPaymentMethods(string.Empty);

                var purchaseOrder = await _channelService.FindPurchaseOrderAsync(purchaseOrderDTO.Id, GetServiceHeader());

                return View(purchaseOrderItemBindingModel);
            }

            return View(purchaseOrderItemBindingModel);
        }


        // POST: Transactions/PurchaseOrder/Post
        [HttpPost]
        public async Task<ActionResult> PostPurchaseOrder(PurchaseOrderItemBindingModel purchaseOrderItemBindingModel)
        {
            await ServeNavigationMenus();

            PurchaseOrderItems = TempData["PurchaseOrderItemBindingModel"] as ObservableCollection<PurchaseOrderItemBindingModel>;

            var purchaseOrderDTO = new PurchaseOrderDTO
            {
                //CustomerId = purchaseOrderItemBindingModel.PurchaseOrderCustomerId,
                Posted = true
            };

            if (purchaseOrderDTO != null)
                purchaseOrderDTO = await _channelService.AddNewPurchaseOrderAsync(purchaseOrderDTO, GetServiceHeader());

            if (purchaseOrderDTO != null)
            {
                foreach (var item in PurchaseOrderItems)
                {
                    if (PurchaseOrderItemDTOs == null)
                        PurchaseOrderItemDTOs = new ObservableCollection<PurchaseOrderItemDTO>();

                    PurchaseOrderItemDTOs.Add(item.MapTo<PurchaseOrderItemDTO>());
                }

                await _channelService.UpdatePurchaseOrderItemsByPurchaseOrderIdAsync(purchaseOrderDTO.Id, PurchaseOrderItemDTOs, GetServiceHeader());

                ViewBag.PurchaseOrderItems = null;

                PurchaseOrderItemDTOs = null;

                return RedirectToAction("Index");
            }

            return View("Create");
        }

        // GET: Transactions/PurchaseOrder/Edit/5
        public async Task<ActionResult> Edit(int id)
        {
            await ServeNavigationMenus();

            return View();
        }

        // POST: Transactions/PurchaseOrder/Edit/5
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

        // GET: Transactions/PurchaseOrder/Delete/5
        public async Task<ActionResult> Delete(int id)
        {
            await ServeNavigationMenus();

            return View();
        }

        // POST: Transactions/PurchaseOrder/Delete/5
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
