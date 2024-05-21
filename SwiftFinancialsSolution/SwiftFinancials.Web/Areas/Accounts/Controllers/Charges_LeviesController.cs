using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Newtonsoft.Json;
using SwiftFinancials.Presentation.Infrastructure.Util;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.Accounts.Controllers
{
    public class Charges_LeviesController : MasterController
    {
         public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            ViewBag.chargeType = GetChargeTypeSelectList(string.Empty);

            //TempData["Levies"] = await _channelService.FindLeviesAsync(GetServiceHeader());

            var levies = await _channelService.FindLeviesAsync(GetServiceHeader());

            TempData["Levies"] = JsonConvert.SerializeObject(levies); // Serialize the data to store it in TempData

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(CommissionDTO commissionDTO)
        {
            commissionDTO.ValidateAll();

            if (!commissionDTO.ErrorMessages.Any())
            {
                await _channelService.AddCommissionAsync(commissionDTO.MapTo<CommissionDTO>(), GetServiceHeader());

                if (commissionDTO != null)
                {
                    //Update CommissionSplits

                    var commissionSplits = new ObservableCollection<CommissionSplitDTO>();

                    if (commissionDTO.CommissionSplits.Any())
                    {
                        foreach (var commissionSplitDTO in commissionDTO.CommissionSplits)
                        {
                            commissionSplitDTO.CommissionId = commissionDTO.Id;
                            commissionDTO.CommissionSplitChartOfAccountId = commissionSplitDTO.ChartOfAccountId;
                            commissionSplitDTO.ChartOfAccountCostCenterId = commissionDTO.CommissionSplit.ChartOfAccountId;
                            commissionSplits.Add(commissionSplitDTO);
                        }
                        if (commissionSplits.Any())
                            await _channelService.UpdateCommissionSplitsByCommissionIdAsync(commissionDTO.Id, commissionSplits, GetServiceHeader());
                    }

                    //Update CommissionLevies

                    //var commissionLevies = new ObservableCollection<CommissionLevyDTO>();

                    //if (commissionDTO.CommissionLevies.Any())
                    //{
                    //    foreach (var commissionLevyDTO in commissionDTO.CommissionLevies)
                    //    {
                    //        commissionLevyDTO.CommissionId = commissionDTO.Id;
                    //        commissionDTO.CommissionSplitChartOfAccountId = commissionDTO.Id;

                    //        commissionLevies.Add(commissionLevyDTO);
                    //    }

                    //    await _channelService.UpdateCommissionLeviesByCommissionIdAsync(commissionDTO.Id, commissionLevies, GetServiceHeader());
                    //}
                }

                return RedirectToAction("Index");
            }
            else
            {
                IEnumerable<ModelError> allErrors = ModelState.Values.SelectMany(v => v.Errors);

                ViewBag.chargeType = GetChargeTypeSelectList(commissionDTO.ToString());

                TempData["Error"] = string.Join(",", allErrors);

                return View(commissionDTO);
            }
        }
    }
}