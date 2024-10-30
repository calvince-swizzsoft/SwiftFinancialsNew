using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.BackOfficeModule;
using Application.MainBoundedContext.DTO.RegistryModule;
using Infrastructure.Crosscutting.Framework.Utils;
using SwiftFinancials.Web.Areas.Registry.DocumentsModel;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.Loaning.Controllers
{
    public class DataProcessingController : MasterController
    {
        private readonly string _connectionString;
        public DataProcessingController()
        {
            // Get connection string from Web.config
            _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;
        }

        // Get Documents ...........................
        private async Task<List<Document>> GetDocumentsAsync(Guid id)
        {
            var documents = new List<Document>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var query = "SELECT PassportPhoto, SignaturePhoto, IDCardFrontPhoto, IDCardBackPhoto FROM swiftFin_SpecimenCapture WHERE CustomerId = @CustomerId";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@CustomerId", id);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            documents.Add(new Document
                            {
                                PassportPhoto = reader.IsDBNull(0) ? null : (byte[])reader[0],
                                SignaturePhoto = reader.IsDBNull(1) ? null : (byte[])reader[1],
                                IDCardFrontPhoto = reader.IsDBNull(2) ? null : (byte[])reader[2],
                                IDCardBackPhoto = reader.IsDBNull(3) ? null : (byte[])reader[3]
                            });
                        }
                    }
                }
            }

            return documents;
        }

        public async Task<ActionResult> LoadDocumentUploadPartialAsync(Guid customerId)
        {

            if (Session["customerID"] != null)
                customerId = (Guid)Session["customerID"];

            var documents = await GetDocumentsAsync(customerId);

            if (documents == null || documents.Count == 0)
            {
                return View();
            }


            return PartialView("_DocumentUploadPartial", documents);
        }


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

            var pageCollectionInfo = await _channelService.FindDataAttachmentPeriodsInPageAsync(
                pageIndex,
                jQueryDataTablesModel.iDisplayLength,
                GetServiceHeader()
            );

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
                    items: new List<DataAttachmentPeriodDTO> { },
                    totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount,
                    sEcho: jQueryDataTablesModel.sEcho
                );
            }
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

            ViewBag.ProductCode = GetProductCodeSelectList(string.Empty);
            ViewBag.RecordStatus = GetRecordStatusSelectList(string.Empty);
            
            
            ViewBag.ProductCode2 = GetProductCodeSelectList(string.Empty);
            ViewBag.RecordStatus2 = GetRecordStatusSelectList(string.Empty);



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




        public async Task<ActionResult> GetSequenceNumber(Guid customerAccountId, int transactionType)
        {
            var sequenceNumber = await GetNextSequenceNumberAsync(customerAccountId, transactionType);

            var model = new DataAttachmentEntryDTO
            {
                SequenceNumber = sequenceNumber
            };

            return View(model);
        }

        private async Task<int> GetNextSequenceNumberAsync(Guid customerAccountId, int transactionType)
        {
            int nextSequenceNumber;

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("GetNextSequenceNumber", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add(new SqlParameter("@CustomerAccountId", SqlDbType.UniqueIdentifier)).Value = customerAccountId;
                    cmd.Parameters.Add(new SqlParameter("@TransactionType", SqlDbType.TinyInt)).Value = transactionType;

                    var outputParameter = new SqlParameter("@NextSequenceNumber", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(outputParameter);

                    await conn.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();

                    nextSequenceNumber = (int)outputParameter.Value;
                }
            }

            return nextSequenceNumber;
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


            var CustomerAccount = await _channelService.FindCustomerAccountAsync(parseId, true, true, true, true, GetServiceHeader());
            var customer = await _channelService.FindCustomerAsync(CustomerAccount.CustomerId, GetServiceHeader());
            if (customer != null)
            {
                Session["customerID"] = customer.Id;
                TempData["Customer"] = customer.FullName.ToUpper();
                //var documents = await GetDocumentsAsync(customer.Id);

                //if (documents == null || documents.Count == 0)
                //{
                //    return View();
                //}

                //dataAttachmentEntryDTO.PassportPhoto = documents[0].PassportPhoto;
                //dataAttachmentEntryDTO.SignaturePhoto = documents[0].SignaturePhoto;
                //dataAttachmentEntryDTO.IDCardFrontPhoto = documents[0].IDCardFrontPhoto;
                //dataAttachmentEntryDTO.IDCardBackPhoto = documents[0].IDCardBackPhoto;

                dataAttachmentEntryDTO.CustomerAccountCustomerId = customer.Id;
                dataAttachmentEntryDTO.CustomerAccountCustomerIndividualFirstName = customer.IndividualSalutationDescription + " " + customer.IndividualFirstName + " " +
                    customer.IndividualLastName;
                dataAttachmentEntryDTO.CustomerAccountCustomerStationZoneDivisionEmployerId = (Guid)customer.StationZoneDivisionEmployerId;
                dataAttachmentEntryDTO.CustomerAccountCustomerStationZoneDivisionEmployerDescription = customer.StationZoneDivisionEmployerDescription;
                dataAttachmentEntryDTO.CustomerAccountCustomerStationZoneDivisionStationId = (Guid)customer.StationId;
                dataAttachmentEntryDTO.CustomerAccountCustomerStationZoneDivisionStationDescription = customer.StationDescription;
                dataAttachmentEntryDTO.CustomerAccountCustomerSerialNumber = customer.SerialNumber;
                dataAttachmentEntryDTO.CustomerAccountCustomerIndividualPayrollNumbers = customer.IndividualPayrollNumbers;
                dataAttachmentEntryDTO.BranchDescription = customer.BranchDescription;
                dataAttachmentEntryDTO.CustomerRemarks = customer.Remarks;
                dataAttachmentEntryDTO.CustomerAccountCustomerPersonalIdentificationNumber = customer.PersonalIdentificationNumber;
                dataAttachmentEntryDTO.Ref1 = customer.Reference1;
                dataAttachmentEntryDTO.Ref2 = customer.Reference2;
                dataAttachmentEntryDTO.Ref3 = customer.Reference3;

                dataAttachmentEntryDTO.AddressAddressLine1 = customer.AddressAddressLine1;
                dataAttachmentEntryDTO.AddressAddressLine2 = customer.AddressAddressLine2;
                dataAttachmentEntryDTO.AddressStreet = customer.AddressStreet;
                dataAttachmentEntryDTO.AddressCity = customer.AddressCity;
                dataAttachmentEntryDTO.AddressPostalCode = customer.AddressPostalCode;
                dataAttachmentEntryDTO.AddressEmail = customer.AddressEmail;
                dataAttachmentEntryDTO.AddressLandLine = customer.AddressLandLine;
                dataAttachmentEntryDTO.AddressMobileLine = customer.AddressMobileLine;


                var CustomerAccounts = await _channelService.FindCustomerAccountsByCustomerIdAsync(customer.Id, true, true, true, true, GetServiceHeader());
                if (CustomerAccounts != null)
                    ViewBag.CustomerAccounts = CustomerAccounts;
                else
                    TempData["customerAccount"] = "The selected customer has no associated/attached accounts";

                dataAttachmentEntryDTO.CustomerDTO = customer;

                Session["customerDetails"] = dataAttachmentEntryDTO;

                foreach (var cId in CustomerAccounts)
                {
                    var idId = cId.Id;

                    Session["customerAccountId"] = idId;
                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        CustomerAccountID = dataAttachmentEntryDTO.CustomerAccountCustomerId,
                        CustomerAccount = dataAttachmentEntryDTO.CustomerAccountCustomerIndividualFirstName,
                        EmployerID = dataAttachmentEntryDTO.CustomerAccountCustomerStationZoneDivisionEmployerId,
                        Employer = dataAttachmentEntryDTO.CustomerAccountCustomerStationZoneDivisionEmployerDescription,
                        StationID = dataAttachmentEntryDTO.CustomerAccountCustomerStationZoneDivisionStationId,
                        Station = dataAttachmentEntryDTO.CustomerAccountCustomerStationZoneDivisionStationDescription,
                        SerialNumber = dataAttachmentEntryDTO.CustomerAccountCustomerSerialNumber,
                        PayrollNumber = dataAttachmentEntryDTO.CustomerAccountCustomerIndividualPayrollNumbers,
                        BranchDescription = dataAttachmentEntryDTO.BranchDescription,
                        Remarks = dataAttachmentEntryDTO.CustomerRemarks,
                        IDNumber = dataAttachmentEntryDTO.CustomerAccountCustomerPersonalIdentificationNumber,
                        Ref1 = dataAttachmentEntryDTO.Ref1,
                        Ref2 = dataAttachmentEntryDTO.Ref2,
                        Ref3 = dataAttachmentEntryDTO.Ref3,

                        AddressLine1 = dataAttachmentEntryDTO.AddressAddressLine1,
                        AddressLine2 = dataAttachmentEntryDTO.AddressAddressLine2,
                        AddressStreet = dataAttachmentEntryDTO.AddressStreet,
                        AddressCity = dataAttachmentEntryDTO.AddressCity,
                        AddressPostalCode = dataAttachmentEntryDTO.AddressPostalCode,
                        AddressEmail = dataAttachmentEntryDTO.AddressEmail,
                        AddressLandLine = dataAttachmentEntryDTO.AddressLandLine,
                        AddressMobileLine = dataAttachmentEntryDTO.AddressMobileLine,

                        CustomerAccounts = CustomerAccounts,

                        // Specimen
                        PassportPhoto = dataAttachmentEntryDTO.PassportPhoto,
                        SignaturePhoto = dataAttachmentEntryDTO.SignaturePhoto,
                        IDFront = dataAttachmentEntryDTO.IDCardFrontPhoto,
                        IDBack = dataAttachmentEntryDTO.IDCardBackPhoto,
                    }
                });
            }

            return Json(new { success = false, message = "Customer account not found" });
        }



        public async Task<ActionResult> CustomerAccountLookUp(Guid? id, DataAttachmentEntryDTO dataAttachmentEntryDTO)
        {
            await ServeNavigationMenus();

            ViewBag.TransactionTypeSelectList = GetDataAttachmentTransactionTypeTypeSelectList(string.Empty);

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            var customerAccount = await _channelService.FindCustomerAccountAsync(parseId, true, true, true, true, GetServiceHeader());
            if (customerAccount != null)
            {
                dataAttachmentEntryDTO.SelectCustomerAccountId = customerAccount.Id;
                dataAttachmentEntryDTO.SelectCustomerAccountFullAccountNumber = customerAccount.FullAccountNumber;
                dataAttachmentEntryDTO.SelectCustomerAccountStatus = customerAccount.StatusDescription;
                dataAttachmentEntryDTO.Remarks = customerAccount.Remarks;
                dataAttachmentEntryDTO.SelectCustomerAccountTypeTargetProductId = customerAccount.CustomerAccountTypeTargetProductId;
                dataAttachmentEntryDTO.SelectCustomerAccountTypeTargetProductDescription = customerAccount.CustomerAccountTypeTargetProductDescription;

                var findloanProductForLoanBalCalc = await _channelService.FindCustomerAccountsByCustomerIdAndCustomerAccountTypeTargetProductIdAsync(customerAccount.CustomerId, dataAttachmentEntryDTO.SelectCustomerAccountTypeTargetProductId,
                    true, true, true, true, GetServiceHeader());

                var bookBalance = findloanProductForLoanBalCalc.Sum(x => x.BookBalance);
                var carryForwards = findloanProductForLoanBalCalc.Sum(i => i.CarryForwardsBalance);
                var currentBalance = bookBalance + carryForwards;

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        SelectCustomerAccountId = dataAttachmentEntryDTO.SelectCustomerAccountId,
                        SelectCustomerAccountFullAccountNumber = dataAttachmentEntryDTO.SelectCustomerAccountFullAccountNumber,
                        SelectCustomerAccountStatus = dataAttachmentEntryDTO.SelectCustomerAccountStatus,
                        Remarks = dataAttachmentEntryDTO.SelectCustomerAccountRemarks,
                        SelectCustomerAccountTypeTargetProductId = dataAttachmentEntryDTO.SelectCustomerAccountTypeTargetProductId,
                        SelectCustomerAccountTypeTargetProductDescription = dataAttachmentEntryDTO.SelectCustomerAccountTypeTargetProductDescription,
                        CurrentBalace = currentBalance
                    }
                });
            }

            return Json(new { success = false, message = "Customer account not found" });
        }




        [HttpPost]
        public async Task<ActionResult> Create(DataAttachmentEntryDTO dataAttachmentEntryDTO)
        {

            dataAttachmentEntryDTO.CustomerAccountId = (Guid)Session["customerAccountId"];

            await GetSequenceNumber(dataAttachmentEntryDTO.customerAccountDTO.Id, dataAttachmentEntryDTO.TransactionType);

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
