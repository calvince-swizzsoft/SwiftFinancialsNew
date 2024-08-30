using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.Accounts.Controllers
{
    public class BatchOrigination_WireTransferController : MasterController
    {
        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public async Task<JsonResult> Index(JQueryDataTablesModel jQueryDataTablesModel)
        {

            DateTime startDate = DateTime.Now.AddDays(-30);
            DateTime endDate = DateTime.Now.AddDays(+30);

            int totalRecordCount = 0;

            int status = 0;

            int searchRecordCount = 0;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindWireTransferBatchesByStatusAndFilterInPageAsync(status,startDate,endDate, jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(debitBatch => debitBatch.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<WireTransferBatchDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }


        [HttpPost]
        public async Task<ActionResult> BatchOrigination_WireTransfer(WireTransferBatchDTO  wireTransferBatchDTO)
        {
            await ServeNavigationMenus();
            ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(string.Empty);
            ViewBag.QueuePrioritySelectList = GetQueuePrioritySelectList(string.Empty);

            Session["HeaderDetails"] = wireTransferBatchDTO;

            return View("Create", wireTransferBatchDTO);
        }


        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var wireTransferBatch = await _channelService.FindWireTransferBatchAsync(id, GetServiceHeader());

            return View(wireTransferBatch);
        }


        public async Task<ActionResult> Create(Guid? id)
        {
            await ServeNavigationMenus();
            ViewBag.BatchType = GetWireTransferBatchTypeSelectList(string.Empty);
            ViewBag.Priority = GetQueuePriorityAsync(string.Empty);

            return View();
        }
        public async Task<ActionResult> WireTransferTypeLookup(Guid? id, WireTransferBatchDTO wireTransferBatchDTO)
        {

            // Check if the id is null or an empty Guid
            if (id == null || id == Guid.Empty)
            {
                await ServeNavigationMenus();
                return View("Create", wireTransferBatchDTO);
            }

            // Fetch all wire transfer types and filter by the provided id
            var wireTransferTypes = await _channelService.FindWireTransferTypesAsync(GetServiceHeader());
            var wireTransferType = wireTransferTypes.FirstOrDefault(wt => wt.Id == id);

            // If the wire transfer type with the specified id is not found, you can handle it accordingly
            if (wireTransferType == null)
            {
                
                return View("Create", wireTransferBatchDTO);
            }

            wireTransferBatchDTO.WireTransferTypeDescription = wireTransferType.ChartOfAccountName;
            wireTransferBatchDTO.WireTransferTypeId = wireTransferType.ChartOfAccountId;

            ViewBag.BatchType = GetWireTransferBatchTypeSelectList(string.Empty);
            ViewBag.Priority = GetQueuePriorityAsync(string.Empty);

            return View("Create", wireTransferBatchDTO);
        }


         
        public async Task<ActionResult> WireTransferCustomerAccountLookUp(Guid? id, WireTransferBatchDTO  wireTransferBatchDTO)
        {

            ViewBag.BatchType = GetWireTransferBatchTypeSelectList(string.Empty);
            ViewBag.Priority = GetQueuePriorityAsync(string.Empty);
            if (Session["HeaderDetails"] != null)
            {
                wireTransferBatchDTO = Session["HeaderDetails"] as WireTransferBatchDTO;
            }

            Guid parseId;
            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                await ServeNavigationMenus();

                return View("Create", wireTransferBatchDTO);
            }


            if (wireTransferBatchDTO != null && wireTransferBatchDTO.WireTransferEntries == null)
            {
                wireTransferBatchDTO.WireTransferEntries = new ObservableCollection<WireTransferBatchEntryDTO>();
            }

            // Ensure at least one entry exists before trying to access it by index
            if (wireTransferBatchDTO.WireTransferEntries.Count == 0)
            {
                wireTransferBatchDTO.WireTransferEntries.Add(new WireTransferBatchEntryDTO());
            }


            var creditcustomerAccount = await _channelService.FindCustomerAccountAsync(parseId, true, true, true, false, GetServiceHeader());

            if (creditcustomerAccount != null)
            {
                wireTransferBatchDTO.WireTransferEntries[0].WireTranferCustomerAccountFullName = creditcustomerAccount.CustomerFullName;
                wireTransferBatchDTO.WireTransferEntries[0].CustomerAccountCustomerReference1 = creditcustomerAccount.CustomerReference1;
                wireTransferBatchDTO.WireTransferEntries[0].CustomerAccountCustomerReference2 = creditcustomerAccount.CustomerReference2;
                wireTransferBatchDTO.WireTransferEntries[0].CustomerAccountCustomerReference3 = creditcustomerAccount.CustomerReference3;
                wireTransferBatchDTO.WireTransferEntries[0].WireTransferAccountIdentificationNumber = creditcustomerAccount.CustomerIndividualIdentityCardNumber;
                wireTransferBatchDTO.WireTransferEntries[0].WiretransferCustomerAccountFullAccountNumber = creditcustomerAccount.FullAccountNumber;
                
                wireTransferBatchDTO.WireTransferEntries[0].ProductDescription = creditcustomerAccount.CustomerAccountTypeProductCodeDescription;
                wireTransferBatchDTO.WireTransferEntries[0].CustomerAccountCustomerId = creditcustomerAccount.Id;
                wireTransferBatchDTO.WireTransferEntries[0].WireTransferAccountStatusDescription = creditcustomerAccount.StatusDescription;
                
                wireTransferBatchDTO.WireTransferEntries[0].CustomerAccountCustomerIndividualPayrollNumbers = creditcustomerAccount.CustomerIndividualPayrollNumbers;



            }

            return View("Create", wireTransferBatchDTO);
        }


        [HttpPost]
        public async Task<ActionResult> Create(WireTransferBatchDTO wireTransferBatchDTO)
        {
            wireTransferBatchDTO.ValidateAll();

            if (!wireTransferBatchDTO.HasErrors)
            {
                await _channelService.AddWireTransferBatchAsync(wireTransferBatchDTO, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = wireTransferBatchDTO.ErrorMessages;
                ViewBag.BatchType = GetWireTransferBatchTypeSelectList(wireTransferBatchDTO.Priority.ToString());
                ViewBag.Priority = GetQueuePriorityAsync(wireTransferBatchDTO.Priority.ToString());
                
                return View(wireTransferBatchDTO);
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            ViewBag.BatchType = GetWireTransferBatchTypeSelectList(string.Empty);
            ViewBag.Priority = GetQueuePriorityAsync(string.Empty);

            var debitBatchDTO = await _channelService.FindDebitBatchAsync(id, GetServiceHeader());

            return View(debitBatchDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, WireTransferBatchDTO wireTransferBatchDTO)
        {
            wireTransferBatchDTO.ValidateAll();

            if (!wireTransferBatchDTO.HasErrors)
            {
                await _channelService.UpdateWireTransferBatchAsync(wireTransferBatchDTO, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = wireTransferBatchDTO.ErrorMessages;

                ViewBag.BatchType = GetWireTransferBatchTypeSelectList(wireTransferBatchDTO.Priority.ToString());
                ViewBag.Priority = GetQueuePriorityAsync(wireTransferBatchDTO.Priority.ToString());

                return View(wireTransferBatchDTO);
            }
        }

        public async Task<ActionResult> Verify(Guid id)
        {
            await ServeNavigationMenus();

            ViewBag.BatchType = GetWireTransferBatchTypeSelectList(string.Empty);
            ViewBag.Priority = GetQueuePriorityAsync(string.Empty);

            var wireTransferBatchDTO = await _channelService.FindWireTransferBatchAsync(id, GetServiceHeader());

            return View(wireTransferBatchDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Verify(Guid id, WireTransferBatchDTO wireTransferBatchDTO)
        {
            wireTransferBatchDTO.ValidateAll();

            if (!wireTransferBatchDTO.HasErrors)
            {
                await _channelService.AuditWireTransferBatchAsync(wireTransferBatchDTO, 1, GetServiceHeader());

                ViewBag.BatchType = GetWireTransferBatchTypeSelectList(wireTransferBatchDTO.Priority.ToString());
                ViewBag.Priority = GetQueuePriorityAsync(wireTransferBatchDTO.Priority.ToString());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = wireTransferBatchDTO.ErrorMessages;

                ViewBag.BatchType = GetWireTransferBatchTypeSelectList(wireTransferBatchDTO.Priority.ToString());
                ViewBag.Priority = GetQueuePriorityAsync(wireTransferBatchDTO.Priority.ToString());

                return View(wireTransferBatchDTO);
            }
        }

        public async Task<ActionResult> Authorize(Guid id)
        {
            await ServeNavigationMenus();

            ViewBag.BatchType = GetWireTransferBatchTypeSelectList(string.Empty);
            ViewBag.Priority = GetQueuePriorityAsync(string.Empty);

            var wireTranferBatchDTO = await _channelService.FindWireTransferBatchAsync(id, GetServiceHeader());

            return View(wireTranferBatchDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Authorize(Guid id, WireTransferBatchDTO wireTransferBatchDTO)
        {
            /*var batchAuthOption = wireTransferBatchDTO.batch*/;
            wireTransferBatchDTO.ValidateAll();



            if (!wireTransferBatchDTO.HasErrors)
            {
                await _channelService.AuthorizeWireTransferBatchAsync(wireTransferBatchDTO, 1, 1, GetServiceHeader());

                ViewBag.BatchType = GetWireTransferBatchTypeSelectList(wireTransferBatchDTO.Priority.ToString());
                ViewBag.Priority = GetQueuePriorityAsync(wireTransferBatchDTO.Priority.ToString());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = wireTransferBatchDTO.ErrorMessages;

                ViewBag.BatchType = GetWireTransferBatchTypeSelectList(wireTransferBatchDTO.Priority.ToString());
                ViewBag.Priority = GetQueuePriorityAsync(wireTransferBatchDTO.Priority.ToString());

                return View(wireTransferBatchDTO);
            }
        }



        //[HttpGet]
        //public async Task<JsonResult> GetDebitBatchesAsync()
        //{
        //    var debitBatchDTOs = await _channelService.FindDebitBatchesAsync(GetServiceHeader());

        //    return Json(debitBatchDTOs, JsonRequestBehavior.AllowGet);
        //}
    }

}
