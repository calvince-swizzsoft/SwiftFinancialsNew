using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Infrastructure.Crosscutting.Framework.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Management;
using TestApis.Controllers;

namespace SwiftFinancials.Web.Areas.Accounts.Controllers
{
    [RoutePrefix("api/investmentsproducts")]
    public class InvestmentsProductController : MasterController
    {


        //[HttpPost]
        //public async Task<IHttpActionResult> Index(JQueryDataTablesModel jQueryDataTablesModel)
        //{
        //    int totalRecordCount = 0;
        //    int searchRecordCount = 0;

        //    var pageCollectionInfo = await _channelService.FindInvestmentProductsByFilterInPageAsync(jQueryDataTablesModel.sSearch, 0, int.MaxValue, GetServiceHeader());

        //    if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
        //    {

        //        var sortedData = pageCollectionInfo.PageCollection
        //            .OrderByDescending(i => i.CreatedDate)
        //            .ToList();

        //        totalRecordCount = sortedData.Count;

        //        var paginatedData = sortedData
        //            .Skip(jQueryDataTablesModel.iDisplayStart)
        //            .Take(jQueryDataTablesModel.iDisplayLength)
        //            .ToList();

        //        searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch)
        //            ? sortedData.Count
        //            : totalRecordCount;

        //        return this.DataTablesJson(
        //            items: paginatedData,
        //            totalRecords: totalRecordCount,
        //            totalDisplayRecords: searchRecordCount,
        //            sEcho: jQueryDataTablesModel.sEcho
        //        );
        //    }

        //    return this.DataTablesJson(
        //        items: new List<InvestmentProductDTO>(),
        //        totalRecords: totalRecordCount,
        //        totalDisplayRecords: searchRecordCount,
        //        sEcho: jQueryDataTablesModel.sEcho
        //    );
        //}

        //public async Task<IHttpActionResult> Details(Guid id)
        //{
        //    await ServeNavigationMenus();

        //    var investmentProductDTO = await _channelService.FindInvestmentProductAsync(id, GetServiceHeader());

        //    // Check if ParentId has a value
        //    if (investmentProductDTO.ParentId.HasValue)
        //    {
        //        var findParentProduct = await _channelService.FindInvestmentProductAsync(investmentProductDTO.ParentId.Value, GetServiceHeader());
        //        investmentProductDTO.ParentChartOfAccountNameDescription = findParentProduct.Description;
        //    }
        //    else
        //    {
        //        investmentProductDTO.ParentChartOfAccountNameDescription = "No Parent Product";
        //    }

        //    return View(investmentProductDTO);
        //}



        //public async Task<IHttpActionResult> Parent(InvestmentProductDTO investmentProductDTO, Guid? id)
        //{
        //    await ServeNavigationMenus();

        //    Guid parseId;

        //    if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
        //    {
        //        return View();
        //    }


        //    var parentGL = await _channelService.FindChartOfAccountAsync(parseId, GetServiceHeader());

        //    if (parentGL != null)
        //    {
        //        investmentProductDTO.ParentId = parentGL.ParentId;
        //        investmentProductDTO.ParentChartOfAccountNameDescription = parentGL.ParentAccountName;
        //    }


        //    return View("Create", investmentProductDTO);
        //}


        //[HttpPost]
        //public async Task<JsonResult> ParentChartOfAccountsIndex(JQueryDataTablesModel jQueryDataTablesModel)
        //{
        //    int totalRecordCount = 0;
        //    int searchRecordCount = 0;

        //    var pageCollectionInfo = await _channelService.
        //        FindChartOfAccountsByFilterInPageAsync(jQueryDataTablesModel.sSearch, 0, int.MaxValue, GetServiceHeader());

        //    if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
        //    {
        //        var filteredData = pageCollectionInfo.PageCollection
        //            .Where(c => c.AccountCategory != (int)ChartOfAccountCategory.HeaderAccount)
        //            .ToList();

        //        var sortedData = filteredData
        //            .OrderByDescending(c => c.CreatedDate)
        //            .ToList();

        //        totalRecordCount = sortedData.Count;

        //        var paginatedData = sortedData
        //            .Skip(jQueryDataTablesModel.iDisplayStart)
        //            .Take(jQueryDataTablesModel.iDisplayLength)
        //            .ToList();

        //        searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch)
        //            ? sortedData.Count
        //            : totalRecordCount;

        //        return this.DataTablesJson(
        //            items: paginatedData,
        //            totalRecords: totalRecordCount,
        //            totalDisplayRecords: searchRecordCount,
        //            sEcho: jQueryDataTablesModel.sEcho
        //        );
        //    }

        //    return this.DataTablesJson(
        //        items: new List<ChartOfAccountDTO>(),
        //        totalRecords: totalRecordCount,
        //        totalDisplayRecords: searchRecordCount,
        //        sEcho: jQueryDataTablesModel.sEcho
        //    );
        //}

