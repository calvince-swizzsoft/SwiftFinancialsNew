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

            return View(fixedDepositTypeDTO);
        }

        public async Task<ActionResult> Create(Guid? Id)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (Id == Guid.Empty || !Guid.TryParse(Id.ToString(), out parseId))
            {
                return View();
            }

            var loanProduct = await _channelService.FindLoanProductAsync(parseId, GetServiceHeader());


            FixedDepositTypeDTO loanProductDTO = new FixedDepositTypeDTO();

            if (loanProduct != null)
            {
                loanProductDTO.Months = loanProduct.LoanRegistrationTermInMonths;
                //loanProductDTO.] = loanProduct.Id;
                //loanProductDTO.EnforceMonthValueDate = loanProduct.Id;
                //loanProductDTO.InterestChargedChartOfAccountId = loanProduct.Id;
                //loanProductDTO.InterestReceivedChartOfAccountAccountName = loanProduct.AccountName;
            }
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(FixedDepositTypeDTO fixedDepositTypeDTO, ObservableCollection<LevyDTO> selectedRows)
        {
            fixedDepositTypeDTO.ValidateAll();
            bool enforceFixedDepositBands = false;
            if (!fixedDepositTypeDTO.HasErrors)
            {
                var result = await _channelService.AddFixedDepositTypeAsync(fixedDepositTypeDTO, enforceFixedDepositBands, GetServiceHeader());
                await _channelService.UpdateLeviesByFixedDepositTypeIdAsync(fixedDepositTypeDTO.Id, selectedRows, GetServiceHeader());
                // if (result.ErrorMessageResult != null || result.ErrorMessageResult != string.Empty)
                if (result.ErrorMessageResult != null)
                {
                    TempData["ErrorMsg"] = result.ErrorMessageResult;
                    await ServeNavigationMenus();
                    return View();
                }
                await _channelService.UpdateLeviesByFixedDepositTypeIdAsync(fixedDepositTypeDTO.Id, selectedRows, GetServiceHeader());
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
