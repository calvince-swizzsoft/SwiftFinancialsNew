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
    public class DebitTypeController : MasterController
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

            var pageCollectionInfo = await _channelService.FindDebitTypesByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<DebitTypeDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var debitTypeDTO = await _channelService.FindDebitTypeAsync(id, GetServiceHeader());

            var applicableCharges = await _channelService.FindCommissionsByDebitTypeIdAsync(id, GetServiceHeader());

            return View(debitTypeDTO);
        }


        public async Task<ActionResult> SavingsProduct(Guid? id, DebitTypeDTO debitTypeDTO)
        {
            await ServeNavigationMenus();

            ViewBag.ProductCodeSelectList = GetProductCodeSelectList(string.Empty);

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            var savingsProduct = await _channelService.FindSavingsProductAsync(parseId, GetServiceHeader());

            if (savingsProduct != null)
            {
                debitTypeDTO.CustomerAccountTypeTargetProductId = savingsProduct.Id;
                debitTypeDTO.CustomerAccountTypeTargetProductDescription = savingsProduct.Description;

                Session["savingsProductid"] = debitTypeDTO.CustomerAccountTypeTargetProductId;
                Session["SavingsProductDescription"] = debitTypeDTO.CustomerAccountTypeTargetProductDescription;
            }

            return View("Create", debitTypeDTO);
        }


        [HttpPost]
        public async Task<ActionResult> LoansView()
        {
            return View();
        }


        [HttpPost]
        public async Task<ActionResult> InvestmentsView()
        {
            return View();
        }



        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            ViewBag.ProductCodeSelectList = GetProductCodeSelectList(string.Empty);

            return View();
        }


        public async Task<ActionResult> DebitType(DebitTypeDTO debitTypeDTO)
        {
            Session["Description"] = debitTypeDTO.Description;
            Session["CustomerAccountTypeProductCode"] = debitTypeDTO.CustomerAccountTypeProductCode;
            Session["isLocked"] = debitTypeDTO.IsLocked;

            return View("Create", debitTypeDTO);
        }


        [HttpPost]
        public async Task<ActionResult> Create(DebitTypeDTO debitTypeDTO, ObservableCollection<CommissionDTO> selectedRows)
        {
            debitTypeDTO.CustomerAccountTypeTargetProductId = (Guid)Session["savingsProductid"];
            debitTypeDTO.CustomerAccountTypeTargetProductDescription = Session["SavingsProductDescription"].ToString();

            debitTypeDTO.Description = Session["Description"].ToString();
            debitTypeDTO.CustomerAccountTypeProductCode = Convert.ToInt32(Session["CustomerAccountTypeProductCode"].ToString());
            debitTypeDTO.IsLocked = (bool)Session["isLocked"];

            debitTypeDTO.ValidateAll();

            if (!debitTypeDTO.HasErrors)
            {
                var result= await _channelService.AddDebitTypeAsync(debitTypeDTO, GetServiceHeader());

                await _channelService.UpdateCommissionsByDebitTypeIdAsync(result.Id, selectedRows, GetServiceHeader());

                TempData["Create"] = "Successfully Created Debit Type";

                debitTypeDTO = null;
                selectedRows = null;

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = debitTypeDTO.ErrorMessages;

                ViewBag.ProductCodeSelectList = GetProductCodeSelectList(debitTypeDTO.CustomerAccountTypeProductCode.ToString());

                TempData["CreateError"] = "Failed to Create Debit Type";

                return View(debitTypeDTO);
            }
        }


        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            ViewBag.ProductCodeSelectList = GetProductCodeSelectList(string.Empty);

            var debitTypeDTO = await _channelService.FindDebitTypeAsync(id, GetServiceHeader());
            Session["Description2"] = debitTypeDTO.Description;
            await GetApplicableCharges(id);

            return View(debitTypeDTO);
        }


        public async Task<ActionResult> GetApplicableCharges(Guid id)
        {
            await _channelService.FindCommissionsByDebitTypeIdAsync(id, GetServiceHeader());

            return View();
        }


        public async Task<ActionResult> SavingsProductEdit(Guid? id, DebitTypeDTO debitTypeDTO)
        {
            await ServeNavigationMenus();

            ViewBag.ProductCodeSelectList = GetProductCodeSelectList(string.Empty);

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            var savingsProduct = await _channelService.FindSavingsProductAsync(parseId, GetServiceHeader());

            if (savingsProduct != null)
            {
                debitTypeDTO.CustomerAccountTypeTargetProductId = savingsProduct.Id;
                debitTypeDTO.CustomerAccountTypeTargetProductDescription = savingsProduct.Description;

                Session["savingsProductid2"] = debitTypeDTO.CustomerAccountTypeTargetProductId;
                Session["SavingsProductDescription2"] = debitTypeDTO.CustomerAccountTypeTargetProductDescription;
            }

            return View("Edit", debitTypeDTO);
        }


        public async Task<ActionResult> DebitTypeEdit(DebitTypeDTO debitTypeDTO)
        {
            Session["Description2"] = debitTypeDTO.Description;
            Session["ProductCode2"] = debitTypeDTO.ProductCode;
            Session["isLocked2"] = debitTypeDTO.IsLocked;

            return View("Edit", debitTypeDTO);
        }


        [HttpPost]
       
        public async Task<ActionResult> Edit(DebitTypeDTO debitTypeDTO, ObservableCollection<CommissionDTO> selectedRows)
        {
            debitTypeDTO.CustomerAccountTypeTargetProductId = (Guid)Session["savingsProductid2"];
            debitTypeDTO.CustomerAccountTypeTargetProductDescription = Session["SavingsProductDescription2"].ToString();

            debitTypeDTO.Description = Session["Description2"].ToString();
            debitTypeDTO.ProductCode = Convert.ToInt32(Session["ProductCode2"].ToString());
            debitTypeDTO.IsLocked = (bool)Session["isLocked2"];

            debitTypeDTO.ValidateAll();

            if (ModelState.IsValid)
            {
                var result = await _channelService.UpdateDebitTypeAsync(debitTypeDTO, GetServiceHeader());

                var findDebitId = await _channelService.FindDebitTypeAsync(debitTypeDTO.Id, GetServiceHeader());

                await _channelService.UpdateCommissionsByDebitTypeIdAsync(findDebitId.Id, selectedRows, GetServiceHeader());

                TempData["Edit"] = "Successfully Edited Debit Type";

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = debitTypeDTO.ErrorMessages;

                ViewBag.ProductCodeSelectList = GetProductCodeSelectList(debitTypeDTO.CustomerAccountTypeProductCode.ToString());

                TempData["EditError"] = "Failed to Edit Debit Type";

                return View(debitTypeDTO);
            }
        }
    }

}
