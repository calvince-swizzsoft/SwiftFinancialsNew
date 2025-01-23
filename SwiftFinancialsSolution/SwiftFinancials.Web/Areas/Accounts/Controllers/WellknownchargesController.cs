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
    public class WellknownchargesController : MasterController
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

            var pageCollectionInfo = await _channelService.FindDebitBatchesByFilterInPageAsync(jQueryDataTablesModel.sSearch, pageIndex, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(debitBatch => debitBatch.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<DebitBatchDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var debitBatchDTO = await _channelService.FindDebitBatchAsync(id, GetServiceHeader());

            return View(debitBatchDTO);
        }


        public async Task<ActionResult> Wellknowncharges(SystemTransactionTypeInCommissionDTO systemTransactionTypeInCommissionDTO)
        {
          //  Session["ComplementFixedAmount"] = systemTransactionTypeInCommissionDTO.ComplementFixedAmount;
            Session["SystemTransactionType"] = systemTransactionTypeInCommissionDTO.SystemTransactionType;
            Session["ComplementType"] = systemTransactionTypeInCommissionDTO.ComplementType;
            
            return View("Create",systemTransactionTypeInCommissionDTO);
        }


        // GET: Accounts/Wellknowncharges/Create
        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();
            var commissionDTOs = await _channelService.FindCommissionsAsync(GetServiceHeader());
            ViewBag.Commisions = commissionDTOs;
            // Load dropdown lists for the view
            ViewBag.SystemTransactionType = GetSystemTransactionTypeList(string.Empty);
            ViewBag.Chargetype = GetChargeTypeSelectList(string.Empty);
            ViewBag.ChargeBenefactor = GetChargeBenefactorSelectList(string.Empty);

            return View();
        }

        [HttpGet]
        public async Task<ActionResult> FindCommissionsAsync()
        {
            var commissionDTOs = await _channelService.FindCommissionsAsync(GetServiceHeader());
            ViewBag.Commisions = commissionDTOs;
            var branchesDTOs = await _channelService.FindCommissionsAsync(GetServiceHeader());
            return Json(branchesDTOs);
        }

        [HttpGet]
        public async Task<ActionResult> GetAction(int systemTransactionTypeId)
        {
            var commissionDTOs = await _channelService.FindCommissionsAsync(GetServiceHeader());
            ViewBag.Commisions = commissionDTOs;
            var linkedTransactionTypes = await _channelService.GetCommissionsForSystemTransactionTypeAsync(systemTransactionTypeId, GetServiceHeader());

            // Identify unlinked commissions
            var unlinkedTransactionTypes = commissionDTOs.Where(c => !linkedTransactionTypes.Any(l => l.Id == c.Id)).ToList();
            ViewBag.SystemTransactionType = GetSystemTransactionTypeList(string.Empty);
            ViewBag.Chargetype = GetChargeTypeSelectList(string.Empty);
            ViewBag.ChargeBenefactor = GetChargeBenefactorSelectList(string.Empty);
            return Json(new
            {
                linkedTransactionTypes = linkedTransactionTypes.Select(c => new { c.Id, c.Description, c.MaximumCharge, c.IsLocked, c.CreatedDate }),
                unlinkedTransactionTypes = unlinkedTransactionTypes.Select(c => new { c.Id, c.Description, c.MaximumCharge, c.IsLocked, c.CreatedDate }),
                commissionDTOs
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public async Task<ActionResult> Create(SystemTransactionTypeInCommissionDTO systemTransactionTypeInCommissionDTO, string[] commisionIds)
        {
            ObservableCollection<CommissionDTO> commissionDTOs = new ObservableCollection<CommissionDTO>();
            var selectedIds = commisionIds.Select(Guid.Parse).ToList();
           // var commissions = new ObservableCollection<CommissionDTO>(selectedIds.Select(rowId => new CommissionDTO { Id = rowId }));
           
        
                foreach (var commisionid in selectedIds)
                {
                    var commission = await _channelService.FindCommissionAsync(commisionid, GetServiceHeader());
                    commissionDTOs.Add(commission);
                }
                // Process the selected IDs as needed
           
            if (!systemTransactionTypeInCommissionDTO.HasErrors)
            {
                var commissions = await _channelService.FindCommissionsAsync(GetServiceHeader());
                ViewBag.Commisions = commissions;

                await _channelService.MapSystemTransactionTypeToCommissionsAsync(
                    systemTransactionTypeInCommissionDTO.SystemTransactionType,
                    commissionDTOs,
                    new ChargeDTO
                    {
                        FixedAmount = systemTransactionTypeInCommissionDTO.ComplementFixedAmount,
                        Percentage = systemTransactionTypeInCommissionDTO.ComplementPercentage,
                        Type = systemTransactionTypeInCommissionDTO.ComplementType
                    },
                    GetServiceHeader()
                ); ;
               
                await ServeNavigationMenus();
                TempData["successfully"] = "Well Known Charge created successfully!";
                ViewBag.SystemTransactionType = GetSystemTransactionTypeList(string.Empty);
                ViewBag.Chargetype = GetChargeTypeSelectList(string.Empty);
                ViewBag.ChargeBenefactor = GetChargeBenefactorSelectList(string.Empty);
                TempData["successfully"] = "Well Known Charge created successfully!";

                return View("Create");
            }
            else
            {
                return Json(new
                {
                    success = false,
                    errorMessage = string.Join(", ", systemTransactionTypeInCommissionDTO.ErrorMessages)
                });
            }
        }
    }
}
