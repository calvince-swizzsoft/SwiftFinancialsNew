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

            // Fetching the loan product details
            var loanProductDTO = await _channelService.FindLoanProductAsync(id, GetServiceHeader());

            // Fetching additional related data
            var appraisalProducts = await _channelService.FindAppraisalProductsByLoanProductIdAsync(id, GetServiceHeader());
            var loanCycles = await _channelService.FindLoanCyclesByLoanProductIdAsync(id, GetServiceHeader());
            var loanProductDeductibles = await _channelService.FindLoanProductDeductiblesByLoanProductIdAsync(id, GetServiceHeader());
            var auxiliaryAppraisalFactors = await _channelService.FindLoanProductAuxilliaryAppraisalFactorsByLoanProductIdAsync(id, GetServiceHeader());
            var dynamicCharges = await _channelService.FindDynamicChargesByLoanProductIdAsync(id, GetServiceHeader());

           
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
                //loanProductDTO.InterestReceivedChartOfAccountAccountName = loanProduct.AccountName;
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
            var observableCharges = new ObservableCollection<DynamicChargeDTO>(selectedCharges);
            Session["selectedCharges"] = observableCharges; // Store as ObservableCollection

            return Json(new { success = true });
        }

        [HttpPost]
        public ActionResult StoreSelectedProducts(List<Guid> selectedProductIds)
        {
            Session["SelectedProducts"] = selectedProductIds;

            return Json(new { success = true });
        }


        [HttpPost]
        public ActionResult ProcessSelectedLoans(List<Guid> selectedLoanIds)
        {
            if (selectedLoanIds == null || !selectedLoanIds.Any())
            {
                return Json(new { success = false, message = "No loans selected." });
            }

            Session["SelectedLoans"] = selectedLoanIds;

            return Json(new { success = true, message = "Loans stored in TempData successfully." });
        }


        [HttpPost]
        public ActionResult ProcessSelectedInvestmentProducts(List<Guid> selectedInvestmentProductIds)
        {
            if (selectedInvestmentProductIds == null || !selectedInvestmentProductIds.Any())
            {
                return Json(new { success = false, message = "No products selected." });
            }

            Session["SelectedInvestmentProducts"] = selectedInvestmentProductIds;

            return Json(new { success = true, message = "Investment products stored in TempData successfully." });
        }

        [HttpPost]
        public ActionResult ProcessSelectedProducts(List<Guid> selectedSavingProductIds)
        {
            if (selectedSavingProductIds == null || !selectedSavingProductIds.Any())
            {
                return Json(new { success = false, message = "No products selected." });
            }

            Session["SelectedSavingProducts"] = selectedSavingProductIds;

            return Json(new { success = true, message = "Products stored in TempData successfully." });
        }

        

        [HttpPost]
        public ActionResult SaveSelection(List<Guid> selectedIds, bool isChecked)
        {
            var selectedProducts = Session["selectedIds"] as List<Guid> ?? new List<Guid>();

            if (isChecked)
            {
                foreach (var id in selectedIds)
                {
                    if (!selectedProducts.Contains(id))
                    {
                        selectedProducts.Add(id);
                    }
                }
            }
            else
            {
                selectedProducts.RemoveAll(id => selectedIds.Contains(id));
            }

            Session["selectedIds"] = selectedProducts;

            return Json(new { success = true, message = "Selection updated successfully." });
        }



        [HttpPost]
        public async Task<ActionResult> Create(LoanProductDTO LoanProductDTO)
        {
            if (!LoanProductDTO.HasErrors)
            {
                var deductiles = Session["deductiles"] as ObservableCollection<LoanProductDeductibleDTO>;
                var cycles = Session["cycles"] as ObservableCollection<LoanCycleDTO>;
                var auxiliaryAppraisal = Session["auxiliaryAppraisal"] as ObservableCollection<LoanProductAuxilliaryAppraisalFactorDTO>;

                var lendingConditions = Session["lendingConditions"] as ObservableCollection<LoanProductAuxiliaryConditionDTO>;

                var selectedCharges = Session["selectedCharges"] as ObservableCollection<DynamicChargeDTO>;
                var selectedProductIds = Session["SelectedProducts"] as List<Guid>;
                var selectedLoanIds = Session["SelectedLoans"] as List<Guid>;
                var selectedInvestmentProductIds = Session["SelectedInvestmentProducts"] as List<Guid>;
                var selectedSavingProductIds = Session["SelectedSavingProducts"] as List<Guid>;
                var selectedIds = Session["selectedIds"] as List<Guid>;




                try
                {
                    var loanProduct = await _channelService.AddLoanProductAsync(LoanProductDTO, GetServiceHeader());
                    if (!loanProduct.HasErrors)
                    {
                        await _channelService.UpdateLoanProductDeductiblesByLoanProductIdAsync(loanProduct.Id, deductiles, GetServiceHeader());
                        await _channelService.UpdateLoanCyclesByLoanProductIdAsync(loanProduct.Id, cycles, GetServiceHeader());
                        await _channelService.UpdateLoanProductAuxilliaryAppraisalFactorsByLoanProductIdAsync(loanProduct.Id, auxiliaryAppraisal, GetServiceHeader());
                        // Check if charges exist
                        if (selectedCharges != null)
                        {
                            var chargesList = selectedCharges.ToList(); // Convert to List<DynamicChargeDTO>
                            await _channelService.UpdateDynamicChargesByLoanProductIdAsync(loanProduct.Id, selectedCharges, GetServiceHeader());
                        }
                        if (selectedProductIds != null && selectedProductIds.Any())
                        {
                            var loanProductCollection = new List<LoanProductDTO>();
                            foreach (var productId in selectedProductIds)
                            {
                                var loanProductIds = new LoanProductDTO
                                {
                                    Id = productId // Assuming LoanProductDTO has this property
                                };
                                loanProductCollection.Add(loanProduct);
                            }

                            var appraisalProductsTuple = new ProductCollectionInfo
                            {
                                LoanProductCollection = loanProductCollection
                            };

                            await _channelService.UpdateAppraisalProductsByLoanProductIdAsync(loanProduct.Id, appraisalProductsTuple, GetServiceHeader());
                        }

                         if (selectedLoanIds != null && selectedLoanIds.Any())
                        {
                            var eligibileIncomeDeductionLoanProductCollection = new List<LoanProductDTO>();
                            foreach (var productId in selectedLoanIds)
                            {
                                var loanProductIds = new LoanProductDTO
                                {
                                    Id = productId
                                };
                                eligibileIncomeDeductionLoanProductCollection.Add(loanProductIds);
                            }
                            var appraisalProductsTuple = new ProductCollectionInfo
                            {
                                EligibileIncomeDeductionLoanProductCollection = eligibileIncomeDeductionLoanProductCollection
                            };
                            await _channelService.UpdateAppraisalProductsByLoanProductIdAsync(loanProduct.Id, appraisalProductsTuple, GetServiceHeader());
                        }

                         if (selectedInvestmentProductIds != null && selectedInvestmentProductIds.Any())
                        {
                            var eligibileIncomeDeductionInvestmentProductCollection = new List<InvestmentProductDTO>();
                            foreach ( var productId in selectedInvestmentProductIds)
                            {
                                var investementProductIds = new InvestmentProductDTO
                                {
                                    ProductId = productId
                                };
                                eligibileIncomeDeductionInvestmentProductCollection.Add(investementProductIds);
                            }
                            var appraisalProductsTuple = new ProductCollectionInfo
                            {
                                EligibileIncomeDeductionInvestmentProductCollection = eligibileIncomeDeductionInvestmentProductCollection
                            };
                            await _channelService.UpdateAppraisalProductsByLoanProductIdAsync(loanProduct.Id, appraisalProductsTuple, GetServiceHeader());

                        }

                         if (selectedSavingProductIds != null && selectedSavingProductIds.Any())
                        {
                            var eligibileIncomeDeductionSavingsProductCollection = new List<SavingsProductDTO>();
                            foreach ( var productId in selectedSavingProductIds)
                            {
                                var savingProductIds = new SavingsProductDTO
                                {
                                    Id = productId
                                };
                                eligibileIncomeDeductionSavingsProductCollection.Add(savingProductIds);
                            }
                            var appraisalProductsTuple = new ProductCollectionInfo
                            {
                                EligibileIncomeDeductionSavingsProductCollection = eligibileIncomeDeductionSavingsProductCollection
                            };
                            await _channelService.UpdateAppraisalProductsByLoanProductIdAsync(loanProduct.Id, appraisalProductsTuple, GetServiceHeader());

                        }

                         if (selectedIds != null && selectedIds.Any())
                        {
                            var investmentProductCollection = new List<InvestmentProductDTO>();
                            foreach ( var productId in selectedIds)
                            {
                                var investmentProductIds = new InvestmentProductDTO
                                {
                                    Id = productId
                                };
                                investmentProductCollection.Add(investmentProductIds);
                            }
                            var appraisalProductsTuple = new ProductCollectionInfo
                            {
                                InvestmentProductCollection = investmentProductCollection
                            };
                            await _channelService.UpdateAppraisalProductsByLoanProductIdAsync(loanProduct.Id, appraisalProductsTuple, GetServiceHeader());

                        }


                    }
                    return Json(new { success = true, message = "Loan product created successfully." });
                }
                catch (Exception ex)
                {
                    // Log the exception here
                    return Json(new { success = false, message = "An error occurred while creating the loan product.", errors = new[] { ex.Message } });
                }
            }
            else
            {
                var errorMessages = LoanProductDTO.ErrorMessages;
                return Json(new { success = false, message = "Validation failed.", errors = errorMessages });
            }
        }


        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var loanProductDTO = await _channelService.FindLoanProductAsync(id, GetServiceHeader());

            return View(loanProductDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, LoanProductDTO loanProductBindingModel)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateLoanProductAsync(loanProductBindingModel, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                return View(loanProductBindingModel);
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