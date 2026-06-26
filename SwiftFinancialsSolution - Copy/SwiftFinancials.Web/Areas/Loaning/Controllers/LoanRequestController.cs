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

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "desc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindLoanRequestsByFilterInPageAsync(DateTime.Today, DateTime.Today, jQueryDataTablesModel.sSearch, 1, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(loanpurpose => loanpurpose.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else
            {
                return this.DataTablesJson(items: new List<LoanRequestDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }

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


        //LookUps
        public async Task<ActionResult> Customers(Guid? id, LoanRequestDTO loanRequestDTO)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View("Create");
            }

            if (Session["loanProductId"] != null)
            {
                loanRequestDTO.LoanProductId = (Guid)Session["loanProductId"];
                loanRequestDTO.LoanProductDescription = Session["loanProductDescription"].ToString();
            }

            if (Session["loanPurposeId"] != null)
            {
                loanRequestDTO.LoanPurposeId = (Guid)Session["loanPurposeId"];
                loanRequestDTO.LoanPurposeDescription = Session["loanPurposeDescription"].ToString();
            }

            if (Session["loanCaseId"] != null)
            {
                loanRequestDTO.LoanCaseId = (Guid)Session["loanCaseId"];
                loanRequestDTO.LoanCaseNumber = Convert.ToInt32(Session["loanCaseNumber"].ToString());

                loanRequestDTO.AmountApplied = Convert.ToDecimal(Session["AmountApplied"].ToString());
                loanRequestDTO.CancelledBy = Session["CancelledBy"].ToString();
                loanRequestDTO.RegisteredDate = (DateTime)Session["RegisteredDate"];
                loanRequestDTO.RegisteredBy = Session["RegisteredBy"].ToString();
                loanRequestDTO.Status = Convert.ToInt32(Session["Status"].ToString());
            }

            var customer = await _channelService.FindCustomerAsync(parseId, GetServiceHeader());
            if (customer != null)
            {
                loanRequestDTO.CustomerId = customer.Id;
                loanRequestDTO.CustomerIndividualFirstName = customer.FullName;

                Session["customerId"] = loanRequestDTO.CustomerId;
                Session["customerFullName"] = loanRequestDTO.CustomerIndividualFirstName;
            }

            return View("create", loanRequestDTO);
        }
        public async Task<ActionResult> LoanProducts(Guid? id, LoanRequestDTO loanRequestDTO)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View("Create");
            }

            if (Session["customerId"] != null)
            {
                loanRequestDTO.CustomerId = (Guid)Session["customerId"];
                loanRequestDTO.CustomerIndividualFirstName = Session["customerFullName"].ToString();
            } 
            
            if (Session["loanPurposeId"] != null)
            {
                loanRequestDTO.LoanPurposeId = (Guid)Session["loanPurposeId"];
                loanRequestDTO.LoanPurposeDescription = Session["loanPurposeDescription"].ToString();
            }

            if (Session["loanCaseId"] != null)
            {
                loanRequestDTO.LoanCaseId = (Guid)Session["loanCaseId"];
                loanRequestDTO.LoanCaseNumber = Convert.ToInt32(Session["loanCaseNumber"].ToString());

                loanRequestDTO.AmountApplied = Convert.ToDecimal(Session["AmountApplied"].ToString());
                loanRequestDTO.CancelledBy = Session["CancelledBy"].ToString();
                loanRequestDTO.RegisteredDate = (DateTime)Session["RegisteredDate"];
                loanRequestDTO.RegisteredBy = Session["RegisteredBy"].ToString();
                loanRequestDTO.Status = Convert.ToInt32(Session["Status"].ToString());
            }

            var loanProduct = await _channelService.FindLoanProductAsync(parseId, GetServiceHeader());
            if (loanProduct != null)
            {
                loanRequestDTO.LoanProductId = loanProduct.Id;
                loanRequestDTO.LoanProductDescription = loanProduct.Description;

                Session["loanProductId"] = loanRequestDTO.LoanProductId;
                Session["loanProductDescription"] = loanRequestDTO.LoanProductDescription;
            }

            return View("create", loanRequestDTO);
        }
        public async Task<ActionResult> LoansPurpose(Guid? id, LoanRequestDTO loanRequestDTO)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View("Create");
            }

            if (Session["customerId"] != null)
            {
                loanRequestDTO.CustomerId = (Guid)Session["customerId"];
                loanRequestDTO.CustomerIndividualFirstName = Session["customerFullName"].ToString();
            }

            if (Session["loanProductId"] != null)
            {
                loanRequestDTO.LoanProductId = (Guid)Session["loanProductId"];
                loanRequestDTO.LoanProductDescription = Session["loanProductDescription"].ToString();
            } 
            
            if (Session["loanCaseId"] != null)
            {
                loanRequestDTO.LoanCaseId = (Guid)Session["loanCaseId"];
                loanRequestDTO.LoanCaseNumber = Convert.ToInt32(Session["loanCaseNumber"].ToString());

                loanRequestDTO.AmountApplied = Convert.ToDecimal(Session["AmountApplied"].ToString());
                loanRequestDTO.CancelledBy = Session["CancelledBy"].ToString();
                loanRequestDTO.RegisteredDate = (DateTime)Session["RegisteredDate"];
                loanRequestDTO.RegisteredBy = Session["RegisteredBy"].ToString();
                loanRequestDTO.Status = Convert.ToInt32(Session["Status"].ToString());
                loanRequestDTO.CancelledDate = (DateTime)Session["CancelledDate"];
            }

            var loanPurpose = await _channelService.FindLoanPurposeAsync(parseId, GetServiceHeader());
            if (loanPurpose != null)
            {
                loanRequestDTO.LoanPurposeId = loanPurpose.Id;
                loanRequestDTO.LoanPurposeDescription = loanPurpose.Description;

                Session["loanPurposeId"] = loanRequestDTO.LoanPurposeId;
                Session["loanPurposeDescription"] = loanRequestDTO.LoanPurposeDescription;
            }

            return View("create", loanRequestDTO);
        } 
        
        public async Task<ActionResult> LoanCases(Guid? id, LoanRequestDTO loanRequestDTO)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View("Create");
            }

            if (Session["customerId"] != null)
            {
                loanRequestDTO.CustomerId = (Guid)Session["customerId"];
                loanRequestDTO.CustomerIndividualFirstName = Session["customerFullName"].ToString();
            }

            if (Session["loanProductId"] != null)
            {
                loanRequestDTO.LoanProductId = (Guid)Session["loanProductId"];
                loanRequestDTO.LoanProductDescription = Session["loanProductDescription"].ToString();
            }

            if (Session["loanPurposeId"] != null)
            {
                loanRequestDTO.LoanPurposeId = (Guid)Session["loanPurposeId"];
                loanRequestDTO.LoanPurposeDescription = Session["loanPurposeDescription"].ToString();
            }

            var loanCases = await _channelService.FindLoanCaseAsync(parseId, GetServiceHeader());
            if (loanCases != null)
            {
                loanRequestDTO.LoanCaseId = loanCases.Id;
                loanRequestDTO.LoanCaseNumber = loanCases.CaseNumber;

                loanRequestDTO.AmountApplied = loanCases.AmountApplied;
                loanRequestDTO.CancelledBy = loanCases.CancelledBy;
                loanRequestDTO.CancelledDate = loanCases.CancelledDate;
                loanRequestDTO.RegisteredDate = loanCases.CreatedDate;
                loanRequestDTO.RegisteredBy = loanCases.CreatedBy;
                loanRequestDTO.Status = loanCases.Status;

                Session["loanCaseId"] = loanRequestDTO.LoanCaseId;
                Session["loanCaseNumber"] = loanRequestDTO.LoanCaseNumber;

                Session["AmountApplied"] = loanRequestDTO.AmountApplied;
                Session["CancelledBy"] = loanRequestDTO.CancelledBy;
                Session["CancelledDate"] = loanRequestDTO.CancelledDate;
                Session["RegisteredDate"] = loanRequestDTO.RegisteredDate;
                Session["RegisteredBy"] = loanRequestDTO.RegisteredBy;
                Session["Status"] = loanRequestDTO.Status;
            }

            return View("create", loanRequestDTO);
        }
        //End Lookups


        [HttpPost]
        public async Task<ActionResult> Create(LoanRequestDTO loanRequestDTO)
        {
            loanRequestDTO.LoanProductId = (Guid)Session["loanProductId"];
            loanRequestDTO.LoanProductDescription = Session["loanProductDescription"].ToString();

            loanRequestDTO.CustomerId = (Guid)Session["customerId"];
            loanRequestDTO.CustomerIndividualFirstName = Session["customerFullName"].ToString();

            loanRequestDTO.LoanPurposeId = (Guid)Session["loanPurposeId"];
            loanRequestDTO.LoanPurposeDescription = Session["loanPurposeDescription"].ToString();

            loanRequestDTO.ValidateAll();

            if (!loanRequestDTO.HasErrors)
            {
                await _channelService.AddLoanRequestAsync(loanRequestDTO, GetServiceHeader());

                Session.Clear();

                TempData["create"] = "Successfully created Loan Request";

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = loanRequestDTO.ErrorMessages;

                TempData["createError"] = "Failed tto create Loan Request";

                return View(loanRequestDTO);
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var loanRequestDTO = await _channelService.FindLoanRequestAsync(id, GetServiceHeader());

            return View(loanRequestDTO);
        }
    }
}

