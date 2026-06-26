using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.Accounts.Controllers
{
    public class FixedDepositTypeController : MasterController
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

            var pageCollectionInfo = await _channelService.FindFixedDepositTypesByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(levy => levy.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<FixedDepositTypeDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var fixedDepositTypeDTO = await _channelService.FindFixedDepositTypeAsync(id, GetServiceHeader());
            var AttachedloanProduct = await _channelService.FindAttachedProductsByFixedDepositTypeIdAsync(id, GetServiceHeader());
            var ApplicableLevies = await _channelService.FindLeviesByFixedDepositTypeIdAsync(id, GetServiceHeader());

            ViewBag.ApplicableLevies = ApplicableLevies;
            ViewBag.AttachedloanProduct = AttachedloanProduct.LoanProductCollection;

            return View(fixedDepositTypeDTO);
        }

        public async Task<ActionResult> Create(Guid? Id)
        {
            await ServeNavigationMenus();
            var AttachedloanProduct = await _channelService.FindLoanProductsAsync(GetServiceHeader());
            ViewBag.AttachedloanProduct = AttachedloanProduct;
            var ApplicableLevies = await _channelService.FindLeviesAsync(GetServiceHeader());
            ViewBag.ApplicableLevies = ApplicableLevies;
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(FixedDepositTypeDTO fixedDepositTypeDTO, string[] commisionIds, string[] ExcemptedommisionId)
        {
            fixedDepositTypeDTO.ValidateAll();
            bool enforceFixedDepositBands = false;

            ObservableCollection<LevyDTO> levyDTOs = new ObservableCollection<LevyDTO>();

            if (ExcemptedommisionId != null && ExcemptedommisionId.Any())
            {
                var selectedIds = ExcemptedommisionId.Select(Guid.Parse).ToList();

                foreach (var commisionid in selectedIds)
                {
                    var commission = await _channelService.FindLevyAsync(commisionid, GetServiceHeader());
                    levyDTOs.Add(commission);
                }
                // Process the selected IDs as needed
            }



            List<LoanProductDTO> commissionDTOs = new List<LoanProductDTO>();
            if (commisionIds != null && commisionIds.Any())
            {
                var selectedIds = commisionIds.Select(Guid.Parse).ToList();

                foreach (var commisionid in selectedIds)
                {
                    var commission = await _channelService.FindLoanProductAsync(commisionid, GetServiceHeader());
                    commissionDTOs.Add(commission);
                }
                // Process the selected IDs as needed
            }

            ProductCollectionInfo productCollectionInfo = new ProductCollectionInfo();
            productCollectionInfo.LoanProductCollection = commissionDTOs;
            if (!fixedDepositTypeDTO.HasErrors)
            {
                var result = await _channelService.AddFixedDepositTypeAsync(fixedDepositTypeDTO, enforceFixedDepositBands, GetServiceHeader());
                await _channelService.UpdateLeviesByFixedDepositTypeIdAsync(result.Id, levyDTOs, GetServiceHeader());
                // if (result.ErrorMessageResult != null || result.ErrorMessageResult != string.Empty)
                if (result.ErrorMessageResult != null)
                {
                    TempData["ErrorMsg"] = result.ErrorMessageResult;
                    await ServeNavigationMenus();
                    return View();
                }

                await _channelService.UpdateAttachedProductsByFixedDepositTypeIdAsync(result.Id, productCollectionInfo, GetServiceHeader());
                TempData["SuccessMessage"] = "Create successful.";
                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = fixedDepositTypeDTO.ErrorMessages;

                return View(fixedDepositTypeDTO);
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var fixedDepositTypeDTO = await _channelService.FindFixedDepositTypeAsync(id, GetServiceHeader());


            return View(fixedDepositTypeDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, FixedDepositTypeDTO fixedDepositTypeBindingModel)
        {
            if (!fixedDepositTypeBindingModel.HasErrors)
            {

                await _channelService.UpdateFixedDepositTypeAsync(fixedDepositTypeBindingModel, true);

                if (fixedDepositTypeBindingModel.ErrorMessageResult != null)
                {
                    TempData["ErrorMsg"] = fixedDepositTypeBindingModel.ErrorMessageResult;
                    await ServeNavigationMenus();
                    return View();
                }

                TempData["SuccessMessage"] = "Edit successful.";
                return RedirectToAction("Index");
            }
            else
            {
                return View(fixedDepositTypeBindingModel);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetFixedDepositTypesAsync(Guid fixeddepositid)
        {
            var fixedDepositTypeDTOs = await _channelService.FindFixedDepositTypeAsync(fixeddepositid, GetServiceHeader());

            return Json(fixedDepositTypeDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}
