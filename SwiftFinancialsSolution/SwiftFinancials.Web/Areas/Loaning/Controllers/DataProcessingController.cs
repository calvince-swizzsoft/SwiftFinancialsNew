using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.BackOfficeModule;
using Application.MainBoundedContext.DTO.RegistryModule;
using Infrastructure.Crosscutting.Framework.Utils;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.Loaning.Controllers
{
    public class DataProcessingController : MasterController
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

            var pageCollectionInfo = await _channelService.FindDataAttachmentPeriodsInPageAsync(jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<DataAttachmentPeriodDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }


        public async Task<ActionResult> Details(Guid? id)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }


            return View();
        }


        public async Task<ActionResult> Create(Guid? id, DataAttachmentEntryDTO dataAttachmentEntryDTO)
        {
            await ServeNavigationMenus();

            ViewBag.TransactionTypeSelectList = GetDataAttachmentTransactionTypeTypeSelectList(string.Empty);

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            var dataPeriod = await _channelService.FindDataAttachmentPeriodAsync(parseId, GetServiceHeader());
            if (dataPeriod != null)
            {
                dataAttachmentEntryDTO.DataAttachmentPeriodId = dataPeriod.Id;
                dataAttachmentEntryDTO.DataAttachmentPeriodDescription = dataPeriod.MonthDescription;
                dataAttachmentEntryDTO.DataPeriodRemarks = dataPeriod.Remarks;

                Session["DataAttachmentPeriod"] = dataAttachmentEntryDTO;
            }

            return View(dataAttachmentEntryDTO);
        }



        public async Task<ActionResult> CustomerLookUp(Guid? id, DataAttachmentEntryDTO dataAttachmentEntryDTO)
        {
            await ServeNavigationMenus();

            ViewBag.TransactionTypeSelectList = GetDataAttachmentTransactionTypeTypeSelectList(string.Empty);

            if (Session["DataAttachmentPeriod"] != null)
                dataAttachmentEntryDTO = Session["DataAttachmentPeriod"] as DataAttachmentEntryDTO;


              if (Session["customerAccountLookUp"] != null)
                dataAttachmentEntryDTO = Session["customerAccountLookUp"] as DataAttachmentEntryDTO;

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }



            var customer = await _channelService.FindCustomerAsync(parseId, GetServiceHeader());
            if (customer != null)
            {
                dataAttachmentEntryDTO.CustomerAccountCustomerId = customer.Id;
                dataAttachmentEntryDTO.CustomerAccountCustomerIndividualFirstName = customer.IndividualSalutationDescription + " " + customer.IndividualFirstName + " " +
                    customer.IndividualLastName;
                dataAttachmentEntryDTO.CustomerAccountCustomerStationZoneDivisionEmployerId = (Guid)customer.StationZoneDivisionEmployerId;
                dataAttachmentEntryDTO.CustomerAccountCustomerStationZoneDivisionEmployerDescription = customer.StationZoneDivisionEmployerDescription;
                dataAttachmentEntryDTO.CustomerAccountCustomerStationZoneDivisionStationId = (Guid)customer.StationId;
                dataAttachmentEntryDTO.CustomerAccountCustomerStationZoneDivisionStationDescription = customer.StationDescription;
                dataAttachmentEntryDTO.CustomerAccountCustomerSerialNumber = customer.SerialNumber;
                dataAttachmentEntryDTO.CustomerAccountCustomerIndividualPayrollNumbers = customer.IndividualPayrollNumbers;
                dataAttachmentEntryDTO.CustomerAccountCustomerNonIndividualRegistrationDate = customer.RegistrationDate;
                dataAttachmentEntryDTO.CustomerRemarks = customer.Remarks;
                dataAttachmentEntryDTO.CustomerAccountCustomerPersonalIdentificationNumber = customer.PersonalIdentificationNumber;
                dataAttachmentEntryDTO.Ref1 = customer.Reference1;
                dataAttachmentEntryDTO.Ref2 = customer.Reference2;
                dataAttachmentEntryDTO.Ref3 = customer.Reference3;


                var CustomerAccounts = await _channelService.FindCustomerAccountsByCustomerIdAsync(parseId, true, true, true, true, GetServiceHeader());
                if (CustomerAccounts != null)
                    ViewBag.CustomerAccounts = CustomerAccounts;
                else
                    TempData["customerAccount"] = "The selected customer has no associated/attached accounts";

                dataAttachmentEntryDTO.CustomerDTO = customer;

                Session["customerDetails"] = dataAttachmentEntryDTO;

                foreach(var cId in CustomerAccounts)
                {
                    var idId = cId.Id;

                    Session["customerAccountId"] = idId;
                }
            }

            return View("create", dataAttachmentEntryDTO);
        }


        public async Task<ActionResult> CustomerAccountLookUp(Guid? id, DataAttachmentEntryDTO dataAttachmentEntryDTO)
        {
            await ServeNavigationMenus();

            ViewBag.TransactionTypeSelectList = GetDataAttachmentTransactionTypeTypeSelectList(string.Empty);

            if (Session["customerDetails"] != null)
                dataAttachmentEntryDTO = Session["customerDetails"] as DataAttachmentEntryDTO;

            if (Session["DataAttachmentPeriod"] != null)
                dataAttachmentEntryDTO = Session["DataAttachmentPeriod"] as DataAttachmentEntryDTO;

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            var customerAccount = await _channelService.FindCustomerAccountAsync(parseId, true, true, true, true, GetServiceHeader());
            if (customerAccount != null)
            {
                dataAttachmentEntryDTO.customerAccountDTO = customerAccount;
                dataAttachmentEntryDTO.CustomerAccountId = customerAccount.Id;

                Session["customerAccountLookUp"] = dataAttachmentEntryDTO;
            }

            return View("create", dataAttachmentEntryDTO);
        }


        [HttpPost]
        public async Task<ActionResult> Create(DataAttachmentEntryDTO dataAttachmentEntryDTO)
        {

            dataAttachmentEntryDTO.CustomerAccountId = (Guid)Session["customerAccountId"];

            dataAttachmentEntryDTO.ValidateAll();

            if (!dataAttachmentEntryDTO.HasErrors)
            {
                await _channelService.AddDataAttachmentEntryAsync(dataAttachmentEntryDTO, GetServiceHeader());

                TempData["message"] = "Successfully created Data Attachment Entry";

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = dataAttachmentEntryDTO.ErrorMessages.ToString();

                TempData["BugdetBalance"] = errorMessages;

                TempData["messageError"] = "Could not create Data Period";

                ViewBag.MonthSelectList = GetDataAttachmentTransactionTypeTypeSelectList(dataAttachmentEntryDTO.TransactionTypeDescription);

                await ServeNavigationMenus();

                return View();
            }
        }
    }
}
