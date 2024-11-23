using System.Windows.Forms;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.FrontOfficeModule;
using System.Threading.Tasks;
using Application.MainBoundedContext.DTO.AdministrationModule;
using Application.MainBoundedContext.DTO.HumanResourcesModule;
using Application.MainBoundedContext.DTO.RegistryModule;
using Infrastructure.Crosscutting.Framework.Utils;
using SwiftFinancials.Presentation.Infrastructure.Models;
using System.Windows.Forms;
using Microsoft.AspNet.Identity;

namespace SwiftFinancials.Web.Areas.Accounts.Controllers
{
    public class UnPayReasonController : MasterController
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

            var pageCollectionInfo = await _channelService.FindUnPayReasonsByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(commission => commission.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<UnPayReasonDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var unPayReasonDTO = await _channelService.FindUnPayReasonAsync(id, GetServiceHeader());

            var applicableCharges = await _channelService.FindCommissionsByUnPayReasonIdAsync(id, GetServiceHeader());

            ViewBag.applicableCharges = applicableCharges;

            return View(unPayReasonDTO);
        }


        public async Task<ActionResult> UnpayReason(UnPayReasonDTO unpayReasonDTO)
        {
            Session["Description"] = unpayReasonDTO.Description;
            Session["Code"] = unpayReasonDTO.Code;
            Session["isLocked"] = unpayReasonDTO.IsLocked;

            return View("Create", unpayReasonDTO);
        }



        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            return View();
        }




        [HttpPost]
        public async Task<ActionResult> Create(UnPayReasonDTO unPayReasonDTO, ObservableCollection<CommissionDTO> selectedRows)
        {
            unPayReasonDTO.Description = Session["Description"].ToString();
            unPayReasonDTO.Code = Convert.ToInt32(Session["Code"].ToString());
            unPayReasonDTO.IsLocked = (bool)Session["isLocked"];

            unPayReasonDTO.ValidateAll();
            
            if (!unPayReasonDTO.HasErrors)
            {

                MessageBox.Show("Unpay Reason","Do you wish to create UnPay reason",MessageBoxButtons.OK,
                                                               MessageBoxIcon.Information,
                                                               MessageBoxDefaultButton.Button1,
                                                               MessageBoxOptions.ServiceNotification

                                                               
                                                           ); 
    var result = await _channelService.AddUnPayReasonAsync(unPayReasonDTO, GetServiceHeader());

                await _channelService.UpdateCommissionsByUnPayReasonIdAsync(result.Id, selectedRows, GetServiceHeader());

                var myId = result.Id;

                if (result.ErrorMessageResult != null)
                {
                    result.Id = myId;
                }

                TempData["Create"] = "Unpay Reason Created Successfully";

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = unPayReasonDTO.ErrorMessages;

                TempData["CreateError"] = "Failed to Create Unpay Reason";

                return View(unPayReasonDTO);
            }
        }



        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            Session["UnpayReasonId"] = id;

            var unPayReasonDTO = await _channelService.FindUnPayReasonAsync(id, GetServiceHeader());

            return View(unPayReasonDTO);
        }

        public async Task<ActionResult> UnpayReasonEdit(UnPayReasonDTO unpayReasonDTO)
        {
            Session["Description"] = unpayReasonDTO.Description;
            Session["Code"] = unpayReasonDTO.Code;
            Session["isLocked"] = unpayReasonDTO.IsLocked;

            return View("Edit", unpayReasonDTO);
        }

        [HttpPost]
        public async Task<ActionResult> Edit(UnPayReasonDTO unpayReasonDTO, ObservableCollection<CommissionDTO> selectedRows)
        {
            Guid findUnpayReasonId = (Guid)Session["UnpayReasonId"];

            //unpayReasonDTO.Description = Session["Description"].ToString();
            //unpayReasonDTO.Code = Convert.ToInt32(Session["Code"].ToString());
            //unpayReasonDTO.IsLocked = (bool)Session["isLocked"];

            if (!unpayReasonDTO.HasErrors)
            {
                await _channelService.UpdateUnPayReasonAsync(unpayReasonDTO, GetServiceHeader());

                await _channelService.UpdateCommissionsByUnPayReasonIdAsync(findUnpayReasonId, selectedRows);

                TempData["Edit"] = "Unpay Reason Edited Successfully";

                return RedirectToAction("Index");
            }
            else
            {
                TempData["EditError"] = "Failed to Edit Unpay Reason";

                return View(unpayReasonDTO);
            }
        }

      
    }
}

