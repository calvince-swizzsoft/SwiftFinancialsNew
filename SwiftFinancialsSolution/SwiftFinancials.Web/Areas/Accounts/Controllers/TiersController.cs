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
    public class TiersController : MasterController
    {
        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            ViewBag.chargeType = GetChargeTypeSelectList(string.Empty);

            return View();
        }


        [HttpPost]
        public async Task<ActionResult> Add(Guid? id, GraduatedScaleDTO graduatedScaleDTO)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            TiersDTOs = TempData["TiersDTOs"] as ObservableCollection<GraduatedScaleDTO>;

            double sumPercentages = 0;


            if (TiersDTOs == null)
                TiersDTOs = new ObservableCollection<GraduatedScaleDTO>();

            foreach (var tiersDTO in graduatedScaleDTO.tierSplits)
            {
                tiersDTO.RangeLowerLimit = tiersDTO.RangeLowerLimit;
                tiersDTO.RangeUpperLimit = tiersDTO.RangeUpperLimit;
                tiersDTO.ChargeType = tiersDTO.ChargeType;
                tiersDTO.ChargePercentage = tiersDTO.ChargePercentage;
                tiersDTO.ChargeFixedAmount = tiersDTO.ChargeFixedAmount;

                TempData["tPercentage"] = "";

                TiersDTOs.Add(tiersDTO);

                //sumPercentages = TiersDTOs.Sum(cs => cs.Percentage);

                Session["tierSplit"] = graduatedScaleDTO.tierSplits;

                //if (sumPercentages > 100)
                //{
                //    TempData["tPercentage"] = "Failed to add \"" + chargeSplitDTO.Description.ToUpper() + "\". Total percentage exceeded 100%.";

                //    ChargeSplitDTOs.Remove(chargeSplitDTO);

                //    Session["chargeSplit"] = ChargeSplitDTOs;
                //}
                //else if (sumPercentages < 1)
                //{
                //    TempData["tPercentage"] = "Total percentage must be at least greater than 1%.";
                //}
            };

            //ViewBag.totalPercentage = sumPercentages;

            //Session["totalPercentage"] = sumPercentages;

            TempData["TiersDTOs"] = TiersDTOs;

            TempData["graduatedScaleDTO"] = graduatedScaleDTO;

            ViewBag.TiersDTOs = TiersDTOs;

            return View("Create", graduatedScaleDTO);
        }


        public async Task<ActionResult> Search(Guid? id, GraduatedScaleDTO graduatedScaleDTO)
        {
            ViewBag.chargeType = GetChargeTypeSelectList(string.Empty);

            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            var commission = await _channelService.FindCommissionAsync(parseId, GetServiceHeader());

            if (commission != null)
            {
                graduatedScaleDTO.CommissionId = commission.Id;
                graduatedScaleDTO.CommissionDescription = commission.Description;
                graduatedScaleDTO.maximumCharge = commission.MaximumCharge;

                Session["graduatedScaleId"] = graduatedScaleDTO.CommissionId;
                Session["graduatedScaleDescription"] = graduatedScaleDTO.CommissionDescription;
            }

            return View("Create", graduatedScaleDTO);
        }


        [HttpPost]
        public async Task<ActionResult> Create(CommissionDTO commissionDTO)
        {
            commissionDTO.ValidateAll();

            if (!commissionDTO.ErrorMessages.Any())
            {
                //await _channelService.UpdateGraduatedScalesByCommissionIdAsync(,);

                return RedirectToAction("Index");
            }
            else
            {
                ViewBag.chargeType = GetChargeTypeSelectList(commissionDTO.ChargeType.ToString());

                return View(commissionDTO);
            }
        }
    }
}