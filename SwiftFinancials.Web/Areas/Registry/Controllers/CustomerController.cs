using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.RegistryModule;
using SwiftFinancials.Presentation.Infrastructure.Util;
using SwiftFinancials.Web.Attributes;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.Registry.Controllers
{
    [RoleBasedAccessControl]
    public class CustomerController : MasterController
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

            var pageCollectionInfo = await _channelService.FindCustomersByFilterInPageAsync(jQueryDataTablesModel.sSearch,1, jQueryDataTablesModel.iDisplayStart,jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<CustomerDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var customerDTO = await _channelService.FindCustomerAsync(id, GetServiceHeader());

            return View(customerDTO);
        }

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(CustomerBindingModel customerBindingModel)
        {
            customerBindingModel.ValidateAll();


            //cheat
            var mandatoryDebitTypes =new List<DebitTypeDTO>();
            var mandatoryProducts = new ProductCollectionInfo();

            if (!customerBindingModel.HasErrors)
            {
                await _channelService.AddCustomerAsync(customerBindingModel.MapTo<CustomerDTO>(), mandatoryDebitTypes, mandatoryProducts,1,GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = customerBindingModel.ErrorMessages;

                return View(customerBindingModel);
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var customerDTO = await _channelService.FindCustomerAsync(id, GetServiceHeader());

            return View(customerDTO.MapTo<CustomerBindingModel>());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, CustomerBindingModel customerBindingModel)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateCustomerAsync(customerBindingModel.MapTo<CustomerDTO>(), GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                return View(customerBindingModel);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetCustomersAsync()
        {
            var CustomersDTOs = await _channelService.FindCustomersAsync(GetServiceHeader());

            return Json(CustomersDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}