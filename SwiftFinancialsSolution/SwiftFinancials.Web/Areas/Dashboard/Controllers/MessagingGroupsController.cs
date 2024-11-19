using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Windows.Forms;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.BackOfficeModule;
using Application.MainBoundedContext.DTO.MessagingModule;
using Application.MainBoundedContext.DTO.RegistryModule;
using Infrastructure.Crosscutting.Framework.List;
using Infrastructure.Crosscutting.Framework.Utils;
using Newtonsoft.Json;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.Dashboard.Controllers
{
    public class MessagingGroupsController : MasterController
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
            int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;
            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;
            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindMessageGroupsByFilterInPageAsync(jQueryDataTablesModel.sSearch, pageIndex, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;
                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch)
                    ? pageCollectionInfo.PageCollection.Count
                    : totalRecordCount;

                var orderedPageCollection = pageCollectionInfo.PageCollection
                    .OrderByDescending(item => item.CreatedDate)
                    .ToList();

                return this.DataTablesJson(
                    items: orderedPageCollection,
                    totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount,
                    sEcho: jQueryDataTablesModel.sEcho
                );
            }
            else
            {
                return this.DataTablesJson(
                    items: new List<MessageGroupDTO> { },
                    totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount,
                    sEcho: jQueryDataTablesModel.sEcho
                );
            }
        }



        [HttpPost]
        public async Task<JsonResult> CustomerIndex(JQueryDataTablesModel jQueryDataTablesModel, int recordStatus, string text, int customerFilter)
        {

            int totalRecordCount = 0;
            int searchRecordCount = 0;
            int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;
            int pageSize = jQueryDataTablesModel.iDisplayLength;
            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;
            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindCustomersByRecordStatusAndFilterInPageAsync(recordStatus, text, customerFilter, pageIndex, pageSize, GetServiceHeader());


            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;
                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch)
                    ? pageCollectionInfo.PageCollection.Count
                    : totalRecordCount;

                var orderedPageCollection = pageCollectionInfo.PageCollection
                    .OrderByDescending(item => item.CreatedDate)
                    .ToList();

                return this.DataTablesJson(
                    items: orderedPageCollection,
                    totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount,
                    sEcho: jQueryDataTablesModel.sEcho
                );
            }
            else
            {
                return this.DataTablesJson(
                    items: new List<CustomerDTO> { },
                    totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount,
                    sEcho: jQueryDataTablesModel.sEcho
                );
            }
        }



        public async Task<ActionResult>Details(Guid id)
        {
            await ServeNavigationMenus();
            var messagingGroups = await _channelService.FindMessageGroupAsync(id, GetServiceHeader());

            ViewBag.TargetValues = JsonConvert.DeserializeObject<ObservableCollection<MessageGroupDTO>>(messagingGroups.TargetValues);

            return View(messagingGroups);
        }



        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            ViewBag.target = GetMessagingGroupTargetSelectList(string.Empty);
            ViewBag.recordStatus = GetRecordStatusSelectList(string.Empty);
            ViewBag.customerFilter = GetCustomerFilterSelectList(string.Empty);

            return View();
        }



        public async Task<ActionResult> CustomerLookUp(Guid? id)
        {
            if (id == Guid.Empty)
            {
                return Json(new { success = false, message = "Invalid ID" });
            }

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            MessageGroupDTO messageGroupDTO = new MessageGroupDTO();

            ViewBag.target = GetMessagingGroupTargetSelectList(string.Empty);
            ViewBag.recordStatus = GetRecordStatusSelectList(string.Empty);
            ViewBag.customerFilter = GetCustomerFilterSelectList(string.Empty);


            var customer = await _channelService.FindCustomerAsync(parseId, GetServiceHeader());

            if (customer != null)
            {
                messageGroupDTO.CustomerID = customer.Id;
                messageGroupDTO.Customer = customer.FullName;
                messageGroupDTO.CustomerEmailAddress = customer.AddressEmail;
                messageGroupDTO.CustomerMobileNumber = customer.AddressMobileLine;

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        CustomerID = messageGroupDTO.CustomerID,
                        Customer = messageGroupDTO.Customer,
                        CustomerEmailAddress = messageGroupDTO.CustomerEmailAddress,
                        CustomerMobileNumber = messageGroupDTO.CustomerMobileNumber
                    }
                });
            }

            return Json(new { success = false, message = "Customer not found" });
        }



        [HttpPost]
        public async Task<ActionResult> Add(string description, int target, Guid customerId, string emailAddress, string customer, string mobileNumber)
        {
            await ServeNavigationMenus();

            MessageGroupDTO messageGroupDTO = new MessageGroupDTO();
            messageGroupDTO.Description = description;
            messageGroupDTO.Target = target;

            try
            {
                if (messageGroupDTO.messageGroupCustomerDTO == null)
                {
                    messageGroupDTO.messageGroupCustomerDTO = new ObservableCollection<MessageGroupDTO>();
                }

                messageGroupDTO.messageGroupCustomerDTO.Add(new MessageGroupDTO
                {
                    CustomerID = customerId,
                    CustomerEmailAddress = emailAddress,
                    Customer = customer,
                    CustomerMobileNumber = mobileNumber
                });


                var messageGroupDTOs = Session["messageGroupDTOs"] as ObservableCollection<MessageGroupDTO>;

                if (messageGroupDTOs == null)
                {
                    messageGroupDTOs = new ObservableCollection<MessageGroupDTO>();
                }


                foreach (var messageGroupEntryDTO in messageGroupDTO.messageGroupCustomerDTO)
                {
                    var existingEntry = messageGroupDTOs.FirstOrDefault(e => e.CustomerID == messageGroupEntryDTO.CustomerID);

                    if (existingEntry != null)
                    {
                        MessageBox.Show(Form.ActiveForm, "The selected customer has already been added to the members list", "Messaging Groups", MessageBoxButtons.OK, MessageBoxIcon.Warning,
                            MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);

                        return Json(new
                        {
                            success = false
                        });
                    }

                    messageGroupDTOs.Add(messageGroupEntryDTO);
                }

                Session["messageGroupDTOs"] = messageGroupDTOs;
                Session["messageGroupDTO"] = messageGroupDTO;

                return Json(new { success = true, entries = messageGroupDTOs });
            }
            catch (Exception ex)
            {

                return Json(new { success = false, message = ex.Message });
            }
        }



        [HttpPost]
        public async Task<JsonResult> Remove(Guid id)
        {
            await ServeNavigationMenus();

            var messageGroupDTOs = Session["messageGroupDTOs"] as ObservableCollection<MessageGroupDTO>;

            if (messageGroupDTOs != null)
            {
                var entryToRemove = messageGroupDTOs.FirstOrDefault(e => e.CustomerID == id);
                if (entryToRemove != null)
                {
                    messageGroupDTOs.Remove(entryToRemove);

                    Session["messageGroupDTOs"] = messageGroupDTOs;
                }
            }

            return Json(new { success = true, data = messageGroupDTOs });
        }



        [HttpPost]
        public async Task<ActionResult> Create(MessageGroupDTO model)
        {
            if (Session["messageGroupDTO"] != null)
            {
                model = Session["messageGroupDTO"] as MessageGroupDTO;
            }

            if (Session["messageGroupDTOs"] != null)
            {
                model.messageGroupCustomerDTO = Session["messageGroupDTOs"] as ObservableCollection<MessageGroupDTO>;

                model.TargetValues = JsonConvert.SerializeObject(model.messageGroupCustomerDTO);
            }

            model.ValidateAll();

            if (!model.HasErrors)
            {
                string message = string.Format(
                                    "Do you want to proceed?"
                                );

                DialogResult result = MessageBox.Show(
                    message,
                    "Messaging Groups",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.ServiceNotification
                );


                if (result == DialogResult.Yes)
                {
                    await _channelService.AddNewMessageGroupAsync(model, GetServiceHeader());
                    MessageBox.Show(Form.ActiveForm, "Operation completed successfully.", "Messaging Groups", MessageBoxButtons.OK, MessageBoxIcon.Information, 
                        MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);

                    Session["messageGroupDTO"] = null;
                    Session["messageGroupDTOs"] = null;

                    return RedirectToAction("Index");
                }
                else
                {
                    await ServeNavigationMenus();

                    var errorMessages = model.ErrorMessages;

                    ViewBag.target = GetMessagingGroupTargetSelectList(model.TargetDescription.ToString());
                    ViewBag.recordStatus = GetRecordStatusSelectList(model.RecordStatusDescription.ToString());
                    ViewBag.customerFilter = GetCustomerFilterSelectList(model.CustomerFilterDescription.ToString());

                    MessageBox.Show(Form.ActiveForm, "Operation cancelled.", "Messaging Groups", MessageBoxButtons.OK, MessageBoxIcon.Hand, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                    return View(model);
                }
            }
            else
            {
                await ServeNavigationMenus();

                var errorMessages = model.ErrorMessages;

                ViewBag.target = GetMessagingGroupTargetSelectList(model.TargetDescription.ToString());
                ViewBag.recordStatus = GetRecordStatusSelectList(model.RecordStatusDescription.ToString());
                ViewBag.customerFilter = GetCustomerFilterSelectList(model.CustomerFilterDescription.ToString());

                MessageBox.Show(Form.ActiveForm, "Operation failed.", "Messaging Groups", MessageBoxButtons.OK, MessageBoxIcon.Hand, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                return View(model);
            }

        }
    }
}