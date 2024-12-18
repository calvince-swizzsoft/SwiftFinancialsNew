using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

            var pageCollectionInfo = await _channelService.
                FindDebitTypesByFilterInPageAsync(jQueryDataTablesModel.sSearch, 0, int.MaxValue, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {

                var sortedData = pageCollectionInfo.PageCollection
                    .OrderByDescending(loanCase => loanCase.CreatedDate)
                    .ToList();

                totalRecordCount = sortedData.Count;

                var paginatedData = sortedData
                    .Skip(jQueryDataTablesModel.iDisplayStart)
                    .Take(jQueryDataTablesModel.iDisplayLength)
                    .ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch)
                    ? sortedData.Count
                    : totalRecordCount;

                return this.DataTablesJson(
                    items: paginatedData,
                    totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount,
                    sEcho: jQueryDataTablesModel.sEcho
                );
            }

            return this.DataTablesJson(
                items: new List<DebitTypeDTO>(),
                totalRecords: totalRecordCount,
                totalDisplayRecords: searchRecordCount,
                sEcho: jQueryDataTablesModel.sEcho
            );
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var debitTypeDTO = await _channelService.FindDebitTypeAsync(id, GetServiceHeader());

            var applicableCharges = await _channelService.FindCommissionsByDebitTypeIdAsync(id, GetServiceHeader());

            ViewBag.applicableCharges = applicableCharges;

            return View(debitTypeDTO);
        }

        public async Task<ActionResult> SavingsProduct(Guid? id, DebitTypeDTO debitTypeDTO)
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

                debitTypeDTO.CustomerAccountTypeTargetProductId = savingsProduct.Id;
                debitTypeDTO.CustomerAccountTypeTargetProductDescription = savingsProduct.Description;
                debitTypeDTO.CustomerAccountTypeProductCode = 1;

                Session["savingsProductId"] = parseId;

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        CustomerAccountTypeTargetProductId = debitTypeDTO.CustomerAccountTypeTargetProductId,
                        CustomerAccountTypeTargetProductDescription = debitTypeDTO.CustomerAccountTypeTargetProductDescription,
                        CustomerAccountTypeProductCode = debitTypeDTO.CustomerAccountTypeProductCode
                    }
                });
            }

            return Json(new { success = false, message = "Product Not Found!" });
        }

        public async Task<ActionResult> LoansProduct(Guid? id, DebitTypeDTO debitTypeDTO)
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

                debitTypeDTO.LoanCustomerAccountTypeTargetProductId = loanProductsdetails.Id;
                debitTypeDTO.LoanCustomerAccountTypeTargetProductDescription = loanProductsdetails.Description;
                debitTypeDTO.CustomerAccountTypeProductCode = 2;
                Session["loanProductId"] = parseId;
                return Json(new
                {
                    success = true,
                    data = new
                    {
                        LoanCustomerAccountTypeTargetProductId = debitTypeDTO.LoanCustomerAccountTypeTargetProductId,
                        LoanCustomerAccountTypeTargetProductDescription = debitTypeDTO.LoanCustomerAccountTypeTargetProductDescription,
                        LoanCustomerAccountTypeProductCode = debitTypeDTO.CustomerAccountTypeProductCode
                    }
                });
            }
            return Json(new { success = false, message = "Product Not Found!" });
        }

        public async Task<ActionResult> InvestmentProduct(Guid? id, DebitTypeDTO debitTypeDTO)
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

                debitTypeDTO.InvestmentCustomerAccountTypeTargetProductId = investmentProductDetails.Id;
                debitTypeDTO.InvestmentCustomerAccountTypeTargetProductDescription = investmentProductDetails.Description;
                debitTypeDTO.CustomerAccountTypeProductCode = 3;
                Session["investmentProductId"] = parseId;
                return Json(new
                {
                    success = true,
                    data = new
                    {
                        InvestmentCustomerAccountTypeTargetProductId = debitTypeDTO.InvestmentCustomerAccountTypeTargetProductId,
                        InvestmentCustomerAccountTypeTargetProductDescription = debitTypeDTO.InvestmentCustomerAccountTypeTargetProductDescription,
                        InvestmentCustomerAccountTypeProductCode = debitTypeDTO.CustomerAccountTypeProductCode
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
        public async Task<ActionResult> Create(DebitTypeDTO debitTypeDTO, string SelectedIds)
        {
            await ServeNavigationMenus();

            if (Session["savingsProductId"] == null && Session["loanProductId"] == null && Session["investmentProductId"] == null)
            {
                TempData["emptyProduct"] = "No Product has been selected!";
                return View(debitTypeDTO);
            }

            if (Session["savingsProductId"] != null)
                debitTypeDTO.CustomerAccountTypeTargetProductId = (Guid)Session["savingsProductId"];
            if (Session["loanProductId"] != null)
                debitTypeDTO.CustomerAccountTypeTargetProductId = (Guid)Session["loanProductId"];
            if (Session["investmentProductId"] != null)
                debitTypeDTO.CustomerAccountTypeTargetProductId = (Guid)Session["investmentProductId"];

            var commissions = new ObservableCollection<CommissionDTO>();

            var ids = SelectedIds.Split(',').Select(Guid.Parse).ToList();

            if (ids != null)
            {
                foreach (var commission in ids)
                {
                    var foundCommission = await _channelService.FindCommissionAsync(commission, GetServiceHeader());
                    commissions.Add(foundCommission);
                }
            }

            debitTypeDTO.ValidateAll();

            if (!debitTypeDTO.HasErrors)
            {
                var result = await _channelService.AddDebitTypeAsync(debitTypeDTO, GetServiceHeader());

                if (result.ErrorMessageResult != null)
                {
                    await ServeNavigationMenus();

                    Session["savingsProductId"] = null;
                    Session["loanProductId"] = null;
                    Session["investmentProductId"] = null;
                    Session.Clear();

                    TempData["ErrorMsg"] = result.ErrorMessageResult;

                    ViewBag.ProductCode = GetProductCodeSelectList(string.Empty);
                    ViewBag.ChargeType = GetChargeTypeSelectList(string.Empty);
                    ViewBag.LoanProductSection = GetLoanRegistrationLoanProductSectionsSelectList(string.Empty);

                    return View();
                }

                await _channelService.UpdateCommissionsByDebitTypeIdAsync(result.Id, commissions, GetServiceHeader());

                Session["savingsProductId"] = null;
                Session["loanProductId"] = null;
                Session["investmentProductId"] = null;
                Session.Clear();


                TempData["Create"] = "Operation Completed Successfully.";

                return RedirectToAction("Index");
            }
            else
            {
                Session["savingsProductId"] = null;
                Session["loanProductId"] = null;
                Session["investmentProductId"] = null;
                Session.Clear();

                var errorMessages = debitTypeDTO.ErrorMessages;
                string errorMessage = string.Join("\n", errorMessages.Where(msg => !string.IsNullOrWhiteSpace(msg)));

                ViewBag.ProductCode = GetProductCodeSelectList(debitTypeDTO.CustomerAccountTypeProductCodeDescription.ToString());
                ViewBag.LoanProductSection = GetLoanRegistrationLoanProductSectionsSelectList(debitTypeDTO.LoanRegistrationLoanProductSectionDescription.ToString());

                TempData["CreateError"] = "Operation Failed: " + errorMessage;
                return View(debitTypeDTO);
            }
        }


        public async Task<ActionResult> Edit(Guid id)
        {
            Session["DebitTypeId"] = id;

            await ServeNavigationMenus();

            ViewBag.ProductCode = GetProductCodeSelectList(string.Empty);

            var debitTypeDTO = await _channelService.FindDebitTypeAsync(id, GetServiceHeader());

            Session["Description"] = debitTypeDTO.Description;

            var productCode = debitTypeDTO.CustomerAccountTypeProductCode;
            Session["productCode"] = productCode;

            var applicableCharges = await _channelService.FindCommissionsByDebitTypeIdAsync(id, GetServiceHeader());

            ViewBag.applicableChargesedit = applicableCharges;

            return View(debitTypeDTO);
        }

        [HttpPost]
        public async Task<ActionResult> Edit(DebitTypeDTO debitTypeDTO, ObservableCollection<CommissionDTO> selectedRows)
        {

            debitTypeDTO.ValidateAll();

            if (!debitTypeDTO.HasErrors)
            {
                var result = await _channelService.UpdateDebitTypeAsync(debitTypeDTO, GetServiceHeader());

                await _channelService.UpdateCommissionsByDebitTypeIdAsync(debitTypeDTO.Id, selectedRows, GetServiceHeader());

                TempData["Edit"] = "Successfully Edited Debit Type";

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = debitTypeDTO.ErrorMessages;

                ViewBag.ProductCode = GetProductCodeSelectList(debitTypeDTO.CustomerAccountTypeProductCode.ToString());

                TempData["EditError"] = "Failed to Edit Debit Type";

                return View(debitTypeDTO);
            }
        }
    }

}
