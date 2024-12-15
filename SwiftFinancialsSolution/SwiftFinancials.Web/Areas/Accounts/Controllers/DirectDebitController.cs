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

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(commission => commission.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<DirectDebitDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }


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
                ViewBag.LoanProductSection = GetLoanRegistrationLoanProductSectionsSelectList(string.Empty);

                directDebitDTO.CustomerAccountTypeTargetProductId = savingsProduct.Id;
                directDebitDTO.CustomerAccountTypeTargetProductDescription = savingsProduct.Description;
                directDebitDTO.CustomerAccountTypeProductCode = 1;

                Session["savingsProductId"] = parseId;

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        CustomerAccountTypeTargetProductId = directDebitDTO.CustomerAccountTypeTargetProductId,
                        CustomerAccountTypeTargetProductDescription = directDebitDTO.CustomerAccountTypeTargetProductDescription,
                        CustomerAccountTypeProductCode = directDebitDTO.CustomerAccountTypeProductCode
                    }
                });
            }

            return Json(new { success = false, message = "Product Not Found!" });
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
                ViewBag.LoanProductSection = GetLoanRegistrationLoanProductSectionsSelectList(string.Empty);

                directDebitDTO.LoanCustomerAccountTypeTargetProductId = loanProductsdetails.Id;
                directDebitDTO.LoanCustomerAccountTypeTargetProductDescription = loanProductsdetails.Description;
                directDebitDTO.CustomerAccountTypeProductCode = 2;
                Session["loanProductId"] = parseId;
                return Json(new
                {
                    success = true,
                    data = new
                    {
                        LoanCustomerAccountTypeTargetProductId = directDebitDTO.LoanCustomerAccountTypeTargetProductId,
                        LoanCustomerAccountTypeTargetProductDescription = directDebitDTO.LoanCustomerAccountTypeTargetProductDescription,
                        LoanCustomerAccountTypeProductCode = directDebitDTO.CustomerAccountTypeProductCode
                    }
                });
            }
            return Json(new { success = false, message = "Product Not Found!" });
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
                ViewBag.LoanProductSection = GetLoanRegistrationLoanProductSectionsSelectList(string.Empty);

                directDebitDTO.InvestmentCustomerAccountTypeTargetProductId = investmentProductDetails.Id;
                directDebitDTO.InvestmentCustomerAccountTypeTargetProductDescription = investmentProductDetails.Description;
                directDebitDTO.CustomerAccountTypeProductCode = 3;
                Session["investmentProductId"] = parseId;
                return Json(new
                {
                    success = true,
                    data = new
                    {
                        InvestmentCustomerAccountTypeTargetProductId = directDebitDTO.InvestmentCustomerAccountTypeTargetProductId,
                        InvestmentCustomerAccountTypeTargetProductDescription = directDebitDTO.InvestmentCustomerAccountTypeTargetProductDescription,
                        InvestmentCustomerAccountTypeProductCode = directDebitDTO.CustomerAccountTypeProductCode
                    }
                });
            }

            return Json(new { success = false, message = "Product Not Found!" });
        }


        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            ViewBag.ProductCode = GetProductCodeSelectList(string.Empty);
            ViewBag.ChargeType = GetChargeTypeSelectList(string.Empty);
            ViewBag.LoanProductSection = GetLoanRegistrationLoanProductSectionsSelectList(string.Empty);

            return View();
        }


        [HttpPost]
        public async Task<ActionResult> Create(DirectDebitDTO directDebitDTO)
        {
            if (Session["savingsProductId"] != null)
                directDebitDTO.CustomerAccountTypeTargetProductId = (Guid)Session["savingsProductId"];
            if (Session["loanProductId"] != null)
                directDebitDTO.CustomerAccountTypeTargetProductId = (Guid)Session["loanProductId"];
            if (Session["investmentProductId"] != null)
                directDebitDTO.CustomerAccountTypeTargetProductId = (Guid)Session["investmentProductId"];

            directDebitDTO.ValidateAll();

            if (!directDebitDTO.HasErrors)
            {
                var result = await _channelService.AddDirectDebitAsync(directDebitDTO, GetServiceHeader());

                if (result.ErrorMessageResult != null)
                {
                    await ServeNavigationMenus();

                    TempData["ErrorMsg"] = result.ErrorMessageResult;

                    ViewBag.ProductCode = GetProductCodeSelectList(string.Empty);
                    ViewBag.ChargeType = GetChargeTypeSelectList(string.Empty);
                    ViewBag.LoanProductSection = GetLoanRegistrationLoanProductSectionsSelectList(string.Empty);

                    return View();
                }

                Session.Clear();
                TempData["Create"] = "Done";

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = directDebitDTO.ErrorMessages;

                ViewBag.ProductCode = GetProductCodeSelectList(directDebitDTO.CustomerAccountTypeProductCodeDescription.ToString());
                ViewBag.ChargeType = GetChargeTypeSelectList(directDebitDTO.ChargeType.ToString());
                ViewBag.LoanProductSection = GetLoanRegistrationLoanProductSectionsSelectList(directDebitDTO.LoanRegistrationLoanProductSectionDescription.ToString());

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