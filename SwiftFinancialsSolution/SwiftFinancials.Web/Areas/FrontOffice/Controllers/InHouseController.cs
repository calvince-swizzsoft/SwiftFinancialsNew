using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.FrontOfficeModule;
using Infrastructure.Crosscutting.Framework.Utils;
using SwiftFinancials.Web.Controllers;
using System.Collections.ObjectModel;
using SwiftFinancials.Web.Helpers;
using Microsoft.AspNet.Identity;  // Add this to access GetUserId()


namespace SwiftFinancials.Web.Areas.FrontOffice.Controllers
{
    public class InHouseController : MasterController
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

            int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;


            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindInHouseChequesByFilterInPageAsync(jQueryDataTablesModel.sSearch, pageIndex, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(postingPeriod => postingPeriod.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<InHouseChequeDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        [HttpPost]
        public async Task<ActionResult> FindExternalChequesByDate(JQueryDataTablesModel jQueryDataTablesModel, DateTime startDate, DateTime endDate)
        {
            int totalRecordCount = 0;
            int searchRecordCount = 0;


            int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;
            int pageSize = jQueryDataTablesModel.iDisplayLength;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc";

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindInHouseChequesByDateRangeAndFilterInPageAsync(
                startDate,
                endDate,
                jQueryDataTablesModel.sSearch,
                pageIndex,
                pageSize,
                GetServiceHeader()
            );

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(
                    items: pageCollectionInfo.PageCollection,
                    totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount,
                    sEcho: jQueryDataTablesModel.sEcho
                );
            }
            else
            {
                return this.DataTablesJson(
                    items: new List<InHouseChequeDTO>(),
                    totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount,
                    sEcho: jQueryDataTablesModel.sEcho
                );
            }
        }
        public async Task<ActionResult> Details()
        {
            await ServeNavigationMenus();
            return View();

        }


        public async Task<ActionResult> Create(Guid? id)
        {
            await ServeNavigationMenus();

            var inHouseChequeDTO = new InHouseChequeDTO();

            if (id.HasValue && id != Guid.Empty)
            {
                var parseId = id.Value;

                var branchDTO = await _channelService.FindBranchAsync(parseId, GetServiceHeader());

                if (branchDTO != null)
                {
                    inHouseChequeDTO.BranchId = branchDTO.Id;
                    inHouseChequeDTO.BranchDescription = branchDTO.Description;
                }
                else
                {
                    TempData["ErrorMessage"] = "Branch details could not be found.";
                    return RedirectToAction("Index");
                }
            }
            else
            {
                inHouseChequeDTO.BranchId = Guid.Parse("143570C6-48BB-E811-A814-000C29142092");
                inHouseChequeDTO.BranchDescription = "HEAD OFFICE";
            }

            TempData["BranchId"] = inHouseChequeDTO.BranchId;
            TempData["BranchDescription"] = inHouseChequeDTO.BranchDescription;

            if (TempData["SuccessMessage"] != null)
            {
                ViewBag.SuccessMessage = TempData["SuccessMessage"];
                ViewBag.ChequePrintData = TempData["ChequePrintData"] as InHouseChequeDTO;
            }

            return View(inHouseChequeDTO);
        }


        //public async Task<ActionResult> Create(Guid? id)
        //{
        //    await ServeNavigationMenus();

        //    var inHouseChequeDTO = new InHouseChequeDTO();

        //    var userDTO = await _applicationUserManager.FindByIdAsync(User.Identity.GetUserId());

        //    if (userDTO.BranchId != null)
        //    {
        //        inHouseChequeDTO.BranchId = (Guid)userDTO.BranchId;
        //    }

        //    return View(inHouseChequeDTO);
        //}







        public async Task<ActionResult> GetGLAccounts()
        {
            try
            {
                var glAccounts = await _channelService.FindChartOfAccountsAsync(GetServiceHeader());
                return Json(new
                {
                    draw = Request["draw"],
                    recordsTotal = glAccounts.Count(),
                    recordsFiltered = glAccounts.Count(),
                    data = glAccounts.Select(a => new
                    {
                        a.ParentAccountName,
                        a.CostCenterDescription,
                        a.CostCenterId,
                        a.AccountCode,
                        a.AccountName,
                        a.AccountTypeDescription,
                        a.Id
                    })
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public async Task<ActionResult> GetCustomerAccountDetails(Guid customerAccountId)
        {
            try
            {
                var customerAccount = await _channelService.FindCustomerAccountAsync(customerAccountId, includeBalances: true);
                if (customerAccount == null)
                {
                    return Json(new { success = false, message = "Customer account not found." }, JsonRequestBehavior.AllowGet);
                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        customerAccountId = customerAccount.Id,
                        customerAccountFullAccountNumber = customerAccount.FullAccountNumber,
                        availableBalance = customerAccount.AvailableBalance,
                        accountId = customerAccount.CustomerId,
                        DebitCustomerAccountCustomerAccountTypeProductCode = customerAccount.CustomerAccountTypeProductCode,
                        DebitCustomerAccountCustomerAccountTypeTargetProductId = customerAccount.CustomerAccountTypeTargetProductId,
                    }
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while fetching the customer account details." }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public async Task<ActionResult> GetChequeTypeDetails(Guid chequeTypeId)
        {
            try
            {
                var chequeType = await _channelService.FindChequeTypeAsync(chequeTypeId);
                if (chequeType == null)
                {
                    return Json(new { success = false, message = "Cheque type not found." }, JsonRequestBehavior.AllowGet);
                }

                return Json(new
                {
                    success = true,
                    chequeTypeId = chequeType.Id,
                    chequeTypeDescription = chequeType.Description
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while fetching cheque type details." }, JsonRequestBehavior.AllowGet);
            }
        }

        public async Task<ActionResult> GetBranches()
        {
            try
            {
                var branches = await _channelService.FindBranchesAsync(GetServiceHeader());
                return Json(new
                {
                    draw = Request["draw"],
                    recordsTotal = branches.Count(),
                    recordsFiltered = branches.Count(),
                    data = branches.Select(b => new
                    {
                        b.Id,
                        b.Description
                    })
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }




        [HttpPost]
        public JsonResult AddEntry(InHouseChequeDTO chequeEntry)
        {
            try
            {
                if (TempData["ChequeEntries"] == null)
                {
                    TempData["ChequeEntries"] = new List<InHouseChequeDTO>();
                }

                var chequeEntries = TempData["ChequeEntries"] as List<InHouseChequeDTO>;

                if (chequeEntry.BranchId == Guid.Empty)
                {
                    chequeEntry.BranchId = Guid.Parse("143570C6-48BB-E811-A814-000C29142092");  
                }

                if (string.IsNullOrEmpty(chequeEntry.BranchDescription))
                {
                    chequeEntry.BranchDescription = "HEAD OFFICE";  
                }

                chequeEntries.Add(chequeEntry);

                TempData["ChequeEntries"] = chequeEntries;

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, errorMessage = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult RemoveEntries(List<Guid> entryIds) 
        {
            try
            {
                if (TempData["ChequeEntries"] == null)
                {
                    return Json(new { success = false, errorMessage = "No entries found." });
                }

                var chequeEntries = TempData["ChequeEntries"] as List<InHouseChequeDTO>;

                chequeEntries.RemoveAll(entry => entryIds.Contains(entry.Id)); 

                TempData["ChequeEntries"] = chequeEntries;

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, errorMessage = ex.Message });
            }
        }


        [HttpPost]
        public async Task<ActionResult> Create()
        {
            try
            {
                var chequeEntries = TempData["ChequeEntries"] as List<InHouseChequeDTO>;

                if (chequeEntries == null || chequeEntries.Count == 0)
                {
                    return RedirectToAction("Index"); 
                }

                var serviceHeader = GetServiceHeader();
                var moduleNavigationItemCode = 1234;

                var result = await _channelService.AddInHouseChequesAsync(
                    new ObservableCollection<InHouseChequeDTO>(chequeEntries),
                    moduleNavigationItemCode,
                    serviceHeader);

                if (result)
                {
                    TempData.Remove("ChequeEntries");

                    TempData["SuccessMessage"] = "Cheque entries submitted successfully.";
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to submit the cheque entries.";
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred: " + ex.Message;
                return RedirectToAction("Index");
            }
        }








        public async Task<ActionResult> Printing(Guid? id)
        {
            await ServeNavigationMenus();

            var inHouseChequeDTO = new InHouseChequeDTO();

            if (id.HasValue && id != Guid.Empty)
            {
                var bankLinkageDTO = await _channelService.FindBankLinkageAsync(id.Value, GetServiceHeader());

                if (bankLinkageDTO != null)
                {
                    inHouseChequeDTO.BankName = bankLinkageDTO.BankName;
                    inHouseChequeDTO.BranchDescription = bankLinkageDTO.BankBranchName;
                    inHouseChequeDTO.DebitChartOfAccountAccountName = bankLinkageDTO.ChartOfAccountAccountName;
                    inHouseChequeDTO.BankAccount = bankLinkageDTO.BankAccountNumber;

                    return View(inHouseChequeDTO);
                }
                else
                {
                    TempData["ErrorMessage"] = "Bank details could not be found.";
                    return Json(new { Error = "Bank details could not be found." });
                }
            }

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> GetPayeeLookupData(string searchText, int start, int length)
        {
            try
            {
                var pageIndex = start / length;  
                var serviceHeader = GetServiceHeader(); 

                var cheques = await _channelService.FindInHouseChequesByFilterInPageAsync(searchText, pageIndex, length, serviceHeader);

                var data = cheques.PageCollection.Select(c => new
                {
                    c.Amount,
                    c.Payee,
                    c.Funding,
                    c.Reference,
                    c.WordifiedAmount,
                    c.PaddedAmount,
                    c.ChequeNumber,
                    CreatedDate = c.CreatedDate.ToString("dd/MM/yyyy hh:mm:ss tt"),
                    c.Id
                });

                return Json(new
                {
                    draw = Request["draw"],
                    recordsTotal = cheques.ItemsCount,
                    recordsFiltered = cheques.ItemsCount,
                    data
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(500, "An error occurred while fetching payee data: " + ex.Message);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Printing(InHouseChequeDTO model)
        {
            if (ModelState.IsValid)
            {
                var bankLinkageDTO = new BankLinkageDTO();
                int moduleNavigationItemCode = 123;
                ServiceHeader serviceHeader = GetServiceHeader();

                bool result = await _channelService.PrintInHouseChequeAsync(model, bankLinkageDTO, moduleNavigationItemCode, serviceHeader);

                if (result)
                {
                    TempData["ChequePrintData"] = model;
                    TempData["SuccessMessage"] = "Cheque printed successfully!";

                    return RedirectToAction("PrintCheque");
                }
                else
                {
                    ModelState.AddModelError("", "Failed to print the cheque.");
                }
            }
            return View(model);
        }

        public ActionResult PrintCheque()
        {
            if (TempData["ChequePrintData"] != null)
            {
                ViewBag.ChequePrintData = TempData["ChequePrintData"];
                ViewBag.SuccessMessage = TempData["SuccessMessage"];
            }
            else
            {
                return RedirectToAction("Create");
            }

            return View();
        }


        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<ActionResult> Printing(InHouseChequeDTO model)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        var bankLinkageDTO = new BankLinkageDTO();
        //        int moduleNavigationItemCode = 123;
        //        ServiceHeader serviceHeader = GetServiceHeader();

        //        bool result = await _channelService.PrintInHouseChequeAsync(model, bankLinkageDTO, moduleNavigationItemCode, serviceHeader);

        //        if (result)
        //        {
        //            // Store the cheque data in TempData for use in the Index view
        //            TempData["ChequePrintData"] = model;
        //            TempData["SuccessMessage"] = "Cheque printed successfully!";
        //            return RedirectToAction("Create");
        //        }
        //        else
        //        {
        //            ModelState.AddModelError("", "Failed to print the cheque.");
        //        }
        //    }
        //    return View(model);
        //}




    }
}
