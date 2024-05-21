using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System;
using System.Collections.Generic;
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

            return View(debitTypeDTO);
        }


        public async Task<ActionResult> Create(Guid? id, DebitTypeDTO debitTypeDTO)
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
            }

            return View(debitTypeDTO);
        }


        [HttpPost]
        public async Task<ActionResult> Create(DebitTypeDTO debitTypeDTO, List<DebitTypeDTO> selectedRows)
        {
            debitTypeDTO.ValidateAll();

            if (!debitTypeDTO.HasErrors)
            {
                foreach (var selectedRow in selectedRows)
                {
                    var commissionsDTO = await _channelService.FindCommissionAsync(selectedRow.Id, GetServiceHeader());

                    await _channelService.UpdateCommissionAsync(commissionsDTO, GetServiceHeader());
                }

                ViewBag.ProductCodeSelectList = GetProductCodeSelectList(debitTypeDTO.CustomerAccountTypeProductCode.ToString());

                return RedirectToAction("Create");
            }
            else
            {
                var errorMessages = debitTypeDTO.ErrorMessages;
                //ViewBag.ProductCodeSelectList = GetProductCodeSelectList(debitTypeDTO.CustomerAccountTypeProductCode.ToString());

                TempData["CreateError"] = "Failed to Create Debit Type";

                return View(debitTypeDTO);
            }



            //if (!debitTypeDTO.HasErrors)
            //{
            //    await _channelService.AddDebitTypeAsync(debitTypeDTO, GetServiceHeader());

            //    TempData["Create"] = "Successfully Created Debit Type";

            //    return RedirectToAction("Index");
            //}
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();
            ViewBag.ProductCodeSelectList = GetProductCodeSelectList(string.Empty);
            var debitTypeDTO = await _channelService.FindDebitTypeAsync(id, GetServiceHeader());

            return View(debitTypeDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, DebitTypeDTO debitTypeDTOBindingModel)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateDebitTypeAsync(debitTypeDTOBindingModel, GetServiceHeader());
                ViewBag.ProductCodeSelectList = GetProductCodeSelectList(debitTypeDTOBindingModel.CustomerAccountTypeProductCode.ToString());

                TempData["Edit"] = "Successfully Edited Debit Type";

                return RedirectToAction("Index");
            }
            else
            {
                TempData["EditError"] = "Failed to Edit Debit Type";

                return View(debitTypeDTOBindingModel);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetApplicableChargesAsync()
        {
            var charges = await _channelService.FindDynamicChargesAsync(GetServiceHeader());

            return Json(charges, JsonRequestBehavior.AllowGet);
        }
    }

}
