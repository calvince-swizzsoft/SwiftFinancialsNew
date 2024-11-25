using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    public class LoanProductController : MasterController
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

            var pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;

            var pageCollectionInfo = await _channelService.FindLoanProductsByFilterInPageAsync(jQueryDataTablesModel.sSearch, pageIndex, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<LoanProductDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }


        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var loanProductDTO = await _channelService.FindLoanProductAsync(id, GetServiceHeader());
            var dynamicCharges = await _channelService.FindDynamicChargesByLoanProductIdAsync(id, GetServiceHeader());
            var loanCycle = await _channelService.FindLoanCyclesByLoanProductIdAsync(id, GetServiceHeader());
            var deductibles = await _channelService.FindLoanProductDeductiblesByLoanProductIdAsync(id, GetServiceHeader());
            var auxiliarys = await _channelService.FindLoanProductAuxilliaryAppraisalFactorsByLoanProductIdAsync(id, GetServiceHeader());
            var loanLending = await _channelService.FindLoanProductAuxiliaryConditionsAsync(id, GetServiceHeader());

            



            ViewBag.DynamicCharges = dynamicCharges;
            ViewBag.LoanCycle = loanCycle;
            ViewBag.Deductibles = deductibles;
            ViewBag.Auxiliarys = auxiliarys;
            ViewBag.LoanLending = loanLending;

            

            return View(loanProductDTO);
        }


        

        [HttpPost]
        public async Task<JsonResult> GetInvestmentProductDetails(Guid id)
        {
            await ServeNavigationMenus();
            var investmentProduct = await _channelService.FindInvestmentProductAsync(id, GetServiceHeader());

            if (investmentProduct == null)
            {
                return Json(new { success = false, message = "Investment product not found" });
            }

            return Json(new
            {
                success = true,
                data = new
                {
                    CustomerAccountTypeTargetProductDescription = investmentProduct.Description,
                    CustomerAccountTypeTargetProductId = investmentProduct.Id
                }
            });
        }

        

        [HttpGet]
        public async Task<ActionResult> GetSavingDetails(Guid savingsProductId)
        {
            try
            {
                var savingProduct = await _channelService.FindSavingsProductAsync(savingsProductId, GetServiceHeader());

                if (savingProduct == null)
                {
                    return Json(new { success = false, message = "saving product not found." }, JsonRequestBehavior.AllowGet);
                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        CustomerAccountTypeTargetProductDescription = savingProduct.Description,
                        CustomerAccountTypeTargetProductId = savingProduct.Id




                    }
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while fetching the customer details." }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public async Task<ActionResult> GetloanDetails(Guid loanProductId)
        {
            try
            {
                var loanProduct = await _channelService.FindLoanProductAsync(loanProductId, GetServiceHeader());

                if (loanProduct == null)
                {
                    return Json(new { success = false, message = "loan product not found." }, JsonRequestBehavior.AllowGet);
                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        CustomerAccountTypeTargetProductDescription = loanProduct.Description,
                        CustomerAccountTypeTargetProductId = loanProduct.Id




                    }
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while fetching the customer details." }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public async Task<JsonResult> GetLoanProductDetails(Guid id)
        {
            await ServeNavigationMenus();
            var loanProduct = await _channelService.FindLoanProductAsync(id, GetServiceHeader());

            if (loanProduct == null)
            {
                return Json(new { success = false, message = "Loan product not found" });
            }

            return Json(new
            {
                success = true,
                data = new
                {
                    TargetLoanProductDescription = loanProduct.Description,
                    TargetLoanProductId = loanProduct.Id
                }
            });
        }



        public async Task<ActionResult> Create(Guid? id)
        {
            await ServeNavigationMenus();
            ViewBag.LoanInterestCalculationModeSelectList = GetLoanInterestCalculationModeSelectList(string.Empty);
            ViewBag.LoanInterestChargeModeSelectList = GetLoanInterestChargeModeSelectList(string.Empty);
            ViewBag.LoanInterestRecoveryModeSelectList = GetLoanInterestRecoveryModeSelectList(string.Empty);
            ViewBag.LoanRegistrationLoanProductCategorySelectList = GetLoanRegistrationLoanProductCategorySelectList(string.Empty);
            ViewBag.LoanRegistrationRoundingTypeSelectList = GetLoanRegistrationRoundingTypeSelectList(string.Empty);
            ViewBag.LoanRegistrationGuarantorSecurityModeSelectList = GetLoanRegistrationGuarantorSecurityModeSelectList(string.Empty);
            ViewBag.LoanRegistrationAggregateCheckOffRecoveryModeSelectList = GetLoanRegistrationAggregateCheckOffRecoveryModeSelectList(string.Empty);
            ViewBag.LoanRegistrationStandingOrderTriggerSelectList = GetLoanRegistrationStandingOrderTriggerSelectList(string.Empty);
            ViewBag.PrioritySelectList = GetRecoveryPrioritySelectList(string.Empty);
            ViewBag.LoanRegistrationLoanProductSectionSelectList = GetLoanRegistrationLoanProductSectionsSelectList(string.Empty);
            ViewBag.LoanRegistrationPayoutRecoveryModeSelectList = GetLoanRegistrationPayoutRecoveryModeSelectList(string.Empty);
            ViewBag.TakeHomeTypeSelectList = GetTakeHomeTypeSelectList(string.Empty);
            ViewBag.LoanRegistrationPaymentDueDateSelectList = GetLoanRegistrationPaymentDueDateSelectList(string.Empty);
            ViewBag.LoanPaymentFrequencyPerYearSelectList = GetLoanPaymentFrequencyPerYearSelectList(string.Empty);
            ViewBag.ProductCodeSelectList = GetProductCodeSelectList(string.Empty);
            ViewBag.ChargeType = GetTakeHomeTypeSelectList(string.Empty);
            ViewBag.AuxiliaryLoanConditions = GetAuxiliaryLoanConditionSelectList(string.Empty);

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            var loanProduct = await _channelService.FindChartOfAccountAsync(parseId, GetServiceHeader());

            LoanProductDTO loanProductDTO = new LoanProductDTO();

            if (loanProduct != null)
            {
                loanProductDTO.ChartOfAccountId = loanProduct.Id;
                loanProductDTO.InterestReceivedChartOfAccountId = loanProduct.Id;
                loanProductDTO.InterestReceivableChartOfAccountId = loanProduct.Id;
                loanProductDTO.InterestChargedChartOfAccountId = loanProduct.Id;
                loanProductDTO.ChartOfAccountName = loanProduct.AccountName;
                loanProductDTO.ChartOfAccountAccountName = loanProduct.AccountName;
                loanProductDTO.InterestReceivedChartOfAccountAccountName = loanProduct.AccountName;
                loanProductDTO.ChartOfAccountCostCenterId = loanProduct.CostCenterId;
                loanProductDTO.InterestChargedChartOfAccountAccountName = loanProduct.AccountName;
                loanProductDTO.InterestChargedChartOfAccountCostCenterId = loanProduct.CostCenterId;
             
            }
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> SelectedCharges(List<DynamicChargeDTO> charges)
        {
            // Process the charges as needed
            foreach (var charge in charges)
            {
                // Handle each charge
            }

            return Json(new { success = true });
        }

        [HttpPost]
        public ActionResult StoreSelectedCharges(List<DynamicChargeDTO> selectedCharges)
        {
            // If selectedCharges is null or empty, clear the session
            if (selectedCharges != null && selectedCharges.Count > 0)
            {
                var observableCharges = new ObservableCollection<DynamicChargeDTO>(selectedCharges);
                Session["selectedCharges"] = observableCharges;
            }
            else
            {
                // Optionally clear session if no charges are selected
                Session.Remove("selectedCharges");
            }

            return Json(new { success = true });
        }


        [HttpPost]
        public ActionResult StoreSelectedProducts(ProductCollectionInfo selectedProducts)
        {
            if (selectedProducts == null || selectedProducts.LoanProductCollection == null || !selectedProducts.LoanProductCollection.Any())
            {
                return Json(new { success = false, message = "No loan products selected." });
            }

            Session["selectedProducts"] = selectedProducts;

            return Json(new { success = true });
        }






        [HttpPost]
        public ActionResult ProcessSelectedLoans(ProductCollectionInfo SelectedLoans)
        {
            if (SelectedLoans == null || SelectedLoans.EligibileIncomeDeductionLoanProductCollection == null || !SelectedLoans.EligibileIncomeDeductionLoanProductCollection.Any())
            {
                return Json(new { success = false, message = "No loan products selected." });
            }

            Session["selectedLoans"] = SelectedLoans;

            return Json(new { success = true, message = "Loans stored in session successfully." });
        }



        [HttpPost]
        public ActionResult ProcessSelectedInvestmentProducts(ProductCollectionInfo selectedInvestmentProducts)
        {
            if (selectedInvestmentProducts == null || selectedInvestmentProducts.EligibileIncomeDeductionInvestmentProductCollection == null || !selectedInvestmentProducts.EligibileIncomeDeductionInvestmentProductCollection.Any())
            {
                return Json(new { success = false, message = "No investment products selected." });
            }

            Session["selectedInvestmentProducts"] = selectedInvestmentProducts;

            return Json(new { success = true, message = "Investment products stored successfully." });
        }


        [HttpPost]
        public ActionResult ProcessSelectedSavingProducts(ProductCollectionInfo selectedSavingProducts)
        {
            if (selectedSavingProducts == null || selectedSavingProducts.EligibileIncomeDeductionSavingsProductCollection == null || !selectedSavingProducts.EligibileIncomeDeductionSavingsProductCollection.Any())
            {
                return Json(new { success = false, message = "No products selected." });
            }

            Session["selectedSavingProducts"] = selectedSavingProducts;

            return Json(new { success = true, message = "Products stored in TempData successfully." });
        }




        [HttpPost]
        public ActionResult SaveSelection(ProductCollectionInfo selectedInvestments, bool isChecked)
        {
            if (selectedInvestments == null || selectedInvestments.InvestmentProductCollection == null || !selectedInvestments.InvestmentProductCollection.Any())
            {
                return Json(new { success = false, message = "No Selected Investments." });
            }

            Session["selectedInvestments"] = selectedInvestments;

            return Json(new { success = true, message = "Selection updated successfully." });
        }




        [HttpPost]
        public async Task<ActionResult> Create(LoanProductDTO loanProductDTO)
        {
            if (loanProductDTO.HasErrors)
            {
                return Json(new { success = false, message = "Validation failed.", errors = loanProductDTO.ErrorMessages });
            }

            try
            {
                // Retrieve session data
                var sessionData = new
                {
                    Deductibles = Session["deductiles"] as ObservableCollection<LoanProductDeductibleDTO>,
                    Cycles = Session["cycles"] as ObservableCollection<LoanCycleDTO>,
                    AuxiliaryAppraisal = Session["auxiliaryAppraisal"] as ObservableCollection<LoanProductAuxilliaryAppraisalFactorDTO>,
                    LendingConditions = Session["lendingConditions"] as ObservableCollection<LoanProductAuxiliaryConditionDTO>,
                    SelectedCharges = Session["selectedCharges"] as ObservableCollection<DynamicChargeDTO>,
                    SelectedProducts = Session["selectedProducts"] as ProductCollectionInfo,
                    SelectedLoans = Session["selectedLoans"] as ProductCollectionInfo,
                    SelectedInvestmentProducts = Session["selectedInvestmentProducts"] as ProductCollectionInfo,
                    SelectedSavingProducts = Session["selectedSavingProducts"] as ProductCollectionInfo,
                    SelectedInvestments = Session["selectedInvestments"] as ProductCollectionInfo,
                };

                // Create loan product
                var loanProduct = await _channelService.AddLoanProductAsync(loanProductDTO, GetServiceHeader());
                if (loanProduct.HasErrors) return Json(new { success = false, message = "An error occurred while creating the loan product.", errors = loanProduct.ErrorMessages });

                // Update associated data
                await UpdateAssociatedData(loanProduct.Id, sessionData);

                return Json(new { success = true, message = "Loan product created successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred while creating the loan product.", errors = new[] { ex.Message } });
            }
        }

        private async Task UpdateAssociatedData(Guid loanProductId, dynamic sessionData)
        {
            await _channelService.UpdateLoanProductDeductiblesByLoanProductIdAsync(loanProductId, sessionData.Deductibles, GetServiceHeader());
            await _channelService.UpdateLoanCyclesByLoanProductIdAsync(loanProductId, sessionData.Cycles, GetServiceHeader());
            await _channelService.UpdateLoanProductAuxilliaryAppraisalFactorsByLoanProductIdAsync(loanProductId, sessionData.AuxiliaryAppraisal, GetServiceHeader());
            await _channelService.UpdateLoanProductAuxiliaryConditionsByBaseLoanProductIdAsync(loanProductId, sessionData.LendingConditions, GetServiceHeader());

            // Handle dynamic charges and product updates
            await UpdateDynamicCharges(loanProductId, sessionData.SelectedCharges);
            await UpdateProductCollections(loanProductId, sessionData.SelectedProducts);
            await UpdateProductCollections(loanProductId, sessionData.SelectedLoans, isLoan: true);
            await UpdateProductCollections(loanProductId, sessionData.SelectedInvestmentProducts);
            await UpdateProductCollections(loanProductId, sessionData.SelectedSavingProducts);
            await UpdateProductCollections(loanProductId, sessionData.SelectedInvestments);
        }

        private async Task UpdateDynamicCharges(Guid loanProductId, ObservableCollection<DynamicChargeDTO> charges)
        {
            if (charges != null) await _channelService.UpdateDynamicChargesByLoanProductIdAsync(loanProductId, charges, GetServiceHeader());
        }

        private async Task UpdateProductCollections(Guid loanProductId, ProductCollectionInfo productCollection, bool isLoan = false)
        {
            if (productCollection?.LoanProductCollection?.Any() == true)
            {
                var loanProductDtos = productCollection.LoanProductCollection.Select(p => new LoanProductDTO { Id = loanProductId }).ToList();
                await _channelService.UpdateAppraisalProductsByLoanProductIdAsync(loanProductId, new ProductCollectionInfo { LoanProductCollection = loanProductDtos }, GetServiceHeader());
            }
            else if (productCollection?.EligibileIncomeDeductionLoanProductCollection?.Any() == true)
            {
                var loanProductDtos = productCollection.EligibileIncomeDeductionLoanProductCollection.Select(p => new LoanProductDTO { Id = loanProductId }).ToList();
                await _channelService.UpdateAppraisalProductsByLoanProductIdAsync(loanProductId, new ProductCollectionInfo { EligibileIncomeDeductionLoanProductCollection = loanProductDtos }, GetServiceHeader());
            }
            else if (productCollection?.EligibileIncomeDeductionInvestmentProductCollection?.Any() == true)
            {
                var investmentDtos = productCollection.EligibileIncomeDeductionInvestmentProductCollection.Select(p => new InvestmentProductDTO { ProductId = loanProductId }).ToList();
                await _channelService.UpdateAppraisalProductsByLoanProductIdAsync(loanProductId, new ProductCollectionInfo { EligibileIncomeDeductionInvestmentProductCollection = investmentDtos }, GetServiceHeader());
            }
            else if (productCollection?.EligibileIncomeDeductionSavingsProductCollection?.Any() == true)
            {
                var savingsDtos = productCollection.EligibileIncomeDeductionSavingsProductCollection.Select(p => new SavingsProductDTO { Id = loanProductId }).ToList();
                await _channelService.UpdateAppraisalProductsByLoanProductIdAsync(loanProductId, new ProductCollectionInfo { EligibileIncomeDeductionSavingsProductCollection = savingsDtos }, GetServiceHeader());
            }
            else if ( productCollection?.InvestmentProductCollection?.Any() == true)
            {
                var investmentDtos = productCollection.InvestmentProductCollection.Select(p => new InvestmentProductDTO { ProductId = loanProductId }).ToList();
                await _channelService.UpdateAppraisalProductsByLoanProductIdAsync(loanProductId, new ProductCollectionInfo { InvestmentProductCollection = investmentDtos }, GetServiceHeader());
            }
        }



        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var loanProductDTO = await _channelService.FindLoanProductAsync(id, GetServiceHeader());

            ViewBag.LoanInterestCalculationModeSelectList = GetLoanInterestCalculationModeSelectList(string.Empty);
            ViewBag.LoanInterestChargeModeSelectList = GetLoanInterestChargeModeSelectList(string.Empty);
            ViewBag.LoanInterestRecoveryModeSelectList = GetLoanInterestRecoveryModeSelectList(string.Empty);
            ViewBag.LoanRegistrationLoanProductCategorySelectList = GetLoanRegistrationLoanProductCategorySelectList(string.Empty);
            ViewBag.LoanRegistrationRoundingTypeSelectList = GetLoanRegistrationRoundingTypeSelectList(string.Empty);
            ViewBag.LoanRegistrationGuarantorSecurityModeSelectList = GetLoanRegistrationGuarantorSecurityModeSelectList(string.Empty);
            ViewBag.LoanRegistrationAggregateCheckOffRecoveryModeSelectList = GetLoanRegistrationAggregateCheckOffRecoveryModeSelectList(string.Empty);
            ViewBag.LoanRegistrationStandingOrderTriggerSelectList = GetLoanRegistrationStandingOrderTriggerSelectList(string.Empty);
            ViewBag.PrioritySelectList = GetRecoveryPrioritySelectList(string.Empty);
            ViewBag.LoanRegistrationLoanProductSectionSelectList = GetLoanRegistrationLoanProductSectionsSelectList(string.Empty);
            ViewBag.LoanRegistrationPayoutRecoveryModeSelectList = GetLoanRegistrationPayoutRecoveryModeSelectList(string.Empty);
            ViewBag.TakeHomeTypeSelectList = GetTakeHomeTypeSelectList(string.Empty);
            ViewBag.LoanRegistrationPaymentDueDateSelectList = GetLoanRegistrationPaymentDueDateSelectList(string.Empty);
            ViewBag.LoanPaymentFrequencyPerYearSelectList = GetLoanPaymentFrequencyPerYearSelectList(string.Empty);
            ViewBag.ProductCodeSelectList = GetProductCodeSelectList(string.Empty);
            ViewBag.ChargeType = GetTakeHomeTypeSelectList(string.Empty);
            ViewBag.AuxiliaryLoanConditions = GetAuxiliaryLoanConditionSelectList(string.Empty);

            return View(loanProductDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, LoanProductDTO loanProductDTO)
        {
            if (loanProductDTO.HasErrors)
            {
                return Json(new { success = false, message = "Validation failed.", errors = loanProductDTO.ErrorMessages });
            }

            try
            {
                // Reload dropdown lists for validation failure or re-rendering the view
                ViewBag.LoanInterestCalculationModeSelectList = GetLoanInterestCalculationModeSelectList(string.Empty);
                ViewBag.LoanInterestChargeModeSelectList = GetLoanInterestChargeModeSelectList(string.Empty);
                ViewBag.LoanInterestRecoveryModeSelectList = GetLoanInterestRecoveryModeSelectList(string.Empty);
                ViewBag.LoanRegistrationLoanProductCategorySelectList = GetLoanRegistrationLoanProductCategorySelectList(string.Empty);
                ViewBag.LoanRegistrationRoundingTypeSelectList = GetLoanRegistrationRoundingTypeSelectList(string.Empty);
                ViewBag.LoanRegistrationGuarantorSecurityModeSelectList = GetLoanRegistrationGuarantorSecurityModeSelectList(string.Empty);
                ViewBag.LoanRegistrationAggregateCheckOffRecoveryModeSelectList = GetLoanRegistrationAggregateCheckOffRecoveryModeSelectList(string.Empty);
                ViewBag.LoanRegistrationStandingOrderTriggerSelectList = GetLoanRegistrationStandingOrderTriggerSelectList(string.Empty);
                ViewBag.PrioritySelectList = GetRecoveryPrioritySelectList(string.Empty);
                ViewBag.LoanRegistrationLoanProductSectionSelectList = GetLoanRegistrationLoanProductSectionsSelectList(string.Empty);
                ViewBag.LoanRegistrationPayoutRecoveryModeSelectList = GetLoanRegistrationPayoutRecoveryModeSelectList(string.Empty);
                ViewBag.TakeHomeTypeSelectList = GetTakeHomeTypeSelectList(string.Empty);
                ViewBag.LoanRegistrationPaymentDueDateSelectList = GetLoanRegistrationPaymentDueDateSelectList(string.Empty);
                ViewBag.LoanPaymentFrequencyPerYearSelectList = GetLoanPaymentFrequencyPerYearSelectList(string.Empty);
                ViewBag.ProductCodeSelectList = GetProductCodeSelectList(string.Empty);
                ViewBag.ChargeType = GetTakeHomeTypeSelectList(string.Empty);
                ViewBag.AuxiliaryLoanConditions = GetAuxiliaryLoanConditionSelectList(string.Empty);

                // Attempt to update the loan product
                loanProductDTO.Id = id; // Ensure the DTO has the correct ID
                var updateSuccess = await _channelService.UpdateLoanProductAsync(loanProductDTO, GetServiceHeader());

                if (!updateSuccess)
                {
                    return Json(new { success = false, message = "Failed to update the loan product. Please try again." });
                }


                return Json(new { success = true, message = "Loan product updated successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred while updating the loan product.", errors = new[] { ex.Message } });
            }
        }






        [HttpPost]
        public async Task<JsonResult> AddDeductibles(LoanProductDeductibleDTO loanProductDeductibleDTO)
        {
            await ServeNavigationMenus();

            // Retrieve the batch entries from the session
            var deductiles = Session["deductiles"] as ObservableCollection<LoanProductDeductibleDTO>;
            if (deductiles == null)
            {
                deductiles = new ObservableCollection<LoanProductDeductibleDTO>();
            }
            if (loanProductDeductibleDTO.Id == Guid.Empty)
            {
                loanProductDeductibleDTO.Id = Guid.NewGuid();
            }
            deductiles.Add(loanProductDeductibleDTO);

            // Save the updated entries back to the session
            Session["deductiles"] = deductiles;


            return Json(new { success = true, data = deductiles });
        }


        [HttpPost]
        public async Task<JsonResult> RemoveDeductibles(Guid id)
        {
            await ServeNavigationMenus();

            var deductiles = Session["deductiles"] as ObservableCollection<LoanProductDeductibleDTO>;

            if (deductiles != null)
            {
                var entryToRemove = deductiles.FirstOrDefault(e => e.Id == id);
                if (entryToRemove != null)
                {
                    deductiles.Remove(entryToRemove);



                    Session["deductiles"] = deductiles;
                }
            }



            return Json(new { success = true, data = deductiles });
        }




        [HttpPost]
        public async Task<JsonResult> AddCycles(LoanCycleDTO loanCycleDTO)
        {
            await ServeNavigationMenus();

            // Retrieve the batch entries from the session
            var cycles = Session["cycles"] as ObservableCollection<LoanCycleDTO>;
            if (cycles == null)
            {
                cycles = new ObservableCollection<LoanCycleDTO>();
            }
            if (loanCycleDTO.Id == Guid.Empty)
            {
                loanCycleDTO.Id = Guid.NewGuid();
            }
            cycles.Add(loanCycleDTO);

            // Save the updated entries back to the session
            Session["cycles"] = cycles;


            return Json(new { success = true, data1 = cycles });
        }


        [HttpPost]
        public async Task<JsonResult> RemoveCycle(Guid id)
        {
            await ServeNavigationMenus();

            var cycles = Session["cycles"] as ObservableCollection<LoanCycleDTO>;

            if (cycles != null)
            {
                var entryToRemove = cycles.FirstOrDefault(e => e.Id == id);
                if (entryToRemove != null)
                {
                    cycles.Remove(entryToRemove);



                    Session["cycles"] = cycles;
                }
            }



            return Json(new { success = true, data1 = cycles });
        }




        [HttpPost]
        public async Task<JsonResult> AddLendingConditions(LoanProductAuxiliaryConditionDTO loanProductAuxiliaryConditionDTO)
        {
            await ServeNavigationMenus();

            // Retrieve the batch entries from the session
            var lendingConditions = Session["lendingConditions"] as ObservableCollection<LoanProductAuxiliaryConditionDTO>;
            if (lendingConditions == null)
            {
                lendingConditions = new ObservableCollection<LoanProductAuxiliaryConditionDTO>();
            }
            if (loanProductAuxiliaryConditionDTO.Id == Guid.Empty)
            {
                loanProductAuxiliaryConditionDTO.Id = Guid.NewGuid();
            }
            lendingConditions.Add(loanProductAuxiliaryConditionDTO);

            // Save the updated entries back to the session
            Session["lendingConditions"] = lendingConditions;


            return Json(new { success = true, data3 = lendingConditions });
        }


        [HttpPost]
        public async Task<JsonResult> RemoveLendingConditions(Guid id)
        {
            await ServeNavigationMenus();

            var lendingConditions = Session["lendingConditions"] as ObservableCollection<LoanProductAuxiliaryConditionDTO>;

            if (lendingConditions != null)
            {
                var entryToRemove = lendingConditions.FirstOrDefault(e => e.Id == id);
                if (entryToRemove != null)
                {
                    lendingConditions.Remove(entryToRemove);



                    Session["lendingConditions"] = lendingConditions;
                }
            }



            return Json(new { success = true, data3 = lendingConditions });
        }

        [HttpPost]
        public async Task<JsonResult> AddAuxiliaryAppraisal(LoanProductAuxilliaryAppraisalFactorDTO loanProductAuxilliaryAppraisalFactorDTO)
        {
            await ServeNavigationMenus();

            // Retrieve the batch entries from the session
            var auxiliaryAppraisal = Session["auxiliaryAppraisal"] as ObservableCollection<LoanProductAuxilliaryAppraisalFactorDTO>;
            if (auxiliaryAppraisal == null)
            {
                auxiliaryAppraisal = new ObservableCollection<LoanProductAuxilliaryAppraisalFactorDTO>();
            }
            if (loanProductAuxilliaryAppraisalFactorDTO.Id == Guid.Empty)
            {
                loanProductAuxilliaryAppraisalFactorDTO.Id = Guid.NewGuid();
            }
            auxiliaryAppraisal.Add(loanProductAuxilliaryAppraisalFactorDTO);

            // Save the updated entries back to the session
            Session["auxiliaryAppraisal"] = auxiliaryAppraisal;


            return Json(new { success = true, data2 = auxiliaryAppraisal });
        }


        [HttpPost]
        public async Task<JsonResult> RemoveAuxiliaryAppraisal(Guid id)
        {
            await ServeNavigationMenus();

            var auxiliaryAppraisal = Session["auxiliaryAppraisal"] as ObservableCollection<LoanProductAuxilliaryAppraisalFactorDTO>;

            if (auxiliaryAppraisal != null)
            {
                var entryToRemove = auxiliaryAppraisal.FirstOrDefault(e => e.Id == id);
                if (entryToRemove != null)
                {
                    auxiliaryAppraisal.Remove(entryToRemove);



                    Session["auxiliaryAppraisal"] = auxiliaryAppraisal;
                }
            }



            return Json(new { success = true, data2 = auxiliaryAppraisal });
        }


        [HttpGet]
        public async Task<JsonResult> GetLoanProductsAsync()
        {
            var loanProductsDTOs = await _channelService.FindLoanProductsAsync(GetServiceHeader());

            return Json(loanProductsDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}