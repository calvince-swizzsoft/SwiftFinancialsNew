using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using SwiftFinancials.Web.Controllers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.Accounts.Controllers
{
    public class WellknownchargesController : MasterController
    {
        // GET: Accounts/Wellknowncharges/Create
        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            // Load dropdown lists for the view
            ViewBag.SystemTransactionType = GetSystemTransactionTypeList(string.Empty);
            ViewBag.Chargetype = GetChargeTypeSelectList(string.Empty);
            ViewBag.ChargeBenefactor = GetChargeBenefactorSelectList(string.Empty);

            return View();
        }

        [HttpGet]
        public async Task<ActionResult> FindCommissionsAsync()
        {
            var branchesDTOs = await _channelService.FindCommissionsAsync(GetServiceHeader());
            return Json(branchesDTOs);
        }

        [HttpGet]
        public async Task<ActionResult> GetAction(int systemTransactionTypeId)
        {
            var commissionDTOs = await _channelService.FindCommissionsAsync(GetServiceHeader());
            var linkedTransactionTypes = await _channelService.GetCommissionsForSystemTransactionTypeAsync(systemTransactionTypeId, GetServiceHeader());

            // Identify unlinked commissions
            var unlinkedTransactionTypes = commissionDTOs.Where(c => !linkedTransactionTypes.Any(l => l.Id == c.Id)).ToList();

            return Json(new
            {
                linkedTransactionTypes = linkedTransactionTypes.Select(c => new { c.Id, c.Description, c.MaximumCharge, c.IsLocked, c.CreatedDate }),
                unlinkedTransactionTypes = unlinkedTransactionTypes.Select(c => new { c.Id, c.Description, c.MaximumCharge, c.IsLocked, c.CreatedDate }),
                commissionDTOs
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public async Task<ActionResult> Create(SystemTransactionTypeInCommissionDTO systemTransactionTypeInCommissionDTO, List<Guid> selectedRows)
        {
            var commissions = new ObservableCollection<CommissionDTO>(selectedRows.Select(rowId => new CommissionDTO { Id = rowId }));

            if (!systemTransactionTypeInCommissionDTO.HasErrors)
            {
                await _channelService.MapSystemTransactionTypeToCommissionsAsync(
                    systemTransactionTypeInCommissionDTO.SystemTransactionType,
                    commissions,
                    new ChargeDTO
                    {
                        FixedAmount = systemTransactionTypeInCommissionDTO.ComplementFixedAmount,
                        Percentage = systemTransactionTypeInCommissionDTO.ComplementPercentage,
                        Type = systemTransactionTypeInCommissionDTO.ComplementType
                    },
                    GetServiceHeader()
                );

                return Json(new
                {
                    success = true,
                    message = "Successfully created well-known charges.",
                    redirectUrl = Url.Action("Create", "Wellknowncharges", new { Area = "Accounts" })
                });
            }
            else
            {
                return Json(new
                {
                    success = false,
                    errorMessage = string.Join(", ", systemTransactionTypeInCommissionDTO.ErrorMessages)
                });
            }
        }
    }
}
