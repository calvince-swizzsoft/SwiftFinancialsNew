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
    public class CreditTypeController : MasterController
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

            var pageCollectionInfo = await _channelService.FindCreditTypesByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<CreditTypeDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var creditTypeDTO = await _channelService.FindCreditTypeAsync(id, GetServiceHeader());

            return View(creditTypeDTO);
        }
        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();
            ViewBag.TransactionOwnershipSelectList = GetTransactionOwnershipSelectList(string.Empty);
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(CreditTypeDTO creditTypeDTO)
        {
            creditTypeDTO.ValidateAll();

            if (!creditTypeDTO.HasErrors)
            {
                await _channelService.AddCreditTypeAsync(creditTypeDTO, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = creditTypeDTO.ErrorMessages;
                ViewBag.TransactionOwnershipSelectList = GetTransactionOwnershipSelectList(creditTypeDTO.TransactionOwnershipDescription.ToString());
                return View(creditTypeDTO);
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            ViewBag.TransactionOwnershipSelectList = GetTransactionOwnershipSelectList(string.Empty);

            var creditTypeDTO = await _channelService.FindCreditTypeAsync(id, GetServiceHeader());

            return View(creditTypeDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, CreditTypeDTO creditTypeDTOBindingModel)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateCreditTypeAsync(creditTypeDTOBindingModel, GetServiceHeader());

                ViewBag.TransactionOwnershipSelectList = GetTransactionOwnershipSelectList(creditTypeDTOBindingModel.TransactionOwnershipDescription.ToString());

                return RedirectToAction("Index");
            }
            else
            {
                return View(creditTypeDTOBindingModel);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetCreditTypesAsync()
        {
            var creditTypeDTOs = await _channelService.FindCreditTypesAsync(GetServiceHeader());

            return Json(creditTypeDTOs, JsonRequestBehavior.AllowGet);
        }
    }
    
    }
