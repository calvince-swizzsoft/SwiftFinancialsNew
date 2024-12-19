using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.Accounts.Controllers
{
    public class WireTransferTypeController : MasterController
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

            var pageCollectionInfo = await _channelService.
                FindWireTransferTypesByFilterInPageAsync(jQueryDataTablesModel.sSearch, 0, int.MaxValue, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {

                var sortedData = pageCollectionInfo.PageCollection
                    .OrderByDescending(loanCase => loanCase.CreatedDate)
                    .ToList();

                totalRecordCount = sortedData.Count;

                var paginatedData = sortedData
                    .Skip(jQueryDataTablesModel.iDisplayStart)
                    .Take(jQueryDataTablesModel.iDisplayLength)
                    .ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch)
                    ? sortedData.Count
                    : totalRecordCount;

                return this.DataTablesJson(
                    items: paginatedData,
                    totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount,
                    sEcho: jQueryDataTablesModel.sEcho
                );
            }

            return this.DataTablesJson(
                items: new List<WireTransferTypeDTO>(),
                totalRecords: totalRecordCount,
                totalDisplayRecords: searchRecordCount,
                sEcho: jQueryDataTablesModel.sEcho
            );
        }


        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var applicableCharges = await _channelService.FindCommissionsByWireTransferTypeIdAsync(id, GetServiceHeader());
            var wireTransferTypes = await _channelService.FindWireTransferTypesAsync(GetServiceHeader());
            var wireTransferType = wireTransferTypes.FirstOrDefault(x => x.Id == id);

            var commissions = await _channelService.FindCommissionsAsync(GetServiceHeader());
            var applicableChargeIds = new HashSet<Guid>(applicableCharges.Select(ac => ac.Id));

            ViewBag.Commissions = commissions;
            ViewBag.CheckedStates = commissions.ToDictionary(
               c => c.Id,
               c => applicableChargeIds.Contains(c.Id)
           );

            return View(wireTransferType);
        }


        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            ViewBag.transactionOwnership = GetTransactionOwnershipSelectList(string.Empty);

            return View();
        }


        public async Task<ActionResult> ChartOfAccountLookUp(Guid? id, InsuranceCompanyDTO insuranceCompanyDTO)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            var chartOfAccount = await _channelService.FindChartOfAccountAsync(parseId, GetServiceHeader());


            if (chartOfAccount != null)
            {
                insuranceCompanyDTO.ChartOfAccountId = chartOfAccount.Id;
                insuranceCompanyDTO.ChartOfAccountAccountName = chartOfAccount.AccountName;


                return Json(new
                {
                    success = true,
                    data = new
                    {
                        ChartOfAccountId = insuranceCompanyDTO.ChartOfAccountId,
                        ChartOfAccountAccountName = insuranceCompanyDTO.ChartOfAccountAccountName
                    }
                });
            }
            return Json(new { success = false, message = "Product Not Found!" });
        }




        [HttpPost]
        public async Task<ActionResult> Create(WireTransferTypeDTO wireTransferTypeDTO, string SelectedIds)
        {
            await ServeNavigationMenus();
            var commissions = new ObservableCollection<CommissionDTO>();

            var ids = SelectedIds.Split(',').Select(Guid.Parse).ToList();

            if (ids != null)
            {
                foreach (var commission in ids)
                {
                    var foundCommission = await _channelService.FindCommissionAsync(commission, GetServiceHeader());
                    commissions.Add(foundCommission);
                }
            }

            wireTransferTypeDTO.ValidateAll();

            if (!wireTransferTypeDTO.HasErrors)
            {
                var result = await _channelService.AddWireTransferTypeAsync(wireTransferTypeDTO, GetServiceHeader());

                if (result.ErrorMessageResult != null)
                {
                    await ServeNavigationMenus();

                    ViewBag.transactionOwnership = GetTransactionOwnershipSelectList(wireTransferTypeDTO.TransactionOwnershipDescription.ToString());
                    TempData["ErrorMessageResult"] = "Operation Failed: Entry with the same name already exists!";
                    return View(wireTransferTypeDTO);
                }

                if (commissions != null)
                    await _channelService.UpdateCommissionsByWireTransferTypeIdAsync(result.Id, commissions, GetServiceHeader());

                TempData["Create"] = "Done";

                return RedirectToAction("Index");
            }
            else
            {
                await ServeNavigationMenus();
                var errorMessages = wireTransferTypeDTO.ErrorMessages;
                string errorMessage = string.Join("\n", errorMessages.Where(msg => !string.IsNullOrWhiteSpace(msg)));

                ViewBag.transactionOwnership = GetTransactionOwnershipSelectList(wireTransferTypeDTO.TransactionOwnershipDescription.ToString());

                TempData["CreateError"] = "Operation Failed: " + errorMessage;

                return View(wireTransferTypeDTO);
            }
        }
    }

}
