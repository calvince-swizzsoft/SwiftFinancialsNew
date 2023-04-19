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

        /*public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var debitTypeDTO = await _channelService.FindDebitTypesAsync(id, GetServiceHeader());

            return View(debitTypeDTO);
        }*/
        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(DebitTypeDTO debitTypeDTO)
        {
            debitTypeDTO.ValidateAll();

            if (!debitTypeDTO.HasErrors)
            {
                await _channelService.AddDebitTypeAsync(debitTypeDTO, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = debitTypeDTO.ErrorMessages;

                return View(debitTypeDTO);
            }
        }

        /*public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var debitTypeDTO = await _channelService.FindDebitTypeAsync(id, GetServiceHeader());

            return View(debitTypeDTO);
        }*/

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, DebitTypeDTO debitTypeDTOBindingModel)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateDebitTypeAsync(debitTypeDTOBindingModel, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                return View(debitTypeDTOBindingModel);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetDebitTypesAsync()
        {
            var debitTypeDTOs = await _channelService.FindDebitTypesAsync(GetServiceHeader());

            return Json(debitTypeDTOs, JsonRequestBehavior.AllowGet);
        }
    }

}
