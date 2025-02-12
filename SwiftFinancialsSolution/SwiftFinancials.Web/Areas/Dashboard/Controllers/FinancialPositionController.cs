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
using System.Windows.Forms;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.BackOfficeModule;
using Application.MainBoundedContext.DTO.RegistryModule;
using Infrastructure.Crosscutting.Framework.Utils;
using SwiftFinancials.Web.Areas.Registry.DocumentsModel;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.Dashboard.Controllers
{
    public class FinancialPositionController : MasterController
    {

        private readonly string _connectionString;
        public FinancialPositionController()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;
        }


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


                var CustomerAccounts = await _channelService.FindCustomerAccountsByCustomerIdAsync(customer.Id, true, true, true, true, GetServiceHeader());

                var products = await _channelService.FindCustomerAccountsByCustomerIdAndProductCodesAsync(customer.Id, new[] { (int)ProductCode.Savings, (int)ProductCode.Loan, (int)ProductCode.Investment },
                  true, true, true, true, GetServiceHeader());
                var investmentProducts = products.Where(p => p.CustomerAccountTypeProductCode == (int)ProductCode.Investment).ToList();
                var savingsProducts = products.Where(p => p.CustomerAccountTypeProductCode == (int)ProductCode.Savings).ToList();
                var loanProducts = products.Where(p => p.CustomerAccountTypeProductCode == (int)ProductCode.Loan).ToList();

                // Investments
                List<decimal> iBalance = new List<decimal>();
                foreach (var investmentsBalances in investmentProducts)
                {
                    iBalance.Add(investmentsBalances.BookBalance);
                }
                var investmentsBalance = iBalance.Sum();

                //Savings
                List<decimal> sBalance = new List<decimal>();
                foreach (var savingsBalances in savingsProducts)
                {
                    sBalance.Add(savingsBalances.BookBalance);
                }
                var savingsBalance = sBalance.Sum();
                var totalShares = iBalance.Sum() + sBalance.Sum();

                //Loans
                List<decimal> lBookBalance = new List<decimal>();
                List<decimal> lCarryForwardBalance = new List<decimal>();
                foreach (var loanBalances in loanProducts)
                {
                    lBookBalance.Add(loanBalances.BookBalance);
                    lCarryForwardBalance.Add(loanBalances.CarryForwardsBalance);
                }
                var loanBalance = lBookBalance.Sum() + lCarryForwardBalance.Sum();

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

                        AddressLine1 = dataAttachmentEntryDTO.AddressAddressLine1,
                        AddressLine2 = dataAttachmentEntryDTO.AddressAddressLine2,
                        AddressStreet = dataAttachmentEntryDTO.AddressStreet,
                        AddressCity = dataAttachmentEntryDTO.AddressCity,
                        AddressPostalCode = dataAttachmentEntryDTO.AddressPostalCode,
                        AddressEmail = dataAttachmentEntryDTO.AddressEmail,
                        AddressLandLine = dataAttachmentEntryDTO.AddressLandLine,
                        AddressMobileLine = dataAttachmentEntryDTO.AddressMobileLine,

                        CustomerAccounts = CustomerAccounts,

                        SavingsBalance = savingsBalance,
                        InvestmentBalance = investmentsBalance,
                        LoanBalance = loanBalance,
                        Networth = totalShares,

                        PassportPhoto = dataAttachmentEntryDTO.PassportPhoto != null ? Convert.ToBase64String(dataAttachmentEntryDTO.PassportPhoto) : null,
                        SignaturePhoto = dataAttachmentEntryDTO.SignaturePhoto != null ? Convert.ToBase64String(dataAttachmentEntryDTO.SignaturePhoto) : null,
                        IDFront = dataAttachmentEntryDTO.IDCardFrontPhoto != null ? Convert.ToBase64String(dataAttachmentEntryDTO.IDCardFrontPhoto) : null,
                        IDBack = dataAttachmentEntryDTO.IDCardBackPhoto != null ? Convert.ToBase64String(dataAttachmentEntryDTO.IDCardBackPhoto) : null
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
                dataAttachmentEntryDTO.CustomerAccountId = customerAccount.Id;
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

                var statement = await _channelService.FindElectronicStatementOrdersByCustomerAccountIdAsync(parseId, true, GetServiceHeader());

                var activeFixedDeposits = await _channelService.FindFixedDepositsByCustomerAccountIdAsync(parseId, true, GetServiceHeader());

                var unclearedCheques = await _channelService.FindUnClearedExternalChequesByCustomerAccountIdAsync(parseId, GetServiceHeader());

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        SelectCustomerAccountId = dataAttachmentEntryDTO.CustomerAccountId,
                        SelectCustomerAccountFullAccountNumber = dataAttachmentEntryDTO.SelectCustomerAccountFullAccountNumber,
                        SelectCustomerAccountStatus = dataAttachmentEntryDTO.SelectCustomerAccountStatus,
                        Remarks = dataAttachmentEntryDTO.SelectCustomerAccountRemarks,
                        SelectCustomerAccountTypeTargetProductId = dataAttachmentEntryDTO.SelectCustomerAccountTypeTargetProductId,
                        SelectCustomerAccountTypeTargetProductDescription = dataAttachmentEntryDTO.SelectCustomerAccountTypeTargetProductDescription,
                        CurrentBalace = currentBalance,

                        Statement = statement,
                        UnclearedCheques = unclearedCheques,
                        ActiveFixedDeposits = activeFixedDeposits
                    }
                });
            }

            return Json(new { success = false, message = "Customer account not found" });
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