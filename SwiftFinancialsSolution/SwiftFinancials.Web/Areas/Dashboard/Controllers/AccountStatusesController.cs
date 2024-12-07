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
using System.Web.UI;
using System.Windows.Forms;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.BackOfficeModule;
using Application.MainBoundedContext.DTO.FrontOfficeModule;
using Application.MainBoundedContext.DTO.RegistryModule;
using Infrastructure.Crosscutting.Framework.Utils;
using SwiftFinancials.Web.Areas.Registry.DocumentsModel;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.Dashboard.Controllers
{
    public class AccountStatusesController : MasterController
    {
        private readonly string _connectionString;
        public AccountStatusesController()
        {
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



        // Get LoanGuarantors ...........................
        private async Task<List<LoanCaseDTO>> GetLoanCasesAsync(Guid customerId)
        {
            var loanCases = new List<LoanCaseDTO>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var query = @"
        SELECT 
            Id
        FROM swiftFin_LoanCases
        WHERE CustomerId = @CustomerId";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@CustomerId", customerId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            loanCases.Add(new LoanCaseDTO
                            {
                                Id = reader.GetGuid(reader.GetOrdinal("Id"))
                            });
                        }
                    }
                }
            }

            return loanCases;
        }



        private async Task<List<LoanGuarantorDTO>> GetLoanGuarantorsAsync(Guid loaneeCustomerId)
        {
            var loanguarantors = new List<LoanGuarantorDTO>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var query = @"
        SELECT 
            *
        FROM swiftFin_LoanGuarantors
        WHERE LoaneeCustomerId = @LoaneeCustomerId";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@LoaneeCustomerId", loaneeCustomerId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var customerNames = await GetCustomerNamesAsync(loaneeCustomerId);

                            loanguarantors.Add(new LoanGuarantorDTO
                            {
                                Id = reader.GetGuid(reader.GetOrdinal("Id")),
                                CustomerId = reader.GetGuid(reader.GetOrdinal("CustomerId")),
                                AppraisalFactor = reader.GetDouble(reader.GetOrdinal("AppraisalFactor")),
                                TotalShares = reader.GetDecimal(reader.GetOrdinal("TotalShares")),
                                CommittedShares = reader.GetDecimal(reader.GetOrdinal("CommittedShares")),
                                AmountGuaranteed = reader.GetDecimal(reader.GetOrdinal("AmountGuaranteed")),
                                CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                            });
                        }
                    }
                }
            }

            return loanguarantors;
        }




        private async Task<CustomerDTO> GetCustomerNamesAsync(Guid customerId)
        {
            CustomerDTO customerNames = null;

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var query = @"
            SELECT 
                Individual_FirstName,
                Individual_LastName
            FROM swiftFin_Customers
            WHERE Id = @CustomerId";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@CustomerId", customerId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            customerNames = new CustomerDTO
                            {
                                IndividualFirstName = reader.GetString(reader.GetOrdinal("Individual_FirstName")),
                                IndividualLastName = reader.GetString(reader.GetOrdinal("Individual_LastName"))
                            };
                        }
                    }
                }
            }

            return customerNames;
        }




        public async Task<ActionResult> CustomerLookUp(Guid? id)
        {
            await ServeNavigationMenus();

            var dataAttachmentEntryDTO = new DataAttachmentEntryDTO();

            ViewBag.TransactionTypeSelectList = GetDataAttachmentTransactionTypeTypeSelectList(string.Empty);

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            var customer = await _channelService.FindCustomerAsync(parseId, GetServiceHeader());
            if (customer != null)
            {
                var documents = await GetDocumentsAsync(customer.Id);

                if (documents.Any())
                {
                    var document = documents.First();
                    TempData["PassportPhoto"] = document.PassportPhoto;
                    TempData["SignaturePhoto"] = document.SignaturePhoto;
                    TempData["IDCardFrontPhoto"] = document.IDCardFrontPhoto;
                    TempData["IDCardBackPhoto"] = document.IDCardBackPhoto;

                    dataAttachmentEntryDTO.PassportPhoto = document.PassportPhoto;
                    dataAttachmentEntryDTO.SignaturePhoto = document.SignaturePhoto;
                    dataAttachmentEntryDTO.IDCardFrontPhoto = document.IDCardFrontPhoto;
                    dataAttachmentEntryDTO.IDCardBackPhoto = document.IDCardBackPhoto;
                }

                dataAttachmentEntryDTO.CustomerAccountCustomerId = customer.Id;
                dataAttachmentEntryDTO.CustomerAccountCustomerIndividualFirstName = customer.IndividualSalutationDescription + " " + customer.IndividualFirstName + " " +
                    customer.IndividualLastName;
                dataAttachmentEntryDTO.CustomerAccountCustomerStationZoneDivisionEmployerId = (Guid)customer.StationZoneDivisionEmployerId;
                dataAttachmentEntryDTO.CustomerAccountCustomerStationZoneDivisionEmployerDescription = customer.StationZoneDivisionEmployerDescription;
                dataAttachmentEntryDTO.CustomerAccountCustomerStationZoneDivisionStationId = (Guid)customer.StationId;
                dataAttachmentEntryDTO.CustomerAccountCustomerStationZoneDivisionStationDescription = customer.StationDescription;
                dataAttachmentEntryDTO.CustomerAccountCustomerSerialNumber = customer.SerialNumber;
                dataAttachmentEntryDTO.CustomerAccountCustomerIndividualPayrollNumbers = customer.IndividualPayrollNumbers;
                dataAttachmentEntryDTO.MembershipPeriod = customer.MembershipPeriod;
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

                dataAttachmentEntryDTO.SalutationDescription = customer.IndividualSalutationDescription;
                dataAttachmentEntryDTO.CustomerAccountCustomerIndividualGender = customer.IndividualGenderDescription;
                dataAttachmentEntryDTO.MaritalStatusDescription = customer.IndividualMaritalStatusDescription;
                dataAttachmentEntryDTO.NationalityDescription = customer.IndividualNationalityDescription;
                dataAttachmentEntryDTO.FirstName = customer.IndividualFirstName;
                dataAttachmentEntryDTO.CustomerAccountCustomerIndividualLastName = customer.IndividualLastName;
                dataAttachmentEntryDTO.IdentityCardTypeDescription = customer.IndividualIdentityCardTypeDescription;
                dataAttachmentEntryDTO.IdentityCardNumberDescription = customer.IndividualIdentityCardNumber;

                dataAttachmentEntryDTO.BirthDate = (DateTime)customer.IndividualBirthDate;
                dataAttachmentEntryDTO.DesignationDescription = customer.IndividualEmploymentDesignation;
                dataAttachmentEntryDTO.EmploymentDate = (DateTime)customer.IndividualEmploymentDate;


                dataAttachmentEntryDTO.IndividualTypeDescription = customer.IndividualTypeDescription;
                dataAttachmentEntryDTO.PersonalIdentificationNumberDescription = customer.PersonalIdentificationNumber;
                dataAttachmentEntryDTO.Remarks = customer.Remarks;

                var employmentDate = Convert.ToDateTime(dataAttachmentEntryDTO.EmploymentDate).ToString("yyyy/MM/dd");
                var birthDate = Convert.ToDateTime(dataAttachmentEntryDTO.BirthDate).ToString("yyyy/MM/dd");

                var CustomerAccounts = await _channelService.FindCustomerAccountsByCustomerIdAsync(customer.Id, true, true, true, true, GetServiceHeader());

                var referees = await _channelService.FindRefereeCollectionByCustomerIdAsync(parseId, GetServiceHeader());
                //var managementHistory=await _channelService.

                ObservableCollection<Guid> customerAccountId = new ObservableCollection<Guid>();
                var customerAccounts = await _channelService.FindCustomerAccountsByCustomerIdAsync(parseId, true, true, true, true, GetServiceHeader());
                foreach (var accounts in customerAccounts)
                {
                    customerAccountId.Add(accounts.Id);
                }

                // Standing Orders
                List<StandingOrderDTO> allStandingOrders = new List<StandingOrderDTO>();
                foreach (var Ids in customerAccountId)
                {
                    var standingOrders = await _channelService.FindStandingOrdersByBeneficiaryCustomerAccountIdAsync(Ids, true, GetServiceHeader());
                    if (standingOrders != null && standingOrders.Any())
                    {
                        allStandingOrders.AddRange(standingOrders);
                    }
                }

                // Signatories
                List<CustomerAccountSignatoryDTO> allSignatories = new List<CustomerAccountSignatoryDTO>();
                foreach (var s in customerAccountId)
                {
                    var signatories = await _channelService.FindCustomerAccountSignatoriesByCustomerAccountIdAsync(s, GetServiceHeader());
                    if (signatories != null && signatories.Any())
                    {
                        allSignatories.AddRange(signatories);
                    }
                }

                // Alternate Channels
                List<AlternateChannelDTO> allAlternateChannels = new List<AlternateChannelDTO>();
                foreach (var a in customerAccountId)
                {
                    var alternateChannels = await _channelService.FindAlternateChannelsByCustomerAccountIdAsync(a, true, GetServiceHeader());
                    if (alternateChannels != null
                        && alternateChannels.Any())
                    {
                        allAlternateChannels.AddRange(alternateChannels);
                    }
                }


                // Uncleared Cheques
                List<ExternalChequeDTO> allExternalCheques = new List<ExternalChequeDTO>();
                foreach (var u in customerAccountId)
                {
                    var unclearedCheques = await _channelService.FindUnClearedExternalChequesByCustomerAccountIdAsync(u, GetServiceHeader());
                    if (unclearedCheques != null && unclearedCheques.Any())
                    {
                        allExternalCheques.AddRange(unclearedCheques);
                    }
                }


                // Fixed Deposits
                List<FixedDepositDTO> allfixedDeposits = new List<FixedDepositDTO>();
                foreach (var f in customerAccountId)
                {
                    var fixedDeposits = await _channelService.FindFixedDepositsByCustomerAccountIdAsync(f, true, GetServiceHeader());
                    if (fixedDeposits != null && fixedDeposits.Any())
                    {
                        allfixedDeposits.AddRange(fixedDeposits);
                    }
                }

                // Loans Guaranteed
                var loansGuaranteed = await _channelService.FindLoanGuarantorsByCustomerIdAsync(parseId, GetServiceHeader());

                // Loan Guarantors
                List<LoanGuarantorDTO> loaneeLoanGuarantors = await GetLoanGuarantorsAsync(parseId);
                for (int i = 0; i < loaneeLoanGuarantors.Count; i++)
                {
                    var customerDetails = loaneeLoanGuarantors[i];
                    var customername = await _channelService.FindCustomerAsync(customerDetails.CustomerId, GetServiceHeader());

                    loaneeLoanGuarantors[i].CustomerIndividualFirstName = customername.IndividualFirstName;
                    loaneeLoanGuarantors[i].CustomerIndividualLastName = customername.IndividualLastName;

                    loaneeLoanGuarantors[i].CreatedDate = Convert.ToDateTime(loaneeLoanGuarantors[i].CreatedDate);
                }




                var savingsBookBalance = CustomerAccounts.Where(w => w.CustomerAccountTypeProductCode == (int)ProductCode.Savings).Sum(e => e.BookBalance);
                var savingsCarryForward = CustomerAccounts.Where(w => w.CustomerAccountTypeProductCode == (int)ProductCode.Savings).Sum(e => e.AvailableBalance);
                var savingsBalance = (savingsBookBalance + savingsCarryForward);


                // Electronic Funds Transfer
                // Method to find electronic funds transfer???



                List<Tuple<decimal, int>> investmentsBalance = new List<Tuple<decimal, int>>();

                var loanAccounts = CustomerAccounts.Where(w => w.CustomerAccountTypeProductCode == (int)ProductCode.Loan);

                foreach (var Ids in loanAccounts)
                {
                    var xFactor = await _channelService.ComputeEligibleLoanAppraisalInvestmentsBalanceAsync(parseId, Ids.CustomerAccountTypeTargetProductId);

                    investmentsBalance.Add(new Tuple<decimal, int>(xFactor, 0));
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
                        MembershipPeriod = dataAttachmentEntryDTO.MembershipPeriod,
                        Remarks = dataAttachmentEntryDTO.CustomerRemarks,
                        IDNumber = dataAttachmentEntryDTO.CustomerAccountCustomerPersonalIdentificationNumber,
                        Ref1 = dataAttachmentEntryDTO.Ref1,
                        Ref2 = dataAttachmentEntryDTO.Ref2,
                        Ref3 = dataAttachmentEntryDTO.Ref3,

                        // Address
                        AddressLine1 = dataAttachmentEntryDTO.AddressAddressLine1,
                        AddressLine2 = dataAttachmentEntryDTO.AddressAddressLine2,
                        AddressStreet = dataAttachmentEntryDTO.AddressStreet,
                        AddressCity = dataAttachmentEntryDTO.AddressCity,
                        AddressPostalCode = dataAttachmentEntryDTO.AddressPostalCode,
                        AddressEmail = dataAttachmentEntryDTO.AddressEmail,
                        AddressLandLine = dataAttachmentEntryDTO.AddressLandLine,
                        AddressMobileLine = dataAttachmentEntryDTO.AddressMobileLine,

                        // Particulars
                        SalutationDescription = dataAttachmentEntryDTO.SalutationDescription,
                        CustomerAccountCustomerIndividualGender = dataAttachmentEntryDTO.CustomerAccountCustomerIndividualGender,
                        MaritalStatusDescription = dataAttachmentEntryDTO.MaritalStatusDescription,
                        NationalityDescription = dataAttachmentEntryDTO.NationalityDescription,
                        CustomerAccountCustomerIndividualFirstName = dataAttachmentEntryDTO.FirstName,
                        CustomerAccountCustomerIndividualLastName = dataAttachmentEntryDTO.CustomerAccountCustomerIndividualLastName,
                        IdentityCardTypeDescription = dataAttachmentEntryDTO.IdentityCardTypeDescription,
                        IdentityCardNumberDescription = dataAttachmentEntryDTO.IdentityCardNumberDescription,
                        BirthDate = birthDate,
                        DesignationDescription = dataAttachmentEntryDTO.DesignationDescription,
                        EmploymentDate = employmentDate,
                        IndividualTypeDescription = dataAttachmentEntryDTO.IndividualTypeDescription,
                        PersonalIdentificationNumberDescription = dataAttachmentEntryDTO.PersonalIdentificationNumberDescription,

                        // Tables Data
                        CustomerAccounts = CustomerAccounts,
                        Referees = referees,
                        StandingOrders = allStandingOrders,
                        Signatories = allSignatories,
                        AlternateChannels = allAlternateChannels,
                        UnclearedCheques = allExternalCheques,
                        FixedDeposits = allfixedDeposits,
                        LoanGuarantors = loaneeLoanGuarantors,
                        LoansGuaranteed = loansGuaranteed,

                        PassportPhoto = dataAttachmentEntryDTO.PassportPhoto != null ? Convert.ToBase64String(dataAttachmentEntryDTO.PassportPhoto) : null,
                        SignaturePhoto = dataAttachmentEntryDTO.SignaturePhoto != null ? Convert.ToBase64String(dataAttachmentEntryDTO.SignaturePhoto) : null,
                        IDFront = dataAttachmentEntryDTO.IDCardFrontPhoto != null ? Convert.ToBase64String(dataAttachmentEntryDTO.IDCardFrontPhoto) : null,
                        IDBack = dataAttachmentEntryDTO.IDCardBackPhoto != null ? Convert.ToBase64String(dataAttachmentEntryDTO.IDCardBackPhoto) : null
                    }
                });
            }

            return Json(new { success = false, message = "Customer not found" });
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




        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();

            ViewBag.StatementTypeSelectList = GetCustomerAccountStatementTypeSelectList(string.Empty);

            ViewBag.ProductCode = GetProductCodeSelectList(string.Empty);
            ViewBag.RecordStatus = GetRecordStatusSelectList(string.Empty);

            ViewBag.recordStatus = GetRecordStatusSelectList(string.Empty);
            ViewBag.customerFilter = GetCustomerFilterSelectList(string.Empty);

            ViewBag.ProductCode2 = GetProductCodeSelectList(string.Empty);
            ViewBag.RecordStatus2 = GetRecordStatusSelectList(string.Empty);

            return View();
        }
    }
}