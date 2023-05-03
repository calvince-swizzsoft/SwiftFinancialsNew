using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.BackOfficeModule;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.Loaning.Controllers
{
    public class LoanRequestController : MasterController
    {
        // GET: Loaning/LoanRequest
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<JsonResult> Index(JQueryDataTablesModel jQueryDataTablesModel)
        {
            int totalRecordCount = 0;

            int searchRecordCount = 0;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindLoanRequestsByFilterInPageAsync(DateTime.Today, DateTime.Today, jQueryDataTablesModel.sSearch, 1, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<LoanRequestDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var loanRequestDTO = await _channelService.FindLoanRequestAsync(id, GetServiceHeader());

            return View(loanRequestDTO);
        }

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(LoanRequestDTO loanRequestDTO)
        {
            loanRequestDTO.ValidateAll();

            if (!loanRequestDTO.HasErrors)
            {
                await _channelService.AddLoanRequestAsync(loanRequestDTO, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = loanRequestDTO.ErrorMessages;

                return View(loanRequestDTO);
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var loanRequestDTO = await _channelService.FindLoanRequestAsync(id, GetServiceHeader());

            return View(loanRequestDTO);
        }

        /* [HttpGet]
         public async Task<JsonResult> GetLoanRequestsAsync()
         {
             var LoanRequestsDTO = await _channelService.FindLoanRequestsAsync(GetServiceHeader());

             return Json(LoanRequestsDTO, JsonRequestBehavior.AllowGet);
         }*/
    }
}

