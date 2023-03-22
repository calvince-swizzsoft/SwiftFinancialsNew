using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Fedhaplus.DashboardApplication.Controllers;
using Fedhaplus.DashboardApplication.Helpers;
using Infrastructure.Crosscutting.Framework.Adapter;
using Infrastructure.Crosscutting.Framework.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Fedhaplus.DashboardApplication.Areas.Accounts.Controllers
{
    public class LevyController : MasterController
    {
        // GET: Accounts/Levy
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

            var pageCollectionInfo = await _channelService.FindLeviesByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }

            else return this.DataTablesJson(items: new List<LevyDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        // GET: Accounts/Levy/Details/5
        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var itemCategoryDTO = await _channelService.FindEmployerAsync(id, GetServiceHeader());

            return View(itemCategoryDTO.ProjectedAs<LevyDTO>());
        }

        //// GET: Accounts/Levy/Create
        //public async Task<ActionResult> Create()
        //{
        //    await ServeNavigationMenus();

        //    LevyDTO levyDTO = new LevyDTO
        //    {
        //        LevyChargeTypes = GetChargeTypes(string.Empty)
        //    };

        //    return View(levyDTO);
        //}

        //// POST: Accounts/Levy/Create
        //[HttpPost]
        //public async Task<ActionResult> Create(LevyDTO levyDTO)
        //{
        //    levyDTO.ChargeType =(int) ChargeType.Percentage;

        //    if (ModelState.IsValid)
        //    {
        //        await _channelService.AddLevyAsync(levyDTO, GetServiceHeader());

        //        return RedirectToAction("Index");
        //    }
        //    else
        //    {
        //        IEnumerable<ModelError> allErrors = ModelState.Values.SelectMany(v => v.Errors);

        //        levyDTO.LevyChargeTypes = GetChargeTypes(string.Empty);

        //        TempData["Error"] = string.Join(",", allErrors);

        //        return View(levyDTO);
        //    }
        //}

        // GET: Accounts/Levy/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: Accounts/Levy/Edit/5
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

        // GET: Accounts/Levy/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: Accounts/Levy/Delete/5
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

        [HttpGet]
        public async Task<JsonResult> GetLeviesAsync()
        {
            var levyDTOs = await _channelService.FindLeviesAsync(GetServiceHeader());

            return Json(levyDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}
