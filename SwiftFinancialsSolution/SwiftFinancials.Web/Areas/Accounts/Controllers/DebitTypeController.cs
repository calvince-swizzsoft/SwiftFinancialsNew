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

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindDebitTypesByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(commission => commission.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<DebitTypeDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
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

                debitTypeDTO.CustomerAccountTypeTargetProductId = savingsProduct.Id;
                debitTypeDTO.CustomerAccountTypeTargetProductDescription = savingsProduct.Description;
                debitTypeDTO.CustomerAccountTypeProductCode = 1;

                Session["savingsProductId"] = debitTypeDTO.CustomerAccountTypeTargetProductId;
                Session["savingsProductDescription"] = debitTypeDTO.CustomerAccountTypeTargetProductDescription;
            }

            return View("Create", debitTypeDTO);
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

                debitTypeDTO.CustomerAccountTypeTargetProductId = loanProductsdetails.Id;
                debitTypeDTO.CustomerAccountTypeTargetProductDescription = loanProductsdetails.Description;
                debitTypeDTO.CustomerAccountTypeProductCode = 2;

                Session["loanProductId"] = debitTypeDTO.CustomerAccountTypeTargetProductId;
                Session["loanProductDescription"] = debitTypeDTO.CustomerAccountTypeTargetProductDescription;
            }

            return View("Create", debitTypeDTO);
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

                debitTypeDTO.CustomerAccountTypeTargetProductId = investmentProductDetails.Id;
                debitTypeDTO.CustomerAccountTypeTargetProductDescription = investmentProductDetails.Description;
                debitTypeDTO.CustomerAccountTypeProductCode = 3;

                Session["investmentProductId"] = debitTypeDTO.CustomerAccountTypeTargetProductId;
                Session["investmentProductDescription"] = debitTypeDTO.CustomerAccountTypeTargetProductDescription;
            }
            return View("Create", debitTypeDTO);
        }


        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            ViewBag.ProductCode = GetProductCodeSelectList(string.Empty);

            return View();
        }


        public async Task<ActionResult> DebitType(DebitTypeDTO debitTypeDTO)
        {
            Session["Description"] = debitTypeDTO.Description;
            Session["CustomerAccountTypeProductCode"] = debitTypeDTO.CustomerAccountTypeProductCode;
            Session["isLocked"] = debitTypeDTO.IsLocked;

            return View("Create", debitTypeDTO);
        }


        [HttpPost]
        public async Task<ActionResult> Create(DebitTypeDTO debitTypeDTO, ObservableCollection<CommissionDTO> selectedRows)
        {
            if (Session["savingsProductId"] != null)
            {
                debitTypeDTO.CustomerAccountTypeTargetProductId = (Guid)Session["savingsProductId"];
                debitTypeDTO.CustomerAccountTypeTargetProductDescription = Session["savingsProductDescription"].ToString();
            }

            if (Session["loanProductId"] != null)
            {
                debitTypeDTO.CustomerAccountTypeTargetProductId = (Guid)Session["loanProductId"];
                debitTypeDTO.CustomerAccountTypeTargetProductDescription = Session["loanProductDescription"].ToString();
            }

            if (Session["investmentProductId"] != null)
            {
                debitTypeDTO.CustomerAccountTypeTargetProductId = (Guid)Session["investmentProductId"];
                debitTypeDTO.CustomerAccountTypeTargetProductDescription = Session["investmentProductDescription"].ToString();
            }

            debitTypeDTO.Description = Session["Description"].ToString();
            debitTypeDTO.CustomerAccountTypeProductCode = Convert.ToInt32(Session["CustomerAccountTypeProductCode"].ToString());
            debitTypeDTO.IsLocked = (bool)Session["isLocked"];

            debitTypeDTO.ValidateAll();

            if (!debitTypeDTO.HasErrors)
            {
                var result = await _channelService.AddDebitTypeAsync(debitTypeDTO, GetServiceHeader());

                await _channelService.UpdateCommissionsByDebitTypeIdAsync(result.Id, selectedRows, GetServiceHeader());

                var myId = result.Id;

                if (result.ErrorMessageResult != null)
                {
                    result.Id = myId;
                }

                TempData["Create"] = "Successfully Created Debit Type";

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = debitTypeDTO.ErrorMessages;

                ViewBag.ProductCode = GetProductCodeSelectList(debitTypeDTO.CustomerAccountTypeProductCode.ToString());

                TempData["CreateError"] = "Failed to Create Debit Type";

                return View(debitTypeDTO);
            }
        }


        public async Task<ActionResult> DebitTypeEdit(DebitTypeDTO debitTypeDTO)
        {
            Session["Description2"] = debitTypeDTO.Description;
            Session["CustomerAccountTypeProductCode2"] = debitTypeDTO.CustomerAccountTypeProductCode;
            Session["isLocked2"] = debitTypeDTO.IsLocked;

            return View("Edit", debitTypeDTO);
        }


        
        public async Task<ActionResult> SavingsProductEdit(Guid? id, DebitTypeDTO debitTypeDTO)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View("Edit");
            }

            var savingsProduct = await _channelService.FindSavingsProductAsync(parseId, GetServiceHeader());
            if (savingsProduct != null)
            {
                ViewBag.ProductCode = GetProductCodeSelectList(string.Empty);

                debitTypeDTO.CustomerAccountTypeTargetProductId = savingsProduct.Id;
                debitTypeDTO.CustomerAccountTypeTargetProductDescription = savingsProduct.Description;
                debitTypeDTO.CustomerAccountTypeProductCode = 1;

                debitTypeDTO.Description = Session["Description"].ToString();

                Session["savingsProductId2"] = debitTypeDTO.CustomerAccountTypeTargetProductId;
                Session["savingsProductDescription2"] = debitTypeDTO.CustomerAccountTypeTargetProductDescription;
            }


            ViewBag.applicableChargesedit = TempData["applicableCharges"] as ObservableCollection<CommissionDTO>;

            return View("Edit", debitTypeDTO);
        }



        public async Task<ActionResult> LoansProductEdit(Guid? id, DebitTypeDTO debitTypeDTO)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View("Edit");
            }

            var loanProductsdetails = await _channelService.FindLoanProductAsync(parseId, GetServiceHeader());
            if (loanProductsdetails != null)
            {
                ViewBag.ProductCode = GetProductCodeSelectList(string.Empty);

                debitTypeDTO.CustomerAccountTypeTargetProductId = loanProductsdetails.Id;
                debitTypeDTO.CustomerAccountTypeTargetProductDescription = loanProductsdetails.Description;
                debitTypeDTO.CustomerAccountTypeProductCode = 2;

                debitTypeDTO.Description = Session["Description"].ToString();

                Session["loanProductId2"] = debitTypeDTO.CustomerAccountTypeTargetProductId;
                Session["loanProductDescription2"] = debitTypeDTO.CustomerAccountTypeTargetProductDescription;
            }


            ViewBag.applicableChargesedit = TempData["applicableCharges"];


            return View("Edit", debitTypeDTO);
        }



        public async Task<ActionResult> InvestmentProductEdit(Guid? id, DebitTypeDTO debitTypeDTO)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View("Edit");
            }

            var investmentProductDetails = await _channelService.FindInvestmentProductAsync(parseId, GetServiceHeader());
            if (investmentProductDetails != null)
            {
                ViewBag.ProductCode = GetProductCodeSelectList(string.Empty);

                debitTypeDTO.CustomerAccountTypeTargetProductId = investmentProductDetails.Id;
                debitTypeDTO.CustomerAccountTypeTargetProductDescription = investmentProductDetails.Description;
                debitTypeDTO.CustomerAccountTypeProductCode = 3;

                debitTypeDTO.Description = Session["Description"].ToString();

                Session["investmentProductId2"] = debitTypeDTO.CustomerAccountTypeTargetProductId;
                Session["investmentProductDescription2"] = debitTypeDTO.CustomerAccountTypeTargetProductDescription;
            }

            ViewBag.applicableChargesedit = TempData["applicableCharges"];


            return View("Edit", debitTypeDTO);
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

            //debitTypeDTO.Description = Session["Description2"].ToString();
            //debitTypeDTO.CustomerAccountTypeProductCode = Convert.ToInt32(Session["CustomerAccountTypeProductCode2"].ToString());
            //debitTypeDTO.IsLocked = (bool)Session["isLocked2"];

            if (Session["savingsProductId2"] != null)
            {
                debitTypeDTO.CustomerAccountTypeTargetProductId = (Guid)Session["savingsProductId2"];
                debitTypeDTO.CustomerAccountTypeTargetProductDescription = Session["savingsProductDescription2"].ToString();
            }

            if (Session["loanProductId2"] != null)
            {
                debitTypeDTO.CustomerAccountTypeTargetProductId = (Guid)Session["loanProductId2"];
                debitTypeDTO.CustomerAccountTypeTargetProductDescription = Session["loanProductDescription2"].ToString();
            }

            if (Session["investmentProductId2"] != null)
            {
                debitTypeDTO.CustomerAccountTypeTargetProductId = (Guid)Session["investmentProductId2"];
                debitTypeDTO.CustomerAccountTypeTargetProductDescription = Session["investmentProductDescription2"].ToString();
            }

            debitTypeDTO.ValidateAll();

            if (!debitTypeDTO.HasErrors)
            {
                var result = await _channelService.UpdateDebitTypeAsync(debitTypeDTO, GetServiceHeader());

                await _channelService.UpdateCommissionsByDebitTypeIdAsync(debitTypeDTO.Id, selectedRows, GetServiceHeader());

                TempData["Edit"] = "Successfully Edited Debit Type";
                Session["Description2"] = null;
                Session["CustomerAccountTypeProductCode2"] = null;
                Session["isLocked2"] = null;
                Session["DebitTypeId"] = null;
                Session["savingsProductId2"] = null;

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
