using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.Accounts.Controllers
{
    public class DirectDebitController : MasterController
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

             var pageCollectionInfo = await _channelService.FindDirectDebitsByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

             if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
             {
                 totalRecordCount = pageCollectionInfo.ItemsCount;

                 searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                 return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
             }
             else return this.DataTablesJson(items: new List<DirectDebitDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
         }

        //public async Task<ActionResult> Details()
        //{
        //    await ServeNavigationMenus();

        //    var directDebitDTO = await _channelService.FindDirectDebitsAsync( GetServiceHeader());

        //    return View(directDebitDTO);
        //}

        public async Task<ActionResult> SavingsProduct(Guid? id, DirectDebitDTO directDebitDTO)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View("Create");
            }

            var savingsProduct = await _channelService.FindSavingsProductAsync(parseId, GetServiceHeader());
            if (savingsProduct != null)
            {
                ViewBag.ProductCode = GetProductCodeSelectList(string.Empty);
                ViewBag.ChargeType = GetChargeTypeSelectList(string.Empty);

                directDebitDTO.CustomerAccountTypeTargetProductId = savingsProduct.Id;
                directDebitDTO.CustomerAccountTypeTargetProductDescription = savingsProduct.Description;
                directDebitDTO.CustomerAccountTypeProductCode = 1;

                Session["savingsProductId"] = directDebitDTO.CustomerAccountTypeTargetProductId;
                Session["savingsProductDescription"] = directDebitDTO.CustomerAccountTypeTargetProductDescription;
            }

            return View("Create", directDebitDTO);
        }



        public async Task<ActionResult> LoansProduct(Guid? id, DirectDebitDTO directDebitDTO)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View("Create");
            }

            var loanProductsdetails = await _channelService.FindLoanProductAsync(parseId, GetServiceHeader());
            if (loanProductsdetails != null)
            {
                ViewBag.ProductCode = GetProductCodeSelectList(string.Empty);
                ViewBag.ChargeType = GetChargeTypeSelectList(string.Empty);

                directDebitDTO.CustomerAccountTypeTargetProductId = loanProductsdetails.Id;
                directDebitDTO.CustomerAccountTypeTargetProductDescription = loanProductsdetails.Description;
                directDebitDTO.CustomerAccountTypeProductCode = 2;

                Session["loanProductId"] = directDebitDTO.CustomerAccountTypeTargetProductId;
                Session["loanProductDescription"] = directDebitDTO.CustomerAccountTypeTargetProductDescription;
            }

            return View("Create", directDebitDTO);
        }



        public async Task<ActionResult> InvestmentProduct(Guid? id, DirectDebitDTO directDebitDTO)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View("Create");
            }

            var investmentProductDetails = await _channelService.FindInvestmentProductAsync(parseId, GetServiceHeader());
            if (investmentProductDetails != null)
            {
                ViewBag.ProductCode = GetProductCodeSelectList(string.Empty);
                ViewBag.ChargeType = GetChargeTypeSelectList(string.Empty);

                directDebitDTO.CustomerAccountTypeTargetProductId = investmentProductDetails.Id;
                directDebitDTO.CustomerAccountTypeTargetProductDescription = investmentProductDetails.Description;
                directDebitDTO.CustomerAccountTypeProductCode = 3;

                Session["investmentProductId"] = directDebitDTO.CustomerAccountTypeTargetProductId;
                Session["investmentProductDescription"] = directDebitDTO.CustomerAccountTypeTargetProductDescription;
            }

            return View("Create", directDebitDTO);
        }


        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            ViewBag.ProductCode = GetProductCodeSelectList(string.Empty);
            ViewBag.ChargeType = GetChargeTypeSelectList(string.Empty);

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(DirectDebitDTO directDebitDTO)
        {

            if(Session["savingsProductId"] != null)
            {
                directDebitDTO.CustomerAccountTypeTargetProductId = (Guid)Session["savingsProductId"];
                directDebitDTO.CustomerAccountTypeTargetProductDescription = Session["savingsProductDescription"].ToString();
            }

            if (Session["loanProductId"] != null)
            {
                directDebitDTO.CustomerAccountTypeTargetProductId = (Guid)Session["loanProductId"];
                directDebitDTO.CustomerAccountTypeTargetProductDescription = Session["loanProductDescription"].ToString();
            }

            if (Session["investmentProductId"] != null)
            {
                directDebitDTO.CustomerAccountTypeTargetProductId = (Guid)Session["investmentProductId"];
                directDebitDTO.CustomerAccountTypeTargetProductDescription = Session["investmentProductDescription"].ToString();
            }

            directDebitDTO.ValidateAll();

            if (!directDebitDTO.HasErrors)
            {
                await _channelService.AddDirectDebitAsync(directDebitDTO, GetServiceHeader());

                Session.Clear();
                TempData["Create"] = "Successfully Created Direct Debit";

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = directDebitDTO.ErrorMessages;

                ViewBag.ProductCode = GetProductCodeSelectList(directDebitDTO.CustomerAccountTypeProductCodeDescription.ToString());
                ViewBag.ChargeType = GetChargeTypeSelectList(directDebitDTO.ChargeType.ToString());

                TempData["CreateError"] = "Failed to create Direct Debit";

                return View(directDebitDTO);
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var directDebitDTO = await _channelService.FindDirectDebitsAsync(GetServiceHeader());

            ViewBag.ProductCode = GetProductCodeSelectList(string.Empty);
            ViewBag.ChargeType = GetChargeTypeSelectList(string.Empty);

            return View(directDebitDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, DirectDebitDTO directDebitDTO)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateDirectDebitAsync(directDebitDTO, GetServiceHeader());

                TempData["Edit"] = "Successfully Edited Direct Debit";

                return RedirectToAction("Index");
            }
            else
            {
                ViewBag.ProductCode = GetProductCodeSelectList(directDebitDTO.CustomerAccountTypeProductCode.ToString());
                ViewBag.ChargeType = GetChargeTypeSelectList(directDebitDTO.ChargeType.ToString());

                TempData["EditError"] = "Failed to Edit Direct Debit";

                return View(directDebitDTO);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetDirectDebitsAsync()
        {
            var directDebitsDTOs = await _channelService.FindDirectDebitsAsync(GetServiceHeader());

            return Json(directDebitsDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}