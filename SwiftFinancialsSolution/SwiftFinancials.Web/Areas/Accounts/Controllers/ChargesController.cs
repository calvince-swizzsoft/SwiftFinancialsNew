using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using SwiftFinancials.Presentation.Infrastructure.Util;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.Accounts.Controllers
{
    public class ChargesController : MasterController
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

            var pageCollectionInfo = await _channelService.FindCommissionsByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(commission => commission.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<CommissionDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }


        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();
            ViewBag.chargeType = GetChargeTypeSelectList(string.Empty);

            var commissionDTO = await _channelService.FindCommissionAsync(id, GetServiceHeader());

            var chargesplits = await _channelService.FindCommissionSplitsByCommissionIdAsync(id, GetServiceHeader());

            ViewBag.chargeSplits = chargesplits;

            return View(commissionDTO);
        }

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            ViewBag.chargeType = GetChargeTypeSelectList(string.Empty);
            var levyDTOs = await _channelService.FindLeviesAsync(GetServiceHeader());
            ViewBag.LevyDTOs = levyDTOs; 
            return View();
        }


        public async Task<ActionResult> Charge(CommissionDTO commissionDTO)
        {
            Session["Description"] = commissionDTO.Description;
            Session["MaximumCharge"] = commissionDTO.MaximumCharge;

            return View("Create", commissionDTO);
        }


        public async Task<ActionResult> Search(Guid? id, CommissionDTO commissionDTO)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            var GLAccountChartOfAccount = await _channelService.FindChartOfAccountAsync(parseId, GetServiceHeader());

            if (GLAccountChartOfAccount != null)
            {
                commissionDTO.chartOfAccount = GLAccountChartOfAccount;

                Session["GLAccount"] = commissionDTO.chargeSplits;
            }

            return View("Create", commissionDTO);
        }


        [HttpPost]
        public async Task<ActionResult> Add(Guid? id, CommissionDTO commissionDTO)
        {
            await ServeNavigationMenus();
            ViewBag.chargeType = GetChargeTypeSelectList(string.Empty);

            ChargeSplitDTOs = TempData["ChargeSplitDTOs"] as ObservableCollection<CommissionSplitDTO>;

            double sumPercentages = 0;

            var glAccount = commissionDTO.chargeSplits;

            if (ChargeSplitDTOs == null)
                ChargeSplitDTOs = new ObservableCollection<CommissionSplitDTO>();

            if (commissionDTO.Description == null || commissionDTO.MaximumCharge == 0)
            {
                TempData["tPercentage"] = "Charge name and maximum charge required to proceed.";
            }
            else
            {
                Guid parseId;

                if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
                {
                    TempData["tPercentage"] = "Could not add charge split. Choose G/L Account.";

                    await ServeNavigationMenus();

                    return View("Create", commissionDTO);
                }


                foreach (var chargeSplitDTO in commissionDTO.chargeSplit)
                {
                    chargeSplitDTO.Id = parseId;
                    chargeSplitDTO.Description = chargeSplitDTO.Description;
                    chargeSplitDTO.ChartOfAccountId = commissionDTO.Id;
                    chargeSplitDTO.ChartOfAccountAccountName = glAccount.ChartOfAccountAccountName;
                    chargeSplitDTO.Percentage = chargeSplitDTO.Percentage;
                    chargeSplitDTO.Leviable = chargeSplitDTO.Leviable;


                    TempData["tPercentage"] = "";


                    if (chargeSplitDTO.Description == null || chargeSplitDTO.ChartOfAccountAccountName == null || chargeSplitDTO.Percentage == 0)
                    {
                        TempData["tPercentage"] = "Could not add charge split. Make sure to provide all charge split details to proceed.";
                    }
                    else
                    {
                        ChargeSplitDTOs.Add(chargeSplitDTO);

                        sumPercentages = ChargeSplitDTOs.Sum(cs => cs.Percentage);

                        Session["chargeSplit"] = commissionDTO.chargeSplit;

                        if (sumPercentages > 100)
                        {
                            TempData["tPercentage"] = "Failed to add \"" + chargeSplitDTO.Description.ToUpper() + "\". Total percentage exceeded 100%.";

                            ChargeSplitDTOs.Remove(chargeSplitDTO);
                        }
                        else if (sumPercentages < 1)
                        {
                            TempData["tPercentage"] = "Total percentage must be at least greater than 1%.";
                        }
                    }
                };
            }

            ViewBag.ChargeSplitDTOs = ChargeSplitDTOs;

            ViewBag.totalPercentage = sumPercentages;

            Session["totalPercentage"] = sumPercentages;

            TempData["ChargeSplitDTOs"] = ChargeSplitDTOs;


            TempData["ChargeDTO"] = commissionDTO;

            return View("Create");
        }


        [HttpPost]
        public async Task<ActionResult> removeChargeSplit(Guid? id, CommissionDTO commissionDTO)
        {
            ViewBag.chargeType = GetChargeTypeSelectList(string.Empty);

            commissionDTO = TempData["ChargeDTO"] as CommissionDTO;

            commissionDTO.chargeSplit = Session["chargeSplit"] as ObservableCollection<CommissionSplitDTO>;

            var percentageOnRemove = commissionDTO.chargeSplit[0].Percentage;
            var currentPercentage = Convert.ToDouble(Session["totalPercentage"].ToString());

            double cPercentage = Convert.ToDouble(currentPercentage);
            double rPercentage = Convert.ToDouble(percentageOnRemove);

            double newpercentage = cPercentage - rPercentage;

            ViewBag.totalPercentage = newpercentage;

            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                await ServeNavigationMenus();

                return View("Create");
            }

            ChargeSplitDTOs = TempData["ChargeSplitDTOs"] as ObservableCollection<CommissionSplitDTO>;

            var glAccount = commissionDTO.chargeSplits;

            if (ChargeSplitDTOs == null)
                ChargeSplitDTOs = new ObservableCollection<CommissionSplitDTO>();

            foreach (var chargeSplitDTO in commissionDTO.chargeSplit)
            {
                chargeSplitDTO.Id = parseId;
                chargeSplitDTO.Description = chargeSplitDTO.Description;
                chargeSplitDTO.ChartOfAccountId = commissionDTO.Id;
                chargeSplitDTO.ChartOfAccountAccountName = chargeSplitDTO.ChartOfAccountAccountName;
                chargeSplitDTO.Percentage = chargeSplitDTO.Percentage;
                chargeSplitDTO.Leviable = chargeSplitDTO.Leviable;

                ChargeSplitDTOs.Remove(chargeSplitDTO);
            };

            TempData["ChargeSplitDTOs"] = ChargeSplitDTOs;

            TempData["ChargeDTO"] = commissionDTO;

            TempData["tPercentage"] = "";

            ViewBag.ChargeSplitDTOs = ChargeSplitDTOs;

            return View("Create", commissionDTO);
        }


        [HttpPost]
        public async Task<ActionResult> Create(CommissionDTO commissionDTO)
        {
            ViewBag.chargeType = GetChargeTypeSelectList(string.Empty);

            commissionDTO = TempData["ChargeDTO"] as CommissionDTO;

            if (TempData["ChargeSplitDTOs"] != null)
            {
                commissionDTO.chargeSplit = TempData["ChargeSplitDTOs"] as ObservableCollection<CommissionSplitDTO>;
            }
            else
            {
                await ServeNavigationMenus();

                TempData["chargeSplit"] = "Each created charge must be allocated to at least one G/L account. If the charge is allocated to a single account, enter 100% as the percentage.";

                return View(commissionDTO);
            }

            commissionDTO.ValidateAll();

            if (!commissionDTO.HasErrors)
            {
                var chargeSplits = new ObservableCollection<CommissionSplitDTO>();

                foreach (var chargeSplitDTO in commissionDTO.chargeSplit)
                {
                    chargeSplitDTO.Description = chargeSplitDTO.Description;
                    chargeSplitDTO.MaximumCharge = chargeSplitDTO.MaximumCharge;
                    chargeSplitDTO.ChartOfAccountId = chargeSplitDTO.ChartOfAccountId;
                    chargeSplitDTO.ChartOfAccountAccountName = chargeSplitDTO.ChartOfAccountAccountName;
                    chargeSplitDTO.Percentage = chargeSplitDTO.Percentage;
                    chargeSplitDTO.Leviable = chargeSplitDTO.Leviable;

                    chargeSplits.Add(chargeSplitDTO);
                };

                var splitCount = chargeSplits.Count();

                if (splitCount == 1)
                {
                    var sumPercentages = chargeSplits.Sum(cs => cs.Percentage);

                    if (sumPercentages < 100)
                    {
                        TempData["splitPercentage"] = "For charges linked to only one G/L Account, enter 100 as percentage";

                        await ServeNavigationMenus();

                        return View("create");
                    }
                    else
                    {
                        var chartOfAccountCounts = chargeSplits.GroupBy(cs => cs.ChartOfAccountId)
                                                               .Select(g => new { ChartOfAccountId = g.Key, Count = g.Count() })
                                                               .ToList();


                        var duplicateChartOfAccounts = chartOfAccountCounts.Where(x => x.Count > 1)
                                                                           .Select(x => x.ChartOfAccountId)
                                                                           .ToList();

                        if (duplicateChartOfAccounts.Any())
                        {
                            TempData["tPercentage"] = "Sorry, you cannot split charge into the same G/L Account more than once";

                            await ServeNavigationMenus();

                            return View("create", commissionDTO);
                        }

                        var charge = await _channelService.AddCommissionAsync(commissionDTO, GetServiceHeader());

                        if (charge.ErrorMessageResult != null)
                        {
                            await ServeNavigationMenus();

                            TempData["ErrorMsg"] = charge.ErrorMessageResult;

                            return View();
                        }

                        TempData["SuccessMessage"] = "Successfully Created Charge";
                        TempData["ChargeDTO"] = "";

                        if (chargeSplits.Any())
                            await _channelService.UpdateCommissionSplitsByCommissionIdAsync(charge.Id, chargeSplits, GetServiceHeader());
                        TempData["ChargeSplitDTOs"] = "";
                    }
                }
                else if (splitCount > 1)
                {
                    var sumPercentages = chargeSplits.Sum(cs => cs.Percentage);

                    if (sumPercentages < 100)
                    {
                        TempData["splitPercentage"] = "Total percentage must be equal to 100%";

                        await ServeNavigationMenus();

                        return View("create", commissionDTO);
                    }
                    else
                    {
                        var chartOfAccountCounts = chargeSplits.GroupBy(cs => cs.ChartOfAccountId)
                                                               .Select(g => new { ChartOfAccountId = g.Key, Count = g.Count() })
                                                               .ToList();


                        var duplicateChartOfAccounts = chartOfAccountCounts.Where(x => x.Count > 1)
                                                                           .Select(x => x.ChartOfAccountId)
                                                                           .ToList();

                        if (duplicateChartOfAccounts.Any())
                        {
                            TempData["tPercentage"] = "Sorry, you cannot split charge into the same G/L Account more than once";

                            await ServeNavigationMenus();

                            return View("create", commissionDTO);
                        }

                        var charge = await _channelService.AddCommissionAsync(commissionDTO, GetServiceHeader());

                        if (charge.ErrorMessageResult != null)
                        {
                            await ServeNavigationMenus();

                            TempData["ErrorMsg"] = charge.ErrorMessageResult;

                            return View();
                        }

                        TempData["SuccessMessage"] = "Successfully Created Charge";
                        TempData["ChargeDTO"] = "";

                        if (chargeSplits.Any())
                            await _channelService.UpdateCommissionSplitsByCommissionIdAsync(charge.Id, chargeSplits, GetServiceHeader());
                        TempData["ChargeSplitDTOs"] = "";
                    }
                }

                return RedirectToAction("Index");
            }
            else
            {
                await ServeNavigationMenus();

                var errorMessages = commissionDTO.ErrorMessages;

                TempData["CreateError"] = "Failed to Create Charge";

                return View(commissionDTO);
            }
        }



        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            Session["Id"] = id;

            var commissionDTO = await _channelService.FindCommissionAsync(id, GetServiceHeader());

            var chargesplits = await _channelService.FindCommissionSplitsByCommissionIdAsync(id, GetServiceHeader());

            ViewBag.chargeSplits = chargesplits;

            TempData["chargeSplitsBeforeEdit"] = ViewBag.chargeSplits;

            return View(commissionDTO);
        }



        [HttpPost]
        public async Task<ActionResult> AddEdit(Guid? id, CommissionDTO commissionDTO)
        {
            await ServeNavigationMenus();

            var chargeId = (Guid)Session["Id"];
            var chargesplits = await _channelService.FindCommissionSplitsByCommissionIdAsync(chargeId, GetServiceHeader());
            ViewBag.chargeSplits = chargesplits;

            //ChargeSplitDTOs = TempData["ChargeSplitDTOs2"] as ObservableCollection<CommissionSplitDTO>;

            var glAccount = commissionDTO.chargeSplits;

            double sumPercentages = 0;

            if (ChargeSplitDTOs == null)
                ChargeSplitDTOs = new ObservableCollection<CommissionSplitDTO>();

            foreach (var chargeSplitDTO in commissionDTO.chargeSplit)
            {
                chargeSplitDTO.Description = chargeSplitDTO.Description;
                chargeSplitDTO.ChartOfAccountId = commissionDTO.Id;
                chargeSplitDTO.ChartOfAccountAccountName = glAccount.ChartOfAccountAccountName;
                chargeSplitDTO.Percentage = chargeSplitDTO.Percentage;
                chargeSplitDTO.Leviable = chargeSplitDTO.Leviable;


                if (chargeSplitDTO.Description == null || chargeSplitDTO.ChartOfAccountAccountName == null || chargeSplitDTO.Percentage == 0)
                {
                    ViewBag.chargeSplits = chargesplits;

                    TempData["tPercentage"] = "Could not add charge split. Make sure to provide all charge split details to proceed.";
                }
                else
                {
                    chargesplits.Add(chargeSplitDTO);
                   
                    sumPercentages = ChargeSplitDTOs.Sum(cs => cs.Percentage);

                    Session["chargeSplit"] = commissionDTO.chargeSplit;

                    if (sumPercentages > 100)
                    {
                        TempData["tPercentage"] = "Failed to add \"" + chargeSplitDTO.Description.ToUpper() + "\". Total percentage exceeded 100%.";

                        ChargeSplitDTOs.Remove(chargeSplitDTO);

                        Session["chargeSplit"] = ChargeSplitDTOs;
                    }
                    else if (sumPercentages < 1)
                    {
                        TempData["tPercentage"] = "Total percentage must be at least greater than 1%.";
                    }
                }
            };

            TempData["ChargeSplitDTOs2"] = chargesplits;

            TempData["ChargeDTO2"] = commissionDTO;

            ViewBag.ChargeSplitDTOs = ChargeSplitDTOs;

            return View("Edit", commissionDTO);
        }


        [HttpPost]
        public async Task<ActionResult> removeChargeSplitEdit(Guid? id, CommissionDTO commissionDTO)
        {
            await ServeNavigationMenus();

            if (id == null || id == Guid.Empty)
            {
                return View("Edit", commissionDTO);
            }

            ChargeSplitDTOs = TempData["chargeSplitsBeforeEdit"] as ObservableCollection<CommissionSplitDTO>;

            if (ChargeSplitDTOs == null)
            {
                ChargeSplitDTOs = new ObservableCollection<CommissionSplitDTO>();
            }

            var chargeSplitToRemove = ChargeSplitDTOs.FirstOrDefault(cs => cs.Id == id);
            if (chargeSplitToRemove != null)
            {
                ChargeSplitDTOs.Remove(chargeSplitToRemove);
            }

            TempData["ChargeSplitDTOs3"] = ChargeSplitDTOs;
            ViewBag.chargeSplits = ChargeSplitDTOs;

            return View("Edit", commissionDTO);
        }



        public async Task<ActionResult> ChargesEdit(CommissionDTO commissionDTO)
        {
            Session["Description"] = commissionDTO.Description;
            Session["MaximumCharge"] = commissionDTO.MaximumCharge;
            Session["IsLocked"] = commissionDTO.IsLocked;

            return View("edit", commissionDTO);
        }



        [HttpPost]
        public async Task<ActionResult> Edit(CommissionDTO commissionDTO)
        {
            var chargeId = (Guid)Session["Id"];

            commissionDTO.Description = Session["Description"].ToString();
            commissionDTO.MaximumCharge = Convert.ToDecimal(Session["MaximumCharge"].ToString());
            commissionDTO.IsLocked = (bool)Session["IsLocked"];

            commissionDTO.ValidateAll();

            if (TempData["ChargeSplitDTOs2"] != null)
            {
                commissionDTO.chargeSplit = TempData["ChargeSplitDTOs2"] as ObservableCollection<CommissionSplitDTO>;

                var chargeSplits = commissionDTO.chargeSplit;

                var splitCount = chargeSplits.Count();

                if (splitCount == 1)
                {
                    var sumPercentages = chargeSplits.Sum(cs => cs.Percentage);

                    if (sumPercentages < 100)
                    {
                        await ServeNavigationMenus();

                        TempData["splitPercentage"] = "For charges linked to only one G/L Account, enter 100 as percentage";

                        ViewBag.chargeSplits = TempData["chargeSplitsBeforeEdit"];

                        return View("edit", commissionDTO);
                    }
                    else
                    {
                        var chartOfAccountCounts = chargeSplits.GroupBy(cs => cs.ChartOfAccountId)
                                                               .Select(g => new { ChartOfAccountId = g.Key, Count = g.Count() })
                                                               .ToList();


                        var duplicateChartOfAccounts = chartOfAccountCounts.Where(x => x.Count > 1)
                                                                           .Select(x => x.ChartOfAccountId)
                                                                           .ToList();

                        if (duplicateChartOfAccounts.Any())
                        {
                            await ServeNavigationMenus();

                            TempData["tPercentage"] = "Sorry, you cannot split charge into the same G/L Account more than once";

                            ViewBag.chargeSplits = TempData["chargeSplitsBeforeEdit"];

                            return View("edit", commissionDTO);
                        }

                        var charge = await _channelService.UpdateCommissionAsync(commissionDTO, GetServiceHeader());


                        TempData["Edit"] = "Successfully Edited Charge";
                        TempData["ChargeDTO3"] = "";


                        if (chargeSplits.Any())
                            await _channelService.UpdateCommissionSplitsByCommissionIdAsync(chargeId, chargeSplits, GetServiceHeader());
                        TempData["ChargeSplitDTOs"] = "";
                    }
                }
                else if (splitCount > 1)
                {
                    var sumPercentages = chargeSplits.Sum(cs => cs.Percentage);

                    if (sumPercentages < 100)
                    {
                        await ServeNavigationMenus();

                        TempData["splitPercentage"] = "Total percentage must be equal to 100%";

                        ViewBag.chargeSplits = TempData["chargeSplitsBeforeEdit"];

                        return View("edit", commissionDTO);
                    }
                    else
                    {
                        var chartOfAccountCounts = chargeSplits.GroupBy(cs => cs.ChartOfAccountId)
                                                               .Select(g => new { ChartOfAccountId = g.Key, Count = g.Count() })
                                                               .ToList();


                        var duplicateChartOfAccounts = chartOfAccountCounts.Where(x => x.Count > 1)
                                                                           .Select(x => x.ChartOfAccountId)
                                                                           .ToList();

                        if (duplicateChartOfAccounts.Any())
                        {
                            await ServeNavigationMenus();

                            TempData["tPercentage"] = "Sorry, you cannot split charge into the same G/L Account more than once";

                            ViewBag.chargeSplits = TempData["chargeSplitsBeforeEdit"];

                            return View("edit", commissionDTO);
                        }

                        var charge = await _channelService.AddCommissionAsync(commissionDTO, GetServiceHeader());

                        if (charge.ErrorMessageResult != null)
                        {
                            await ServeNavigationMenus();

                            TempData["ErrorMsg"] = charge.ErrorMessageResult;

                            ViewBag.chargeSplits = TempData["chargeSplitsBeforeEdit"];

                            return View();
                        }

                        TempData["Edit"] = "Successfully Edited Charge";
                        TempData["ChargeDTO3"] = "";


                        if (chargeSplits.Any())
                            await _channelService.UpdateCommissionSplitsByCommissionIdAsync(chargeId, chargeSplits, GetServiceHeader());
                        TempData["ChargeSplitDTOs"] = "";
                    }

                    return RedirectToAction("Index");
                }
            }



            if (!commissionDTO.HasErrors)
            {
                await _channelService.UpdateCommissionAsync(commissionDTO, GetServiceHeader());

                TempData["Edit"] = "Successfully Edited Charge";

                var chargeSplits = TempData["chargeSplitsBeforeEdit"] as ObservableCollection<CommissionSplitDTO>;

                await _channelService.UpdateCommissionSplitsByCommissionIdAsync(chargeId, chargeSplits, GetServiceHeader());

                TempData["chargeSplitsBeforeEdit"] = "";

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = commissionDTO.ErrorMessages;

                ViewBag.chargeSplits = TempData["chargeSplitsBeforeEdit"];

                TempData["EditError"] = "Failed to Edit Charge";

                return View("edit");
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetCommissionsAsync()
        {
            var commissionsDTOs = await _channelService.FindCommissionsAsync(GetServiceHeader());

            return Json(commissionsDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}