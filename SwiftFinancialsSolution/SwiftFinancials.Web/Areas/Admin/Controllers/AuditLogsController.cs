using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AdministrationModule;
using DistributedServices.MainBoundedContext.Identity;
using Domain.MainBoundedContext.AdministrationModule.Aggregates.BankBranchAgg;
using Infrastructure.Crosscutting.Framework.Utils;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.DataProtection;
using Newtonsoft.Json;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using SwiftFinancials.Web.Identity;
using SwiftFinancials.Web.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.Admin.Controllers
{
    public class AuditLogsController : MasterController
    {
        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();
            return View();
        }

        [HttpPost]
        public async Task<JsonResult> Index(JQueryDataTablesModel jQueryDataTablesModel)
        {

            var user = await _applicationUserManager.FindByIdAsync(User.Identity.GetUserId());

            //if (user != null)
            //{
            //    return Json(RedirectToAction("Index"));

            //}ENDDATE

            DateTime start = DateTime.Now.AddDays(-2555);
            DateTime end = DateTime.Now;
            string text = "";
            int totalRecordCount = 0;

            int searchRecordCount = 0;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            List<string> sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;

            var pageCollectionInfo = await _channelService.FindAuditLogsByDateRangeAndFilterInPageAsync(pageIndex, jQueryDataTablesModel.iDisplayLength,start, end,text,GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(bank => bank.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<AuditLogDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }
        //
        //// GET: /Account/Login
        //[AllowAnonymous]
        //public ActionResult Login(string returnUrl)
        //{
        //    ViewBag.ReturnUrl = returnUrl;
        //    return View();
        //}

        ////
        //// POST: /Account/Login
        //[HttpPost]
        //[AllowAnonymous]
        //[ValidateAntiForgeryToken]
        //public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        //{
        //    using (_applicationUserManager)
        //    {
        //        if (!ModelState.IsValid)
        //            return View(model);

        //        ApplicationUser user = await _applicationUserManager.FindByEmailAsync(model.Email);

        //        if (user != null)
        //        {
        //            if (user.LastPasswordChangedDate == null)
        //            {
        //                TempData["ApplicationUser"] = user;

        //                return RedirectToAction("ForceChangePassword", "Account");
        //            }
        //            else if (user.LastPasswordChangedDate.Value.AddDays(_passwordExpiryPeriod) < DateTime.Now)
        //            {
        //                TempData["ApplicationUser"] = user;

        //                return RedirectToAction("ForceChangePassword", "Account");
        //            }
        //            else if (user.LockoutEnabled)
        //            {
        //                return View("Lockout");
        //            }
        //            else
        //            {
        //                // This doesn't count login failures towards account lockout
        //                // To enable password failures to trigger account lockout, change to shouldLockout: true
        //                var result = await _signInManager.PasswordSignInAsync(userName: user.UserName, password: model.Password, isPersistent: false, shouldLockout: false);

        //                switch (result)
        //                {
        //                    case SignInStatus.Success:
        //                        //load navigation access rights
        //                        await LoadModuleAccessRights(user.UserName);
        //                        return View("Index");
        //                    case SignInStatus.LockedOut:
        //                        return View("Lockout");
        //                    case SignInStatus.RequiresVerification:
        //                        return RedirectToAction("SendCode", new { model.Email, ReturnUrl = returnUrl });
        //                    case SignInStatus.Failure:
        //                    default:
        //                        ModelState.AddModelError("", "Invalid login attempt.");
        //                        return View(model);
        //                }
        //            }
        //        }

        //        return View(model);
        //    }
        //}

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var BankDTO = await _channelService.FindBankAsync(id, GetServiceHeader());
            await _channelService.FindBankBranchesByBankIdAsync(BankDTO.Id, GetServiceHeader());

            return View(BankDTO);
        }

        //public async Task<ActionResult> Create()
        //{
        //    await ServeNavigationMenus();

        //    return View();
        //}
        [HttpPost]
        public JsonResult Add(BankDTO bank, string branchdetails)
        {
            // Add bank branch to the bankBranches list (assuming bank.BankBranches is the correct object to add)
            bankBranches.Add(bank.BankBranches);

            // Return JSON response with success flag and a redirect URL
            return Json(new
            {
                success = true,
                data = bankBranches,
                redirectUrl = Url.Action("Create", "Bank") // Generate URL to redirect
            });
        }

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();
            var bankDto = new BankDTO
            {
                BankBranche = new ObservableCollection<BankBranchDTO>()
            };
            return View(bankDto);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(BankDTO bank)
        {
            bank.ValidateAll();

            if (!bank.HasErrors)
            {
                var bankDTO = await _channelService.AddBankAsync(bank, GetServiceHeader());
                BankBranchDTO j = new BankBranchDTO();
                j.Description = bank.Description;
                bankBranches.Add(j);
                await _channelService.UpdateBankBranchesByBankIdAsync(bankDTO.Id, bankBranches, GetServiceHeader());

                return RedirectToAction("Index");
            }
            return View(bank);
        }

        [HttpPost]
        public JsonResult Remove(Guid id, BankDTO bank)
        {
            foreach (var branch in bank.BankBranche)
            {
                bank.Description = branch.Description;

            }
            return Json(new { success = true, data = JournalVoucherEntryDTOs });

        }
        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var BankDTO = await _channelService.FindBankAsync(id, GetServiceHeader());

            return View(BankDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, BankDTO BankBindingModel)
        {
            BankBindingModel.ValidateAll();

            if (!BankBindingModel.HasErrors)
            {
                await _channelService.UpdateBankAsync(BankBindingModel, GetServiceHeader());
                TempData["SuccessMessage"] = "Bank Edited successfully";
                return RedirectToAction("Index");
            }
            else
            {
                return View(BankBindingModel);
            }
        }

        /* [HttpGet]
         public async Task<JsonResult> GetBanksAsync()
         {
             var banksDTOs = await _channelService.FindBanksAsync(GetServiceHeader());

             return Json(banksDTOs, JsonRequestBehavior.AllowGet);
         }*/
    }
}