        //public async Task<ActionResult> ChartOfAccountLookUp(Guid? id, InvestmentProductDTO investmentProductDTO)
        //{
        //    await ServeNavigationMenus();

        //    Guid parseId;

        //    if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
        //    {
        //        return View();
        //    }

        //    var chartOfAccount = await _channelService.FindChartOfAccountAsync(parseId, GetServiceHeader());


        //    if (chartOfAccount != null)
        //    {
        //        investmentProductDTO.ChartOfAccountId = chartOfAccount.Id;
        //        investmentProductDTO.ChartOfAccountAccountName = chartOfAccount.AccountName;


        //        return Json(new
        //        {
        //            success = true,
        //            data = new
        //            {
        //                ChartOfAccountId = investmentProductDTO.ChartOfAccountId,
        //                ChartOfAccountAccountName = investmentProductDTO.ChartOfAccountAccountName
        //            }
        //        });
        //    }
        //    return Json(new { success = false, message = "Product Not Found!" });
        //}


        //public async Task<ActionResult> ParentProductLookUp(Guid? id, InvestmentProductDTO investmentProductDTO)
        //{
        //    await ServeNavigationMenus();

        //    Guid parseId;

        //    if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
        //    {
        //        return View();
        //    }

        //    var parentProduct = await _channelService.FindInvestmentProductAsync(parseId, GetServiceHeader());


        //    if (parentProduct != null)
        //    {
        //        investmentProductDTO.ParentId = parentProduct.Id;
        //        investmentProductDTO.ParentChartOfAccountNameDescription = parentProduct.Description;

        //        return Json(new
        //        {
        //            success = true,
        //            data = new
        //            {
        //                ParentId = investmentProductDTO.ParentId,
        //                ParentChartOfAccountNameDescription = investmentProductDTO.ParentChartOfAccountNameDescription
        //            }
        //        });
        //    }
        //    return Json(new { success = false, message = "Product Not Found!" });
        //}


        //public async Task<IHttpActionResult> Create(Guid? id, InvestmentProductDTO investmentProductDTO)
        //{
        //    await ServeNavigationMenus();

        //    Guid parseId;

        //    if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
        //    {
        //        return View();
        //    }

        //    var parentGL = await _channelService.FindChartOfAccountAsync(parseId, GetServiceHeader());

        //    if (parentGL != null)
        //    {
        //        investmentProductDTO.ParentId = parentGL.ParentId;
        //        investmentProductDTO.ChartOfAccountAccountName = parentGL.ParentAccountName;
        //    }

        //    ViewBag.RecoveryPriority = GetRecoveryPrioritySelectList(string.Empty);

        //    return View(investmentProductDTO);
        //}

        [HttpPost]
        [Route("")]
        public async Task<IHttpActionResult> Create(InvestmentProductDTO investmentProductDTO)
        {
            investmentProductDTO.ValidateAll();

            if (!investmentProductDTO.HasErrors)
            {
                var createdInvestmentProduct = await _channelService.AddInvestmentProductAsync(investmentProductDTO, GetServiceHeader());

                return Ok(createdInvestmentProduct);
            }
            else
            {
                var errorMessages = investmentProductDTO.ErrorMessages;

                return Json(new
                {
                    success = false,
                    message = errorMessages.ToString()
                });
            }
        }

        //[HttpPut]
        //[Route()]
        //public async Task<IHttpActionResult> Edit(Guid id)
        //{
        //    await ServeNavigationMenus();

        //    var investmentProductDTO = await _channelService.FindInvestmentProductAsync(id, GetServiceHeader());

        //    // Check if ParentId has a value
        //    if (investmentProductDTO.ParentId.HasValue)
        //    {
        //        var findParentProduct = await _channelService.FindInvestmentProductAsync(investmentProductDTO.ParentId.Value, GetServiceHeader());
        //        investmentProductDTO.ParentChartOfAccountNameDescription = findParentProduct.Description;
        //    }
        //    else
        //    {
        //        investmentProductDTO.ParentChartOfAccountNameDescription = "No Parent Product";
        //    }

     
        //    return Ok(investmentProductDTO);
        //}


        [HttpPut]

        [Route("")]
        public async Task<IHttpActionResult> Edit(InvestmentProductDTO investmentProductBindingModel)
        {
           
            investmentProductBindingModel.ValidateAll();

            if (ModelState.IsValid)
            {
                await _channelService.UpdateInvestmentProductAsync(investmentProductBindingModel, GetServiceHeader());

             
                return Json(new{
                success = true,
                message = "Edited Invetsments Product successfully"
                });
            }
            else
            {
                return Json(new
                {
                    success = false,
                    message = "Failed to edit product"
                });
            }
        }

        [HttpGet]
        [Route("")]
        public async Task<IHttpActionResult> GetInvestmentProductsAsync()
        {
            var investmentProductDTOs = await _channelService.FindInvestmentProductsAsync(GetServiceHeader());

            return Ok(investmentProductDTOs);
        }
    }
}
