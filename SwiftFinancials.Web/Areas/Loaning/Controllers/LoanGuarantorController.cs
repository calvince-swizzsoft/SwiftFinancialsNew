using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.BackOfficeModule;
using Application.MainBoundedContext.DTO.RegistryModule;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.Loaning.Controllers
{
    public class LoanGuarantorController : MasterController
    {
        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();

            return View();
        }

        //[HttpPost]
        //public async Task<JsonResult> Index(JQueryDataTablesModel jQueryDataTablesModel)
        //{
        //    int totalRecordCount = 0;

        // int searchRecordCount = 0;

        //    var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

        //   var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

        //    var pageCollectionInfo = await _channelService.FindLoanGuarantorsByCustomerIdAndFilterInPageAsync(customerId, jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iColumns, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, false, GetServiceHeader());

        //    if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
        //    {
        //       totalRecordCount = pageCollectionInfo.ItemsCount;

        //       searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

        //        return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        //    }
        //    else return this.DataTablesJson(items: new List<LoanGuarantorDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        //}

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var loanGuarantorDTO = await _channelService.FindLoanGuarantorAsync(id, GetServiceHeader());

            return View(loanGuarantorDTO);
        }

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            return View();
        }


        [HttpPost]
        public async Task<ActionResult> Search(LoanGuarantorDTO loanGuarantorDTO)
        {
            await ServeNavigationMenus();

            var loanCasesDTOs = await _channelService.FindLoanCaseByLoanCaseNumberAsync(loanGuarantorDTO.LoanCaseCaseNumber, GetServiceHeader());

            LoanGuarantorDTOs = TempData["LoanGuarantorsDTOs"] as ObservableCollection<LoanGuarantorDTO>;

            foreach (var loanCaseDTO in loanCasesDTOs.ToList())
            {
                loanGuarantorDTO.LoanCaseCaseNumber = loanCaseDTO.CaseNumber; // Use the provided caseNumber
                loanGuarantorDTO.LoanCaseId = loanCaseDTO.Id;
                loanGuarantorDTO.LoaneeCustomerIndividualFirstName = loanCaseDTO.CustomerIndividualFirstName;
                loanGuarantorDTO.LoanCaseAmountApplied = loanCaseDTO.AmountApplied;
                loanGuarantorDTO.LoanProductId = loanCaseDTO.LoanProductId;
                loanGuarantorDTO.LoaneeCustomerId = loanCaseDTO.CustomerId;
                loanGuarantorDTO.LoanProductDescription = loanCaseDTO.LoanProductDescription;
            }

            TempData["LoanGuarantorsDTOs"] = loanGuarantorDTO;

            return View("Create", loanGuarantorDTO);
        }


        [HttpPost]
        public async Task<ActionResult> SearchA(LoanGuarantorDTO loanGuarantorDTO)
        {
            await ServeNavigationMenus();

            loanGuarantorDTO = TempData["LoanGuarantorsDTOs"] as LoanGuarantorDTO;

            var customerDTOs = await _channelService.FindCustomersAsync(GetServiceHeader());

            LoanGuarantorDTOs = TempData["LoanGuarantorDTOs"] as ObservableCollection<LoanGuarantorDTO>;

            var customerDTO = customerDTOs.FirstOrDefault();
            if (customerDTOs != null)
            {
                loanGuarantorDTO.CustomerId = customerDTO.Id;
                loanGuarantorDTO.CustomerIndividualIdentityCardNumber = customerDTO.IndividualIdentityCardNumber;
                loanGuarantorDTO.CustomerIndividualFirstName = customerDTO.IndividualFirstName;
                loanGuarantorDTO.CustomerIndividualLastName = customerDTO.IndividualLastName;

            }
            
            TempData["LoanGuarantorDTOs"] = loanGuarantorDTO;

            return View("Create", loanGuarantorDTO);
        }

        [HttpPost]
        public async Task<ActionResult> Create(LoanGuarantorDTO loanGuarantorDTO)
        {
            
            loanGuarantorDTO = TempData["LoanGuarantorDTOs"] as LoanGuarantorDTO;

            loanGuarantorDTO.ValidateAll();

            if (!loanGuarantorDTO.HasErrors)
            {
                await _channelService.AddLoanGuarantorAsync(loanGuarantorDTO, GetServiceHeader());

                TempData["LoanGuarantorDTOs"] = null;

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = loanGuarantorDTO.ErrorMessages;

                return View(loanGuarantorDTO);
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var loanGuarantorDTO = await _channelService.FindLoanGuarantorAsync(id, GetServiceHeader());

            return View(loanGuarantorDTO);
        }

        /*[HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, LoanGuarantorDTO loanGuarantorBindingModel)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateLoanGuarantorAsync(loanGuarantorBindingModel, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                return View(loanGuarantorBindingModel);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetLoanGuarantorsAsync()
        {
            var loanGuarantorDTOs = await _channelService.FindLoanGuarantorsAsync(GetServiceHeader());

            return Json(loanGuarantorDTOs, JsonRequestBehavior.AllowGet);
        }*/
    }
}