//using System.Web.Http.Cors;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.AdministrationModule;
using Application.MainBoundedContext.DTO.FrontOfficeModule;
using Application.MainBoundedContext.DTO.HumanResourcesModule;
using Application.MainBoundedContext.DTO.MessagingModule;
using Application.MainBoundedContext.DTO.RegistryModule;
using DistributedServices.MainBoundedContext;
using Infrastructure.Crosscutting.Framework.Configuration;
using Infrastructure.Crosscutting.Framework.Extensions;
using Infrastructure.Crosscutting.Framework.Utils;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.Ajax.Utilities;
using Newtonsoft.Json;

//using Microsoft.AspNetCore.Cors;
using SwiftFinancials.Presentation.Infrastructure.Models;
using SwiftFinancials.Presentation.Infrastructure.Services;
using SwiftFinancials.Presentation.Infrastructure.Util;
using SwiftFinancials.TextAlertDispatcher.Celcom.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.Entity.Validation;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;
using TestApis.Models;
using TestApis.Services;
using static SwiftFinancials.Presentation.Infrastructure.Models.CustomerTransactionModel;

namespace TestApis.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    [AllowAnonymous]
    [RoutePrefix("api/values")]
    public class ValuesController : ApiController
    {
        private readonly MasterController master;
        private readonly string _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;

        EmployeeDTO _selectedEmployee;

        CustomerAccountDTO _selectedCustomerAccount;

        BranchDTO _selectedBranch;

        TellerDTO _selectedTeller;

        PaymentVoucherDTO _selectedPaymentVoucher;

        CustomerDTO _selectedCustomer;

        PostingPeriodDTO _currentPostingPeriod;

        //private readonly string _connectionString;




        private string receiptContent;
        decimal PreviousTellerBalance;
        decimal NewTellerBalance;
        private PageCollectionInfo<GeneralLedgerTransaction> TellerStatements;



        private bool IsBusy { get; set; } // Property to indicate if an operation is in progress

        //public CashDepositController()
        //{
        //    // Get connection string from Web.config
        //    _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;
        //}

        public class OperationResult
        {
            public bool Success { get; set; }

            public bool Dialog { get; set; }
            public string Message { get; set; }

            public CustomerTransactionModel TransactionData { get; set; }

            public JournalDTO TransactionJournal { get; set; }
        }

        public PostingPeriodDTO CurrentPostingPeriod
        {

            get { return _currentPostingPeriod; }

            set
            {
                if (_currentPostingPeriod != value)
                {
                    _currentPostingPeriod = value;
                }
            }
        }

        public CustomerDTO SelectedCustomer

        {

            get { return _selectedCustomer; }

            set
            {
                if (_selectedCustomer != value)
                {

                    _selectedCustomer = value;
                }
            }
        }



        public PaymentVoucherDTO SelectedPaymentVoucher
        {
            get { return _selectedPaymentVoucher; }

            set
            {
                if (_selectedPaymentVoucher != value)
                {

                    _selectedPaymentVoucher = value;
                }
            }

        }

        public EmployeeDTO SelectedEmployee
        {
            get { return _selectedEmployee; }
            set
            {
                if (_selectedEmployee != value)
                {
                    _selectedEmployee = value;

                }
            }
        }


        public CustomerAccountDTO SelectedCustomerAccount
        {
            get { return _selectedCustomerAccount; }
            set
            {
                if (_selectedCustomerAccount != value)
                {
                    _selectedCustomerAccount = value;

                }
            }
        }

        public BranchDTO SelectedBranch
        {
            get { return _selectedBranch; }
            set
            {
                if (_selectedBranch != value)
                {
                    _selectedBranch = value;

                }
            }
        }

        public TellerDTO SelectedTeller
        {
            get { return _selectedTeller; }
            set
            {
                if (_selectedTeller != value)
                {
                    _selectedTeller = value;

                }
            }
        }


        public ValuesController()
        {
            master = new MasterController();
        }


        [HttpGet]
        [Route("GetCustomerJournal/{mobileNumber}")]
        public async Task<IHttpActionResult> GetCustomerJournal(string mobileNumber)
        {
            // Keep only digits
            mobileNumber = new string(mobileNumber.Where(char.IsDigit).ToArray());

            // Convert local format to international (+254...)
            if (mobileNumber.StartsWith("0"))
            {
                mobileNumber = "+254" + mobileNumber.Substring(1);
            }

            CustomerJournalDTO result = null;

            string query = @"
        SELECT 
            c.Individual_FirstName + ' ' + c.Individual_LastName AS FullName,
            c.Address_MobileLine,
            c.Address_Email,
            ca.Status,
            SUM(je.Amount) AS TotalAmount
        FROM swiftfin_Customers c
        INNER JOIN swiftfin_CustomerAccounts ca
            ON ca.CustomerId = c.Id
        INNER JOIN swiftFin_JournalEntries je
            ON je.CustomerAccountId = ca.Id
        WHERE c.Address_MobileLine = @MobileNumber
        GROUP BY 
            c.Individual_FirstName, 
            c.Individual_LastName, 
            c.Address_MobileLine,
            c.Address_Email,
            ca.Status;";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.Add("@MobileNumber", SqlDbType.VarChar).Value = mobileNumber;

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        result = new CustomerJournalDTO
                        {
                            FullName = reader["FullName"].ToString(),
                            Address_MobileLine = reader["Address_MobileLine"].ToString(),
                            Address_Email = reader["Address_Email"].ToString(),
                            Status = reader["Status"].ToString(),
                            Amount = reader.GetDecimal(reader.GetOrdinal("TotalAmount"))
                        };
                    }
                }
            }

            // Return wrapped JSON response
            return Json(new ApiResponse<object>
            {
                Success = result != null,
                Message = result != null
                    ? "Customer journal retrieved successfully."
                    : "No records found for the given mobile number.",
                Data = result
            });
        }



        [HttpGet]
        [Route("GetChartOfAccount")]
        public async Task<IHttpActionResult> Get()
        {
            try
            {
                var serviceHeader = master.GetServiceHeader();
                var chartOfAccountDTOs = await master._channelService.FindChartOfAccountsAsync(serviceHeader);

                return Json(new ApiResponse<object>
                {
                    Success = true,
                    Message = chartOfAccountDTOs?.Count > 0 ? $"{chartOfAccountDTOs.Count} chart Of AccountDTOs retrieved." : "No chartOfAccountDTOs  found.",
                    Data = chartOfAccountDTOs
                });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while retrieving chartOfAccountDTOs.",
                    Data = ex.Message
                });
            }
        }


        [HttpGet]
        [Route("getBanks")]
        public async Task<IHttpActionResult> getBanks()
        {
            try
            {
                var effectivePageSize = 20;
                var effectivePageIndex = 0;
                var serviceHeader = master.GetServiceHeader();
                var pageCollectionInfo = await master._channelService.FindBanksInPageAsync((int)effectivePageSize, (int)effectivePageIndex, serviceHeader);
                var sortedData = pageCollectionInfo.PageCollection.OrderByDescending(x => x.Id).ToList();

                return Json(new ApiResponse<object>
                {
                    Success = true,
                    Message = sortedData?.Count > 0 ? $"{sortedData.Count} Transactions Found." : "No Transaction  found.",
                    Data = sortedData
                });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while retrieving the transactions.",
                    Data = ex.Message
                });
            }
        }


        [HttpGet]
        [Route("GetSystemMapItems")]
        public async Task<IHttpActionResult> GetSystemMapItems()
        {

            try
            {

                var serviceHeader = master.GetServiceHeader();

                var mapItems = master.GetSystemGeneralLedgerAccountCodeSelectList("Account Payables");

                return Json(mapItems);
            }

            catch (Exception ex)
            {

                return Json(ex.Message);


            }


        }


        [HttpGet]
        [Route("GetPaymentAccountTypeSelectList")]
        public async Task<IHttpActionResult> GetPaymentAccountTypes()
        {

            try
            {

                var serviceHeader = master.GetServiceHeader();

                var accountTypes = master.GetPaymentAccountTypeSelectList("Vendor");

                return Json(accountTypes);
            }

            catch (Exception ex)
            {

                return Json(ex.Message);


            }


        }



        [HttpGet]
        [Route("GetPaymentDocumentTypeSelectList")]
        public async Task<IHttpActionResult> GetPaymentDocumentTypes()
        {

            try
            {

                var serviceHeader = master.GetServiceHeader();

                var accountTypes = master.GetPaymentDocumentTypeSelectList("Invoice");

                return Json(accountTypes);
            }

            catch (Exception ex)
            {

                return Json(ex.Message);


            }


        }





        [HttpGet]
        [Route("GetPurchaseInvoiceEntryTypes")]
        public async Task<IHttpActionResult> GetPurchaseInvoiceTypes()
        {

            try
            {

                var serviceHeader = master.GetServiceHeader();

                var items = master.GetPurchaseInvoiceEntryTypeSelectList("G/L Account");
                return Json(items);
            }

            catch (Exception ex)
            {

                return Json(ex.Message);


            }


        }



        [HttpGet]
        [Route("getSystemMappings")]
        public async Task<IHttpActionResult> getSystemMappings()
        {

            try
            {

                var serviceHeader = master.GetServiceHeader();

                var mappings = await master._channelService.FindSystemGeneralLedgerAccountMappingsAsync(serviceHeader);

                var coas = await master._channelService.FindChartOfAccountsAsync(serviceHeader);

                foreach (SystemGeneralLedgerAccountMappingDTO mapping in mappings)
                {
                    var coa = coas.FirstOrDefault(c => c.Id == mapping.ChartOfAccountId);

                    if (coa != null)
                    {
                        mapping.ChartOfAccountAccountName = coa.AccountName;
                    }
                    // else: do nothing if not found
                }



                return Json(new ApiResponse<object>
                {
                    Success = true,
                    Message = mappings?.Count > 0 ? $"{mappings.Count} Mappings Found." : "No Mapping  found.",
                    Data = mappings?.ToList()
                });

            }

            catch (Exception ex)
            {


                return Json(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Error: {ex.Message}",
                    Data = null
                });
            }
        }


        [HttpPost]
        [Route("addSystemMapping")]
        public async Task<IHttpActionResult> addSystemMapping([FromBody] SystemGeneralLedgerAccountMappingDTO systemGeneralLedgerAccountMappingDTO)
        {

            try
            {

                var serviceHeader = master.GetServiceHeader();


                systemGeneralLedgerAccountMappingDTO.ValidateAll();

                var result = master._channelService.AddSystemGeneralLedgerAccountMappingAsync(systemGeneralLedgerAccountMappingDTO, serviceHeader);


                return Json(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Mapping created successfully.",
                    Data = result
                });

            }

            catch (Exception ex)
            {



                return Json(new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while retrieving the transactions.",
                    Data = ex.Message
                });
            }
        }


        [HttpPut]
        [Route("UpdateSystemMapping")]
        public async Task<IHttpActionResult> UpdateSystemMapping([FromBody] SystemGeneralLedgerAccountMappingDTO systemGeneralLedgerAccountMappingDTO)
        {
            try
            {
                var serviceHeader = master.GetServiceHeader();

                systemGeneralLedgerAccountMappingDTO.ValidateAll();

                if (systemGeneralLedgerAccountMappingDTO.ErrorMessages.Count > 0)
                {

                    return Json(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "failed, bad request"
                    });
                }

                else
                {


                    var result = await master._channelService.UpdateSystemGeneralLedgerAccountMappingAsync(
                        systemGeneralLedgerAccountMappingDTO,
                        serviceHeader);


                    if (result)
                    {

                        return Json(new ApiResponse<object>
                        {
                            Success = true,
                            Message = "Success updating"

                        });
                    }


                    else
                    {

                        return Json(new ApiResponse<object>
                        {
                            Success = true,
                            Message = "Failed in updating"
                        });
                    }
                }

            }

            catch (Exception ex)
            {

                return Json(new ApiResponse<object>
                {

                    Success = false,
                    Message = "An error occurred"
                });
            }
        }




        [HttpGet]
        [Route("getBankWithLinkages")]
        public async Task<IHttpActionResult> getBankWithLinkages()
        {
            try
            {
                var serviceHeader = master.GetServiceHeader();
                var bankLinkageDTOs = await master._channelService.FindBankLinkagesAsync(serviceHeader);


                // Get all general ledger accounts
                var generalLedgerAccounts = await master._channelService.FindGeneralLedgerAccountsAsync(true, true, serviceHeader);

                var balanceDict = generalLedgerAccounts
    .Where(x => x.Id != Guid.Empty)
    .GroupBy(x => x.Id)
    .ToDictionary(g => g.Key, g => g.First().Balance);

                foreach (var linkageDTO in bankLinkageDTOs)
                {


                    var relatedBank = await master._channelService.FindBankAsync(linkageDTO.BankId, serviceHeader);

                    if (relatedBank != null && relatedBank.Id != Guid.Empty)
                    {

                        linkageDTO.SwiftCode = relatedBank.SwiftCode;
                        linkageDTO.Address = relatedBank.Address;
                        linkageDTO.City = relatedBank.City;
                        linkageDTO.IbanNo = relatedBank.IbanNo;
                        linkageDTO.No = relatedBank.No;

                    }



                    if (linkageDTO.ChartOfAccountId != Guid.Empty && balanceDict.ContainsKey(linkageDTO.ChartOfAccountId))
                    {
                        linkageDTO.BankLinkageBalance = balanceDict[linkageDTO.ChartOfAccountId];
                    }
                    else
                    {
                        linkageDTO.BankLinkageBalance = 0m;
                    }
                }

                return Json(new ApiResponse<object>
                {
                    Success = true,
                    Message = bankLinkageDTOs?.Count > 0 ? $"{bankLinkageDTOs.Count} Transactions Found." : "No Transaction  found.",
                    Data = bankLinkageDTOs
                });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while retrieving the transactions.",
                    Data = ex.Message
                });
            }
        }


        [HttpPost]
        [Route("AddBankWithLinkages")]
        public async Task<IHttpActionResult> AddBankWithLinkages([FromBody] BankDTO bankDTO)
        {
            var serviceHeader = master.GetServiceHeader();

            if (bankDTO == null || bankDTO == null)
                return Json(new ApiResponse<object> { Success = false, Message = "Invalid data.", Data = null });


            bankDTO.ValidateAll();
            if (bankDTO.HasErrors)
            {
                return Json(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Bank validation failed.",
                    Data = bankDTO.ErrorMessages
                });
            }

            var result = await master._channelService.AddBankAsync(bankDTO, serviceHeader);
            if (result.ErrorMessageResult != null)
            {

                return Json(new ApiResponse<object>
                {
                    Success = false,
                    Message = result.ErrorMessageResult,
                    Data = null
                });
            }


            await master._channelService.UpdateBankBranchesByBankIdAsync(result.Id, bankDTO.BankBranchesDTO, serviceHeader);


            var bankLinkageDTO = new BankLinkageDTO
            {
                BankAccountNumber = bankDTO.BankAccountNumber,
                BankId = result.Id,
                BankBranchName = bankDTO.BankBranchName,
                BankName = bankDTO.BankName,
                BranchDescription = bankDTO.BranchDescription,
                BranchId = bankDTO.BranchId,
                ChartOfAccountAccountCode = bankDTO.ChartOfAccountAccountCode,
                ChartOfAccountAccountName = bankDTO.ChartOfAccountAccountName,
                ChartOfAccountId = bankDTO.ChartOfAccountId,
                ChartOfAccountAccountType = bankDTO.ChartOfAccountAccountType,
                ChartOfAccountCostCenterId = bankDTO.ChartOfAccountCostCenterId,
                ChartOfAccountCostCenterDescription = bankDTO.ChartOfAccountCostCenterDescription
            };


            var linkageResult = await master._channelService.AddBankLinkageAsync(bankLinkageDTO, serviceHeader);

            if (linkageResult.ErrorMessages.Count != 0)
            {
                return Json(new ApiResponse<object>
                {
                    Success = false,
                    Message = linkageResult.ErrorMessages.ToString(),
                    Data = null
                });
            }


            return Json(new ApiResponse<object>
            {
                Success = true,
                Message = "Bank and linkages created successfully.",
                Data = bankDTO
            });
        }



        [HttpGet]
        [Route("GetGeneralLeadgersBalances")]
        public async Task<IHttpActionResult> GetGeneralLeadgers()
        {
            try
            {
                var serviceHeader = master.GetServiceHeader();
                var chartOfAccountDTOs =
                    await master._channelService.FindGeneralLedgerAccountsAsync(true, true, serviceHeader);

                if (chartOfAccountDTOs != null)
                {
                    foreach (var item in chartOfAccountDTOs)
                    {
                        item.Balance = Math.Abs(item.Balance);
                    }
                }

                return Json(new ApiResponse<object>
                {
                    Success = true,
                    Message = chartOfAccountDTOs?.Count > 0
                        ? $"{chartOfAccountDTOs.Count} Transactions Found."
                        : "No Transaction found.",
                    Data = chartOfAccountDTOs
                });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while retrieving the transactions.",
                    Data = ex.Message
                });
            }
        }


        [HttpGet]
        [Route("GetGeneralLedgers")]
        public async Task<IHttpActionResult> GetGeneralLedgers(
       [FromUri] string text = null,
       int? accountCategory = null,
       bool updateDepth = false)
        {
            try
            {
                var serviceHeader = master.GetServiceHeader();

                var gls =
                    await master._channelService.FindGeneralLedgerAccountsWithCategoryAndTextAsync(
                        accountCategory,
                        text,
                        updateDepth,
                        serviceHeader
                    );

                // :white_check_mark: Ensure Balance is always positive
                if (gls != null)
                {
                    foreach (var account in gls)
                    {
                        account.Balance = Math.Abs(account.Balance);
                    }
                }

                return Json(new ApiResponse<object>
                {
                    Success = true,
                    Message = gls != null && gls.Count > 0
                        ? $"{gls.Count} Accounts Found."
                        : "No accounts found.",
                    Data = gls
                });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while retrieving the accounts.",
                    Data = ex.Message
                });
            }
        }



        [HttpGet]
        [Route("GetGeneralLeadgers")]
        public async Task<IHttpActionResult> GetGeneralLeadgers(int? pagesize, int? pageindex)
        {
            try
            {
                var serviceHeader = master.GetServiceHeader();
                var pageCollectionInfo = await master._channelService.FindGeneralLedgersInPageAsync((int)pagesize, (int)pageindex, serviceHeader);
                var sortedData = pageCollectionInfo.PageCollection.OrderByDescending(x => x.Id).ToList();

                return Json(new ApiResponse<object>
                {
                    Success = true,
                    Message = sortedData?.Count > 0 ? $"{sortedData.Count} Transactions Found." : "No Transaction  found.",
                    Data = sortedData
                });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while retrieving the transactions.",
                    Data = ex.Message
                });
            }
        }

        [HttpGet]
        [Route("test")]
        public IHttpActionResult Test()
        {
            return Json(new ApiResponse<object>
            {
                Success = true,
                Message = "Test GET endpoint is working!",
                Data = DateTime.UtcNow
            });
        }

        [HttpPost]
        [Route("chartofaccount")]
        public async Task<IHttpActionResult> CreateChartOfAccount([FromBody] ChartOfAccountDTO chartOfAccountDTO)
        {
            var serviceHeader = master.GetServiceHeader();

            if (chartOfAccountDTO == null)
                return Json(new ApiResponse<object> { Success = false, Message = "Invalid data.", Data = null });

            chartOfAccountDTO.CostCenterId = Guid.Parse("A66AE3A0-AE25-F011-8982-28C63F4EECBE");


            chartOfAccountDTO.ValidateAll();

            if (!chartOfAccountDTO.HasErrors)
            {
                var result = await master._channelService.AddChartOfAccountAsync(chartOfAccountDTO, serviceHeader);

                if (result.ErrorMessageResult != null)
                {
                    return Json(new ApiResponse<object>
                    {
                        Success = false,
                        Message = result.ErrorMessageResult,
                        Data = null
                    });
                }

                return Json(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Chart of account created successfully.",
                    Data = chartOfAccountDTO
                });
            }

            return Json(new ApiResponse<object>
            {
                Success = false,
                Message = "Validation failed.",
                Data = chartOfAccountDTO.ErrorMessages
            });
        }

        [HttpGet]
        [Route("customers")]
        public async Task<IHttpActionResult> GetAllCustomers()
        {
            var serviceHeader = master.GetServiceHeader();
            var customers = await master._channelService.FindCustomersAsync(serviceHeader);

            return Json(new ApiResponse<object>
            {
                Success = true,
                Message = customers?.Count > 0 ? $"{customers.Count} customers found." : "No customers found.",
                Data = customers
            });
        }

        [HttpGet]
        [Route("invoicelines")]
        public async Task<IHttpActionResult> GetAllInvoiceLines()
        {
            var serviceHeader = master.GetServiceHeader();
            //var customers = await master._channelService.FindCustomersAsync(serviceHeader);

            var invoices = await master._channelService.FindPurchaseInvoiceLinesAsync(serviceHeader);

            //var invoices = await master._channelService.

            return Json(new ApiResponse<object>
            {
                Success = true,
                Message = invoices?.Count > 0 ? $"{invoices.Count} invoice lines found." : "No invoices found.",
                Data = invoices
            });
        }

        [HttpGet]
        [Route("accounts")]
        public async Task<IHttpActionResult> GetAccounts([FromUri] int pageIndex, [FromUri] int pageSize, [FromUri] int? journalEntryFilter, [FromUri] DateTime? startDate, [FromUri] DateTime? endDate, [FromUri] string text = "")
        {
            try
            {
                var serviceHeader = master.GetServiceHeader();
                var accounts = await master._channelService.FindGeneralLedgerTransactionsByDateRangeAndFilterInPageAsync(
                    pageIndex, pageSize, (DateTime)startDate, (DateTime)endDate, text, (int)journalEntryFilter, serviceHeader);

                var sortedData = accounts.PageCollection.OrderByDescending(x => x.JournalCreatedDate).ToList();

                return Json(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Transactions fetched successfully.",
                    Data = sortedData
                });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Error fetching transactions.",
                    Data = ex.Message
                });
            }
        }


        //Posting Periods
        [HttpGet]
        [Route("GetPostingperiods")]
        public async Task<IHttpActionResult> GetPostingperiods()
        {
            var serviceHeader = master.GetServiceHeader();
            var reports = await master._channelService.FindPostingPeriodsAsync(serviceHeader);

            return Json(new ApiResponse<object>
            {
                Success = true,
                Message = reports?.Count > 0 ? $"{reports.Count} POsting Periods found." : "No Posting periods found.",
                Data = reports
            });
        }



        [Route("AddPostingPeriod")]
        public async Task<IHttpActionResult> addPostingPeriod([FromBody] PostingPeriodDTO postingPeriodDTO)
        {
            var serviceHeader = master.GetServiceHeader();
            var result = await master._channelService.AddPostingPeriodAsync(postingPeriodDTO, serviceHeader);

            return Json(new ApiResponse<object>
            {
                Success = true,
                Message = "Posting period added successfully.",
                Data = result
            });
        }


        [HttpPut]
        [Route("UpdatePostingPeriod")]
        public async Task<IHttpActionResult> UpdatePostingPeriod([FromBody] PostingPeriodDTO postingPeriodDTO)
        {
            var serviceHeader = master.GetServiceHeader();
            var result = await master._channelService.UpdatePostingPeriodAsync(postingPeriodDTO, serviceHeader);

            return Json(new ApiResponse<object>
            {
                Success = true,
                Message = "Posting period updated successfully.",
                Data = result
            });
        }



        [HttpPost]
        [Route("ClosePostingPeriod")]
        public async Task<IHttpActionResult> ClosePostingPeriod([FromBody] PostingPeriodDTO postingPeriodDTO)
        {

            int moduleNavigationItemCode = 1; // Assuming 1 represents the Accounts module

            var serviceHeader = master.GetServiceHeader();

            var result = await master._channelService.ClosePostingPeriodAsync(postingPeriodDTO, moduleNavigationItemCode, serviceHeader);


            if (result == true)
            {
                return Json(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Successfully closed posting period.",
                    Data = null
                });
            }

            else
            {

                return Json(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Error closing posting period.",
                    Data = null
                });
            }

        }

        [HttpGet]
        [Route("branches")]
        public async Task<IHttpActionResult> GetBranches()
        {
            var serviceHeader = master.GetServiceHeader();
            var branches = await master._channelService.FindBranchesAsync(serviceHeader);

            return Json(new ApiResponse<object>
            {
                Success = true,
                Message = branches?.Count > 0 ? $"{branches.Count} branches found." : "No branches found.",
                Data = branches
            });
        }

        [HttpGet]
        [Route("roles")]
        public async Task<IHttpActionResult> GetRoles()
        {
            var serviceHeader = master.GetServiceHeader();
            var roles = await master._channelService.GetAllRolesAsync(serviceHeader);

            return Json(new ApiResponse<object>
            {
                Success = true,
                Message = roles?.Count > 0 ? $"{roles.Count} roles found." : "No roles found.",
                Data = roles
            });
        }

        [HttpGet]
        [Route("products")]
        public async Task<IHttpActionResult> GetProducts()
        {
            var serviceHeader = master.GetServiceHeader();
            var products = await master._channelService.FindInvestmentProductsAsync(serviceHeader);

            return Json(new ApiResponse<object>
            {
                Success = true,
                Message = products?.Count > 0 ? $"{products.Count} products found." : "No products found.",
                Data = products
            });
        }

        [HttpGet]
        [Route("costcenters")]
        public async Task<IHttpActionResult> GetCostCenters()
        {
            var serviceHeader = master.GetServiceHeader();
            var centers = await master._channelService.FindCostCentersAsync(serviceHeader);

            return Json(new ApiResponse<object>
            {
                Success = true,
                Message = centers?.Count > 0 ? $"{centers.Count} cost centers found." : "No cost centers found.",
                Data = centers
            });
        }

        [HttpPost]
        [Route("add-customer")]
        public async Task<IHttpActionResult> AddCustomer([FromBody] CustomerBindingModel customerBindingModel)
        {
            var serviceHeader = master.GetServiceHeader();

            // Mandatory collections
            var mandatoryInvestmentProducts = new List<InvestmentProductDTO>();
            var mandatorySavingsProducts = new List<SavingsProductDTO>();
            var mandatoryDebitTypes = new ObservableCollection<DebitTypeDTO>();
            var mandatoryProducts = new ProductCollectionInfo();

            //  Get all savings products and find mandatory one
            var savingsDTO = await master._channelService.FindSavingsProductsAsync(serviceHeader);

            string mandatoryDescription = "M-WALLETACCOUNT";
            var savingsProductDTO = savingsDTO.FirstOrDefault(s => string.Equals(s.Description, mandatoryDescription, StringComparison.OrdinalIgnoreCase));

            if (savingsProductDTO == null)
            {
                return Json(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Mandatory savings product '{mandatoryDescription}' not found.",
                    Data = null
                });
            }

            mandatorySavingsProducts.Add(savingsProductDTO);
            mandatoryProducts.SavingsProductCollection = mandatorySavingsProducts;

            var investmentDTO = await master._channelService.FindInvestmentProductsAsync(serviceHeader);
            string investmentDescription = "ENTRANCEFEE";

            var invest = investmentDTO.FirstOrDefault(s => string.Equals(s.Description, investmentDescription, StringComparison.OrdinalIgnoreCase));
            if (investmentDTO == null)
            {
                return Json(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Mandatory investment product not found.",
                    Data = null
                });
            }

            mandatoryInvestmentProducts.Add(invest);
            mandatoryProducts.InvestmentProductCollection = mandatoryInvestmentProducts;

            var debitTypeDTO = await master._channelService.FindDebitTypesAsync(serviceHeader);
            string debitTypeDTODescription = "Entrance Fees";

            var debitType = debitTypeDTO.FirstOrDefault(s => string.Equals(s.Description, debitTypeDTODescription, StringComparison.OrdinalIgnoreCase));
            if (debitTypeDTO == null)
            {
                return Json(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Mandatory debit type not found.",
                    Data = null
                });
            }

            mandatoryDebitTypes.Add(debitType);

            var customerDTO = customerBindingModel.MapTo<CustomerDTO>();

            var result = await master._channelService.AddCustomerAsync(
                customerDTO,
                mandatoryDebitTypes.ToList(),
                mandatoryInvestmentProducts,
                mandatorySavingsProducts,
                mandatoryProducts,
                1,
                serviceHeader
            );

            return Json(new ApiResponse<object>
            {
                Success = true,
                Message = "Customer added successfully.",
                Data = result
            });
        }


        [HttpPost]
        [Route("add-product")]
        public async Task<IHttpActionResult> AddProduct([FromBody] InvestmentProductDTO product)
        {
            var serviceHeader = master.GetServiceHeader();
            var result = await master._channelService.AddInvestmentProductAsync(product, serviceHeader);

            return Json(new ApiResponse<object>
            {
                Success = true,
                Message = "Product added successfully.",
                Data = result
            });
        }

        [HttpPost]
        [Route("add-branch")]
        public async Task<IHttpActionResult> AddBranch([FromBody] BranchDTO branch)
        {
            var serviceHeader = master.GetServiceHeader();
            var result = await master._channelService.AddBranchAsync(branch, serviceHeader);

            return Json(new ApiResponse<object>
            {
                Success = true,
                Message = "Branch added successfully.",
                Data = result
            });
        }

        [HttpPost]
        [Route("add-costcenter")]
        public async Task<IHttpActionResult> AddCostCenter([FromBody] CostCenterDTO center)
        {
            var serviceHeader = master.GetServiceHeader();
            var result = await master._channelService.AddCostCenterAsync(center, serviceHeader);

            return Json(new ApiResponse<object>
            {
                Success = true,
                Message = "Cost center added successfully.",
                Data = result
            });
        }

        //

        //IF ACCOUNT TYPE IS CUSTOMER, D0 THIS

        //IF ACCOUNT TYPE IS GL, DO THAT

        [HttpPost]
        [Route("PostJournal")]
        public async Task<IHttpActionResult> HandleDirectPosting([FromBody] List<TransactionModel> transactionModels)
        {
            var serviceHeader = master.GetServiceHeader();

            if (transactionModels == null || !transactionModels.Any())
            {
                return Json(new ApiResponse<object>
                {
                    Success = false,
                    Message = "No journal entries provided.",
                    Data = null
                });
            }

            var results = new List<object>();
            foreach (var transactionModel in transactionModels)
            {

                // Single entry (self-balanced)
                if (transactionModel.ChartOfAccountId != Guid.Empty && transactionModel.ContraChartOfAccountId != Guid.Empty)
                {
                    if (transactionModel.DebitAmount > 0)
                    {
                        transactionModel.DebitChartOfAccountId = transactionModel.ChartOfAccountId;
                        transactionModel.CreditChartOfAccountId = transactionModel.ContraChartOfAccountId;
                    }
                    else
                    {
                        transactionModel.CreditChartOfAccountId = transactionModel.ChartOfAccountId;
                        transactionModel.DebitChartOfAccountId = transactionModel.ContraChartOfAccountId;
                    }

                    var result = await master._channelService.AddJournalAsync(transactionModel, null, serviceHeader);
                    results.Add(result);
                }

                // Multiple entries
                else if (transactionModel.CreditChartOfAccountId != Guid.Empty || transactionModel.DebitChartOfAccountId != Guid.Empty)
                {

                    if (transactionModel.CreditChartOfAccountId != Guid.Empty && transactionModel.CreditAmount > 0)
                    {
                        transactionModel.JournalType = (int)JournalVoucherType.CreditGLAccount;
                    }
                    else if (transactionModel.DebitChartOfAccountId != Guid.Empty && transactionModel.DebitAmount > 0)
                    {
                        transactionModel.JournalType = (int)JournalVoucherType.DebitGLAccount;
                    }

                    var result = await master._channelService.AddJournalSingleEntryAsync(transactionModel, null, serviceHeader);
                    results.Add(result);
                }
                else
                {
                    // Invalid entry, skip or handle as needed
                    continue;
                }
            }

            return Json(new ApiResponse<object>
            {
                Success = true,
                Message = "Journal(s) added successfully.",
                Data = results
            });
        }


        [HttpGet]
        [Route("GeneralLedgerTransactions")]
        public async Task<IHttpActionResult> GetGeneralLedgerTransactions(Guid chartOfAccountId)
        {

            bool tallyDebitsCredits = true;
            int transactionDateFilter = 1;
            int journalEntryFilter = 0;
            string textFilter = "";
            int pageIndex = 0;
            int pageSize = 20;
            try
            {
                var serviceHeader = master.GetServiceHeader();

                var effectiveStartDate = new DateTime(1900, 1, 1);

                var effectiveEndDate = DateTime.Today;

                var result = await master._channelService
                    .FindGeneralLedgerTransactionsByChartOfAccountIdAndDateRangeAndFilterInPageAsync(
                        pageIndex,
                        pageSize,
                        chartOfAccountId,
                        (DateTime)effectiveStartDate,
                        (DateTime)effectiveEndDate,
                        textFilter,
                        journalEntryFilter,
                        transactionDateFilter,
                        tallyDebitsCredits,
                        serviceHeader);
                result.PageIndex = pageIndex;
                result.PageSize = pageSize;
                result.TotalPages = (int)Math.Ceiling((double)result.ItemsCount / pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }



        [HttpPost]
        [Route("PostJournalVoucher")]
        public async Task<IHttpActionResult> postJournalVoucher([FromBody] JournalVoucherDTO journalVoucherDTO)
        {
            try
            {
                var serviceHeader = master.GetServiceHeader();

                // Validate that journal voucher entries balance (for batch posting)
                if (journalVoucherDTO.JournalVoucherEntries != null && journalVoucherDTO.JournalVoucherEntries.Any())
                {
                    var totalDebits = journalVoucherDTO.JournalVoucherEntries.Where(e => e.Amount > 0).Sum(e => e.Amount);
                    var totalCredits = journalVoucherDTO.JournalVoucherEntries.Where(e => e.Amount < 0).Sum(e => Math.Abs(e.Amount));

                    if (Math.Abs(totalDebits - totalCredits) > 0.01m)
                    {
                        return BadRequest(
                            "Journal entries are not balanced");
                    }

                    // Set total value to match entries for batch posting
                    journalVoucherDTO.TotalValue = totalDebits;
                }

                var createdVoucher = await master._channelService.AddJournalVoucherAsync(journalVoucherDTO, serviceHeader);

                if (createdVoucher != null)
                {
                    // If entries are provided, update the voucher entries collection
                    if (journalVoucherDTO.JournalVoucherEntries != null && journalVoucherDTO.JournalVoucherEntries.Any())
                    {
                        var entriesAdded = await master._channelService.UpdateJournalVoucherEntryCollectionAsync(
                            createdVoucher.Id, journalVoucherDTO.JournalVoucherEntries, serviceHeader);

                        if (!entriesAdded)
                        {
                            return BadRequest("Failed to add journal entries");
                        }
                    }

                    // Auto-audit and authorize for direct posting (bypass workflow)
                    var auditResult = await master._channelService.AuditJournalVoucherAsync(
                        createdVoucher, (int)JournalVoucherAuthOption.Post, serviceHeader);

                    var authorizeResult = await master._channelService.AuthorizeJournalVoucherAsync(
                        createdVoucher, (int)JournalVoucherAuthOption.Post,
                        0, serviceHeader); // Use appropriate module code

                    if (!auditResult || !authorizeResult)
                    {
                        return BadRequest("Failed to post journal voucher");
                    }

                    return Ok(new
                    {
                        Success = true,
                        VoucherId = createdVoucher.Id,
                        VoucherNumber = createdVoucher.VoucherNumber,
                        Message = "Journal voucher posted successfully",
                        TotalAmount = createdVoucher.TotalValue,
                        EntriesCount = journalVoucherDTO.JournalVoucherEntries?.Count ?? 0
                    });
                }
                else
                {
                    return BadRequest("Failed to create journal voucher");
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("Failed to post journal voucher: " + ex.Message));
            }
        }




        [HttpPost]
        [Route("AddGeneralLedgers")]
        public async Task<IHttpActionResult> AddGeneralLedgers([FromBody] GeneralLedgerDTO generalLedgerDTO)
        {
            var serviceHeader = master.GetServiceHeader();

            if (generalLedgerDTO == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Batch cannot be null."
                });
            }

            // Make sure there are entries to process
            if (generalLedgerDTO.GeneralLedgerEntries == null || !generalLedgerDTO.GeneralLedgerEntries.Any())
            {



                return Json(new
                {
                    success = false,
                    message = "At least one ledger entry is required."
                });
            }

            decimal sumAmount = generalLedgerDTO.GeneralLedgerEntries.Sum(e => e.Amount);
            decimal totalValue = generalLedgerDTO.TotalValue;

            if (sumAmount != totalValue)
            {
                var balance = totalValue - sumAmount;
                return Json(new
                {
                    success = false,
                    message = $"The total value ({totalValue}) should be equal to the sum of the entries ({sumAmount}). Balance: {balance}"
                });
            }

            // Validate the DTO
            generalLedgerDTO.ValidateAll();
            if (generalLedgerDTO.ErrorMessages.Count > 0)
            {
                return Json(new
                {
                    success = false,
                    message = generalLedgerDTO.ErrorMessages
                });
            }

            // Save the batch data
            var generalLedgerBatch = await master._channelService.AddGeneralLedgerAsync(generalLedgerDTO, serviceHeader);
            if (generalLedgerBatch.HasErrors)
            {
                return Json(new
                {
                    success = false,
                    message = generalLedgerBatch.ErrorMessages
                });
            }

            // Save each entry
            foreach (var entry in generalLedgerDTO.GeneralLedgerEntries)
            {
                entry.GeneralLedgerId = generalLedgerBatch.Id;
                entry.BranchId = generalLedgerBatch.BranchId;
                entry.BranchDescription = generalLedgerBatch.BranchDescription;
                await master._channelService.AddGeneralLedgerEntryAsync(entry, serviceHeader);
            }

            return Json(new
            {
                success = true,
                message = "Successfully created refund batch."
            });
        }

        [HttpGet]
        [Route("GetPurchaseInvoices")]
        public async Task<IHttpActionResult> GetPurchaseInvoices(bool? posted = null)
        {
            var serviceHeader = master.GetServiceHeader();
            var invoices = await master._channelService.FindPurchaseInvoicesAsync(serviceHeader);

            // Apply filtering if 'posted' param is provided
            if (posted.HasValue)
            {
                invoices = invoices.Where(i => i.Posted == posted.Value).ToList();
            }

            return Json(new ApiResponse<object>
            {
                Success = true,
                Message = invoices?.Count > 0 ? $"{invoices.Count} invoices found." : "No invoices found.",
                Data = invoices
            });
        }



        [HttpPost]
        [Route("AddPurchaseInvoice")]
        public async Task<IHttpActionResult> AddPurchaseInvoice([FromBody] PurchaseInvoiceDTO purchaseInvoiceDTO)
        {

            var serviceHeader = master.GetServiceHeader();

            if (purchaseInvoiceDTO != null)
            {

                purchaseInvoiceDTO.PaidAmount = 0;
                purchaseInvoiceDTO.RemainingAmount = purchaseInvoiceDTO.TotalAmount;


                var linesTotal = 0.00m;
                //purchaseInvoiceDTO.RemainingAmount = purchaseInvoiceDTO.

                foreach (var gl in purchaseInvoiceDTO.PurchaseInvoiceLines)
                {

                    linesTotal = linesTotal + gl.Amount;

                    if (gl.DebitChartOfAccountId != Guid.Empty)
                    {

                        var debitGl = await master._channelService.FindChartOfAccountAsync(gl.DebitChartOfAccountId);
                        gl.No = debitGl.AccountCode;

                    }
                    else
                    {
                        return Json(new
                        {
                            success = false,
                            message = "YOU HAVE A LINE WITHOUT PROPERT DEBITCHARTOFAACCOUNTID"
                        });
                    }

                }

                if (purchaseInvoiceDTO.TotalAmount != linesTotal)
                {

                    return Json(new
                    {
                        success = false,
                        message = "Amounts in Lines dont add up to value of Total Amount"
                    });
                }

                purchaseInvoiceDTO.ValidateAll();


                if (purchaseInvoiceDTO.ErrorMessages.Count > 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = purchaseInvoiceDTO.ErrorMessages
                    });
                }

                var result = await master._channelService.AddNewPurchaseInvoiceAsync(purchaseInvoiceDTO, serviceHeader);


                if (result != null)
                {


                    return Json(new
                    {
                        success = true,
                        message = "Successfully added Purchase header with lines."
                    });
                }


                else
                {
                    return Json(new
                    {
                        success = false,
                        message = "Failed to add Purchase header with lines."
                    });
                }

            }


            else
            {

                return Json(new
                {
                    success = false,
                    message = "Request Body is incomplete"
                });

            }

        }


        [HttpPut]
        [Route("UpdatePurchaseInvoice")]
        public async Task<IHttpActionResult> UpdatePurchaseInvoice([FromBody] PurchaseInvoiceDTO purchaseInvoiceDTO)
        {

            var serviceHeader = master.GetServiceHeader();

            if (purchaseInvoiceDTO != null)
            {

                purchaseInvoiceDTO.ValidateAll();


                if (purchaseInvoiceDTO.ErrorMessages.Count > 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = purchaseInvoiceDTO.ErrorMessages
                    });
                }

                var result = await master._channelService.UpdatePurchaseInvoiceAsync(purchaseInvoiceDTO, serviceHeader);


                if (result != null)
                {


                    return Json(new
                    {
                        success = true,
                        message = "Successfully updated Purchase header with lines."
                    });
                }


                else
                {
                    return Json(new
                    {
                        success = false,
                        message = "Failed to update Purchase header with lines."
                    });
                }

            }


            else
            {

                return Json(new
                {
                    success = false,
                    message = "Request Body is incomplete"
                });

            }

        }


        [HttpPost]
        [Route("PostPurchaseInvoice/{id}")]
        public async Task<IHttpActionResult> PostPurchaseInvoice(Guid id)
        {

            var serviceHeader = master.GetServiceHeader();

            PurchaseInvoiceDTO purchaseInvoiceDTO = null;


            var purchaseInvoiceDTOs = await master._channelService.FindPurchaseInvoicesAsync(serviceHeader);

            if (purchaseInvoiceDTOs != null)
            {

                purchaseInvoiceDTO = purchaseInvoiceDTOs.FirstOrDefault(p => p.Id == id);
            }


            if (purchaseInvoiceDTO != null)
            {

                var banks = await master._channelService.FindBankLinkagesAsync(serviceHeader);

                var bank = banks[0];



                purchaseInvoiceDTO.BranchId = bank.BranchId;
                purchaseInvoiceDTO.BankId = bank.Id;
                purchaseInvoiceDTO.BankBranchName = bank.BankBranchName;

                purchaseInvoiceDTO.ValidateAll();
                if (purchaseInvoiceDTO.ErrorMessages.Count > 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = purchaseInvoiceDTO.ErrorMessages
                    });
                }

                //var transactionModel = new TransactionModel();

                int moduleNavigationItemCode = 0;

                var tariffs = new ObservableCollection<TariffWrapper>();

                //var result = await master._channelService.AddJournalAsync(transactionModel, tariffs, serviceHeader);

                var result = await master._channelService.PostPurchaseInvoiceAsync(purchaseInvoiceDTO, moduleNavigationItemCode, serviceHeader);

                if (result != null)
                {

                    return Json(new
                    {
                        success = true,
                        message = "Succesfully posted Journal",
                        data = result
                    });
                }

                else
                {


                    return Json(new
                    {
                        success = false,
                        message = "Failed to post journal"
                    });
                }


            }

            else
            {

                return Json(new
                {
                    success = false,
                    message = "Request Object is empty"
                });
            }

        }


        [HttpPost]
        [Route("PostPaymentVoucher")]
        public async Task<IHttpActionResult> PayVendorInvoice(PaymentDTO paymentDTO)
        {

            var serviceHeader = master.GetServiceHeader();

            if (paymentDTO != null && paymentDTO.PaymentLines.Any())
            {

                decimal totalOfLines = paymentDTO.PaymentLines.Sum(x => x.Amount);

                if (paymentDTO.TotalAmount != totalOfLines)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Total mismatch: Header TotalAmount ({paymentDTO.TotalAmount:N2}) " +
                                  $"does not equal sum of PaymentLines ({totalOfLines:N2})."
                    });
                }

                int moduleNavigationItemCode = 0;

                var tariffs = new ObservableCollection<TariffWrapper>();

                var result = await master._channelService.PostPaymentAsync(paymentDTO, moduleNavigationItemCode, serviceHeader);

                if (result != null)
                {

                    return Json(new
                    {
                        success = true,
                        message = "Succesfully posted Journal",
                        data = result
                    });
                }

                else
                {


                    return Json(new
                    {
                        success = false,
                        message = "Failed to post journal"
                    });
                }


            }

            else
            {

                return Json(new
                {
                    success = false,
                    message = "Request Object is empty"
                });
            }

        }

        [Route("GetPayments")]
        public async Task<IHttpActionResult> GetPayments()
        {
            var serviceHeader = master.GetServiceHeader();

            //var salesInvoices = await master._channelService.FindSalesInvoicesAsync(serviceHeader);

            var payments = await master._channelService.FindPaymentsAsync(serviceHeader);





            return Json(new ApiResponse<object>
            {
                Success = true,
                Message = payments?.Count > 0 ? $"{payments.Count} payments found." : "No payments found.",
                Data = payments
            });
        }



        [Route("GetPurchaseCreditMemos")]
        public async Task<IHttpActionResult> GetPurchaseCreditMemos(bool? posted = null)
        {
            var serviceHeader = master.GetServiceHeader();

            var creditMemos = await master._channelService.FindPurchaseCreditMemosAsync(serviceHeader);

            // Apply filtering if 'posted' param is provided
            if (posted.HasValue)
            {
                creditMemos = creditMemos.Where(i => i.Posted == posted.Value).ToList();
            }

            return Json(new ApiResponse<object>
            {
                Success = true,
                Message = creditMemos?.Count > 0 ? $"{creditMemos.Count} credit memos found." : "No credit memos found.",
                Data = creditMemos
            });
        }





        [HttpPost]
        [Route("AddPurchaseCreditMemo")]
        public async Task<IHttpActionResult> AddPurchaseCreditMemo([FromBody] PurchaseCreditMemoDTO purchaseCreditMemoDTO)
        {

            var serviceHeader = master.GetServiceHeader();

            if (purchaseCreditMemoDTO != null)
            {


                foreach (var gl in purchaseCreditMemoDTO.PurchaseCreditMemoLines)
                {

                    if (gl.CreditChartOfAccountId != Guid.Empty)
                    {

                        var debitGl = await master._channelService.FindChartOfAccountAsync(gl.CreditChartOfAccountId);
                        gl.No = debitGl.AccountCode;

                    }

                    else
                    {

                        return Json(new
                        {
                            success = false,
                            message = "YOU HAVE A LINE WITHOUT PROPERT CREDITCHARTOFAACCOUNTID"
                        });


                    }



                }

                purchaseCreditMemoDTO.ValidateAll();


                if (purchaseCreditMemoDTO.ErrorMessages.Count > 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = purchaseCreditMemoDTO.ErrorMessages
                    });
                }

                var result = await master._channelService.AddNewPurchaseCreditMemoAsync(purchaseCreditMemoDTO, serviceHeader);


                if (result != null)
                {


                    return Json(new
                    {
                        success = true,
                        message = "Successfully added Purchase CREDIT MEMO with lines."
                    });
                }


                else
                {
                    return Json(new
                    {
                        success = false,
                        message = "Failed to add Purchase CREDIT MEMO with lines."
                    });
                }

            }


            else
            {

                return Json(new
                {
                    success = false,
                    message = "Request Body is incomplete"
                });

            }

        }



        [HttpPut]
        [Route("UpdatePurchaseCreditMemo")]
        public async Task<IHttpActionResult> UpdatePurchaseCreditMemo([FromBody] PurchaseCreditMemoDTO purchaseCreditMemoDTO)
        {
            var serviceHeader = master.GetServiceHeader();

            if (purchaseCreditMemoDTO != null)
            {
                purchaseCreditMemoDTO.ValidateAll();

                if (purchaseCreditMemoDTO.ErrorMessages.Count > 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = purchaseCreditMemoDTO.ErrorMessages
                    });
                }

                var result = await master._channelService.UpdatePurchaseCreditMemoAsync(purchaseCreditMemoDTO, serviceHeader);

                if (result != null)
                {
                    return Json(new
                    {
                        success = true,
                        message = "Successfully updated Purchase Credit Memo header with lines."
                    });
                }
                else
                {
                    return Json(new
                    {
                        success = false,
                        message = "Failed to update Purchase Credit Memo header with lines."
                    });
                }
            }
            else
            {
                return Json(new
                {
                    success = false,
                    message = "Request Body is incomplete"
                });
            }
        }






        [HttpPost]
        [Route("PostPurchaseCreditMemo/{id}")]
        public async Task<IHttpActionResult> PostPurchaseCreditMemo(Guid id)
        {

            var serviceHeader = master.GetServiceHeader();

            //PurchaseCreditMemoDTO purchaseCreditMemoDTO = null;


            var purchaseCreditMemoDTOs = await master._channelService.FindPurchaseCreditMemosAsync(serviceHeader);

            var purchaseCreditMemoDTO = purchaseCreditMemoDTOs.FirstOrDefault(p => p.Id == id);
            if (purchaseCreditMemoDTO == null)
            {
                return Json(new { success = false, message = "Purchase Credit Memo not found" });
            }


            if (purchaseCreditMemoDTO != null)
            {

                var banks = await master._channelService.FindBankLinkagesAsync(serviceHeader);

                var bank = banks[0];



                purchaseCreditMemoDTO.BranchId = bank.BranchId;
                purchaseCreditMemoDTO.BankId = bank.Id;
                purchaseCreditMemoDTO.BankBranchName = bank.BankBranchName;

                purchaseCreditMemoDTO.ValidateAll();
                if (purchaseCreditMemoDTO.ErrorMessages.Count > 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = purchaseCreditMemoDTO.ErrorMessages
                    });
                }

                //var transactionModel = new TransactionModel();

                int moduleNavigationItemCode = 0;

                var tariffs = new ObservableCollection<TariffWrapper>();

                //var result = await master._channelService.AddJournalAsync(transactionModel, tariffs, serviceHeader);

                var result = await master._channelService.PostPurchaseCreditMemoAsync(purchaseCreditMemoDTO, moduleNavigationItemCode, serviceHeader);

                if (result != null)
                {

                    return Json(new
                    {
                        success = true,
                        message = "Succesfully posted Journal",
                        data = result
                    });
                }

                else
                {


                    return Json(new
                    {
                        success = false,
                        message = "Failed to post journal"
                    });
                }


            }

            else
            {

                return Json(new
                {
                    success = false,
                    message = "Request Object is empty"
                });
            }

        }



        [Route("GetSalesInvoices")]
        public async Task<IHttpActionResult> GetSalesInvoices(bool? posted = null)
        {
            var serviceHeader = master.GetServiceHeader();

            var salesInvoices = await master._channelService.FindSalesInvoicesAsync(serviceHeader);

            // Apply filtering if 'posted' param is provided
            if (posted.HasValue)
            {
                salesInvoices = salesInvoices.Where(i => i.Posted == posted.Value).ToList();
            }

            return Json(new ApiResponse<object>
            {
                Success = true,
                Message = salesInvoices?.Count > 0 ? $"{salesInvoices.Count} sales invoices found." : "No sales invoices found.",
                Data = salesInvoices
            });
        }


        [HttpPost]
        [Route("AddSalesInvoice")]
        public async Task<IHttpActionResult> AddSalesInvoice([FromBody] SalesInvoiceDTO salesInvoiceDTO)
        {

            var serviceHeader = master.GetServiceHeader();

            var totalLines = 0.00m;

            if (salesInvoiceDTO != null)
            {



                salesInvoiceDTO.PaidAmount = 0;
                salesInvoiceDTO.RemainingAmount = salesInvoiceDTO.TotalAmount;


                foreach (var gl in salesInvoiceDTO.SalesInvoiceLines)
                {

                    totalLines += gl.Amount;

                    if (gl.CreditChartOfAccountId != Guid.Empty)
                    {

                        var debitGl = await master._channelService.FindChartOfAccountAsync(gl.CreditChartOfAccountId);
                        gl.No = debitGl.AccountCode;

                    }
                    else
                    {
                        return Json(new
                        {
                            success = false,
                            message = "YOU HAVE A LINE WITHOUT PROPERT CREDITCHARTOFAACCOUNTID"
                        });
                    }

                }

                if (salesInvoiceDTO.TotalAmount != totalLines)
                {

                    return Json(new
                    {

                        success = false,
                        message = "Amounts in lines do not add up to the totala mount"
                    });
                }
                salesInvoiceDTO.ValidateAll();


                if (salesInvoiceDTO.ErrorMessages.Count > 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = salesInvoiceDTO.ErrorMessages
                    });
                }

                var result = await master._channelService.AddNewSalesInvoiceAsync(salesInvoiceDTO, serviceHeader);


                if (result != null)
                {


                    return Json(new
                    {
                        success = true,
                        message = "Successfully added Sales header with lines."
                    });
                }


                else
                {
                    return Json(new
                    {
                        success = false,
                        message = "Failed to add Sales header with lines."
                    });
                }

            }


            else
            {

                return Json(new
                {
                    success = false,
                    message = "Request Body is incomplete"
                });

            }

        }


        [HttpPut]
        [Route("UpdateSalesInvoice")]
        public async Task<IHttpActionResult> UpdateSalesInvoice([FromBody] SalesInvoiceDTO salesInvoiceDTO)
        {

            var serviceHeader = master.GetServiceHeader();

            if (salesInvoiceDTO != null)
            {

                salesInvoiceDTO.ValidateAll();


                if (salesInvoiceDTO.ErrorMessages.Count > 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = salesInvoiceDTO.ErrorMessages
                    });
                }

                var result = await master._channelService.UpdateSalesInvoiceAsync(salesInvoiceDTO, serviceHeader);


                if (result != null)
                {


                    return Json(new
                    {
                        success = true,
                        message = "Successfully updated Sales header with lines."
                    });
                }


                else
                {
                    return Json(new
                    {
                        success = false,
                        message = "Failed to update Sales header with lines."
                    });
                }

            }


            else
            {

                return Json(new
                {
                    success = false,
                    message = "Request Body is incomplete"
                });

            }

        }


        [HttpPost]
        [Route("PostSalesInvoice/{id}")]
        public async Task<IHttpActionResult> PostSalesInvoice(Guid id)
        {

            var serviceHeader = master.GetServiceHeader();

            SalesInvoiceDTO salesInvoiceDTO = null;


            var salesInvoiceDTOs = await master._channelService.FindSalesInvoicesAsync(serviceHeader);

            if (salesInvoiceDTOs != null)
            {

                salesInvoiceDTO = salesInvoiceDTOs.FirstOrDefault(p => p.Id == id);
            }


            if (salesInvoiceDTO != null && !salesInvoiceDTO.Posted)
            {

                var banks = await master._channelService.FindBankLinkagesAsync(serviceHeader);

                var bank = banks[0];



                salesInvoiceDTO.BranchId = bank.BranchId;
                salesInvoiceDTO.BankId = bank.Id;
                salesInvoiceDTO.BankBranchName = bank.BankBranchName;

                salesInvoiceDTO.ValidateAll();
                if (salesInvoiceDTO.ErrorMessages.Count > 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = salesInvoiceDTO.ErrorMessages
                    });
                }

                //var transactionModel = new TransactionModel();

                int moduleNavigationItemCode = 0;

                var tariffs = new ObservableCollection<TariffWrapper>();

                //var result = await master._channelService.AddJournalAsync(transactionModel, tariffs, serviceHeader);

                var result = await master._channelService.PostSalesInvoiceAsync(salesInvoiceDTO, moduleNavigationItemCode, serviceHeader);

                if (result != null)
                {

                    return Json(new
                    {
                        success = true,
                        message = "Succesfully posted Journal",
                        data = result
                    });
                }

                else
                {


                    return Json(new
                    {
                        success = false,
                        message = "Failed to post journal"
                    });
                }


            }

            else
            {

                return Json(new
                {
                    success = false,
                    message = "Target Invoice is already posted, or is missing"
                });
            }

        }





        //[HttpPost]
        //[Route("AddSalesCreditMemo")]
        //public async Task<IHttpActionResult> AddSalesCreditMemo([FromBody] SalesCreditMemoDTO salesCreditMemoDTO)
        //{

        //    var serviceHeader = master.GetServiceHeader();

        //    var totalLines = 0.00m;



        //    if (salesCreditMemoDTO != null)
        //    {


        //        var targetSalesInvoice = await master._channelService.findsale(salesCreditMemoDTO.SalesInvoiceId, serviceHeader);

        //        if (targetSalesInvoice == null)
        //        {

        //            return Json(new
        //            {

        //                success = false,
        //                message = "target invoice does not exist" //or doesnt exist??
        //            });
        //        }

        //        foreach (var gl in salesCreditMemoDTO.SalesCreditMemoLines)
        //        {

        //            totalLines += gl.Amount;

        //            if (gl.DebitChartOfAccountId != Guid.Empty)
        //            {

        //                var debitGl = await master._channelService.FindChartOfAccountAsync(gl.DebitChartOfAccountId);
        //                gl.No = debitGl.AccountCode;

        //            }

        //            else
        //            {

        //                return Json(new
        //                {
        //                    success = false,
        //                    message = "YOU HAVE A LINE WITHOUT PROPERT DEBITCHARTOFAACCOUNTID"
        //                });


        //            }
        //        }





        //        if (totalLines != salesCreditMemoDTO.TotalAmount)
        //            {

        //                return Json(new
        //                {

        //                    success = false,
        //                    mesSage = "amount in lines do no equal totalamount"
        //                });
        //            }

        //        salesCreditMemoDTO.ValidateAll();


        //        if (salesCreditMemoDTO.ErrorMessages.Count > 0)
        //        {
        //            return Json(new
        //            {
        //                success = false,
        //                message = salesCreditMemoDTO.ErrorMessages
        //            });
        //        }

        //        var result = await master._channelService.AddNewSalesCreditMemoAsync(salesCreditMemoDTO, serviceHeader);


        //        if (result != null)
        //        {


        //            return Json(new
        //            {
        //                success = true,
        //                message = "Successfully added Sales CREDIT MEMO with lines."
        //            });
        //        }


        //        else
        //        {
        //            return Json(new
        //            {
        //                success = false,
        //                message = "Failed to add SALES CREDIT MEMO with lines."
        //            });
        //        }

        //    }


        //    else
        //    {

        //        return Json(new
        //        {
        //            success = false,
        //            message = "Request Body is incomplete"
        //        });

        //    }

        //}



        [HttpPut]
        [Route("UpdateSalesCreditMemo")]
        public async Task<IHttpActionResult> UpdateSalesCreditMemo([FromBody] SalesCreditMemoDTO salesCreditMemoDTO)
        {
            var serviceHeader = master.GetServiceHeader();

            if (salesCreditMemoDTO != null)
            {
                salesCreditMemoDTO.ValidateAll();

                if (salesCreditMemoDTO.ErrorMessages.Count > 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = salesCreditMemoDTO.ErrorMessages
                    });
                }

                var result = await master._channelService.UpdateSalesCreditMemoAsync(salesCreditMemoDTO, serviceHeader);

                if (result)
                {
                    return Json(new
                    {
                        success = true,
                        message = "Successfully updated Sales Credit Memo header with lines."
                    });
                }
                else
                {
                    return Json(new
                    {
                        success = false,
                        message = "Failed to update Sales Credit Memo header with lines."
                    });
                }
            }
            else
            {
                return Json(new
                {
                    success = false,
                    message = "Request Body is incomplete"
                });
            }
        }


        [Route("GetSalesCreditMemos")]
        public async Task<IHttpActionResult> GetSalesCreditMemos(bool? posted = null)
        {
            var serviceHeader = master.GetServiceHeader();

            var creditMemos = await master._channelService.FindSalesCreditMemosAsync(serviceHeader);

            // Apply filtering if 'posted' param is provided
            if (posted.HasValue)
            {
                creditMemos = creditMemos.Where(i => i.Posted == posted.Value).ToList();
            }

            return Json(new ApiResponse<object>
            {
                Success = true,
                Message = creditMemos?.Count > 0 ? $"{creditMemos.Count} credit memos found." : "No credit memos found.",
                Data = creditMemos
            });
        }





        //customer rcpt
        [HttpPost]
        [Route("PostSalesCreditMemo/{id}")]
        public async Task<IHttpActionResult> PostSalesCreditMemo(Guid id)
        {

            var serviceHeader = master.GetServiceHeader();

            SalesCreditMemoDTO salesCreditMemoDTO = null;


            var salesCreditMemoDTOs = await master._channelService.FindSalesCreditMemosAsync(serviceHeader);

            if (salesCreditMemoDTOs != null)
            {

                salesCreditMemoDTO = salesCreditMemoDTOs.FirstOrDefault(p => p.Id == id);
            }


            if (salesCreditMemoDTO != null && !salesCreditMemoDTO.Posted)
            {

                var banks = await master._channelService.FindBankLinkagesAsync(serviceHeader);

                var bank = banks[0];



                salesCreditMemoDTO.BranchId = bank.BranchId;
                salesCreditMemoDTO.BankId = bank.Id;
                salesCreditMemoDTO.BankBranchName = bank.BankBranchName;

                salesCreditMemoDTO.ValidateAll();
                if (salesCreditMemoDTO.ErrorMessages.Count > 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = salesCreditMemoDTO.ErrorMessages
                    });
                }

                //var transactionModel = new TransactionModel();

                int moduleNavigationItemCode = 0;

                var tariffs = new ObservableCollection<TariffWrapper>();

                //var result = await master._channelService.AddJournalAsync(transactionModel, tariffs, serviceHeader);

                var result = await master._channelService.PostSalesCreditMemoAsync(salesCreditMemoDTO, moduleNavigationItemCode, serviceHeader);

                if (result != null)
                {

                    return Json(new
                    {
                        success = true,
                        message = "Succesfully posted Journal",
                        data = result
                    });
                }

                else
                {


                    return Json(new
                    {
                        success = false,
                        message = "Failed to post journal"
                    });
                }


            }

            else
            {

                return Json(new
                {
                    success = false,
                    message = "Target Sales Credit Memo is already posted, or is missing"
                });
            }

        }



        [Route("GetARCustomers")]
        public async Task<IHttpActionResult> GetARCustomers()
        {
            var serviceHeader = master.GetServiceHeader();

            var arCustomers = await master._channelService.FindARCustomersAsync(serviceHeader);

            return Json(new ApiResponse<object>
            {
                Success = true,
                Message = arCustomers?.Count > 0 ? $"{arCustomers.Count} customers found." : "No customers  found.",
                Data = arCustomers
            });
        }


        [HttpPut]
        [Route("UpdateARCustomer")]
        public async Task<IHttpActionResult> UpdateARCustomer([FromBody] ARCustomerDTO arCustomerDTO)
        {
            var serviceHeader = master.GetServiceHeader();

            if (arCustomerDTO != null)
            {
                arCustomerDTO.ValidateAll();

                if (arCustomerDTO.ErrorMessages.Count > 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = arCustomerDTO.ErrorMessages
                    });
                }

                var result = await master._channelService.UpdateARCustomerAsync(arCustomerDTO, serviceHeader);

                if (result)
                {
                    return Json(new
                    {
                        success = true,
                        message = "Successfully updated AR Customer."
                    });
                }
                else
                {
                    return Json(new
                    {
                        success = false,
                        message = "Failed to update AR Customer"
                    });
                }
            }
            else
            {
                return Json(new
                {
                    success = false,
                    message = "Request Body is incomplete"
                });
            }
        }


        [HttpPost]
        [Route("AddARCustomer")]
        public async Task<IHttpActionResult> AddARCustomer([FromBody] ARCustomerDTO arCustomerDTO)
        {

            var serviceHeader = master.GetServiceHeader();

            if (arCustomerDTO != null)
            {

                arCustomerDTO.ValidateAll();


                if (arCustomerDTO.ErrorMessages.Count > 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = arCustomerDTO.ErrorMessages
                    });
                }

                var result = await master._channelService.AddARCustomerAsync(arCustomerDTO, serviceHeader);

                if (result != null)
                {


                    return Json(new
                    {
                        success = true,
                        message = "Successfully added Customer."
                    });
                }


                else
                {
                    return Json(new
                    {
                        success = false,
                        message = "Failed to add customer."
                    });
                }

            }


            else
            {

                return Json(new
                {
                    success = false,
                    message = "Request Body is incomplete"
                });

            }

        }




        [HttpPost]
        [Route("AddNewReceipt")]
        public async Task<IHttpActionResult> AddNewReceipt(ReceiptDTO receiptDTO)
        {

            var serviceHeader = master.GetServiceHeader();



            if (receiptDTO != null && receiptDTO.ReceiptLines.Any())
            {



                decimal totalOfLines = receiptDTO.ReceiptLines.Sum(x => x.Amount);

                if (receiptDTO.TotalAmount != totalOfLines)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Total mismatch: Header TotalAmount ({receiptDTO.TotalAmount:N2}) " +
                                  $"does not equal sum of PaymentLines ({totalOfLines:N2})."
                    });
                }

                int moduleNavigationItemCode = 0;

                var tariffs = new ObservableCollection<TariffWrapper>();

                //var result = await master._channelService.PostReceiptAsync(receiptDTO, moduleNavigationItemCode, serviceHeader);

                var result = await master._channelService.AddNewReceiptAsync(receiptDTO, serviceHeader);

                if (result != null)
                {

                    return Json(new
                    {
                        success = true,
                        message = "Succesfully Added New Receipt",
                        data = result
                    });
                }

                else
                {


                    return Json(new
                    {
                        success = false,
                        message = "Failed to add receipt"
                    });
                }


            }

            else
            {

                return Json(new
                {
                    success = false,
                    message = "Request Object is empty"
                });
            }

        }


        [HttpPost]
        [Route("PostReceipt")]
        public async Task<IHttpActionResult> ReceiveCustomerPayment(ReceiptDTO receiptDTO)
        {
            try
            {
                var serviceHeader = master.GetServiceHeader();

                if (receiptDTO != null && receiptDTO.ReceiptLines.Any())
                {
                    decimal totalOfLines = receiptDTO.ReceiptLines.Sum(x => x.Amount);

                    if (receiptDTO.TotalAmount != totalOfLines)
                    {
                        return Json(new
                        {
                            success = false,
                            message = $"Total mismatch: Header TotalAmount ({receiptDTO.TotalAmount:N2}) " +
                                      $"does not equal sum of PaymentLines ({totalOfLines:N2})."
                        });
                    }

                    int moduleNavigationItemCode = 0;
                    var tariffs = new ObservableCollection<TariffWrapper>();

                    var result = await master._channelService.PostReceiptAsync(receiptDTO, moduleNavigationItemCode, serviceHeader);

                    if (result != null)
                    {
                        return Json(new
                        {
                            success = true,
                            message = "Successfully posted Journal",
                            data = result
                        });
                    }
                    else
                    {
                        // Log more details
                        System.Diagnostics.Trace.TraceError($"PostReceiptAsync returned null. ReceiptDTO: {Newtonsoft.Json.JsonConvert.SerializeObject(receiptDTO)}");

                        return Json(new
                        {
                            success = false,
                            message = "Failed to post journal - service returned null result",
                            details = "The WCF service call completed but returned null. Check service logs."
                        });
                    }
                }
                else
                {
                    return Json(new
                    {
                        success = false,
                        message = "Request Object is empty"
                    });
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                System.Diagnostics.Trace.TraceError($"Controller exception: {ex.Message}");
                System.Diagnostics.Trace.TraceError($"Stack Trace: {ex.StackTrace}");

                return Json(new
                {
                    success = false,
                    message = $"Failed to process request: {ex.Message}",
                    innerException = ex.InnerException?.Message
                });
            }
        }

        [Route("GetReceipts")]
        public async Task<IHttpActionResult> GetReceipts()
        {
            var serviceHeader = master.GetServiceHeader();

            //var salesInvoices = await master._channelService.FindSalesInvoicesAsync(serviceHeader);

            var receipts = await master._channelService.FindReceiptsAsync(serviceHeader);





            return Json(new ApiResponse<object>
            {
                Success = true,
                Message = receipts?.Count > 0 ? $"{receipts.Count} receipts found." : "No receipts found.",
                Data = receipts
            });
        }



        // this applies for loan, investment or savings accounts
        [HttpGet]
        [Route("GetCustomerAccountHistory")]
        public async Task<IHttpActionResult> GetCustomerAccountHistory(Guid CustomerAccountId)
        {
            try
            {
                var serviceHeader = master.GetServiceHeader();

                var history = await master._channelService
                    .FindCustomerAccountHistoryByCustomerAccountIdAsync(CustomerAccountId, serviceHeader);

                if (history == null || history.Count == 0)
                {
                    return NotFound(); // 404
                }

                return Ok(history); // 200 + JSON list
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }


        [HttpGet]
        [Route("CustomerAccount/{customerAccountId}")]
        public async Task<IHttpActionResult> GetCustomerAccountById(Guid customerAccountId)
        {
            try
            {
                var serviceHeader = master.GetServiceHeader();

                var account = await master._channelService.FindCustomerAccountAsync(
                    customerAccountId,
                    includeBalances: true,
                    includeProductDescription: true,
                    includeInterestBalanceForLoanAccounts: true,
                    considerMaturityPeriodForInvestmentAccounts: false,
                    serviceHeader: serviceHeader
                );

                if (account == null)
                {
                    return NotFound();
                }

                return Ok(account);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("CustomerAccount/by-customer")]
        public async Task<IHttpActionResult> GetCustomerAccountsByCustomerId(Guid customerId)
        {
            try
            {
                if (customerId == Guid.Empty)
                {
                    return BadRequest("Customer ID is required");
                }

                var serviceHeader = master.GetServiceHeader();

                var accounts = await master._channelService.FindCustomerAccountsByCustomerIdAsync(
                    customerId,
                    includeBalances: true,
                    includeProductDescription: true,
                    includeInterestBalanceForLoanAccounts: true,
                    considerMaturityPeriodForInvestmentAccounts: false,
                    serviceHeader: serviceHeader
                );

                if (accounts == null || !accounts.Any())
                {
                    return NotFound();
                }

                return Ok(accounts);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }






        [HttpPost]
        [Route("CustomerReceipt")]
        public async Task<IHttpActionResult> Create(dynamic requestData)
        {
            try
            {
                // -------------------- DETERMINE REQUEST TYPE --------------------
                bool isBatchRequest = false;
                List<CustomerReceiptBatchRequest> batchReceipts = null;
                CustomerTransactionModel singleTransactionModel = null;
                BatchCustomerReceiptRequest batchRequest = null;

                try
                {
                    // Try to parse as batch request first
                    var jsonString = requestData.ToString();
                    batchRequest = JsonConvert.DeserializeObject<BatchCustomerReceiptRequest>(jsonString);

                    if (batchRequest != null && batchRequest.Receipts != null && batchRequest.Receipts.Any())
                    {
                        isBatchRequest = true;
                        batchReceipts = batchRequest.Receipts;
                    }
                }
                catch
                {
                    // Not a batch request, try as single transaction
                    singleTransactionModel = JsonConvert.DeserializeObject<CustomerTransactionModel>(requestData.ToString());
                }

                // If neither parsing succeeded, try direct conversion
                if (!isBatchRequest && singleTransactionModel == null)
                {
                    singleTransactionModel = requestData as CustomerTransactionModel;
                    if (singleTransactionModel == null)
                        return Ok(new { success = false, message = "Invalid request format" });
                }

                // -------------------- PROCESS BASED ON REQUEST TYPE --------------------
                if (isBatchRequest)
                {

                    return await ProcessBatchReceipt(batchRequest);
                }
                else
                {
                    return await ProcessSingleReceipt(singleTransactionModel);
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // Private method to generate system reference number using database table
        private string GenerateReceiptReferenceNumber()
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    // First, check if ReferenceNumberSequence table exists
                    string checkTableQuery = @"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.TABLES 
                WHERE TABLE_NAME = 'ReferenceNumberSequence'";

                    using (var cmd = new SqlCommand(checkTableQuery, conn))
                    {
                        int tableExists = (int)cmd.ExecuteScalar();

                        if (tableExists == 0)
                        {
                            // Create the table if it doesn't exist
                            string createTableQuery = @"
                        CREATE TABLE ReferenceNumberSequence (
                            Id INT PRIMARY KEY IDENTITY(1,1),
                            ReferenceType VARCHAR(50) NOT NULL,
                            Prefix VARCHAR(10) NOT NULL,
                            CurrentNumber INT NOT NULL DEFAULT 0,
                            LastUsedDate DATETIME NULL,
                            CreatedDate DATETIME DEFAULT GETDATE()
                        )";

                            using (var createCmd = new SqlCommand(createTableQuery, conn))
                            {
                                createCmd.ExecuteNonQuery();
                            }
                        }
                    }

                    // Check if we have a record for Customer Receipt
                    string checkRecordQuery = @"
                SELECT COUNT(*) 
                FROM ReferenceNumberSequence 
                WHERE ReferenceType = 'CustomerReceipt'";

                    using (var cmd = new SqlCommand(checkRecordQuery, conn))
                    {
                        int recordExists = (int)cmd.ExecuteScalar();

                        if (recordExists == 0)
                        {
                            // Insert initial record
                            string insertQuery = @"
                        INSERT INTO ReferenceNumberSequence (ReferenceType, Prefix, CurrentNumber, LastUsedDate)
                        VALUES ('CustomerReceipt', 'CR', 0, NULL)";

                            using (var insertCmd = new SqlCommand(insertQuery, conn))
                            {
                                insertCmd.ExecuteNonQuery();
                            }
                        }
                    }

                    // Now get and increment the reference number using OUTPUT clause
                    string updateQuery = @"
                UPDATE ReferenceNumberSequence 
                SET CurrentNumber = CurrentNumber + 1,
                    LastUsedDate = GETDATE()
                OUTPUT INSERTED.Prefix, INSERTED.CurrentNumber
                WHERE ReferenceType = 'CustomerReceipt'";

                    using (var cmd = new SqlCommand(updateQuery, conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string prefix = reader.GetString(0);
                                int currentNumber = reader.GetInt32(1);

                                // Format as CR0001, CR0002, etc.
                                return $"{prefix}{currentNumber.ToString("D4")}";
                            }
                        }
                    }

                    // Fallback if something went wrong
                    return $"CR{DateTime.Now:yyMMddHHmmss}";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"Error generating receipt reference: {ex.Message}");

                // Fallback: Use timestamp-based reference
                return $"CR{DateTime.Now:yyMMddHHmmss}";
            }
        }

        // Alternative: Thread-safe version with transaction isolation
        private string GenerateThreadSafeReferenceNumber()
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    // Use transaction with serializable isolation level for thread safety
                    using (var transaction = conn.BeginTransaction(IsolationLevel.Serializable))
                    {
                        try
                        {
                            // Ensure table exists
                            EnsureReferenceTableExists(conn, transaction);

                            // Get current number
                            string selectQuery = @"
                        SELECT Prefix, CurrentNumber 
                        FROM ReferenceNumberSequence WITH (UPDLOCK)
                        WHERE ReferenceType = 'CustomerReceipt'";

                            string prefix = "CR";
                            int currentNumber;

                            using (var cmd = new SqlCommand(selectQuery, conn, transaction))
                            {
                                using (var reader = cmd.ExecuteReader())
                                {
                                    if (reader.Read())
                                    {
                                        prefix = reader.GetString(0);
                                        currentNumber = reader.GetInt32(1);
                                        reader.Close();
                                    }
                                    else
                                    {
                                        reader.Close();
                                        // Insert new record
                                        string insertQuery = @"
                                    INSERT INTO ReferenceNumberSequence (ReferenceType, Prefix, CurrentNumber, LastUsedDate)
                                    VALUES ('CustomerReceipt', 'CR', 0, NULL)";

                                        using (var insertCmd = new SqlCommand(insertQuery, conn, transaction))
                                        {
                                            insertCmd.ExecuteNonQuery();
                                        }
                                        currentNumber = 0;
                                    }
                                }
                            }

                            // Increment and update
                            int newNumber = currentNumber + 1;

                            string updateQuery = @"
                        UPDATE ReferenceNumberSequence 
                        SET CurrentNumber = @NewNumber,
                            LastUsedDate = GETDATE()
                        WHERE ReferenceType = 'CustomerReceipt'";

                            using (var updateCmd = new SqlCommand(updateQuery, conn, transaction))
                            {
                                updateCmd.Parameters.AddWithValue("@NewNumber", newNumber);
                                updateCmd.ExecuteNonQuery();
                            }

                            transaction.Commit();

                            return $"{prefix}{newNumber.ToString("D4")}";
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            throw new Exception($"Failed to generate reference number: {ex.Message}", ex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"Error in thread-safe reference generation: {ex.Message}");
                return $"CR{DateTime.Now:yyMMddHHmmss}";
            }
        }

        // Helper method to ensure table exists
        private void EnsureReferenceTableExists(SqlConnection conn, SqlTransaction transaction)
        {
            string checkTableQuery = @"
        IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ReferenceNumberSequence')
        BEGIN
            CREATE TABLE ReferenceNumberSequence (
                Id INT PRIMARY KEY IDENTITY(1,1),
                ReferenceType VARCHAR(50) NOT NULL,
                Prefix VARCHAR(10) NOT NULL,
                CurrentNumber INT NOT NULL DEFAULT 0,
                LastUsedDate DATETIME NULL,
                CreatedDate DATETIME DEFAULT GETDATE()
            )
        END";

            using (var cmd = new SqlCommand(checkTableQuery, conn, transaction))
            {
                cmd.ExecuteNonQuery();
            }
        }

        // Private method for single receipt processing
        private async Task<IHttpActionResult> ProcessSingleReceipt(CustomerTransactionModel transactionModel)
        {
            // -------------------- SERVICE HEADER --------------------
            var serviceHeader = master.GetServiceHeader();

            // -------------------- VALIDATE REQUEST --------------------
            if (transactionModel == null)
                return Ok(new { success = false, message = "Invalid transaction request" });

            if (transactionModel.CustomerAccount == null || transactionModel.CustomerAccount.Id == Guid.Empty)
                return Ok(new { success = false, message = "Please select a customer account" });

            if (transactionModel.BankAccountId == Guid.Empty)
                return Ok(new { success = false, message = "Please select a receiving bank account" });

            // -------------------- FETCH CUSTOMER ACCOUNT --------------------
            bool includeBalances = true;
            bool includeProductDescription = true;
            bool includeInterestBalanceForLoanAccounts = true;
            bool considerMaturityPeriodForInvestmentAccounts = true;

            var selectedCustomerAccount =
                await master._channelService.FindCustomerAccountAsync(
                    transactionModel.CustomerAccount.Id,
                    includeBalances,
                    includeProductDescription,
                    includeInterestBalanceForLoanAccounts,
                    considerMaturityPeriodForInvestmentAccounts,
                    serviceHeader
                );

            if (selectedCustomerAccount == null)
                return Ok(new { success = false, message = "Customer account not found" });

            if ((RecordStatus)selectedCustomerAccount.RecordStatus != RecordStatus.Approved)
                return Ok(new { success = false, message = "Sorry, account is not approved yet" });


            // -------------------- ACCOUNT TYPE FLAGS --------------------
            bool isSavings = selectedCustomerAccount.CustomerAccountTypeProductCode == (int)ProductCode.Savings;
            bool isLoan = selectedCustomerAccount.CustomerAccountTypeProductCode == (int)ProductCode.Savings;

            if (!isSavings && !isLoan)
                return Ok(new { success = false, message = "Unsupported account type" });


            // -------------------- SET CUSTOMER ACCOUNT REFERENCES --------------------
            transactionModel.CreditCustomerAccount = selectedCustomerAccount;
            transactionModel.CreditCustomerAccountId = selectedCustomerAccount.Id;
            transactionModel.DebitCustomerAccount = selectedCustomerAccount;
            transactionModel.DebitCustomerAccountId = selectedCustomerAccount.Id;

            // -------------------- FETCH TARGET PRODUCT --------------------
            Guid creditChartOfAccountId;
            string systemReference;

            if (isSavings)
            {
                var savingsProduct =
                    await master._channelService.FindSavingsProductAsync(
                        selectedCustomerAccount.CustomerAccountTypeTargetProductId,
                        serviceHeader
                    );

                if (savingsProduct == null)
                    return Ok(new { success = false, message = "Savings product not found" });

                transactionModel.CreditChartOfAccountId = savingsProduct.ChartOfAccountId;


                // -------------------- FETCH BANK LINKAGE --------------------
                var selectedBankLinkage =
                    await master._channelService.FindBankLinkageByBankAccountIdAsync(
                        transactionModel.BankAccountId,
                        serviceHeader
                    );

                if (selectedBankLinkage == null)
                    return Ok(new { success = false, message = "Bank Account is missing, please select a receiving bank" });

                // -------------------- FETCH POSTING PERIOD --------------------
                var postingPeriod = transactionModel.PostingPeriodId != Guid.Empty
                    ? await master._channelService.FindPostingPeriodAsync(transactionModel.PostingPeriodId, serviceHeader)
                    : await master._channelService.FindCurrentPostingPeriodAsync(serviceHeader);

                if (postingPeriod == null)
                    return Ok(new { success = false, message = "Posting period not found" });

                // -------------------- POPULATE TRANSACTION MODEL --------------------
                transactionModel.PostingPeriodId = postingPeriod.Id;

                if (string.IsNullOrEmpty(transactionModel.PrimaryDescription))
                    transactionModel.PrimaryDescription = "Customer Receipt";

                if (string.IsNullOrEmpty(transactionModel.SecondaryDescription))
                    transactionModel.SecondaryDescription = $"BC {selectedBankLinkage.BankName}";

                // GENERATE SYSTEM REFERENCE NUMBER
                systemReference = GenerateThreadSafeReferenceNumber();

                // If user provided a reference, append it, otherwise use only system reference
                if (!string.IsNullOrEmpty(transactionModel.Reference))
                {
                    transactionModel.Reference = $"{systemReference} - {transactionModel.Reference}";
                }
                else
                {
                    transactionModel.Reference = systemReference;
                }

                transactionModel.DebitChartOfAccountId = selectedBankLinkage.ChartOfAccountId;
                transactionModel.TransactionCode = (int)SystemTransactionCode.CashDeposit;
            }
            else // isLoan
            {
                var loanProduct =
                    await master._channelService.FindLoanProductAsync(
                        selectedCustomerAccount.CustomerAccountTypeTargetProductId,
                        serviceHeader
                    );

                if (loanProduct == null)
                    return Ok(new { success = false, message = "Loan product not found" });

                transactionModel.CreditChartOfAccountId = loanProduct.ChartOfAccountId;

                // -------------------- FETCH BANK LINKAGE --------------------
                var selectedBankLinkage =
                    await master._channelService.FindBankLinkageByBankAccountIdAsync(
                        transactionModel.BankAccountId,
                        serviceHeader
                    );

                if (selectedBankLinkage == null)
                    return Ok(new { success = false, message = "Bank Account is missing, please select a receiving bank" });

                // -------------------- FETCH POSTING PERIOD --------------------
                var postingPeriod = transactionModel.PostingPeriodId != Guid.Empty
                    ? await master._channelService.FindPostingPeriodAsync(transactionModel.PostingPeriodId, serviceHeader)
                    : await master._channelService.FindCurrentPostingPeriodAsync(serviceHeader);

                if (postingPeriod == null)
                    return Ok(new { success = false, message = "Posting period not found" });

                // -------------------- POPULATE TRANSACTION MODEL --------------------
                transactionModel.PostingPeriodId = postingPeriod.Id;

                if (string.IsNullOrEmpty(transactionModel.PrimaryDescription))
                    transactionModel.PrimaryDescription = "Customer Receipt";

                if (string.IsNullOrEmpty(transactionModel.SecondaryDescription))
                    transactionModel.SecondaryDescription = $"BC {selectedBankLinkage.BankName}";

                // GENERATE SYSTEM REFERENCE NUMBER
                systemReference = GenerateThreadSafeReferenceNumber();

                // If user provided a reference, append it, otherwise use only system reference
                if (!string.IsNullOrEmpty(transactionModel.Reference))
                {
                    transactionModel.Reference = $"{systemReference} - {transactionModel.Reference}";
                }
                else
                {
                    transactionModel.Reference = systemReference;
                }
                transactionModel.CreditChartOfAccountId = selectedCustomerAccount.CustomerAccountTypeTargetProductChartOfAccountId;

                transactionModel.DebitChartOfAccountId = selectedBankLinkage.ChartOfAccountId;
                transactionModel.TransactionCode = (int)SystemTransactionCode.CashDeposit;
            }



            // -------------------- VALIDATE BUSINESS RULES --------------------
            transactionModel.ValidateAll();

            if (transactionModel.HasErrors)
            {
                var combinedErrors = string.Join("; ", transactionModel.ErrorMessages);
                return Ok(new { success = false, message = $"Transaction Error: {combinedErrors}" });
            }

            // -------------------- POST TRANSACTION --------------------
            var journal =
                await master._channelService.AddJournalWithCustomerAccountAsync(
                    transactionModel,
                    serviceHeader
                );

            // -------------------- UPDATE CUSTOMER BALANCE --------------------
            selectedCustomerAccount.NewAvailableBalance =
                selectedCustomerAccount.AvailableBalance + transactionModel.TotalValue;

            var updateResult =
                await master._channelService.UpdateCustomerAccountAsync(
                    selectedCustomerAccount,
                    serviceHeader
                );

            if (!updateResult)
            {
                return Ok(new
                {
                    success = false,
                    message = "Sorry, but the authorized cash deposit could not be posted!"
                });
            }

            // -------------------- SUCCESS RESPONSE --------------------
            return Ok(new
            {
                success = true,
                message = $"Operation success: Customer's new balance is {selectedCustomerAccount.NewAvailableBalance}",
                systemReference = systemReference,
                journal = new
                {
                    id = journal.Id,
                    sequentialId = journal.SequentialId,
                    branchDescription = journal.BranchDescription,
                    primaryDescription = journal.PrimaryDescription,
                    secondaryDescription = journal.SecondaryDescription,
                    postingPeriodDescription = journal.PostingPeriodDescription,
                    applicationUserName = journal.ApplicationUserName,
                    createdDate = journal.CreatedDate,
                    totalValue = journal.TotalValue,
                    reference = journal.Reference
                }
            });
        }

        // Private method for batch receipt processing
        private async Task<IHttpActionResult> ProcessBatchReceipt(BatchCustomerReceiptRequest batchRequest)
        {
            var results = new List<object>();
            var successfulTransactions = 0;
            decimal totalAmount = 0;

            // -------------------- VALIDATE BATCH REQUEST --------------------
            if (batchRequest == null || batchRequest.Receipts == null || !batchRequest.Receipts.Any())
                return Ok(new { success = false, message = "Invalid batch request" });

            if (batchRequest.BankAccountId == Guid.Empty)
                return Ok(new { success = false, message = "Please select a receiving bank account" });

            // Generate batch prefix
            string batchPrefix = $"BATCH{DateTime.Now:yyyyMMdd}";

            // Process each receipt with unique reference numbers
            int receiptCounter = 1;

            foreach (var receipt in batchRequest.Receipts)
            {
                if (receipt.CustomerAccount == null || receipt.CustomerAccount.Id == Guid.Empty)
                {
                    results.Add(new
                    {
                        customerAccountId = receipt.CustomerAccount?.Id,
                        success = false,
                        message = "Invalid customer account"
                    });
                    continue;
                }

                try
                {
                    // -------------------- SERVICE HEADER --------------------
                    var serviceHeader = master.GetServiceHeader();

                    // Create transaction model from batch receipt
                    var transactionModel = new CustomerTransactionModel
                    {
                        BranchId = batchRequest.BranchId,
                        TotalValue = receipt.TotalValue,
                        BankAccountId = batchRequest.BankAccountId,
                        PostingPeriodId = batchRequest.PostingPeriodId != Guid.Empty ? batchRequest.PostingPeriodId : receipt.PostingPeriodId,
                        CustomerAccount = receipt.CustomerAccount,
                        CustomerDTO = receipt.CustomerDTO,
                        PrimaryDescription = !string.IsNullOrEmpty(batchRequest.PrimaryDescription) ?
                            batchRequest.PrimaryDescription : receipt.PrimaryDescription,
                        // For batch, we'll generate individual references but include batch prefix
                        Reference = receipt.Reference,
                        ValueDate = receipt.PostedDate

                    };

                    // -------------------- FETCH CUSTOMER ACCOUNT --------------------
                    bool includeBalances = true;
                    bool includeProductDescription = true;
                    bool includeInterestBalanceForLoanAccounts = true;
                    bool considerMaturityPeriodForInvestmentAccounts = true;
                    var selectedCustomerAccount =
                        await master._channelService.FindCustomerAccountAsync(
                            transactionModel.CustomerAccount.Id,
                            includeBalances,
                            includeProductDescription,
                            includeInterestBalanceForLoanAccounts,
                            considerMaturityPeriodForInvestmentAccounts,
                            serviceHeader
                        );

                    if (selectedCustomerAccount == null)
                    {
                        results.Add(new
                        {
                            customerAccountId = transactionModel.CustomerAccount.Id,
                            success = false,
                            message = "Customer account not found"
                        });
                        continue;
                    }

                    if ((RecordStatus)selectedCustomerAccount.RecordStatus != RecordStatus.Approved)
                    {
                        results.Add(new
                        {
                            customerAccountId = transactionModel.CustomerAccount.Id,
                            success = false,
                            message = "Sorry, account is not approved yet"
                        });
                        continue;
                    }

                    // -------------------- ACCOUNT TYPE FLAGS --------------------
                    bool isSavings = selectedCustomerAccount.CustomerAccountTypeProductCode == (int)ProductCode.Savings;
                    bool isLoan = selectedCustomerAccount.CustomerAccountTypeProductCode == (int)ProductCode.Loan;
                    string systemReference;
                    string batchItemReference;
                    if (!isSavings && !isLoan)
                        return Ok(new { success = false, message = "Unsupported account type" });


                    // -------------------- SET CUSTOMER ACCOUNT REFERENCES --------------------
                    transactionModel.CreditCustomerAccount = selectedCustomerAccount;
                    transactionModel.CreditCustomerAccountId = selectedCustomerAccount.Id;
                    transactionModel.DebitCustomerAccount = selectedCustomerAccount;
                    transactionModel.DebitCustomerAccountId = selectedCustomerAccount.Id;

                    // -------------------- FETCH TARGET PRODUCT --------------------

                    if (isSavings)
                    {
                        var savingsProduct =
                            await master._channelService.FindSavingsProductAsync(
                                selectedCustomerAccount.CustomerAccountTypeTargetProductId,
                                serviceHeader
                            );

                        if (savingsProduct == null)
                            return Ok(new { success = false, message = "Savings product not found" });

                        transactionModel.CreditChartOfAccountId = savingsProduct.ChartOfAccountId;

                        // -------------------- FETCH BANK LINKAGE --------------------
                        var selectedBankLinkage =
                            await master._channelService.FindBankLinkageByBankAccountIdAsync(
                                batchRequest.BankAccountId,
                                serviceHeader
                            );

                        if (selectedBankLinkage == null)
                        {
                            results.Add(new
                            {
                                customerAccountId = transactionModel.CustomerAccount.Id,
                                success = false,
                                message = "Bank Account is missing, please select a receiving bank"
                            });
                            continue;
                        }

                        // -------------------- FETCH POSTING PERIOD --------------------
                        var postingPeriod = transactionModel.PostingPeriodId != Guid.Empty
                            ? await master._channelService.FindPostingPeriodAsync(transactionModel.PostingPeriodId, serviceHeader)
                            : await master._channelService.FindCurrentPostingPeriodAsync(serviceHeader);

                        if (postingPeriod == null)
                        {
                            results.Add(new
                            {
                                customerAccountId = transactionModel.CustomerAccount.Id,
                                success = false,
                                message = "Posting period not found"
                            });
                            continue;
                        }

                        // -------------------- POPULATE TRANSACTION MODEL --------------------
                        transactionModel.PostingPeriodId = postingPeriod.Id;

                        if (string.IsNullOrEmpty(transactionModel.PrimaryDescription))
                            transactionModel.PrimaryDescription = "Customer Receipt";

                        if (string.IsNullOrEmpty(transactionModel.SecondaryDescription))
                            transactionModel.SecondaryDescription = $"BC {selectedBankLinkage.BankName}";

                        // GENERATE BATCH REFERENCE NUMBER
                        // For batch processing, we still get a unique system reference but prefix it with batch info
                        systemReference = GenerateThreadSafeReferenceNumber();
                        batchItemReference = $"{batchPrefix}-{receiptCounter.ToString("D3")}";
                        string fullReference = $"{batchItemReference} ({systemReference})";

                        if (!string.IsNullOrEmpty(transactionModel.Reference))
                        {
                            transactionModel.Reference = $"{fullReference} - {transactionModel.Reference}";
                        }
                        else
                        {
                            transactionModel.Reference = fullReference;
                        }

                        transactionModel.DebitChartOfAccountId = selectedBankLinkage.ChartOfAccountId;
                        transactionModel.TransactionCode = (int)SystemTransactionCode.CashDeposit;
                    }
                    #region Else block  commented ut
                    //                    else // isLoan
                    //                    {
                    //                        var loanProduct =
                    //                            await master._channelService.FindLoanProductAsync(
                    //                                selectedCustomerAccount.CustomerAccountTypeTargetProductId,
                    //                                serviceHeader
                    //                            );

                    //                        if (loanProduct == null)
                    //                            return Ok(new { success = false, message = "Loan product not found" });

                    //                        var loanCases = await master._channelService.FindLoanCasesAsync(serviceHeader);


                    //                        var loanCaseDto = loanCases?
                    //                            .FirstOrDefault(lc => lc.LoanProductId == selectedCustomerAccount.CustomerAccountTypeTargetProductId
                    //                                               && lc.CustomerId == selectedCustomerAccount.CustomerId);



                    //                        if (loanCaseDto == null)
                    //                            return Ok(new { success = false, message = "Loan loanCase not found" + "Accounts Found= " + loanCases.Count });



                    //                        Guid Intrestreceivable = loanProduct.InterestReceivableChartOfAccountId;
                    //                        Guid Intrestintrestreceived = loanProduct.InterestReceivedChartOfAccountId;

                    //                        TransactionModel transaction = new TransactionModel();

                    //                        transaction.CreditChartOfAccountId = Intrestintrestreceived;
                    //                        transaction.DebitChartOfAccountId = Intrestreceivable;
                    //                        transaction.ChartOfAccountId = Intrestintrestreceived;

                    //                        decimal monthlyRate = (decimal)loanProduct.LoanInterestAnnualPercentageRate / 100m / 12m;

                    //                        decimal principal = loanCaseDto.AmountApplied; // or ApprovedAmount

                    //                        decimal interest = principal * monthlyRate;

                    //                        // enforce minimum
                    //                        //if (interest < loanProduct.LoanRegistrationMinimumInterestAmount)
                    //                        //    interest = loanProduct.LoanRegistrationMinimumInterestAmount;

                    //                        // money rounding at posting boundary
                    //                        interest = Math.Round(interest, 2, MidpointRounding.AwayFromZero);

                    //                        // remaining after interest deduction from receipt
                    //                        decimal remainingAmount = receipt.TotalValue - interest;

                    //                        if (remainingAmount < 0 && remainingAmount > loanProduct.LoanRegistrationMinimumInterestAmount)
                    //                            throw new InvalidOperationException("Receipt amount is less than calculated interest.");


                    //                        transaction.CreditAmount = interest;
                    //                        transaction.DebitAmount = interest;
                    //                        transaction.TotalValue = interest;


                    //                        transaction.BranchId = transactionModel.BranchId;
                    //                        transaction.PostingPeriodId = transactionModel.PostingPeriodId;
                    //                        transaction.JournalType = (int)JournalVoucherType.CreditGLAccount;
                    //                        transaction.PrimaryDescription = "Intrest Charged";
                    //                        transaction.SecondaryDescription = loanCaseDto.CustomerIndividualFirstName + " " + loanCaseDto.CustomerIndividualLastName;
                    //                        transactionModel.Reference = loanProduct.Description + " " + loanCaseDto.AmountApplied;
                    //                        systemReference = GenerateThreadSafeReferenceNumber();
                    //                        batchItemReference = $"{batchPrefix}-{receiptCounter.ToString("D3")}";
                    //                        string fullReference = $"{batchItemReference} ({systemReference})";

                    //                        if (!string.IsNullOrEmpty(transactionModel.Reference))
                    //                        {
                    //                            transactionModel.Reference = $"{fullReference} - {transactionModel.Reference}";
                    //                        }
                    //                        else
                    //                        {
                    //                            transactionModel.Reference = fullReference;
                    //                        }


                    //                        TariffWrapper tariff = new TariffWrapper();
                    //                        tariff.CreditGLAccountId = Intrestintrestreceived;
                    //                        tariff.Amount = interest;
                    //                        ObservableCollection<TariffWrapper> tariffWrappers = new ObservableCollection<TariffWrapper>();
                    //                        tariffWrappers.Add(tariff);

                    //                        var journalDTO = await master._channelService.AddJournalSingleEntryAsync(transaction, tariffWrappers, serviceHeader);
                    //                        decimal principalBase = loanCaseDto.DisbursedAmount > 0
                    //    ? loanCaseDto.DisbursedAmount
                    //    : loanCaseDto.ApprovedAmount;

                    //                        decimal loanBalance = loanCaseDto.TotalLoansBalance - (interest + remainingAmount);
                    //                        decimal remainingPayback = loanCaseDto.TotalPaybackAmount - receipt.TotalValue;

                    //                        using (SqlConnection conn = new SqlConnection(_connectionString))
                    //                        {
                    //                            conn.Open();

                    //                            using (SqlTransaction tx = conn.BeginTransaction())
                    //                            using (SqlCommand cmd = new SqlCommand(@"
                    //UPDATE swiftFin_LoanCases
                    //SET
                    //    TotalLoansBalance     = @LoanBalance,
                    //    TotalPaybackAmount    = @RemainingPayback,
                    //    MonthlyPaybackAmount  = @MonthlyPaybackAmount
                    //WHERE Id = @Id
                    //", conn, tx))
                    //                            {
                    //                                var pBal = cmd.Parameters.Add("@LoanBalance", SqlDbType.Decimal);
                    //                                pBal.Precision = 18; pBal.Scale = 2; pBal.Value = loanBalance;

                    //                                var pPay = cmd.Parameters.Add("@RemainingPayback", SqlDbType.Decimal);
                    //                                pPay.Precision = 18; pPay.Scale = 2; pPay.Value = remainingPayback;

                    //                                var pMon = cmd.Parameters.Add("@MonthlyPaybackAmount", SqlDbType.Decimal);
                    //                                pMon.Precision = 18; pMon.Scale = 2; pMon.Value = loanCaseDto.MonthlyPaybackAmount;

                    //                                cmd.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = loanCaseDto.Id;

                    //                                int rows = cmd.ExecuteNonQuery();
                    //                                if (rows != 1)
                    //                                {
                    //                                    tx.Rollback();
                    //                                    throw new Exception("Loan financials update failed.");
                    //                                }

                    //                                tx.Commit();
                    //                            }
                    //                        }


                    //                        if (journalDTO.HasErrors)
                    //                        {
                    //                            results.Add(new
                    //                            {
                    //                                success = false,
                    //                                message = "Invalid Accounts"
                    //                            });
                    //                            break;
                    //                        }

                    //                        transactionModel.TotalValue = remainingAmount;

                    //                        transactionModel.CreditChartOfAccountId = loanProduct.ChartOfAccountId;


                    //                        // -------------------- FETCH BANK LINKAGE --------------------
                    //                        var selectedBankLinkage =
                    //                            await master._channelService.FindBankLinkageByBankAccountIdAsync(
                    //                                batchRequest.BankAccountId,
                    //                                serviceHeader
                    //                            );

                    //                        if (selectedBankLinkage == null)
                    //                        {
                    //                            results.Add(new
                    //                            {
                    //                                customerAccountId = transactionModel.CustomerAccount.Id,
                    //                                success = false,
                    //                                message = "Bank Account is missing, please select a receiving bank"
                    //                            });
                    //                            continue;
                    //                        }

                    //                        // -------------------- FETCH POSTING PERIOD --------------------
                    //                        var postingPeriod = transactionModel.PostingPeriodId != Guid.Empty
                    //                            ? await master._channelService.FindPostingPeriodAsync(transactionModel.PostingPeriodId, serviceHeader)
                    //                            : await master._channelService.FindCurrentPostingPeriodAsync(serviceHeader);

                    //                        if (postingPeriod == null)
                    //                        {
                    //                            results.Add(new
                    //                            {
                    //                                customerAccountId = transactionModel.CustomerAccount.Id,
                    //                                success = false,
                    //                                message = "Posting period not found"
                    //                            });
                    //                            continue;
                    //                        }

                    //                        // -------------------- POPULATE TRANSACTION MODEL --------------------
                    //                        transactionModel.PostingPeriodId = postingPeriod.Id;

                    //                        if (string.IsNullOrEmpty(transactionModel.PrimaryDescription))
                    //                            transactionModel.PrimaryDescription = "Loan Repayment";

                    //                        if (string.IsNullOrEmpty(transactionModel.SecondaryDescription))
                    //                            transactionModel.SecondaryDescription = $"BC {selectedBankLinkage.BankName}";

                    //                        // GENERATE BATCH REFERENCE NUMBER
                    //                        // For batch processing, we still get a unique system reference but prefix it with batch info
                    //                        systemReference = GenerateThreadSafeReferenceNumber();
                    //                        batchItemReference = $"{batchPrefix}-{receiptCounter.ToString("D3")}";
                    //                        fullReference = $"{batchItemReference} ({systemReference})";

                    //                        if (!string.IsNullOrEmpty(transactionModel.Reference))
                    //                        {
                    //                            transactionModel.Reference = $"{fullReference} - {transactionModel.Reference}";
                    //                        }
                    //                        else
                    //                        {
                    //                            transactionModel.Reference = fullReference;
                    //                        }

                    //                        transactionModel.DebitChartOfAccountId = selectedBankLinkage.ChartOfAccountId;
                    //                        transactionModel.TransactionCode = (int)SystemTransactionCode.CashDeposit;

                    //                    }
                    #endregion

                    else // isLoan
                    {
                        // ================== LOAN PRODUCT ==================
                        var loanProduct =
                            await master._channelService.FindLoanProductAsync(
                                selectedCustomerAccount.CustomerAccountTypeTargetProductId,
                                serviceHeader);

                        if (loanProduct == null)
                            return Ok(new { success = false, message = "Loan product not found" });

                        // ================== LOAN CASE ==================
                        var loanCases = await master._channelService.FindLoanCasesAsync(serviceHeader);

                        var loanCaseDto = loanCases?
                            .FirstOrDefault(lc =>
                                lc.CustomerId == selectedCustomerAccount.CustomerId &&
                                lc.LoanProductId == selectedCustomerAccount.CustomerAccountTypeTargetProductId &&
                                lc.AmountApplied > 0);

                        if (loanCaseDto == null)
                            return Ok(new { success = false, message = "Active loan case not found" });

                        // ================== BANK + POSTING PERIOD ==================
                        var selectedBankLinkage =
                            await master._channelService.FindBankLinkageByBankAccountIdAsync(
                                batchRequest.BankAccountId, serviceHeader);

                        if (selectedBankLinkage == null)
                        {
                            results.Add(new
                            {
                                customerAccountId = selectedCustomerAccount.Id,
                                success = false,
                                message = "Receiving bank account not configured"
                            });
                            continue;
                        }

                        var postingPeriod =
                            transactionModel.PostingPeriodId != Guid.Empty
                                ? await master._channelService.FindPostingPeriodAsync(
                                    transactionModel.PostingPeriodId, serviceHeader)
                                : await master._channelService.FindCurrentPostingPeriodAsync(serviceHeader);

                        if (postingPeriod == null)
                        {
                            results.Add(new
                            {
                                customerAccountId = selectedCustomerAccount.Id,
                                success = false,
                                message = "Posting period not found"
                            });
                            continue;
                        }

                        // ================== OPENING BALANCE ==================
                        decimal principalBalance =
                            loanCaseDto.TotalLoansBalance;


                        // ================== RATE ==================
                        decimal monthlyRate =
                            (decimal)loanProduct.LoanInterestAnnualPercentageRate / 100m / 12m;

                        // ================== REMAINING TERM ==================
                        // Ideally: original term minus paid installments
                        int remainingTermMonths = loanProduct.LoanRegistrationTermInMonths;

                        // ================== EMI (SCHEDULED PAYMENT) ==================
                        decimal scheduledPayment;

                        //if (monthlyRate == 0)
                        //{
                        //    scheduledPayment = principalBalance / remainingTermMonths;
                        //}
                        //else


                        decimal rPow = (decimal)Math.Pow(1 + (double)monthlyRate, remainingTermMonths);
                        //scheduledPayment = principalBalance * monthlyRate * rPow / (rPow - 1);
                        scheduledPayment = loanCaseDto.AmountApplied * monthlyRate * rPow / (rPow - 1);


                        scheduledPayment = Math.Round(scheduledPayment, 2, MidpointRounding.AwayFromZero);

                        // ================== INTEREST FOR PERIOD ==================
                        decimal interestPortion =
                            Math.Round(principalBalance * monthlyRate, 2, MidpointRounding.AwayFromZero);

                        // ================== PRINCIPAL (FROM SCHEDULE, NOT RECEIPT) ==================
                        decimal principalPortion = scheduledPayment - interestPortion;

                        // ================== CASH RECONCILIATION ==================
                        if (receipt.TotalValue < interestPortion)
                            throw new InvalidOperationException("Receipt does not cover interest due.");

                        // prepayment handling
                        decimal extra = receipt.TotalValue - scheduledPayment;
                        if (extra > 0)
                        {
                            principalPortion += extra; // reduce balance faster
                        }

                        // ================== NEW BALANCE ==================
                        decimal newBalance = principalBalance - principalPortion;

                        // ==========================================================
                        // 1A) INTEREST: CLEAR RECEIVABLE (DR BANK / CR INT RECEIVED)
                        // ==========================================================

                        var periodTransactions = await master._channelService.FindGeneralLedgerTransactionsByDateRangeAndFilterInPageAsync(int.MaxValue,int.MaxValue,postingPeriod.DurationStartDate,postingPeriod.DurationEndDate,"",(int)JournalEntryFilter.JournalSecondaryDescription,serviceHeader);

                        var now = DateTime.UtcNow;

                        // Check if loan was disbursed this month
                        bool isFirstMonth = loanCaseDto.DisbursedDate.HasValue &&
                                            loanCaseDto.DisbursedDate.Value.Year == now.Year &&
                                            loanCaseDto.DisbursedDate.Value.Month == now.Month;

                        // Check if interest was already posted this month
                        bool interestAlreadyCharged = periodTransactions.PageCollection
                            .Where(t => t.GLAccountId == loanProduct.InterestReceivedChartOfAccountId)
                            .Any(t =>
                                receipt.PostedDate.HasValue &&
                                receipt.PostedDate.Value.Year == now.Year &&
                                receipt.PostedDate.Value.Month == now.Month &&
                                t.JournalPrimaryDescription?.IndexOf("Loan Interest Payment", StringComparison.OrdinalIgnoreCase) >= 0
                            );

                        // PAYMENT ALLOCATION
                        if (isFirstMonth || interestAlreadyCharged)
                        {
                            // Skip interest, entire payment goes to principal
                            principalPortion = receipt.TotalValue;
                            interestPortion = 0;
                        }
                        else
                        {
                            interestPortion = Math.Round(principalBalance * monthlyRate, 2, MidpointRounding.AwayFromZero);
                            principalPortion = receipt.TotalValue - interestPortion;

                            if (principalPortion < 0)
                                throw new InvalidOperationException("Receipt does not cover interest due.");
                        }

                        // INTEREST POSTING (ONLY IF NOT SKIPPING)
                        if (!(isFirstMonth || interestAlreadyCharged) && interestPortion > 0)
                        {
                            var interestTxn = new CustomerTransactionModel
                            {
                                BranchId = transactionModel.BranchId,
                                PostingPeriodId = postingPeriod.Id,
                                DebitChartOfAccountId = selectedBankLinkage.ChartOfAccountId,
                                CreditChartOfAccountId = loanProduct.InterestReceivedChartOfAccountId,
                                DebitCustomerAccountId = selectedCustomerAccount.Id,
                                CreditCustomerAccountId = selectedCustomerAccount.Id,
                                DebitCustomerAccount = selectedCustomerAccount,
                                CreditCustomerAccount = selectedCustomerAccount,
                                TotalValue = interestPortion,
                                PrimaryDescription = "Loan Interest Payment",
                                SecondaryDescription = "Normal Loan Repayment Interest",
                                Reference = loanCaseDto.CaseNumber.ToString(),
                                TransactionCode = (int)SystemTransactionCode.CashDeposit
                            };

                            var interestJournal = await master._channelService.AddJournalWithCustomerAccountAsync(
                                interestTxn, serviceHeader);

                            if (interestJournal.HasErrors)
                                throw new Exception("Interest posting failed");
                        }



                        //var clrJournal =
                        //    await master._channelService.AddJournalWithCustomerAccountAsync(
                        //        clearInterestReceivableTxn, serviceHeader);

                        //if (clrJournal.HasErrors)
                        //    throw new Exception("Interest receivable clearance failed");


                        // ==========================================================
                        // 2) PRINCIPAL REPAYMENT (DR BANK / CR LOAN CONTROL)
                        // ==========================================================
                        var principalTxn = new CustomerTransactionModel
                        {
                            BranchId = transactionModel.BranchId,
                            PostingPeriodId = postingPeriod.Id,

                            DebitChartOfAccountId = selectedBankLinkage.ChartOfAccountId, // Bank
                            CreditChartOfAccountId = loanProduct.ChartOfAccountId, // Loan Control (Asset)

                            DebitCustomerAccountId = selectedCustomerAccount.Id,
                            CustomerAccount = selectedCustomerAccount,
                            CreditCustomerAccount = selectedCustomerAccount,
                            DebitCustomerAccount = selectedCustomerAccount,
                            CreditCustomerAccountId = selectedCustomerAccount.Id,
                            TotalValue = principalPortion,
                            PrimaryDescription = "Loan Principal Repayment",
                            SecondaryDescription = "Normal Loan Repayment",
                            Reference = loanCaseDto.CaseNumber.ToString(),
                            TransactionCode = (int)SystemTransactionCode.BackOfficeCashReceipt
                        };

                        var principalJournal =
                            await master._channelService.AddJournalWithCustomerAccountAsync(
                                principalTxn, serviceHeader);

                        if (principalJournal.HasErrors)
                            throw new Exception("Principal posting failed");

                        // ================== UPDATE LOAN CASE ==================
                        decimal newLoanBalance = principalBalance - principalPortion;
                        decimal remainingPayback = loanCaseDto.TotalPaybackAmount - receipt.TotalValue;


                        // -------------------- UPDATE CUSTOMER BALANCE --------------------
                        selectedCustomerAccount.NewAvailableBalance = selectedCustomerAccount.AvailableBalance + principalPortion;
                        loanCaseDto.TotalLoansBalance = newBalance;
                        loanCaseDto.TotalPaybackAmount = remainingPayback;

                        var loancase = await master._channelService.UpdateLoanCaseAsync(loanCaseDto, serviceHeader);
                        var updateloanAccount =
                            await master._channelService.UpdateCustomerAccountAsync(
                                selectedCustomerAccount,
                                serviceHeader
                            );

                        if (!updateloanAccount)
                        {
                            results.Add(new
                            {
                                customerAccountId = transactionModel.CustomerAccount.Id,
                                success = false,
                                message = "Sorry, but the authorized Transaction could not be posted!"
                            });
                            continue;
                        }



                        return Ok(new
                        {
                            customerAccountId = transactionModel.CustomerAccount.Id,
                            success = true,
                            message = $"Operation success: Customer's new balance is {selectedCustomerAccount.BookBalance}",
                            systemReference = principalJournal.Reference,
                            batchReference = "",
                            journal = new
                            {
                                id = principalJournal.Id,
                                sequentialId = principalJournal.SequentialId,
                                branchDescription = principalJournal.BranchDescription,
                                primaryDescription = principalJournal.PrimaryDescription,
                                secondaryDescription = principalJournal.SecondaryDescription,
                                postingPeriodDescription = principalJournal.PostingPeriodDescription,
                                applicationUserName = principalJournal.ApplicationUserName,
                                createdDate = principalJournal.CreatedDate,
                                totalValue = principalJournal.TotalValue,
                                reference = principalJournal.Reference
                            }
                        });

                    }

                    // -------------------- VALIDATE BUSINESS RULES --------------------
                    transactionModel.ValidateAll();

                    if (transactionModel.HasErrors)
                    {
                        var combinedErrors = string.Join("; ", transactionModel.ErrorMessages);
                        results.Add(new
                        {
                            customerAccountId = transactionModel.CustomerAccount.Id,
                            success = false,
                            message = $"Transaction Error: {combinedErrors}"
                        });
                        continue;
                    }

                    // -------------------- POST TRANSACTION --------------------
                    var journal =
                        await master._channelService.AddJournalWithCustomerAccountAsync(
                            transactionModel,
                            serviceHeader
                        );

                    // -------------------- UPDATE CUSTOMER BALANCE --------------------
                    selectedCustomerAccount.NewAvailableBalance =
                        selectedCustomerAccount.AvailableBalance + transactionModel.TotalValue;

                    var updateResult =
                        await master._channelService.UpdateCustomerAccountAsync(
                            selectedCustomerAccount,
                            serviceHeader
                        );

                    if (!updateResult)
                    {
                        results.Add(new
                        {
                            customerAccountId = transactionModel.CustomerAccount.Id,
                            success = false,
                            message = "Sorry, but the authorized Transaction could not be posted!"
                        });
                        continue;
                    }

                    // -------------------- SUCCESS RESPONSE --------------------
                    successfulTransactions++;
                    totalAmount += transactionModel.TotalValue;

                    results.Add(new
                    {
                        customerAccountId = transactionModel.CustomerAccount.Id,
                        success = true,
                        message = $"Operation success: Customer's new balance is {selectedCustomerAccount.NewAvailableBalance}",
                        systemReference = systemReference,
                        batchReference = batchItemReference,
                        journal = new
                        {
                            id = journal.Id,
                            sequentialId = journal.SequentialId,
                            branchDescription = journal.BranchDescription,
                            primaryDescription = journal.PrimaryDescription,
                            secondaryDescription = journal.SecondaryDescription,
                            postingPeriodDescription = journal.PostingPeriodDescription,
                            applicationUserName = journal.ApplicationUserName,
                            createdDate = journal.CreatedDate,
                            totalValue = journal.TotalValue,
                            reference = journal.Reference
                        }
                    });

                    receiptCounter++;
                }
                catch (Exception ex)
                {
                    results.Add(new
                    {
                        customerAccountId = receipt.CustomerAccount?.Id,
                        success = false,
                        message = $"Error processing receipt: {ex.Message}"
                    });
                }
            }

            // -------------------- BATCH SUMMARY --------------------
            return Ok(new
            {
                success = successfulTransactions > 0,
                summary = new
                {
                    totalReceipts = batchRequest.Receipts.Count,
                    successful = successfulTransactions,
                    failed = batchRequest.Receipts.Count - successfulTransactions,
                    totalAmount = totalAmount,
                    batchReference = batchPrefix
                },
                results = results
            });
        }


        [HttpGet]
        [Route("GeneralLedgerTransactions")]
        public async Task<IHttpActionResult> GetGeneralLedgerTransactions(Guid chartOfAccountId, int pageIndex, int pageSize)
        {

            bool tallyDebitsCredits = true;
            int transactionDateFilter = 1;
            int journalEntryFilter = 0;
            string textFilter = "";
            //int pageIndex = pageIndex;
            //int pageSize = pageSize;
            try
            {
                var serviceHeader = master.GetServiceHeader();

                var effectiveStartDate = new DateTime(1900, 1, 1);

                var effectiveEndDate = DateTime.Today;

                var result = await master._channelService
                    .FindGeneralLedgerTransactionsByChartOfAccountIdAndDateRangeAndFilterInPageAsync(
                        pageIndex,
                        pageSize,
                        chartOfAccountId,
                        (DateTime)effectiveStartDate,
                        (DateTime)effectiveEndDate,
                        textFilter,
                        journalEntryFilter,
                        transactionDateFilter,
                        tallyDebitsCredits,
                        serviceHeader);
                result.PageIndex = pageIndex;
                result.PageSize = pageSize;
                result.TotalPages = (int)Math.Ceiling((double)result.ItemsCount / pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpPost]
        [Route("PostCustomerReceipt")]
        public async Task<IHttpActionResult> Create(CustomerTransactionModel transactionModel)
        {
            try
            {
                // -------------------- SERVICE HEADER --------------------
                var serviceHeader = master.GetServiceHeader();

                // -------------------- VALIDATE REQUEST --------------------
                if (transactionModel == null)
                    return Ok(new { success = false, message = "Invalid transaction request" });

                if (transactionModel.CustomerAccount == null || transactionModel.CustomerAccount.Id == Guid.Empty)
                    return Ok(new { success = false, message = "Please select a customer account" });

                if (transactionModel.BankAccountId == Guid.Empty)
                    return Ok(new { success = false, message = "Please select a receiving bank account" });

                // -------------------- FETCH CUSTOMER ACCOUNT --------------------
                bool includeBalances = true;
                bool includeProductDescription = true;
                bool includeInterestBalanceForLoanAccounts = true;
                bool considerMaturityPeriodForInvestmentAccounts = true;
                transactionModel.CustomerAccount.Id = new Guid("68968568-0EEC-F011-BDCF-901B0ECBCFB5");

                var selectedCustomerAccount =
                    await master._channelService.FindCustomerAccountAsync(
                        transactionModel.CustomerAccount.Id,
                        includeBalances,
                        includeProductDescription,
                        includeInterestBalanceForLoanAccounts,
                        considerMaturityPeriodForInvestmentAccounts,
                        serviceHeader
                    );

                if (selectedCustomerAccount == null)
                    return Ok(new { success = false, message = "Customer account not found" });

                if ((RecordStatus)selectedCustomerAccount.RecordStatus != RecordStatus.Approved)
                    return Ok(new { success = false, message = "Sorry, account is not approved yet" });

                // -------------------- SET CUSTOMER ACCOUNT REFERENCES --------------------
                transactionModel.CreditCustomerAccount = selectedCustomerAccount;
                transactionModel.CreditCustomerAccountId = selectedCustomerAccount.Id;

                transactionModel.DebitCustomerAccount = selectedCustomerAccount;


                transactionModel.DebitCustomerAccountId = selectedCustomerAccount.Id;


                var targetProduct = await master._channelService.FindSavingsProductAsync(
                    selectedCustomerAccount.CustomerAccountTypeTargetProductId,
                    serviceHeader
                );

                if (targetProduct == null)
                    return Ok(new { success = false, message = "Customer account product not found" });

                transactionModel.CreditChartOfAccountId = targetProduct.ChartOfAccountId;

                // -------------------- FETCH BANK LINKAGE --------------------
                Guid selectedBankId = transactionModel.BankAccountId;

                var selectedBankLinkage =
                    await master._channelService.FindBankLinkageByBankAccountIdAsync(
                        selectedBankId,
                        serviceHeader
                    );

                if (selectedBankLinkage == null)
                    return Ok(new { success = false, message = "Bank Account is missing, please select a receiving bank" });

                // -------------------- FETCH POSTING PERIOD --------------------
                // Use provided postingPeriodId if available, otherwise get current
                var postingPeriod = transactionModel.PostingPeriodId != Guid.Empty
                    ? await master._channelService.FindPostingPeriodAsync(transactionModel.PostingPeriodId, serviceHeader)
                    : await master._channelService.FindCurrentPostingPeriodAsync(serviceHeader);

                if (postingPeriod == null)
                    return Ok(new { success = false, message = "Posting period not found" });

                // -------------------- POPULATE TRANSACTION MODEL --------------------
                transactionModel.PostingPeriodId = postingPeriod.Id;

                // Only set if not already provided
                if (string.IsNullOrEmpty(transactionModel.PrimaryDescription))
                    transactionModel.PrimaryDescription = "Customer Receipt";

                if (string.IsNullOrEmpty(transactionModel.SecondaryDescription))
                    transactionModel.SecondaryDescription = $"BC {selectedBankLinkage.BankName}";

                if (string.IsNullOrEmpty(transactionModel.Reference))
                    transactionModel.Reference = selectedCustomerAccount.CustomerReference1;

                transactionModel.DebitChartOfAccountId = selectedBankLinkage.ChartOfAccountId;
                transactionModel.TransactionCode = (int)SystemTransactionCode.CashDeposit;

                // -------------------- VALIDATE BUSINESS RULES --------------------
                transactionModel.ValidateAll();

                if (transactionModel.HasErrors)
                {
                    var combinedErrors = string.Join("; ", transactionModel.ErrorMessages);
                    return Ok(new { success = false, message = $"Transaction Error: {combinedErrors}" });
                }

                // -------------------- POST TRANSACTION --------------------
                var journal =
                    await master._channelService.AddJournalWithCustomerAccountAsync(
                        transactionModel,
                        serviceHeader
                    );

                // -------------------- UPDATE CUSTOMER BALANCE --------------------
                selectedCustomerAccount.NewAvailableBalance =
                    selectedCustomerAccount.AvailableBalance + transactionModel.TotalValue;

                var updateResult =
                    await master._channelService.UpdateCustomerAccountAsync(
                        selectedCustomerAccount,
                        serviceHeader
                    );

                if (!updateResult)
                {
                    return Ok(new
                    {
                        success = false,
                        message = "Sorry, but the authorized cash deposit could not be posted!"
                    });
                }

                // -------------------- SUCCESS RESPONSE --------------------
                return Ok(new
                {
                    success = true,
                    message = $"Operation success: Customer's new balance is {selectedCustomerAccount.NewAvailableBalance}",
                    journal = new
                    {
                        id = journal.Id,
                        sequentialId = journal.SequentialId,
                        branchDescription = journal.BranchDescription,
                        primaryDescription = journal.PrimaryDescription,
                        secondaryDescription = journal.SecondaryDescription,
                        postingPeriodDescription = journal.PostingPeriodDescription,
                        applicationUserName = journal.ApplicationUserName,
                        createdDate = journal.CreatedDate,
                        totalValue = journal.TotalValue,
                        reference = journal.Reference
                    }
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }




        FrontOfficeTransactionType _frontOfficeTransactionType;
        public FrontOfficeTransactionType FrontOfficeTransactionType
        {
            get { return _frontOfficeTransactionType; }
            set
            {
                if (_frontOfficeTransactionType != value)
                {
                    _frontOfficeTransactionType = value;

                }
            }
        }


        [HttpPost]
        [Route("PostingPeriods")]
        public async Task<IHttpActionResult> CreatePostingPeriod([FromBody] PostingPeriodDTO postingPeriodDTO)
        {
            try
            {
                var serviceHeader = master.GetServiceHeader();

                // Set default values
                postingPeriodDTO.Id = Guid.NewGuid();
                postingPeriodDTO.IsLocked = false;
                postingPeriodDTO.IsClosed = false;
                postingPeriodDTO.ClosedDate = null;
                postingPeriodDTO.ClosedBy = null;
                postingPeriodDTO.CreatedDate = DateTime.Now;

                // Validate the DTO using the built-in validation
                postingPeriodDTO.ValidateAll();

                if (postingPeriodDTO.HasErrors)
                {
                    return Json(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Validation failed for posting period.",
                        Data = postingPeriodDTO.ErrorMessages
                    });
                }

                // Check for overlapping periods
                var existingPeriods = await master._channelService.FindPostingPeriodsAsync(serviceHeader);
                if (existingPeriods != null)
                {
                    var hasOverlap = existingPeriods.Any(p =>
                        !p.IsClosed &&
                        (postingPeriodDTO.DurationStartDate <= p.DurationEndDate &&
                         postingPeriodDTO.DurationEndDate >= p.DurationStartDate));

                    if (hasOverlap)
                    {
                        return Json(new ApiResponse<object>
                        {
                            Success = false,
                            Message = "Posting period overlaps with an existing active period.",
                            Data = null
                        });
                    }
                }

                var result = await master._channelService.AddPostingPeriodAsync(postingPeriodDTO, serviceHeader);

                if (result.ErrorMessageResult != null)
                {
                    return Json(new ApiResponse<object>
                    {
                        Success = false,
                        Message = (string)result.ErrorMessageResult,
                        Data = null
                    });
                }

                return Json(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Posting period created successfully.",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while creating posting period.",
                    Data = ex.Message
                });
            }
        }



        // Add these controllers inside your ValuesController class

        // CREATE GENERAL LEDGER CONTROLLERS

        /// <summary>
        /// Creates a new general ledger with entries
        /// </summary>
        [HttpPost]
        [Route("GeneralLedgers")]
        public async Task<IHttpActionResult> CreateGeneralLedger([FromBody] GeneralLedgerDTO generalLedgerDTO)
        {
            try
            {
                var serviceHeader = master.GetServiceHeader();

                // Set default values
                generalLedgerDTO.Id = Guid.NewGuid();
                generalLedgerDTO.CreatedDate = DateTime.Now;
                generalLedgerDTO.Status = (int)GeneralLedgerStatus.Pending; // Using Pending = 1
                generalLedgerDTO.LedgerNumber = 0; // Will be auto-generated by system

                // Validate the DTO
                generalLedgerDTO.ValidateAll();

                if (generalLedgerDTO.HasErrors)
                {
                    return Json(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Validation failed for general ledger.",
                        Data = generalLedgerDTO.ErrorMessages
                    });
                }

                // Check if entries are provided and balanced
                if (generalLedgerDTO.GeneralLedgerEntries != null && generalLedgerDTO.GeneralLedgerEntries.Any())
                {
                    decimal totalAmount = 0;

                    foreach (var entry in generalLedgerDTO.GeneralLedgerEntries)
                    {
                        // Validate each entry
                        entry.GeneralLedgerId = generalLedgerDTO.Id;
                        entry.BranchId = generalLedgerDTO.BranchId;
                        entry.CreatedDate = DateTime.Now;
                        entry.Status = (int)GeneralLedgerEntryStatus.Pending;

                        entry.ValidateAll();

                        if (entry.HasErrors)
                        {
                            return Json(new ApiResponse<object>
                            {
                                Success = false,
                                Message = $"Entry validation failed: {string.Join(", ", entry.ErrorMessages)}",
                                Data = null
                            });
                        }

                        totalAmount += Math.Abs(entry.Amount);
                    }

                    // Set total value from entries (divided by 2 because double-entry)
                    generalLedgerDTO.TotalValue = totalAmount / 2;
                }

                // Create the general ledger
                var result = await master._channelService.AddGeneralLedgerAsync(generalLedgerDTO, serviceHeader);

                if (result.ErrorMessageResult != null)
                {
                    return Json(new ApiResponse<object>
                    {
                        Success = false,
                        Message = (string)result.ErrorMessageResult,
                        Data = null
                    });
                }

                // Add entries if provided
                if (generalLedgerDTO.GeneralLedgerEntries != null && generalLedgerDTO.GeneralLedgerEntries.Any())
                {
                    var entriesAdded = await master._channelService.UpdateGeneralLedgerEntryCollectionAsync(
                        result.Id,
                        generalLedgerDTO.GeneralLedgerEntries,
                        serviceHeader
                    );

                    if (!entriesAdded)
                    {
                        return Json(new ApiResponse<object>
                        {
                            Success = false,
                            Message = "General ledger created but failed to add entries.",
                            Data = result
                        });
                    }
                }

                return Json(new ApiResponse<object>
                {
                    Success = true,
                    Message = "General ledger created successfully with entries.",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while creating general ledger.",
                    Data = ex.Message
                });
            }
        }

        /// <summary>
        /// Creates a new general ledger entry for an existing ledger
        /// </summary>
        [HttpPost]
        [Route("GeneralLedgerEntries")]
        public async Task<IHttpActionResult> CreateGeneralLedgerEntry([FromBody] GeneralLedgerEntryDTO generalLedgerEntryDTO)
        {
            try
            {
                var serviceHeader = master.GetServiceHeader();

                // Set default values
                generalLedgerEntryDTO.Id = Guid.NewGuid();
                generalLedgerEntryDTO.CreatedDate = DateTime.Now;
                generalLedgerEntryDTO.Status = (int)GeneralLedgerEntryStatus.Pending;

                // Validate the DTO
                generalLedgerEntryDTO.ValidateAll();

                if (generalLedgerEntryDTO.HasErrors)
                {
                    return Json(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Validation failed for general ledger entry.",
                        Data = generalLedgerEntryDTO.ErrorMessages
                    });
                }

                // For double-entry (G/L to G/L), both accounts must be specified
                if (generalLedgerEntryDTO.ChartOfAccountId != Guid.Empty &&
                    generalLedgerEntryDTO.ContraChartOfAccountId != Guid.Empty)
                {
                    // This is a self-balanced entry
                    if (generalLedgerEntryDTO.Amount == 0)
                    {
                        return Json(new ApiResponse<object>
                        {
                            Success = false,
                            Message = "Amount must not be zero for double-entry transaction.",
                            Data = null
                        });
                    }
                }
                // For customer account entries, validate accordingly
                else if (generalLedgerEntryDTO.CustomerAccountId.HasValue ||
                         generalLedgerEntryDTO.ContraCustomerAccountId.HasValue)
                {
                    // Customer account entry logic
                    if (generalLedgerEntryDTO.Amount == 0)
                    {
                        return Json(new ApiResponse<object>
                        {
                            Success = false,
                            Message = "Amount must not be zero for customer account transaction.",
                            Data = null
                        });
                    }
                }
                else
                {
                    return Json(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Either G/L accounts or Customer accounts must be specified.",
                        Data = null
                    });
                }

                var result = await master._channelService.AddGeneralLedgerEntryAsync(generalLedgerEntryDTO, serviceHeader);

                if (result.ErrorMessageResult != null)
                {
                    return Json(new ApiResponse<object>
                    {
                        Success = false,
                        Message = (string)result.ErrorMessageResult,
                        Data = null
                    });
                }

                return Json(new ApiResponse<object>
                {
                    Success = true,
                    Message = "General ledger entry created successfully.",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while creating general ledger entry.",
                    Data = ex.Message
                });
            }
        }

        /// <summary>
        /// Creates a complete general ledger with balanced entries in one request
        /// </summary>
        [HttpPost]
        [Route("GeneralLedgers/Complete")]
        public async Task<IHttpActionResult> CreateCompleteGeneralLedger([FromBody] CreateCompleteGeneralLedgerRequest request)
        {
            try
            {
                var serviceHeader = master.GetServiceHeader();

                if (request == null || request.Entries == null || !request.Entries.Any())
                {
                    return Json(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "No ledger data provided.",
                        Data = null
                    });
                }

                // Create the general ledger header
                var generalLedgerDTO = new GeneralLedgerDTO
                {
                    Id = Guid.NewGuid(),
                    BranchId = request.BranchId,
                    PostingPeriodId = request.PostingPeriodId,
                    Remarks = request.Remarks,
                    CreatedDate = DateTime.Now,
                    Status = (int)GeneralLedgerStatus.Pending,
                    LedgerNumber = 0,
                    GeneralLedgerEntries = new ObservableCollection<GeneralLedgerEntryDTO>()
                };

                // Validate and prepare entries
                decimal totalDebits = 0;
                decimal totalCredits = 0;

                foreach (var entry in request.Entries)
                {
                    entry.Id = Guid.NewGuid();
                    entry.GeneralLedgerId = generalLedgerDTO.Id;
                    entry.BranchId = request.BranchId;
                    entry.CreatedDate = DateTime.Now;
                    entry.Status = (int)GeneralLedgerEntryStatus.Pending;

                    // Validate entry
                    entry.ValidateAll();
                    if (entry.HasErrors)
                    {
                        return Json(new ApiResponse<object>
                        {
                            Success = false,
                            Message = $"Entry validation failed: {string.Join(", ", entry.ErrorMessages)}",
                            Data = null
                        });
                    }

                    // Track debits and credits
                    if (entry.Amount > 0)
                    {
                        totalDebits += entry.Amount;
                    }
                    else
                    {
                        totalCredits += Math.Abs(entry.Amount);
                    }

                    generalLedgerDTO.GeneralLedgerEntries.Add(entry);
                }

                // Check if entries are balanced
                if (Math.Abs(totalDebits - totalCredits) > 0.01m)
                {
                    return Json(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Entries are not balanced. Debits: {totalDebits}, Credits: {totalCredits}",
                        Data = null
                    });
                }

                // Set total value
                generalLedgerDTO.TotalValue = totalDebits;

                // Validate general ledger
                generalLedgerDTO.ValidateAll();
                if (generalLedgerDTO.HasErrors)
                {
                    return Json(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"General ledger validation failed: {string.Join(", ", generalLedgerDTO.ErrorMessages)}",
                        Data = null
                    });
                }

                // Create the general ledger
                var ledgerResult = await master._channelService.AddGeneralLedgerAsync(generalLedgerDTO, serviceHeader);

                if (ledgerResult.ErrorMessageResult != null)
                {
                    return Json(new ApiResponse<object>
                    {
                        Success = false,
                        Message = (string)ledgerResult.ErrorMessageResult,
                        Data = null
                    });
                }

                // Add entries to the ledger
                var entriesAdded = await master._channelService.UpdateGeneralLedgerEntryCollectionAsync(
                    ledgerResult.Id,
                    generalLedgerDTO.GeneralLedgerEntries,
                    serviceHeader
                );

                if (!entriesAdded)
                {
                    return Json(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "General ledger created but failed to add entries.",
                        Data = ledgerResult
                    });
                }

                return Json(new ApiResponse<object>
                {
                    Success = true,
                    Message = $"General ledger created successfully with {request.Entries.Count} entries.",
                    Data = new
                    {
                        Ledger = ledgerResult,
                        TotalDebits = totalDebits,
                        TotalCredits = totalCredits,
                        EntryCount = request.Entries.Count
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while creating complete general ledger.",
                    Data = ex.Message
                });
            }
        }

        // Request models
        public class CreateCompleteGeneralLedgerRequest
        {
            public Guid BranchId { get; set; }
            public Guid PostingPeriodId { get; set; }
            public string Remarks { get; set; }
            public List<GeneralLedgerEntryDTO> Entries { get; set; }
        }


        [HttpGet]
        [Route("GetGeneralLedgerTransactionsByDateRange")]
        public async Task<IHttpActionResult> GetGeneralLedgerTransactionsByDateRange(
    int pageIndex,
    int pageSize,
    DateTime startDate,
    DateTime endDate,
    string text = null,
    int journalEntryFilter = 0)
        {
            try
            {
                var serviceHeader = master.GetServiceHeader();

                var result = await master._channelService.FindGeneralLedgerTransactionsByDateRangeAndFilterInPageAsync(
                    pageIndex,
                    pageSize,
                    startDate,
                    endDate,
                    text,
                    journalEntryFilter,
                    serviceHeader
                );

                if (result == null)
                {
                    return Json(new ApiResponse<object>
                    {
                        Success = true,
                        Message = "No transactions found for the specified date range.",
                        Data = null
                    });
                }

                var sortedData = result.PageCollection?.OrderByDescending(x => x.JournalCreatedDate).ToList();

                return Json(new ApiResponse<object>
                {
                    Success = true,
                    Message = sortedData?.Count > 0 ? $"{sortedData.Count} transactions found." : "No transactions found.",
                    Data = sortedData
                });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while retrieving the transactions.",
                    Data = ex.Message
                });
            }
        }


        [HttpGet]
        [Route("GetMembersWithDetails")]
        public async Task<IHttpActionResult> GetMembersWithDetails(
            [FromUri] int pageIndex = 0,
            [FromUri] int pageSize = 20,
            [FromUri] bool includeAccounts = true,
            [FromUri] bool includeNextOfKin = true,
            [FromUri] bool includeAccountBalances = true,
            [FromUri] bool includeProductDescription = true,
            [FromUri] bool includeInterestBalanceForLoanAccounts = false,
            [FromUri] bool considerMaturityPeriodForInvestmentAccounts = false,
            [FromUri] bool includeStatements = false,
            [FromUri] DateTime? statementStartDate = null,
            [FromUri] DateTime? statementEndDate = null)
        {
            try
            {
                var serviceHeader = master.GetServiceHeader();

                // Get paginated customers
                var customersPage = await master._channelService.FindCustomersInPageAsync(
                    pageIndex,
                    pageSize,
                    serviceHeader
                );

                if (customersPage == null || customersPage.PageCollection == null || !customersPage.PageCollection.Any())
                {
                    return Json(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "No members found.",
                        Data = null
                    });
                }

                int currentPageIndex = Convert.ToInt32(customersPage.PageIndex);
                int currentPageSize = Convert.ToInt32(customersPage.PageSize);
                int totalCount = Convert.ToInt32(customersPage.TotalCount);
                int totalPages = Convert.ToInt32(customersPage.TotalPages);
                int pageCollectionCount = customersPage.PageCollection.Count;

                var membersWithDetails = new List<object>();

                // Process each customer to get their details
                foreach (var customer in customersPage.PageCollection)
                {
                    var memberDetail = new
                    {
                        Customer = new
                        {
                            customer.Id,
                            customer.FullName,
                            customer.SerialNumber,
                            customer.PaddedSerialNumber,
                            customer.Type,
                            customer.TypeDescription,
                            customer.IndividualType,
                            customer.IndividualTypeDescription,
                            customer.IndividualFirstName,
                            customer.IndividualLastName,
                            customer.IndividualIdentityCardNumber,
                            customer.AddressMobileLine,
                            customer.AddressEmail,
                            customer.PersonalIdentificationNumber,
                            customer.Reference1, // Account Number
                            customer.Reference2, // Membership Number
                            customer.Reference3, // Personal File Number
                            customer.BranchDescription,
                            customer.RegistrationDate,
                            customer.RecordStatus,
                            customer.RecordStatusDescription,
                            customer.IsDefaulter,
                            customer.IsLocked,
                            Age = CalculateProperAge(customer),
                            customer.MembershipPeriod,
                            customer.NonIndividualDateEstablished,
                            customer.IndividualBirthDate,
                        },
                        Accounts = new List<object>(),
                        NextOfKin = new List<object>(),
                        Statements = new List<object>()
                    };

                    // Get customer accounts if requested
                    if (includeAccounts && customer.Id != Guid.Empty)
                    {
                        try
                        {
                            var accounts = await master._channelService.FindCustomerAccountsByCustomerIdAsync(
                                customer.Id,
                                includeAccountBalances,
                                includeProductDescription,
                                includeInterestBalanceForLoanAccounts,
                                considerMaturityPeriodForInvestmentAccounts,
                                serviceHeader
                            );

                            if (accounts != null && accounts.Any())
                            {
                                var accountIds = accounts.Select(a => a.Id).ToList();

                                // Get statements for all accounts at once if requested
                                Dictionary<Guid, List<object>> accountStatements = null;
                                if (includeStatements && accountIds.Any())
                                {
                                    accountStatements = await GetAccountStatementsAsync(
                                        accountIds,
                                        statementStartDate,
                                        statementEndDate
                                    );
                                }

                                foreach (var account in accounts)
                                {
                                    memberDetail.Accounts.Add(new
                                    {
                                        account.Id,
                                        account.FullAccountNumber,
                                        account.CustomerAccountTypeTargetProductDescription,
                                        account.CustomerAccountTypeProductCode,
                                        account.CustomerAccountTypeProductCodeDescription,
                                        account.Status,
                                        account.StatusDescription,
                                        account.RecordStatus,
                                        account.RecordStatusDescription,
                                        account.BookBalance,
                                        account.AvailableBalance,
                                        account.PrincipalBalance,
                                        account.InterestBalance,
                                        account.CarryForwardsBalance,
                                        account.PrincipalArrearagesBalance,
                                        account.InterestArrearagesBalance,
                                        account.CreatedDate,
                                        account.Remarks
                                    });

                                    // Add statements for this account if available
                                    if (includeStatements && accountStatements != null &&
                                        accountStatements.ContainsKey(account.Id))
                                    {
                                        memberDetail.Statements.AddRange(accountStatements[account.Id]);
                                    }
                                }
                            }
                        }
                        catch (Exception accountEx)
                        {
                            // Log error but continue processing other customers
                            System.Diagnostics.Trace.TraceError($"Error fetching accounts for customer {customer.Id}: {accountEx.Message}");
                        }
                    }

                    // Get next of kin if requested
                    if (includeNextOfKin && customer.Id != Guid.Empty)
                    {
                        try
                        {
                            var nextOfKins = await master._channelService.FindNextOfKinCollectionByCustomerIdAsync(
                                customer.Id,
                                serviceHeader
                            );

                            if (nextOfKins != null && nextOfKins.Any())
                            {
                                foreach (var nextOfKin in nextOfKins)
                                {
                                    memberDetail.NextOfKin.Add(new
                                    {
                                        nextOfKin.Id,
                                        nextOfKin.FullName,
                                        nextOfKin.Relationship,
                                        nextOfKin.RelationshipDescription,
                                        nextOfKin.AddressMobileLine,
                                        nextOfKin.AddressEmail,
                                        nextOfKin.AddressAddressLine1,
                                        nextOfKin.AddressCity,
                                        nextOfKin.NominatedPercentage,
                                        nextOfKin.CreatedDate
                                    });
                                }
                            }
                        }
                        catch (Exception nextOfKinEx)
                        {
                            // Log error but continue processing other customers
                            System.Diagnostics.Trace.TraceError($"Error fetching next of kin for customer {customer.Id}: {nextOfKinEx.Message}");
                        }
                    }

                    membersWithDetails.Add(memberDetail);
                }

                // Prepare pagination metadata with proper type casting
                var paginationInfo = new
                {
                    PageIndex = currentPageIndex,
                    PageSize = currentPageSize,
                    TotalCount = totalCount,
                    TotalPages = totalPages,
                    HasPreviousPage = currentPageIndex > 0,
                    HasNextPage = currentPageIndex < totalPages - 1
                };

                return Json(new ApiResponse<object>
                {
                    Success = true,
                    Message = $"Retrieved {pageCollectionCount} members with details.",
                    Data = new
                    {
                        Pagination = paginationInfo,
                        Members = membersWithDetails,
                        Summary = new
                        {
                            TotalMembers = totalCount,
                            MembersInPage = pageCollectionCount,
                            IncludeAccounts = includeAccounts,
                            IncludeNextOfKin = includeNextOfKin,
                            IncludeStatements = includeStatements,
                            StatementDateRange = includeStatements ? new
                            {
                                StartDate = statementStartDate?.ToString("yyyy-MM-dd"),
                                EndDate = statementEndDate?.ToString("yyyy-MM-dd")
                            } : null
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"Error in GetMembersWithDetails: {ex.Message}");
                System.Diagnostics.Trace.TraceError($"Stack Trace: {ex.StackTrace}");

                return Json(new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while retrieving member details.",
                    Data = new { Error = ex.Message, InnerError = ex.InnerException?.Message }
                });
            }
        }

        private int CalculateProperAge(CustomerDTO customer)
        {
            DateTime? dateToUse = null;

            switch ((CustomerType)customer.Type)
            {
                case CustomerType.Individual:
                    dateToUse = customer.IndividualBirthDate;
                    break;
                case CustomerType.Partnership:
                case CustomerType.Corporation:
                case CustomerType.MicroCredit:
                    dateToUse = customer.NonIndividualDateEstablished ?? customer.IndividualBirthDate;
                    break;
                default:
                    dateToUse = customer.IndividualBirthDate ?? customer.NonIndividualDateEstablished;
                    break;
            }

            if (dateToUse.HasValue && dateToUse.Value <= DateTime.Now)
            {
                var today = DateTime.Today;
                var age = today.Year - dateToUse.Value.Year;

                if (dateToUse.Value.Date > today.AddYears(-age))
                    age--;

                return age;
            }

            return -1;
        }

        private int CalculateAgeBasedOnData(CustomerDTO customer)
        {
            if (customer.Type == (byte)CustomerType.Individual && customer.IndividualBirthDate.HasValue)
            {
                // Use IndividualBirthDate for individuals
                return UberUtil.GetAge(customer.IndividualBirthDate.Value);
            }
            else if (customer.NonIndividualDateEstablished.HasValue)
            {
                // Use NonIndividualDateEstablished for non-individuals
                return UberUtil.GetAge(customer.NonIndividualDateEstablished.Value);
            }
            else if (customer.Type != (byte)CustomerType.Individual && customer.IndividualBirthDate.HasValue)
            {
                // Fallback: if non-individual has birth date, use it
                return UberUtil.GetAge(customer.IndividualBirthDate.Value);
            }

            return -1;
        }

        // Helper method to get account statements using direct SQL queries
        public async Task<Dictionary<Guid, List<object>>> GetAccountStatementsAsync(
            List<Guid> accountIds,
            DateTime? startDate,
            DateTime? endDate)
        {
            var accountStatements = new Dictionary<Guid, List<object>>();

            try
            {
                // Create a connection to the database
                using (var connection = new SqlConnection(GetConnectionString()))
                {
                    await connection.OpenAsync();

                    // Build the SQL query to get journal entries and journal data
                    var sql = @"
                SELECT 
                    -- Journal Entry fields
                    je.Id as JournalEntryId,
                    je.JournalId,
                    je.ChartOfAccountId,
                    je.ContraChartOfAccountId,
                    je.CustomerAccountId,
                    je.Amount,
                    je.ValueDate as JournalEntryValueDate,
                    je.IntegrityHash as JournalEntryIntegrityHash,
                    je.SequentialId as JournalEntrySequentialId,
                    je.CreatedBy as JournalEntryCreatedBy,
                    je.CreatedDate as JournalEntryCreatedDate,
                    
                    -- Journal fields
                    j.Id as JournalId,
                    j.ParentId,
                    j.PostingPeriodId,
                    j.BranchId,
                    j.AlternateChannelLogId,
                    j.TotalValue,
                    j.PrimaryDescription,
                    j.SecondaryDescription,
                    j.Reference,
                    j.ApplicationUserName,
                    j.EnvironmentUserName,
                    j.EnvironmentMachineName,
                    j.EnvironmentDomainName,
                    j.EnvironmentOSVersion,
                    j.EnvironmentMACAddress,
                    j.EnvironmentMotherboardSerialNumber,
                    j.EnvironmentProcessorId,
                    j.EnvironmentIPAddress,
                    j.ModuleNavigationItemCode,
                    j.TransactionCode,
                    j.ValueDate as JournalValueDate,
                    j.SuppressAccountAlert,
                    j.IsLocked,
                    j.IntegrityHash as JournalIntegrityHash,
                    j.SequentialId as JournalSequentialId,
                    j.CreatedBy as JournalCreatedBy,
                    j.CreatedDate as JournalCreatedDate
                    
                FROM swiftFin_JournalEntries je
                INNER JOIN swiftFin_Journals j ON je.JournalId = j.Id
                WHERE je.CustomerAccountId IN ({0})";

                    // Add date filtering if provided
                    if (startDate.HasValue || endDate.HasValue)
                    {
                        sql += " AND (";
                        var conditions = new List<string>();

                        if (startDate.HasValue)
                        {
                            conditions.Add("(je.ValueDate >= @StartDate OR j.ValueDate >= @StartDate OR je.CreatedDate >= @StartDate OR j.CreatedDate >= @StartDate)");
                        }

                        if (endDate.HasValue)
                        {
                            conditions.Add("(je.ValueDate <= @EndDate OR j.ValueDate <= @EndDate OR je.CreatedDate <= @EndDate OR j.CreatedDate <= @EndDate)");
                        }

                        sql += string.Join(" AND ", conditions) + ")";
                    }

                    sql += " ORDER BY ISNULL(je.ValueDate, j.ValueDate) DESC, je.CreatedDate DESC, j.CreatedDate DESC";

                    // Create parameterized query
                    var accountIdParameters = string.Join(",", accountIds.Select((id, index) => $"@AccountId{index}"));
                    sql = string.Format(sql, accountIdParameters);

                    using (var command = new SqlCommand(sql, connection))
                    {
                        // Add account ID parameters
                        for (int i = 0; i < accountIds.Count; i++)
                        {
                            command.Parameters.AddWithValue($"@AccountId{i}", accountIds[i]);
                        }

                        // Add date parameters if provided
                        if (startDate.HasValue)
                        {
                            command.Parameters.AddWithValue("@StartDate", startDate.Value);
                        }

                        if (endDate.HasValue)
                        {
                            command.Parameters.AddWithValue("@EndDate", endDate.Value);
                        }

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var customerAccountId = reader.GetGuid(reader.GetOrdinal("CustomerAccountId"));

                                if (!accountStatements.ContainsKey(customerAccountId))
                                {
                                    accountStatements[customerAccountId] = new List<object>();
                                }

                                // Get journal entry value date
                                DateTime? journalEntryValueDate = null;
                                var journalEntryValueDateOrdinal = reader.GetOrdinal("JournalEntryValueDate");
                                if (!reader.IsDBNull(journalEntryValueDateOrdinal))
                                {
                                    journalEntryValueDate = reader.GetDateTime(journalEntryValueDateOrdinal);
                                }

                                // Get journal value date
                                DateTime? journalValueDate = null;
                                var journalValueDateOrdinal = reader.GetOrdinal("JournalValueDate");
                                if (!reader.IsDBNull(journalValueDateOrdinal))
                                {
                                    journalValueDate = reader.GetDateTime(journalValueDateOrdinal);
                                }

                                var statement = new
                                {
                                    // Journal Entry details
                                    JournalEntry = new
                                    {
                                        Id = reader.GetGuid(reader.GetOrdinal("JournalEntryId")),
                                        JournalId = reader.GetGuid(reader.GetOrdinal("JournalId")),
                                        ChartOfAccountId = reader.GetGuid(reader.GetOrdinal("ChartOfAccountId")),
                                        ContraChartOfAccountId = reader.GetGuid(reader.GetOrdinal("ContraChartOfAccountId")),
                                        CustomerAccountId = customerAccountId,
                                        Amount = reader.GetDecimal(reader.GetOrdinal("Amount")),
                                        ValueDate = journalEntryValueDate,
                                        IntegrityHash = reader.GetString(reader.GetOrdinal("JournalEntryIntegrityHash")),
                                        SequentialId = reader.GetGuid(reader.GetOrdinal("JournalEntrySequentialId")),
                                        CreatedBy = reader.GetString(reader.GetOrdinal("JournalEntryCreatedBy")),
                                        CreatedDate = reader.GetDateTime(reader.GetOrdinal("JournalEntryCreatedDate"))
                                    },

                                    // Journal details
                                    Journal = new
                                    {
                                        Id = reader.GetGuid(reader.GetOrdinal("JournalId")),
                                        ParentId = reader.IsDBNull(reader.GetOrdinal("ParentId")) ? (Guid?)null : reader.GetGuid(reader.GetOrdinal("ParentId")),
                                        PostingPeriodId = reader.GetGuid(reader.GetOrdinal("PostingPeriodId")),
                                        BranchId = reader.GetGuid(reader.GetOrdinal("BranchId")),
                                        AlternateChannelLogId = reader.IsDBNull(reader.GetOrdinal("AlternateChannelLogId")) ? (Guid?)null : reader.GetGuid(reader.GetOrdinal("AlternateChannelLogId")),
                                        TotalValue = reader.GetDecimal(reader.GetOrdinal("TotalValue")),
                                        PrimaryDescription = reader.GetString(reader.GetOrdinal("PrimaryDescription")),
                                        SecondaryDescription = reader.GetString(reader.GetOrdinal("SecondaryDescription")),
                                        Reference = reader.GetString(reader.GetOrdinal("Reference")),
                                        ApplicationUserName = reader.GetString(reader.GetOrdinal("ApplicationUserName")),
                                        EnvironmentUserName = reader.GetString(reader.GetOrdinal("EnvironmentUserName")),
                                        EnvironmentMachineName = reader.GetString(reader.GetOrdinal("EnvironmentMachineName")),
                                        EnvironmentDomainName = reader.GetString(reader.GetOrdinal("EnvironmentDomainName")),
                                        EnvironmentOSVersion = reader.GetString(reader.GetOrdinal("EnvironmentOSVersion")),
                                        EnvironmentMACAddress = reader.GetString(reader.GetOrdinal("EnvironmentMACAddress")),
                                        EnvironmentMotherboardSerialNumber = reader.GetString(reader.GetOrdinal("EnvironmentMotherboardSerialNumber")),
                                        EnvironmentProcessorId = reader.GetString(reader.GetOrdinal("EnvironmentProcessorId")),
                                        EnvironmentIPAddress = reader.GetString(reader.GetOrdinal("EnvironmentIPAddress")),
                                        ModuleNavigationItemCode = reader.GetInt32(reader.GetOrdinal("ModuleNavigationItemCode")),
                                        TransactionCode = reader.GetInt32(reader.GetOrdinal("TransactionCode")),
                                        ValueDate = journalValueDate,
                                        SuppressAccountAlert = reader.GetBoolean(reader.GetOrdinal("SuppressAccountAlert")),
                                        IsLocked = reader.GetBoolean(reader.GetOrdinal("IsLocked")),
                                        IntegrityHash = reader.GetString(reader.GetOrdinal("JournalIntegrityHash")),
                                        SequentialId = reader.GetGuid(reader.GetOrdinal("JournalSequentialId")),
                                        CreatedBy = reader.GetString(reader.GetOrdinal("JournalCreatedBy")),
                                        CreatedDate = reader.GetDateTime(reader.GetOrdinal("JournalCreatedDate"))
                                    },

                                    // Combined statement summary
                                    StatementSummary = new
                                    {
                                        TransactionId = reader.GetGuid(reader.GetOrdinal("JournalId")),
                                        TransactionDate = reader.GetDateTime(reader.GetOrdinal("JournalCreatedDate")),
                                        ValueDate = journalEntryValueDate ?? journalValueDate ?? reader.GetDateTime(reader.GetOrdinal("JournalCreatedDate")),
                                        Description = reader.GetString(reader.GetOrdinal("PrimaryDescription")),
                                        Reference = reader.GetString(reader.GetOrdinal("Reference")),
                                        Amount = reader.GetDecimal(reader.GetOrdinal("Amount")),
                                        TransactionCode = reader.GetInt32(reader.GetOrdinal("TransactionCode")),
                                        IsReversed = reader.GetBoolean(reader.GetOrdinal("IsLocked"))
                                    }
                                };

                                accountStatements[customerAccountId].Add(statement);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"Error in GetAccountStatementsAsync: {ex.Message}");
                System.Diagnostics.Trace.TraceError($"Stack Trace: {ex.StackTrace}");
                throw;
            }

            return accountStatements;
        }

        // Method to get connection string (you'll need to implement this based on your configuration)
        private string GetConnectionString()
        {
            // Get connection string from your configuration
            // This could be from web.config, appsettings.json, or other configuration source
            return System.Configuration.ConfigurationManager.ConnectionStrings["SwiftFinancialsDB_Live"].ConnectionString;
        }




        [HttpPost]
        [Route("creditbatch/add")]
        public async Task<IHttpActionResult> AddCreditBatch(CreditBatchDTO creditBatchDTO)
        {


            var serviceHeader = master.GetServiceHeader();

            CreditBatchDTO creditBatchResponse = null;

            try
            {
                creditBatchResponse = await master._channelService.AddCreditBatchAsync(creditBatchDTO, serviceHeader);


                if (creditBatchResponse == null)
                {
                    return Ok(new
                    {
                        success = false,
                        message = "Sorry, but the batch could not be created!"
                    });
                }
                // -------------------- SUCCESS RESPONSE --------------------



                //await master._channelService.ParseCreditBatchImportAsync(creditBatchResponse.Id, creditBatchDTO.ImportFileName, serviceHeader);

                return Ok(creditBatchResponse);


            }
            catch (Exception ex)
            {

                return InternalServerError(ex);
            }

        }


        [HttpGet]
        [Route("creditbatches")]
        public async Task<IHttpActionResult> FindCreditBatches()
        {
            var serviceHeader = master.GetServiceHeader();

            try
            {
                var batches = await master._channelService.FindCreditBatchesAsync(serviceHeader);
                return Ok(batches);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("credittypes")]
        public async Task<IHttpActionResult> FindCreditTypes()
        {
            var serviceHeader = master.GetServiceHeader();

            try
            {
                var credittypes = await master._channelService.FindCreditTypesAsync(serviceHeader);
                return Ok(credittypes);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        //[Route("batchtypes")]
        //public async Task<IHttpActionResult> FindCreditBatchTypes()
        //{
        //    var serviceHeader = master.GetServiceHeader();

        //    try
        //    {
        //        var creditBatchTypes = await master._channelService.FindCreditBat
        //    }
        //}

        [HttpGet]
        [Route("{creditBatchId}/entries")]

        public async Task<IHttpActionResult> FindCreditBatchEntriesById(Guid CreditBatchId)
        {
            var serviceHeader = master.GetServiceHeader();

            try
            {

                var creditBatchEntries = await master._channelService.FindCreditBatchEntriesByCreditBatchIdAsync(CreditBatchId, true, serviceHeader);
                return Ok(creditBatchEntries);
            }

            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }


        [HttpGet]
        [Route("entries/by-customer")]
        public async Task<IHttpActionResult> FindCreditBatchEntriesByCustomerId(Guid CustomerId, int CreditBatchType)
        {
            var serviceHeader = master.GetServiceHeader();

            try
            {


                var creditBatchEntries = await master._channelService.FindCreditBatchEntriesByCustomerIdAsync(CreditBatchType, CustomerId, true, serviceHeader); return Ok(creditBatchEntries);
            }

            catch (Exception ex)
            {


                return InternalServerError(ex);
            }


        }




        [HttpPost]
        [Route("creditbatch/{batchId}/import")]
        public async Task<IHttpActionResult> CreditBatchImport(Guid batchId)
        {
            try
            {
                var httpRequest = HttpContext.Current.Request;

                if (httpRequest.Files.Count == 0)
                    return BadRequest("No file uploaded.");

                var postedFile = httpRequest.Files[0];

                if (postedFile == null || postedFile.ContentLength == 0)
                    return BadRequest("Invalid file.");

                // 🔹 Fixed upload directory
                var uploadDirectory = @"C:\swiftfin_file_uploads\";

                if (!Directory.Exists(uploadDirectory))
                    Directory.CreateDirectory(uploadDirectory);

                // 🔹 Sanitize filename
                var fileName = Path.GetFileName(postedFile.FileName);

                // 🔹 Optional: make filename unique
                var uniqueFileName =
                    $"{Path.GetFileNameWithoutExtension(fileName)}_{DateTime.UtcNow:yyyyMMddHHmmss}{Path.GetExtension(fileName)}";

                var filePath = Path.Combine(uploadDirectory, uniqueFileName);

                // 🔹 Save file
                postedFile.SaveAs(filePath);

                var serviceHeader = master.GetServiceHeader();

                // 🔹 Pass saved file path to service
                var mismatches = await master._channelService.ParseCreditBatchImportAsync(
                    batchId,
                    filePath,
                    serviceHeader
                );

                return Ok(new
                {
                    BatchId = batchId,
                    FileName = uniqueFileName,
                    SavedPath = filePath,
                    MismatchCount = mismatches?.Count ?? 0,
                    Mismatches = mismatches
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
        [HttpPost]
        [Route("creditbatch/{batchid}/post")]
        public async Task<IHttpActionResult> PostCreditBatch(Guid batchId)
        {
            try
            {
                var serviceHeader = master.GetServiceHeader();
                var creditBatchDTO = await master._channelService.FindCreditBatchAsync(batchId, serviceHeader);

                var success = await master._channelService.AuthorizeCreditBatchAsync(creditBatchDTO, 1, 0, serviceHeader);

                var creditBatchEntryDTOs = await master._channelService.FindCreditBatchEntriesByCreditBatchIdAsync(batchId, true, serviceHeader);

                foreach (var creditBatchEntry in creditBatchEntryDTOs)
                {
                    // Assuming PostCreditBatchEntryAsync needs the entry and an amount (here 0) + header
                    var postResult = await master._channelService.PostCreditBatchEntryAsync(creditBatchEntry.Id, 0, serviceHeader);

                }
                if (success)
                {
                    return Ok(creditBatchDTO);
                }
                else
                {
                    return BadRequest("post failed");
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpPost]
        [Route("InterAccountTransferBatch")]
        public async Task<IHttpActionResult> InterAccountTransferBatch(InterAccountTransferBatchDTO interAccountTransferBatchDTO)
        {
            // Retrieve the DTO stored in session
            var serviceHeader = master.GetServiceHeader();

            if (interAccountTransferBatchDTO == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Batch cannot be null."
                });
            }
            var customerAccount = await master._channelService.FindCustomerAccountAsync(interAccountTransferBatchDTO.CustomerAccountId, true, true, true, true, serviceHeader);
            interAccountTransferBatchDTO.CustomerId = customerAccount.CustomerId;

            // If there are no entries, initialize a new list

            if (interAccountTransferBatchDTO.interAccountBatchEntries != null)
            {


                decimal SumAmount = interAccountTransferBatchDTO.interAccountBatchEntries.Sum(e => e.Principal + e.Interest);
                decimal totalValue = interAccountTransferBatchDTO?.AvailableBalance ?? 0;

                if (SumAmount != totalValue)
                {
                    var balance = totalValue - SumAmount;
                    return Json(new { success = false, message = $"The total value ({totalValue}) should be equal to the sum of the entries ({SumAmount}). Balance: {balance}" });
                }
            }

            // Validate the DTO
            interAccountTransferBatchDTO.ValidateAll();
            if (interAccountTransferBatchDTO.ErrorMessages.Count != 0)
            {
                return Json(new
                {
                    success = false,
                    message = interAccountTransferBatchDTO.ErrorMessages
                });
            }

            // Save the batch data
            var interAccountTransferBatch = await master._channelService.AddInterAccountTransferBatchAsync(interAccountTransferBatchDTO, serviceHeader);
            if (interAccountTransferBatchDTO.HasErrors)
            {
                return Json(new
                {
                    success = false,
                    message = interAccountTransferBatchDTO.ErrorMessages
                });
            }

            // Save each batch entry
            foreach (var InterAccountTransferBatchEntry in interAccountTransferBatchDTO.interAccountBatchEntries)
            {
                InterAccountTransferBatchEntry.InterAccountTransferBatchId = interAccountTransferBatch.Id;
                await master._channelService.AddInterAccountTransferBatchEntryAsync(InterAccountTransferBatchEntry, serviceHeader);
            }

            // Return success message in JSON
            return Json(new
            {
                success = true,
                message = "Successfully created refund batch."
            });
        }



        [HttpGet]
        [Route("FindInterTransferBatches")]
        public async Task<IHttpActionResult> FindInterTransferBatches(int pageIndex = 0, int pageSize = 20)
        {
            var serviceHeader = master.GetServiceHeader();

            if (pageIndex < 0) pageIndex = 0;
            if (pageSize <= 0 || pageSize > 100) pageSize = 20; // guardrails

            try
            {
                var pageCollectionInfo =
                    await master._channelService.FindInterAccountTransferBatchesInPageAsync(pageIndex, pageSize, serviceHeader);

                return Ok(new
                {
                    success = true,
                    data = pageCollectionInfo.PageCollection,
                    paging = new
                    {
                        pageIndex,
                        pageSize,
                        totalCount = pageCollectionInfo.TotalCount,
                        totalPages = pageCollectionInfo.TotalPages
                    }
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
        [HttpGet]
        [Route("FindInterTransferBatchesEntries")]
        public async Task<IHttpActionResult> FindInterTransferBatchesEntries(Guid batchId)
        {
            var serviceHeader = master.GetServiceHeader();

            try
            {
                var pageCollectionInfo = await master._channelService.FindInterAccountTransferBatchEntriesByInterAccountTransferBatchIdAsync(batchId, serviceHeader);

                return Ok(new
                {
                    success = true,
                    Data = pageCollectionInfo
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }




        [HttpPost]
        [Route("PostinterAccount")]
        public async Task<IHttpActionResult> PostinterAccount(Guid batchId)
        {
            try
            {
                var serviceHeader = master.GetServiceHeader();
                var interAccountTransferBatchDTO = await master._channelService.FindInterAccountTransferBatchAsync(batchId, serviceHeader);
                var success = await master._channelService.AuthorizeInterAccountTransferBatchAsync(interAccountTransferBatchDTO, (int)BatchAuthOption.Post, 0, serviceHeader);
                if (success)
                {
                    return Ok(interAccountTransferBatchDTO);
                }
                else
                {
                    return BadRequest("post failed");
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }











        [HttpGet]
        [Route("GetCustomerShareStatement/{customerAccountId}")]
        public async Task<HttpResponseMessage> GetCustomerShareStatement(Guid customerAccountId, DateTime? startDate = null, DateTime? endDate = null, bool downloadPdf = false)
        {
            try
            {
                // Create a simple model for SQL results
                var customerData = new
                {
                    FirstName = "",
                    LastName = "",
                    Mobile = "",
                    Email = "",
                    Reference2 = "",
                    Reference3 = "",
                    BranchCode = 0,
                    CustomerSerialNumber = 0,
                    ProductCode = 0,
                    TargetProductCode = 0
                };

                var statementRows = new List<CustomerShareStatementRow>();
                decimal totalContribution = 0;

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Get customer and account information - FIXED query
                    string customerQuery = @"
               SELECT TOP 1 
                   c.Individual_FirstName,
                   c.Individual_LastName,
                   c.Address_MobileLine,
                   c.Address_Email,
                   c.Reference2,
                   c.Reference3,
                   b.Code as BranchCode,
                   c.SerialNumber as CustomerSerialNumber,
                   ca.CustomerAccountType_ProductCode,
                   ca.CustomerAccountType_TargetProductCode
               FROM [SwiftFinancialsDB_Live].[dbo].[swiftFin_CustomerAccounts] ca
               INNER JOIN [SwiftFinancialsDB_Live].[dbo].[swiftFin_Customers] c 
                   ON ca.CustomerId = c.Id
               INNER JOIN [SwiftFinancialsDB_Live].[dbo].[swiftFin_Branches] b
                   ON ca.BranchId = b.Id
               WHERE ca.Id = @CustomerAccountId";

                    using (var cmd = new SqlCommand(customerQuery, connection))
                    {
                        cmd.Parameters.Add("@CustomerAccountId", SqlDbType.UniqueIdentifier).Value = customerAccountId;

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                customerData = new
                                {
                                    FirstName = reader["Individual_FirstName"]?.ToString() ?? "",
                                    LastName = reader["Individual_LastName"]?.ToString() ?? "",
                                    Mobile = reader["Address_MobileLine"]?.ToString() ?? "",
                                    Email = reader["Address_Email"]?.ToString() ?? "",
                                    Reference2 = reader["Reference2"]?.ToString() ?? "",
                                    Reference3 = reader["Reference3"]?.ToString() ?? "",
                                    BranchCode = Convert.ToInt32(reader["BranchCode"]),
                                    CustomerSerialNumber = Convert.ToInt32(reader["CustomerSerialNumber"]),
                                    ProductCode = Convert.ToInt32(reader["CustomerAccountType_ProductCode"]),
                                    TargetProductCode = Convert.ToInt32(reader["CustomerAccountType_TargetProductCode"])
                                };
                            }
                            else
                            {
                                // Customer not found
                                var response = Request.CreateResponse(HttpStatusCode.NotFound);
                                response.Content = new StringContent(
                                    JsonConvert.SerializeObject(new ApiResponse<object>
                                    {
                                        Success = false,
                                        Message = "Customer account not found.",
                                        Data = null
                                    }),
                                    Encoding.UTF8,
                                    "application/json");
                                return response;
                            }
                        }
                    }

                    // Now get share statement
                    using (var command = new SqlCommand("usp_GetCustomerShareStatement", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        // Add parameters
                        command.Parameters.Add("@CustomerAccountId", SqlDbType.UniqueIdentifier).Value = customerAccountId;
                        command.Parameters.Add("@StartDate", SqlDbType.Date).Value = (object)startDate ?? DBNull.Value;
                        command.Parameters.Add("@EndDate", SqlDbType.Date).Value = (object)endDate ?? DBNull.Value;

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            // First result set: Statement rows
                            while (await reader.ReadAsync())
                            {
                                var row = new CustomerShareStatementRow
                                {
                                    Date = reader["Date"].ToString(),
                                    ShareContribution = Convert.ToDecimal(reader["Share Contribution"]),
                                    Cumulative = Convert.ToDecimal(reader["Cumulative"]),
                                    Description = reader["Description"].ToString()
                                };
                                statementRows.Add(row);
                            }

                            // Move to second result set: Total contribution
                            if (await reader.NextResultAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    totalContribution = reader["TotalContribution"] != DBNull.Value ?
                                        Convert.ToDecimal(reader["TotalContribution"]) : 0;
                                }
                            }
                        }
                    }
                }

                // Build the full account number using the same logic as in the DTO
                string fullAccountNumber = string.Format("{0}-{1}-{2}-{3}",
                    customerData.BranchCode.ToString().PadLeft(3, '0'),
                    customerData.CustomerSerialNumber.ToString().PadLeft(7, '0'),
                    customerData.ProductCode.ToString().PadLeft(3, '0'),
                    customerData.TargetProductCode.ToString().PadLeft(3, '0'));

                // Create the result object with customer info
                var shareStatementResult = new
                {
                    Customer = new
                    {
                        FullName = $"{customerData.FirstName} {customerData.LastName}".Trim(),
                        AccountNumber = fullAccountNumber,
                        StaffNo = customerData.Reference2,
                        PFNumber = customerData.Reference3,
                        Mobile = customerData.Mobile,
                        Email = customerData.Email
                    },
                    Statement = statementRows,
                    TotalContribution = totalContribution
                };

                // If PDF download is requested
                if (downloadPdf)
                {
                    byte[] pdfBytes = GenerateShareStatementPdf(customerData, fullAccountNumber, statementRows, totalContribution, startDate, endDate);

                    var response = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new ByteArrayContent(pdfBytes)
                    };

                    response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

                    string customerName = $"{customerData.FirstName}_{customerData.LastName}".Replace(" ", "_");
                    response.Content.Headers.ContentDisposition =
                        new ContentDispositionHeaderValue("attachment")
                        {
                            FileName = $"ShareStatement_{customerName}_{DateTime.Now:yyyyMMdd}.pdf"
                        };

                    return response;
                }
                else
                {
                    // Return JSON response
                    var response = Request.CreateResponse(HttpStatusCode.OK);
                    response.Content = new StringContent(
                        JsonConvert.SerializeObject(new ApiResponse<object>
                        {
                            Success = true,
                            Message = statementRows.Count > 0 ?
                                $"Share statement retrieved successfully. Total: {totalContribution:C}" :
                                "No transactions found for the given period.",
                            Data = shareStatementResult
                        }),
                        Encoding.UTF8,
                        "application/json");
                    return response;
                }
            }
            catch (Exception ex)
            {
                var response = Request.CreateResponse(HttpStatusCode.InternalServerError);
                response.Content = new StringContent(
                    JsonConvert.SerializeObject(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "An error occurred while retrieving customer share statement.",
                        Data = ex.Message
                    }),
                    Encoding.UTF8,
                    "application/json");
                return response;
            }
        }
        #region GenerateShareStatementPdf
        private byte[] GenerateShareStatementPdf(dynamic customerData, string fullAccountNumber,
 List<CustomerShareStatementRow> statementRows, decimal totalContribution,
 DateTime? startDate = null, DateTime? endDate = null)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                // Create document with smaller margins for better space utilization
                Document document = new Document(PageSize.A4, 30, 30, 50, 30);
                PdfWriter writer = PdfWriter.GetInstance(document, ms);

                document.Open();

                // ===== RUBANI SACCO COLOR THEME =====
                BaseColor SkyBlue = new BaseColor(0, 174, 239); // #00AEEF
                BaseColor Red = new BaseColor(255, 0, 0);       // #FF0000
                BaseColor DarkGray = new BaseColor(26, 26, 26); // #1A1A1A
                BaseColor LightGray = new BaseColor(217, 217, 217); // #D9D9D9
                BaseColor White = BaseColor.WHITE;
                BaseColor TableHeaderBlue = new BaseColor(173, 216, 230); // Light blue for table headers

                // Fonts using Rubani Sacco theme
                Font titleFont = new Font(FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16, DarkGray));
                Font headerFont = new Font(FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, DarkGray));
                Font subHeaderFont = new Font(FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11, White));
                Font normalFont = new Font(FontFactory.GetFont(FontFactory.HELVETICA, 9));
                Font boldFont = new Font(FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9));
                Font smallFont = new Font(FontFactory.GetFont(FontFactory.HELVETICA, 8, DarkGray));
                Font amountFont = new Font(FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, DarkGray));

                // ===== CUSTOM HEADER WITH RUBANI SACCO LOGO =====
                try
                {
                    // Create a table with 1 column for logo on top, then company info below
                    PdfPTable headerTable = new PdfPTable(1)
                    {
                        WidthPercentage = 100,
                        SpacingAfter = 10f
                    };

                    // Row 1: Logo centered at top
                    PdfPCell logoCell = new PdfPCell();
                    logoCell.Border = Rectangle.NO_BORDER;
                    logoCell.HorizontalAlignment = Element.ALIGN_CENTER;
                    logoCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    logoCell.PaddingBottom = 5f;

                    // Try to load logo from local path
                    string logoPath = @"C:/Users/Karenju/Desktop/testapidebug/Assets/Images/rubani-logo.jpeg";
                    if (File.Exists(logoPath))
                    {
                        try
                        {
                            Image logo = Image.GetInstance(logoPath);
                            logo.ScaleToFit(100, 100); // Increased size for better visibility
                            logoCell.AddElement(logo);
                        }
                        catch (Exception)
                        {
                            // Fallback to text if image fails to load
                            logoCell.AddElement(new Paragraph("RUBANI SACCO",
                                FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14, SkyBlue))
                            {
                                Alignment = Element.ALIGN_CENTER
                            });
                        }
                    }
                    else
                    {
                        // Use text if no logo file
                        logoCell.AddElement(new Paragraph("RUBANI SACCO",
                            FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14, SkyBlue))
                        {
                            Alignment = Element.ALIGN_CENTER
                        });
                    }

                    headerTable.AddCell(logoCell);

                    // Row 2: Company Info - LEFT ALIGNED BELOW LOGO
                    PdfPCell infoCell = new PdfPCell();
                    infoCell.Border = Rectangle.NO_BORDER;
                    infoCell.HorizontalAlignment = Element.ALIGN_LEFT;
                    infoCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    infoCell.PaddingTop = 5f;

                    // Company name - LEFT ALIGNED
                    var companyNamePara = new Paragraph("RUBANI SACCO",
                        FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, SkyBlue))
                    {
                        Alignment = Element.ALIGN_LEFT
                    };
                    infoCell.AddElement(companyNamePara);

                    // Address - LEFT ALIGNED
                    var address = new Paragraph("Rubani House, Off Airport North Embakasi",
                        FontFactory.GetFont(FontFactory.HELVETICA, 10))
                    {
                        Alignment = Element.ALIGN_LEFT
                    };
                    infoCell.AddElement(address);

                    // Email - LEFT ALIGNED
                    var email = new Paragraph("rubanisacco@gmail.com",
                        FontFactory.GetFont(FontFactory.HELVETICA, 10))
                    {
                        Alignment = Element.ALIGN_LEFT
                    };
                    infoCell.AddElement(email);

                    headerTable.AddCell(infoCell);

                    document.Add(headerTable);

                    // Add decorative line (Blue-Red-Blue)
                    var lineTable = new PdfPTable(3)
                    {
                        WidthPercentage = 100,
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        SpacingAfter = 10f
                    };
                    lineTable.SetWidths(new float[] { 33, 34, 33 });

                    lineTable.AddCell(new PdfPCell()
                    {
                        BackgroundColor = SkyBlue,
                        FixedHeight = 2f,
                        Border = Rectangle.NO_BORDER
                    });

                    lineTable.AddCell(new PdfPCell()
                    {
                        BackgroundColor = Red,
                        FixedHeight = 2f,
                        Border = Rectangle.NO_BORDER
                    });

                    lineTable.AddCell(new PdfPCell()
                    {
                        BackgroundColor = SkyBlue,
                        FixedHeight = 2f,
                        Border = Rectangle.NO_BORDER
                    });

                    document.Add(lineTable);
                }
                catch (Exception)
                {
                    // Fallback header if anything goes wrong
                    var fallbackPara = new Paragraph("RUBANI SACCO\nRubani House, Off Airport North Embakasi\nrubanisacco@gmail.com",
                        FontFactory.GetFont(FontFactory.HELVETICA, 10))
                    {
                        Alignment = Element.ALIGN_LEFT,
                        SpacingAfter = 15f,
                        IndentationLeft = 0f
                    };
                    document.Add(fallbackPara);
                }

                // ===== STATEMENT TITLE =====
                document.Add(new Paragraph("SHARES STATEMENT", titleFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 10f
                });

                // ===== MEMBER INFORMATION SECTION =====
                string fullName = $"{customerData.FirstName} {customerData.LastName}".Trim().ToUpper();
                string staffNo = customerData.Reference2;
                string pfNumber = customerData.Reference3;

                // Use Paragraphs for member info (like in loan statement)
                Paragraph memberInfo = new Paragraph();
                memberInfo.Alignment = Element.ALIGN_LEFT;

                // Add member info with proper formatting
                memberInfo.Add(new Chunk("Name: ", boldFont));
                memberInfo.Add(new Chunk(fullName, normalFont));
                memberInfo.Add(new Chunk("   MemberNo: ", boldFont));
                memberInfo.Add(new Chunk(staffNo ?? "N/A", normalFont));
                memberInfo.Add(Chunk.NEWLINE);

                memberInfo.Add(new Chunk("Staff No: ", boldFont));
                memberInfo.Add(new Chunk(pfNumber ?? "N/A", normalFont));
                memberInfo.Add(new Chunk("   Account No: ", boldFont));
                memberInfo.Add(new Chunk(fullAccountNumber, normalFont));

                memberInfo.SpacingAfter = 15f;
                document.Add(memberInfo);

                // ===== STATEMENT PERIOD SECTION =====
                if (startDate.HasValue || endDate.HasValue)
                {
                    string periodText = "Statement Period: ";
                    if (startDate.HasValue && endDate.HasValue)
                        periodText += $"{startDate.Value:dd/MM/yyyy} to {endDate.Value:dd/MM/yyyy}";
                    else if (startDate.HasValue)
                        periodText += $"From {startDate.Value:dd/MM/yyyy}";
                    else if (endDate.HasValue)
                        periodText += $"To {endDate.Value:dd/MM/yyyy}";

                    var periodPara = new Paragraph(periodText, boldFont)
                    {
                        Alignment = Element.ALIGN_LEFT,
                        SpacingAfter = 10f
                    };
                    document.Add(periodPara);
                }

                // ===== SUMMARY SECTION =====
                PdfPTable summaryTable = new PdfPTable(2)
                {
                    WidthPercentage = 100,
                    SpacingAfter = 15f
                };
                summaryTable.SetWidths(new float[] { 50, 50 });

                // Summary header
                var summaryHeader = new PdfPCell(new Phrase("SUMMARY", subHeaderFont))
                {
                    BackgroundColor = DarkGray,
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    Padding = 8,
                    Colspan = 2,
                    Border = Rectangle.NO_BORDER,
                    BorderWidthBottom = 2f,
                    BorderColorBottom = SkyBlue
                };
                summaryTable.AddCell(summaryHeader);

                // Total Contribution row
                summaryTable.AddCell(new PdfPCell(new Phrase("Total Share Contribution:", boldFont))
                {
                    Border = Rectangle.NO_BORDER,
                    Padding = 8,
                    BackgroundColor = TableHeaderBlue
                });

                summaryTable.AddCell(new PdfPCell(new Phrase(totalContribution.ToString("N2"), amountFont))
                {
                    Border = Rectangle.NO_BORDER,
                    Padding = 8,
                    HorizontalAlignment = Element.ALIGN_RIGHT,
                    BackgroundColor = TableHeaderBlue
                });

                document.Add(summaryTable);

                // ===== TRANSACTIONS SECTION =====
                if (statementRows != null && statementRows.Count > 0)
                {
                    // Section header
                    var sectionHeaderPara = new Paragraph("TRANSACTION DETAILS",
                        FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, White))
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingAfter = 5f
                    };

                    // Create a background for the header
                    PdfPTable headerBgTable = new PdfPTable(1)
                    {
                        WidthPercentage = 100,
                        SpacingAfter = 10f
                    };

                    PdfPCell headerCell = new PdfPCell(sectionHeaderPara)
                    {
                        BackgroundColor = DarkGray,
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        Padding = 8,
                        Border = Rectangle.NO_BORDER,
                        BorderWidthBottom = 2f,
                        BorderColorBottom = SkyBlue
                    };
                    headerBgTable.AddCell(headerCell);
                    document.Add(headerBgTable);

                    // Transactions table with 4 columns - NO BORDERS
                    PdfPTable transTable = new PdfPTable(4)
                    {
                        WidthPercentage = 100,
                        SpacingAfter = 5f
                    };
                    transTable.SetWidths(new float[] { 20, 40, 20, 20 });

                    // Table headers - NO BORDERS
                    string[] headers = { "Date", "Description", "Share Contribution", "Cumulative" };

                    for (int i = 0; i < headers.Length; i++)
                    {
                        PdfPCell headerCellItem = new PdfPCell(new Phrase(headers[i], headerFont))
                        {
                            BackgroundColor = LightGray,
                            HorizontalAlignment = Element.ALIGN_CENTER,
                            Padding = 5,
                            // NO BORDERS - remove all border widths
                            BorderWidthTop = 0f,
                            BorderWidthBottom = 0f,
                            BorderWidthLeft = 0f,
                            BorderWidthRight = 0f
                        };
                        transTable.AddCell(headerCellItem);
                    }

                    // Add transactions with NO BORDERS between rows
                    for (int rowIndex = 0; rowIndex < statementRows.Count; rowIndex++)
                    {
                        var row = statementRows[rowIndex];

                        // Date cell - NO BORDERS
                        PdfPCell dateCell = new PdfPCell(new Phrase(row.Date, normalFont));
                        dateCell.HorizontalAlignment = Element.ALIGN_CENTER;
                        dateCell.Padding = 5f;
                        // NO BORDERS - remove all border widths
                        dateCell.BorderWidthTop = 0f;
                        dateCell.BorderWidthBottom = 0f;
                        dateCell.BorderWidthLeft = 0f;
                        dateCell.BorderWidthRight = 0f;
                        transTable.AddCell(dateCell);

                        // Description cell - NO BORDERS
                        PdfPCell descCell = new PdfPCell(new Phrase(row.Description ?? "", normalFont));
                        descCell.HorizontalAlignment = Element.ALIGN_LEFT;
                        descCell.Padding = 5f;
                        // NO BORDERS - remove all border widths
                        descCell.BorderWidthTop = 0f;
                        descCell.BorderWidthBottom = 0f;
                        descCell.BorderWidthLeft = 0f;
                        descCell.BorderWidthRight = 0f;
                        transTable.AddCell(descCell);

                        // Share Contribution cell - NO BORDERS
                        PdfPCell shareCell = new PdfPCell(new Phrase(row.ShareContribution.ToString("N2"), normalFont));
                        shareCell.HorizontalAlignment = Element.ALIGN_RIGHT;
                        shareCell.Padding = 5f;
                        // NO BORDERS - remove all border widths
                        shareCell.BorderWidthTop = 0f;
                        shareCell.BorderWidthBottom = 0f;
                        shareCell.BorderWidthLeft = 0f;
                        shareCell.BorderWidthRight = 0f;
                        transTable.AddCell(shareCell);

                        // Cumulative cell - NO BORDERS
                        PdfPCell cumulativeCell = new PdfPCell(new Phrase(row.Cumulative.ToString("N2"), normalFont));
                        cumulativeCell.HorizontalAlignment = Element.ALIGN_RIGHT;
                        cumulativeCell.Padding = 5f;
                        // NO BORDERS - remove all border widths
                        cumulativeCell.BorderWidthTop = 0f;
                        cumulativeCell.BorderWidthBottom = 0f;
                        cumulativeCell.BorderWidthLeft = 0f;
                        cumulativeCell.BorderWidthRight = 0f;
                        transTable.AddCell(cumulativeCell);
                    }

                    document.Add(transTable);
                }
                else
                {
                    var noTransPara = new Paragraph("No transactions found for the selected period.", normalFont)
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingBefore = 20f,
                        SpacingAfter = 20f
                    };
                    document.Add(noTransPara);
                }

                // ===== GRAND TOTAL SECTION =====
                document.Add(new Paragraph("\n"));
                PdfPTable grandTotalTable = new PdfPTable(2)
                {
                    WidthPercentage = 100,
                    SpacingAfter = 20f
                };
                grandTotalTable.SetWidths(new float[] { 70, 30 });

                grandTotalTable.AddCell(new PdfPCell(new Phrase("GRAND TOTAL SHARE CONTRIBUTION:",
                    FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11, DarkGray)))
                {
                    Border = Rectangle.NO_BORDER,
                    Padding = 10,
                    BackgroundColor = TableHeaderBlue
                });

                grandTotalTable.AddCell(new PdfPCell(new Phrase(totalContribution.ToString("N2"),
                    FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11, Red)))
                {
                    Border = Rectangle.NO_BORDER,
                    Padding = 10,
                    HorizontalAlignment = Element.ALIGN_RIGHT,
                    BackgroundColor = TableHeaderBlue
                });

                document.Add(grandTotalTable);

                // ===== CUSTOM FOOTER =====
                document.Add(new Paragraph("\n"));
                var footerPara = new Paragraph(
                    $"Statement Generated on: {DateTime.Now:dd/MM/yyyy HH:mm:ss} | Page: 1",
                    smallFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingBefore = 10f
                };
                document.Add(footerPara);

                // ===== FOOTER NOTES =====
                document.Add(new Paragraph("\n"));
                document.Add(new Paragraph("This is a system generated statement.", smallFont)
                {
                    Alignment = Element.ALIGN_CENTER
                });
                document.Add(new Paragraph("For any queries, contact: rubanisacco@gmail.com", smallFont)
                {
                    Alignment = Element.ALIGN_CENTER
                });

                document.Close();
                writer.Close();

                return ms.ToArray();
            }
        }

        // Updated helper method with NO borders at all
        private PdfPCell CreateShareStyledCell(string text, Font font, int alignment = Element.ALIGN_LEFT)
        {
            PdfPCell cell = new PdfPCell(new Phrase(text ?? "", font));
            cell.HorizontalAlignment = alignment;
            cell.Padding = 5f;

            // REMOVE ALL BORDERS
            cell.BorderWidthLeft = 0f;
            cell.BorderWidthRight = 0f;
            cell.BorderWidthTop = 0f;
            cell.BorderWidthBottom = 0f;

            return cell;
        }

        public class SasraForm6Row
        {
            public string ReportSection { get; set; }
            public string LineItem { get; set; }
            public decimal? Amount { get; set; }
            public int DisplayOrder { get; set; }
        }

        public class SasraForm6Meta
        {
            public string SaccoName { get; set; }
            public DateTime FiscalStartDate { get; set; }
            public DateTime PeriodEndingDate { get; set; }
            public DateTime GeneratedDate { get; set; }
        }

        public class SasraReportRow
        {
            public string ReportSection { get; set; }
            public string LineItem { get; set; }
            public decimal? Amount { get; set; }
            public int DisplayOrder { get; set; }
        }

        public class SasraReportMeta
        {
            public string SaccoName { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public DateTime GeneratedDate { get; set; }
        }

        public class SasraForm5Row
        {
            public string RefNo { get; set; }
            public string Description { get; set; }
            public decimal Amount { get; set; }
        }

        public class SasraFormMeta
        {
            public string SaccoName { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public DateTime GeneratedDate { get; set; }
        }


        [HttpGet]
        [Route("GenerateSasraForm6Pdf")]
        public HttpResponseMessage GenerateSasraForm6Pdf(DateTime startDate, DateTime endDate)
        {
            var reportRows = new List<SasraForm6Row>();
            SasraForm6Meta meta = null;

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("dbo.sp_GenerateSASRAForm6_Report", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@StartDate", SqlDbType.Date).Value = startDate;
                cmd.Parameters.Add("@EndDate", SqlDbType.Date).Value = endDate;

                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    // 🔹 First result set (report body)
                    while (reader.Read())
                    {
                        reportRows.Add(new SasraForm6Row
                        {
                            ReportSection = reader["ReportSection"].ToString(),
                            LineItem = reader["LineItem"].ToString(),
                            Amount = reader["Amount"] == DBNull.Value
                                ? (decimal?)null
                                : Convert.ToDecimal(reader["Amount"]),
                            DisplayOrder = Convert.ToInt32(reader["DisplayOrder"])
                        });
                    }

                    // 🔹 Second result set (metadata)
                    if (reader.NextResult() && reader.Read())
                    {
                        meta = new SasraForm6Meta
                        {
                            SaccoName = reader["SaccoName"].ToString(),
                            FiscalStartDate = Convert.ToDateTime(reader["FiscalStartDate"]),
                            PeriodEndingDate = Convert.ToDateTime(reader["PeriodEndingDate"]),
                            GeneratedDate = Convert.ToDateTime(reader["GeneratedDate"])
                        };
                    }
                }
            }

            // ================= PDF GENERATION =================
            var ms = new MemoryStream();
            var document = new Document(PageSize.A4, 36, 36, 36, 36);
            PdfWriter.GetInstance(document, ms);

            document.Open();

            // 🔹 Fonts
            var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14);
            var sectionFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11);
            var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);
            var amountFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);

            // 🔹 Header
            document.Add(new Paragraph(meta.SaccoName, titleFont));
            document.Add(new Paragraph("SASRA FORM 6 – CAPITAL ADEQUACY REPORT", titleFont));
            document.Add(new Paragraph(
                $"Period: {meta.FiscalStartDate:dd-MMM-yyyy} to {meta.PeriodEndingDate:dd-MMM-yyyy}",
                normalFont));
            document.Add(new Paragraph($"Generated: {meta.GeneratedDate:dd-MMM-yyyy HH:mm}", normalFont));
            document.Add(new Paragraph(" "));

            // 🔹 Table with NO BORDERS
            PdfPTable table = new PdfPTable(2)
            {
                WidthPercentage = 100
            };

            // Set border width to 0 to remove all grid lines
            table.DefaultCell.BorderWidth = 0;
            table.DefaultCell.Border = Rectangle.NO_BORDER;

            table.SetWidths(new float[] { 70, 30 });

            string currentSection = null;

            foreach (var row in reportRows.OrderBy(r => r.DisplayOrder))
            {
                // Section Header
                if (row.ReportSection != currentSection)
                {
                    currentSection = row.ReportSection;

                    var sectionCell = new PdfPCell(new Phrase(row.LineItem, sectionFont))
                    {
                        Colspan = 2,
                        BackgroundColor = BaseColor.LIGHT_GRAY,
                        Padding = 5,
                        Border = Rectangle.NO_BORDER,  // Remove border from section cells too
                        BorderWidth = 0
                    };
                    table.AddCell(sectionCell);
                    continue;
                }

                // Regular row cells
                var descriptionCell = new PdfPCell(new Phrase(row.LineItem, normalFont))
                {
                    Border = Rectangle.NO_BORDER,
                    BorderWidth = 0,
                    PaddingBottom = 3  // Add some spacing between rows
                };
                table.AddCell(descriptionCell);

                var amountCell = new PdfPCell(new Phrase(
                    row.Amount.HasValue ? row.Amount.Value.ToString("N2") : "",
                    amountFont))
                {
                    HorizontalAlignment = Element.ALIGN_RIGHT,
                    Border = Rectangle.NO_BORDER,
                    BorderWidth = 0,
                    PaddingBottom = 3
                };
                table.AddCell(amountCell);
            }

            document.Add(table);
            document.Close();

            // ================= RESPONSE =================
            var result = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(ms.ToArray())
            };

            result.Content.Headers.ContentType =
                new MediaTypeHeaderValue("application/pdf");

            result.Content.Headers.ContentDisposition =
                new ContentDispositionHeaderValue("attachment")
                {
                    FileName = $"SASRA_Form6_{endDate:yyyyMMdd}.pdf"
                };

            return result;
        }


        [HttpGet]
        [Route("GenerateSasraForm2LiquidityPdf")]
        public HttpResponseMessage GenerateSasraForm2LiquidityPdf(
     DateTime startDate,
     DateTime endDate)
        {
            var rows = new List<SasraReportRow>();
            SasraReportMeta meta = null;

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(
                "dbo.sp_GenerateSASRAForm2_Liquidity_Report", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@StartDate", SqlDbType.Date).Value = startDate;
                cmd.Parameters.Add("@EndDate", SqlDbType.Date).Value = endDate;

                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    // ===== Result Set 1: Report Body =====
                    while (reader.Read())
                    {
                        rows.Add(new SasraReportRow
                        {
                            ReportSection = reader["ReportSection"].ToString(),
                            LineItem = reader["LineItem"].ToString(),
                            Amount = reader["Amount"] == DBNull.Value
                                ? (decimal?)null
                                : Convert.ToDecimal(reader["Amount"]),
                            DisplayOrder = Convert.ToInt32(reader["DisplayOrder"])
                        });
                    }

                    // ===== Result Set 2: Metadata =====
                    if (reader.NextResult() && reader.Read())
                    {
                        meta = new SasraReportMeta
                        {
                            SaccoName = reader["SaccoName"].ToString(),
                            StartDate = Convert.ToDateTime(reader["StartDate"]),
                            EndDate = Convert.ToDateTime(reader["EndDate"]),
                            GeneratedDate = Convert.ToDateTime(reader["GeneratedDate"])
                        };
                    }
                }
            }

            // ================= PDF =================
            var ms = new MemoryStream();
            var doc = new Document(PageSize.A4, 36, 36, 36, 36);
            PdfWriter.GetInstance(doc, ms);
            doc.Open();

            var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14);
            var sectionFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11);
            var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);

            // ===== Header =====
            doc.Add(new Paragraph(meta.SaccoName, titleFont));
            doc.Add(new Paragraph("SASRA FORM 2 – LIQUIDITY RETURN", titleFont));
            doc.Add(new Paragraph(
                $"Period: {meta.StartDate:dd-MMM-yyyy} to {meta.EndDate:dd-MMM-yyyy}",
                normalFont));
            doc.Add(new Paragraph(
                $"Generated: {meta.GeneratedDate:dd-MMM-yyyy HH:mm}",
                normalFont));
            doc.Add(new Paragraph(" "));

            // ===== Table =====
            PdfPTable table = new PdfPTable(2)
            {
                WidthPercentage = 100
            };
            table.SetWidths(new float[] { 70, 30 });

            // Remove all table borders
            table.DefaultCell.Border = Rectangle.NO_BORDER;

            string currentSection = null;

            foreach (var row in rows.OrderBy(r => r.DisplayOrder))
            {
                // Section headers
                if (row.Amount == null)
                {
                    var sectionCell = new PdfPCell(
                        new Phrase(row.LineItem, sectionFont))
                    {
                        Colspan = 2,
                        BackgroundColor = BaseColor.LIGHT_GRAY,
                        Padding = 6,
                        Border = Rectangle.NO_BORDER  // Remove border for section cells too
                    };
                    table.AddCell(sectionCell);
                    continue;
                }

                // Create normal cells with NO_BORDER
                var lineItemCell = new PdfPCell(
                    new Phrase(row.LineItem, normalFont))
                {
                    Border = Rectangle.NO_BORDER
                };

                var amountCell = new PdfPCell(
                    new Phrase(row.Amount.Value.ToString("N2"), normalFont))
                {
                    HorizontalAlignment = Element.ALIGN_RIGHT,
                    Border = Rectangle.NO_BORDER
                };

                table.AddCell(lineItemCell);
                table.AddCell(amountCell);
            }

            doc.Add(table);
            doc.Close();

            // ================= RESPONSE =================
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(ms.ToArray())
            };

            response.Content.Headers.ContentType =
                new MediaTypeHeaderValue("application/pdf");

            response.Content.Headers.ContentDisposition =
                new ContentDispositionHeaderValue("attachment")
                {
                    FileName = $"SASRA_Form2_Liquidity_{endDate:yyyyMMdd}.pdf"
                };

            return response;
        }

        [HttpGet]
        [Route("GenerateSasraForm5InvestmentPdf")]
        public HttpResponseMessage GenerateSasraForm5InvestmentPdf(
    DateTime startDate,
    DateTime endDate)
        {
            var rows = new List<SasraForm5Row>();
            SasraFormMeta meta = null;

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(
                "dbo.sp_GenerateSASRAForm5_Investment_Return", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@StartDate", SqlDbType.Date).Value = startDate;
                cmd.Parameters.Add("@EndDate", SqlDbType.Date).Value = endDate;

                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    // ===== Result Set 1: Form 5 Rows =====
                    while (reader.Read())
                    {
                        rows.Add(new SasraForm5Row
                        {
                            RefNo = reader["RefNo"].ToString(),
                            Description = reader["Description"].ToString(),
                            Amount = Convert.ToDecimal(reader["Amount"])
                        });
                    }

                    // ===== Result Set 2: Metadata =====
                    if (reader.NextResult() && reader.Read())
                    {
                        meta = new SasraFormMeta
                        {
                            SaccoName = reader["SaccoName"].ToString(),
                            StartDate = Convert.ToDateTime(reader["StartDate"]),
                            EndDate = Convert.ToDateTime(reader["EndDate"]),
                            GeneratedDate = Convert.ToDateTime(reader["GeneratedDate"])
                        };
                    }
                }
            }

            // ================= PDF GENERATION =================
            var ms = new MemoryStream();
            var doc = new Document(PageSize.A4, 36, 36, 36, 36);
            PdfWriter.GetInstance(doc, ms);
            doc.Open();

            var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14);
            var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11);
            var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);

            // ===== Header =====
            doc.Add(new Paragraph(meta.SaccoName, titleFont));
            doc.Add(new Paragraph("SASRA FORM 5 – INVESTMENT RETURN", titleFont));
            doc.Add(new Paragraph(
                $"Period: {meta.StartDate:dd-MMM-yyyy} to {meta.EndDate:dd-MMM-yyyy}",
                normalFont));
            doc.Add(new Paragraph(
                $"Generated: {meta.GeneratedDate:dd-MMM-yyyy HH:mm}",
                normalFont));
            doc.Add(new Paragraph(" "));

            // ===== Table =====
            PdfPTable table = new PdfPTable(3)
            {
                WidthPercentage = 100
            };
            table.SetWidths(new float[] { 15, 55, 30 });

            // Remove all table borders
            table.DefaultCell.Border = Rectangle.NO_BORDER;
            table.DefaultCell.Padding = 4;
            table.DefaultCell.PaddingBottom = 6;

            // Table header - create cells with NO_BORDER
            var refHeaderCell = new PdfPCell(new Phrase("Ref", headerFont))
            {
                Border = Rectangle.NO_BORDER,
                PaddingBottom = 8
            };

            var descHeaderCell = new PdfPCell(new Phrase("Description", headerFont))
            {
                Border = Rectangle.NO_BORDER,
                PaddingBottom = 8
            };

            var amountHeaderCell = new PdfPCell(new Phrase("Amount / %", headerFont))
            {
                HorizontalAlignment = Element.ALIGN_RIGHT,
                Border = Rectangle.NO_BORDER,
                PaddingBottom = 8
            };

            table.AddCell(refHeaderCell);
            table.AddCell(descHeaderCell);
            table.AddCell(amountHeaderCell);

            // Optional: Add a separator line after headers (if you want some visual separation)
            // If you want a clean look without any lines, remove this section
            var separatorCell = new PdfPCell(new Phrase(""))
            {
                Colspan = 3,
                Border = Rectangle.BOTTOM_BORDER,
                BorderWidthBottom = 0.5f,
                BorderColorBottom = BaseColor.LIGHT_GRAY,
                Padding = 0,
                FixedHeight = 1
            };
            table.AddCell(separatorCell);

            foreach (var row in rows)
            {
                var refCell = new PdfPCell(new Phrase(row.RefNo, normalFont))
                {
                    Border = Rectangle.NO_BORDER
                };

                var descCell = new PdfPCell(new Phrase(row.Description, normalFont))
                {
                    Border = Rectangle.NO_BORDER
                };

                var amountCell = new PdfPCell(
                    new Phrase(row.Amount.ToString("N2"), normalFont))
                {
                    HorizontalAlignment = Element.ALIGN_RIGHT,
                    Border = Rectangle.NO_BORDER
                };

                table.AddCell(refCell);
                table.AddCell(descCell);
                table.AddCell(amountCell);
            }

            doc.Add(table);
            doc.Close();

            // ================= HTTP RESPONSE =================
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(ms.ToArray())
            };

            response.Content.Headers.ContentType =
                new MediaTypeHeaderValue("application/pdf");

            response.Content.Headers.ContentDisposition =
                new ContentDispositionHeaderValue("attachment")
                {
                    FileName = $"SASRA_Form5_Investment_{endDate:yyyyMMdd}.pdf"
                };

            return response;
        }

        [HttpGet]
        [Route("GenerateSasraForm7IncomeStatementPdf")]
        public HttpResponseMessage GenerateSasraForm7IncomeStatementPdf(
    DateTime startDate,
    DateTime endDate,
    string csCode = null)
        {
            var rows = new List<SasraForm7Row>();
            SasraForm7Meta meta = null;

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("dbo.sp_GenerateSASRAForm7_Report", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@StartDate", SqlDbType.Date).Value = startDate;
                cmd.Parameters.Add("@EndDate", SqlDbType.Date).Value = endDate;
                cmd.Parameters.Add("@CSCode", SqlDbType.NVarChar, 50).Value =
                    string.IsNullOrEmpty(csCode) ? DBNull.Value : (object)csCode;

                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    // ===== Result Set 1: Form 7 Rows =====
                    while (reader.Read())
                    {
                        rows.Add(new SasraForm7Row
                        {
                            RefNo = reader["RefNo"].ToString(),
                            LineItem = reader["LineItem"].ToString(),
                            AmountInThousands = reader["AmountInThousands"].ToString(),
                            DisplayOrder = Convert.ToInt32(reader["DisplayOrder"]),
                            IsBold = Convert.ToBoolean(reader["IsBold"]),
                            IsSubTotal = Convert.ToBoolean(reader["IsSubTotal"]),
                            IsTotal = Convert.ToBoolean(reader["IsTotal"])
                        });
                    }

                    // ===== Result Set 2: Metadata =====
                    if (reader.NextResult() && reader.Read())
                    {
                        meta = new SasraForm7Meta
                        {
                            SaccoName = reader["SaccoName"].ToString(),
                            CSCode = reader["CSCode"].ToString(),
                            PeriodStartDate = Convert.ToDateTime(reader["PeriodStartDate"]),
                            PeriodEndDate = Convert.ToDateTime(reader["PeriodEndDate"]),
                            FinalNetIncome = Convert.ToDecimal(reader["FinalNetIncome"]),
                            TotalIncome = Convert.ToDecimal(reader["TotalIncome"]),
                            TotalExpense = Convert.ToDecimal(reader["TotalExpense"]),
                            GeneratedDate = Convert.ToDateTime(reader["GeneratedDate"])
                        };
                    }
                }
            }

            // ================= PDF GENERATION =================
            var ms = new MemoryStream();
            var doc = new Document(PageSize.A4.Rotate(), 25, 25, 25, 25); // Landscape for better fit
            PdfWriter.GetInstance(doc, ms);
            doc.Open();

            var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
            var subtitleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);
            var boldFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10);
            var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);
            var smallFont = FontFactory.GetFont(FontFactory.HELVETICA, 9);
            var italicFont = FontFactory.GetFont(FontFactory.HELVETICA_OBLIQUE, 9);

            // ===== Header Section =====
            // Main title
            var titleParagraph = new Paragraph("FORM 7 SASRA/007", titleFont)
            {
                Alignment = Element.ALIGN_CENTER,
                SpacingAfter = 5
            };
            doc.Add(titleParagraph);

            var subtitleParagraph = new Paragraph("STATEMENT OF COMPREHENSIVE INCOME", subtitleFont)
            {
                Alignment = Element.ALIGN_CENTER,
                SpacingAfter = 10
            };
            doc.Add(subtitleParagraph);

            // SACCO Information - Borderless table
            var saccoInfoTable = new PdfPTable(4)
            {
                WidthPercentage = 100,
                SpacingBefore = 5,
                SpacingAfter = 10
            };
            saccoInfoTable.SetWidths(new float[] { 30, 20, 30, 20 });
            saccoInfoTable.DefaultCell.Border = Rectangle.NO_BORDER; // Remove all borders

            saccoInfoTable.AddCell(CreateBorderlessCell("SACCO SOCIETY:", boldFont, Element.ALIGN_LEFT));
            saccoInfoTable.AddCell(CreateBorderlessCell(meta.SaccoName, normalFont, Element.ALIGN_LEFT));
            saccoInfoTable.AddCell(CreateBorderlessCell("CS NUMBER:", boldFont, Element.ALIGN_LEFT));
            saccoInfoTable.AddCell(CreateBorderlessCell(meta.CSCode ?? "N/A", normalFont, Element.ALIGN_LEFT));

            saccoInfoTable.AddCell(CreateBorderlessCell("FINANCIAL YEAR:", boldFont, Element.ALIGN_LEFT));
            saccoInfoTable.AddCell(CreateBorderlessCell($"{meta.PeriodStartDate:yyyy} - {meta.PeriodEndDate:yyyy}", normalFont, Element.ALIGN_LEFT));
            saccoInfoTable.AddCell(CreateBorderlessCell("REPORTING PERIOD:", boldFont, Element.ALIGN_LEFT));
            saccoInfoTable.AddCell(CreateBorderlessCell($"{meta.PeriodStartDate:dd-MMM-yyyy} to {meta.PeriodEndDate:dd-MMM-yyyy}", normalFont, Element.ALIGN_LEFT));

            saccoInfoTable.AddCell(CreateBorderlessCell("GENERATED:", boldFont, Element.ALIGN_LEFT));
            saccoInfoTable.AddCell(CreateBorderlessCell(meta.GeneratedDate.ToString("dd-MMM-yyyy HH:mm"), normalFont, Element.ALIGN_LEFT));
            saccoInfoTable.AddCell(CreateBorderlessCell("", boldFont, Element.ALIGN_LEFT));
            saccoInfoTable.AddCell(CreateBorderlessCell("", normalFont, Element.ALIGN_LEFT));

            doc.Add(saccoInfoTable);

            // ===== Income Statement Table =====
            var incomeTable = new PdfPTable(3)
            {
                WidthPercentage = 100,
                SpacingBefore = 10
            };
            incomeTable.SetWidths(new float[] { 15, 70, 15 });
            incomeTable.DefaultCell.Border = Rectangle.NO_BORDER; // Remove all borders

            // Table header - also borderless
            incomeTable.AddCell(CreateBorderlessCell("Ref No.", boldFont, Element.ALIGN_CENTER));
            incomeTable.AddCell(CreateBorderlessCell("", boldFont, Element.ALIGN_CENTER));
            incomeTable.AddCell(CreateBorderlessCell("KShs.'000'", boldFont, Element.ALIGN_CENTER));

            incomeTable.AddCell(CreateBorderlessCell("", boldFont, Element.ALIGN_CENTER));
            incomeTable.AddCell(CreateBorderlessCell("Year to Date", boldFont, Element.ALIGN_CENTER));
            incomeTable.AddCell(CreateBorderlessCell("", boldFont, Element.ALIGN_CENTER));

            // Add borderless rows
            foreach (var row in rows.Where(r => r.RefNo != "HEADER" && r.RefNo != "AUTH" && r.RefNo != "NOTE"))
            {
                // Determine font style based on row properties
                var cellFont = row.IsBold ? boldFont :
                              (row.IsSubTotal || row.IsTotal) ? boldFont : normalFont;

                // Reference Number cell
                incomeTable.AddCell(CreateBorderlessCell(row.RefNo, cellFont,
                    row.IsBold ? Element.ALIGN_LEFT : Element.ALIGN_LEFT));

                // Description cell
                incomeTable.AddCell(CreateBorderlessCell(row.LineItem, cellFont,
                    row.IsBold ? Element.ALIGN_LEFT : Element.ALIGN_LEFT));

                // Amount cell
                if (!string.IsNullOrEmpty(row.AmountInThousands))
                {
                    if (decimal.TryParse(row.AmountInThousands, out decimal amount))
                    {
                        incomeTable.AddCell(CreateBorderlessCell(amount.ToString("N2"), cellFont,
                            row.IsBold ? Element.ALIGN_RIGHT : Element.ALIGN_RIGHT));
                    }
                    else
                    {
                        incomeTable.AddCell(CreateBorderlessCell(row.AmountInThousands, cellFont,
                            row.IsBold ? Element.ALIGN_CENTER : Element.ALIGN_CENTER));
                    }
                }
                else
                {
                    incomeTable.AddCell(CreateBorderlessCell("", cellFont, Element.ALIGN_RIGHT));
                }
            }

            doc.Add(incomeTable);

            // ===== Summary Section =====
            doc.Add(new Paragraph(" "));

            var summaryTable = new PdfPTable(2)
            {
                WidthPercentage = 50,
                SpacingBefore = 20,
                HorizontalAlignment = Element.ALIGN_RIGHT
            };
            summaryTable.SetWidths(new float[] { 60, 40 });
            summaryTable.DefaultCell.Border = Rectangle.NO_BORDER; // Remove all borders

            summaryTable.AddCell(CreateBorderlessCell("Total Income:", boldFont, Element.ALIGN_LEFT));
            summaryTable.AddCell(CreateBorderlessCell((meta.TotalIncome / 1000).ToString("N2"), boldFont, Element.ALIGN_RIGHT));

            summaryTable.AddCell(CreateBorderlessCell("Total Expense:", boldFont, Element.ALIGN_LEFT));
            summaryTable.AddCell(CreateBorderlessCell((meta.TotalExpense / 1000).ToString("N2"), boldFont, Element.ALIGN_RIGHT));

            summaryTable.AddCell(CreateBorderlessCell("Net Income (After Tax & Donations):", boldFont, Element.ALIGN_LEFT));
            summaryTable.AddCell(CreateBorderlessCell((meta.FinalNetIncome / 1000).ToString("N2"), boldFont, Element.ALIGN_RIGHT));

            doc.Add(summaryTable);

            // ===== Notes and Authorization =====
            doc.Add(new Paragraph(" "));
            doc.Add(new Paragraph(" "));

            // Notes
            var notes = rows.Where(r => r.RefNo == "NOTE").ToList();
            foreach (var note in notes)
            {
                doc.Add(new Paragraph(note.LineItem, smallFont));
            }

            // Authorization section
            var authRows = rows.Where(r => r.RefNo == "AUTH").OrderBy(r => r.DisplayOrder).ToList();

            doc.Add(new Paragraph(" "));
            doc.Add(new Paragraph("AUTHORIZATION:", boldFont));

            foreach (var auth in authRows.Skip(1)) // Skip the "AUTHORIZATION:" header since we already added it
            {
                if (auth.DisplayOrder == 106) // Declaration text
                {
                    doc.Add(new Paragraph(auth.LineItem, italicFont) { SpacingBefore = 10 });
                }
                else
                {
                    var font = auth.DisplayOrder >= 107 ? normalFont : smallFont;
                    var alignment = auth.LineItem.Contains("sign") || auth.LineItem.Contains("Name") ?
                                  Element.ALIGN_LEFT : Element.ALIGN_LEFT;
                    doc.Add(new Paragraph(auth.LineItem, font) { Alignment = alignment });
                }
            }

            doc.Close();

            // ================= HTTP RESPONSE =================
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(ms.ToArray())
            };

            response.Content.Headers.ContentType =
                new MediaTypeHeaderValue("application/pdf");

            response.Content.Headers.ContentDisposition =
                new ContentDispositionHeaderValue("attachment")
                {
                    FileName = $"SASRA_Form7_IncomeStatement_{endDate:yyyyMMdd}.pdf"
                };

            return response;
        }

        // Helper method to create borderless table cells
        private PdfPCell CreateBorderlessCell(string text, Font font, int alignment)
        {
            var cell = new PdfPCell(new Phrase(text, font))
            {
                HorizontalAlignment = alignment,
                Padding = 6,
                PaddingBottom = 8,
                Border = Rectangle.NO_BORDER // This removes all border lines
            };

            return cell;
        }

        // Model classes
        public class SasraForm7Row
        {
            public string RefNo { get; set; }
            public string LineItem { get; set; }
            public string AmountInThousands { get; set; }
            public int DisplayOrder { get; set; }
            public bool IsBold { get; set; }
            public bool IsSubTotal { get; set; }
            public bool IsTotal { get; set; }
        }

        public class SasraForm7Meta
        {
            public string SaccoName { get; set; }
            public string CSCode { get; set; }
            public DateTime PeriodStartDate { get; set; }
            public DateTime PeriodEndDate { get; set; }
            public decimal FinalNetIncome { get; set; }
            public decimal TotalIncome { get; set; }
            public decimal TotalExpense { get; set; }
            public DateTime GeneratedDate { get; set; }
        }



        [HttpGet]
        [Route("GetLoanStatement/{loanCaseId}")]
        public async Task<HttpResponseMessage> GetLoanStatement(Guid loanCaseId, bool downloadPdf = false)
        {
            try
            {
                // Create a simple model for SQL results
                var customerData = new
                {
                    FirstName = "",
                    LastName = "",
                    Mobile = "",
                    Email = "",
                    Reference2 = "",
                    Reference3 = "",
                    BranchCode = 0,
                    CustomerSerialNumber = 0,
                    ProductCode = 0,
                    TargetProductCode = 0
                };

                var loanHeader = new
                {
                    LoanNumber = "",
                    LoanProductType = "",
                    AppliedLoanAmount = 0m,  // Changed from ApprovedLoanAmount
                    MonthlyRepayment = 0m,
                    CustomerAccountId = Guid.Empty,
                    MemberNumber = ""
                };

                var statementRows = new List<LoanStatementRow>();
                var summary = new LoanSummary();
                var recentTransactions = new List<RecentTransaction>(); // Not used in updated SP

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = new SqlCommand("sp_GenerateMemberLoanStatement", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.Add("@LoanCaseId", SqlDbType.UniqueIdentifier).Value = loanCaseId;

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            // Result Set 1: Loan Header
                            if (await reader.ReadAsync())
                            {
                                loanHeader = new
                                {
                                    LoanNumber = reader["LoanNumber"]?.ToString() ?? "",
                                    LoanProductType = reader["LoanProductType"]?.ToString() ?? "",
                                    AppliedLoanAmount = reader["AppliedLoanAmount"] != DBNull.Value ? Convert.ToDecimal(reader["AppliedLoanAmount"]) : 0m, // Changed
                                    MonthlyRepayment = reader["MonthlyRepayment"] != DBNull.Value ? Convert.ToDecimal(reader["MonthlyRepayment"]) : 0m,
                                    CustomerAccountId = reader["CustomerAccountId"] != DBNull.Value ? (Guid)reader["CustomerAccountId"] : Guid.Empty,
                                    MemberNumber = reader["MemberNumber"]?.ToString() ?? ""
                                };
                            }
                            else
                            {
                                // If no loan header found, return not found
                                var errorResponse = Request.CreateResponse(HttpStatusCode.NotFound);
                                errorResponse.Content = new StringContent(
                                    JsonConvert.SerializeObject(new ApiResponse<object>
                                    {
                                        Success = false,
                                        Message = "Loan case not found.",
                                        Data = null
                                    }),
                                    Encoding.UTF8,
                                    "application/json");
                                return errorResponse;
                            }

                            // Result Set 2: Statement rows
                            if (await reader.NextResultAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    var row = new LoanStatementRow
                                    {
                                        PostingDate = reader["PostingDate"] != DBNull.Value ? Convert.ToDateTime(reader["PostingDate"]).ToString("yyyy-MM-dd") : "",
                                        TransactionType = reader["TransactionType"]?.ToString() ?? "",
                                        DocumentNo = reader["DocumentNo"]?.ToString() ?? "",
                                        Description = reader["Description"]?.ToString() ?? "",
                                        Debit = reader["Debit"] != DBNull.Value ? Convert.ToDecimal(reader["Debit"]) : 0m,
                                        Credit = reader["Credit"] != DBNull.Value ? Convert.ToDecimal(reader["Credit"]) : 0m,
                                        Balance = reader["RunningBalance"] != DBNull.Value ? Convert.ToDecimal(reader["RunningBalance"]) : 0m // Changed from Balance to RunningBalance
                                    };
                                    statementRows.Add(row);
                                }
                            }

                            // Result Set 3: Summary
                            if (await reader.NextResultAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    summary = new LoanSummary
                                    {
                                        TotalDisbursed = reader["TotalDisbursed"] != DBNull.Value ? Convert.ToDecimal(reader["TotalDisbursed"]) : 0m,
                                        TotalPrincipalRepaid = reader["TotalPrincipalPaid"] != DBNull.Value ? Convert.ToDecimal(reader["TotalPrincipalPaid"]) : 0m, // Changed from TotalPrincipalRepaid
                                        TotalInterestPaid = reader["TotalInterestPaid"] != DBNull.Value ? Convert.ToDecimal(reader["TotalInterestPaid"]) : 0m,
                                        TotalInterestAccrued = reader["TotalInterestCharged"] != DBNull.Value ? Convert.ToDecimal(reader["TotalInterestCharged"]) : 0m, // Changed from TotalInterestAccrued
                                        OutstandingLoanAmount = reader["OutstandingPrincipal"] != DBNull.Value ? Convert.ToDecimal(reader["OutstandingPrincipal"]) : 0m, // Changed from OutstandingLoanAmount
                                        OutstandingLoanInterest = reader["OutstandingInterest"] != DBNull.Value ? Convert.ToDecimal(reader["OutstandingInterest"]) : 0m, // Changed from OutstandingLoanInterest
                                        TotalOutstandingBalance = reader["TotalOutstandingBalance"] != DBNull.Value ? Convert.ToDecimal(reader["TotalOutstandingBalance"]) : 0m
                                    };
                                }
                            }

                            // Note: The updated stored procedure only returns 3 result sets, not 4
                            // So we don't need to read the 4th result set (RecentTransactions)
                        }
                    }

                    // Get customer details using CustomerAccountId from loan header
                    if (loanHeader.CustomerAccountId != Guid.Empty)
                    {
                        string customerQuery = @"
                SELECT TOP 1 
                    c.Individual_FirstName,
                    c.Individual_LastName,
                    c.Address_MobileLine,
                    c.Address_Email,
                    c.Reference2,
                    c.Reference3,
                    ISNULL(b.Code, 0) as BranchCode,
                    ISNULL(c.SerialNumber, 0) as CustomerSerialNumber,
                    ISNULL(ca.CustomerAccountType_ProductCode, 0) as ProductCode,
                    ISNULL(ca.CustomerAccountType_TargetProductCode, 0) as TargetProductCode
                FROM [SwiftFinancialsDB_Live].[dbo].[swiftFin_CustomerAccounts] ca
                INNER JOIN [SwiftFinancialsDB_Live].[dbo].[swiftFin_Customers] c 
                    ON ca.CustomerId = c.Id
                LEFT JOIN [SwiftFinancialsDB_Live].[dbo].[swiftFin_Branches] b
                    ON ca.BranchId = b.Id
                WHERE ca.Id = @CustomerAccountId";

                        using (var cmd = new SqlCommand(customerQuery, connection))
                        {
                            cmd.Parameters.Add("@CustomerAccountId", SqlDbType.UniqueIdentifier).Value = loanHeader.CustomerAccountId;

                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    customerData = new
                                    {
                                        FirstName = reader["Individual_FirstName"]?.ToString() ?? "",
                                        LastName = reader["Individual_LastName"]?.ToString() ?? "",
                                        Mobile = reader["Address_MobileLine"]?.ToString() ?? "",
                                        Email = reader["Address_Email"]?.ToString() ?? "",
                                        Reference2 = reader["Reference2"]?.ToString() ?? "",
                                        Reference3 = reader["Reference3"]?.ToString() ?? "",
                                        BranchCode = reader["BranchCode"] != DBNull.Value ? Convert.ToInt32(reader["BranchCode"]) : 0,
                                        CustomerSerialNumber = reader["CustomerSerialNumber"] != DBNull.Value ? Convert.ToInt32(reader["CustomerSerialNumber"]) : 0,
                                        ProductCode = reader["ProductCode"] != DBNull.Value ? Convert.ToInt32(reader["ProductCode"]) : 0,
                                        TargetProductCode = reader["TargetProductCode"] != DBNull.Value ? Convert.ToInt32(reader["TargetProductCode"]) : 0
                                    };
                                }
                            }
                        }
                    }
                }

                // Build the full account number
                string fullAccountNumber = string.Format("{0}-{1}-{2}-{3}",
                    customerData.BranchCode.ToString().PadLeft(3, '0'),
                    customerData.CustomerSerialNumber.ToString().PadLeft(7, '0'),
                    customerData.ProductCode.ToString().PadLeft(3, '0'),
                    customerData.TargetProductCode.ToString().PadLeft(3, '0'));

                // Create the result object
                var loanStatementResult = new
                {
                    Customer = new
                    {
                        FullName = $"{customerData.FirstName} {customerData.LastName}".Trim(),
                        AccountNumber = fullAccountNumber,
                        StaffNo = customerData.Reference2,
                        PFNumber = customerData.Reference3,
                        Mobile = customerData.Mobile,
                        Email = customerData.Email
                    },
                    LoanDetails = new
                    {
                        LoanNumber = loanHeader.LoanNumber,
                        LoanProductType = loanHeader.LoanProductType,
                        AppliedAmount = loanHeader.AppliedLoanAmount, // Changed from ApprovedAmount
                        MonthlyRepayment = loanHeader.MonthlyRepayment,
                        MemberNumber = loanHeader.MemberNumber
                    },
                    Statement = statementRows,
                    Summary = summary
                    // Note: RecentTransactions is not included as it's not returned by the updated SP
                };

                // If PDF download is requested
                if (downloadPdf)
                {
                    byte[] pdfBytes = GenerateLoanStatementPdf(customerData, loanHeader, fullAccountNumber,
                        statementRows, summary, recentTransactions);

                    var response = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new ByteArrayContent(pdfBytes)
                    };

                    response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

                    string customerName = $"{customerData.FirstName}_{customerData.LastName}".Replace(" ", "_");
                    response.Content.Headers.ContentDisposition =
                        new ContentDispositionHeaderValue("attachment")
                        {
                            FileName = $"LoanStatement_{loanHeader.LoanNumber}_{customerName}_{DateTime.Now:yyyyMMdd}.pdf"
                        };

                    return response;
                }
                else
                {
                    // Return JSON response
                    var response = Request.CreateResponse(HttpStatusCode.OK);
                    response.Content = new StringContent(
                        JsonConvert.SerializeObject(new ApiResponse<object>
                        {
                            Success = true,
                            Message = statementRows.Count > 0 ?
                                $"Loan statement retrieved successfully. Outstanding Balance: {summary.TotalOutstandingBalance:C}" :
                                "No transactions found for this loan.",
                            Data = loanStatementResult
                        }),
                        Encoding.UTF8,
                        "application/json");
                    return response;
                }
            }
            catch (Exception ex)
            {
                var response = Request.CreateResponse(HttpStatusCode.InternalServerError);
                response.Content = new StringContent(
                    JsonConvert.SerializeObject(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "An error occurred while retrieving loan statement.",
                        Data = ex.Message + " | Inner: " + (ex.InnerException?.Message ?? "None")
                    }),
                    Encoding.UTF8,
                    "application/json");
                return response;
            }
        }

        private byte[] GenerateLoanStatementPdf(dynamic customerData, dynamic loanHeader, string fullAccountNumber,
    List<LoanStatementRow> statementRows, LoanSummary summary, List<RecentTransaction> recentTransactions)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                // Create document
                Document document = new Document(PageSize.A4, 30, 30, 50, 30);
                PdfWriter writer = PdfWriter.GetInstance(document, ms);

                document.Open();

                // ===== RUBANI SACCO COLOR THEME =====
                BaseColor SkyBlue = new BaseColor(0, 174, 239); // #00AEEF
                BaseColor Red = new BaseColor(255, 0, 0);       // #FF0000
                BaseColor DarkGray = new BaseColor(26, 26, 26); // #1A1A1A
                BaseColor LightGray = new BaseColor(217, 217, 217); // #D9D9D9
                BaseColor White = BaseColor.WHITE;

                // Fonts
                Font titleFont = new Font(FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16, DarkGray));
                Font headerFont = new Font(FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, DarkGray));
                Font subHeaderFont = new Font(FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11, White));
                Font normalFont = new Font(FontFactory.GetFont(FontFactory.HELVETICA, 9));
                Font boldFont = new Font(FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9));
                Font smallFont = new Font(FontFactory.GetFont(FontFactory.HELVETICA, 8, DarkGray));
                Font companyNameFont = new Font(FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14, SkyBlue));
                Font companyInfoFont = new Font(FontFactory.GetFont(FontFactory.HELVETICA, 10));

                // ===== CUSTOM HEADER WITH RUBANI SACCO LOGO =====
                try
                {
                    // Create a table with 1 column for left-aligned content
                    PdfPTable headerTable = new PdfPTable(1)
                    {
                        WidthPercentage = 100,
                        SpacingAfter = 10f
                    };

                    // Row 1: Logo left-aligned at top
                    PdfPCell logoCell = new PdfPCell();
                    logoCell.Border = Rectangle.NO_BORDER;
                    logoCell.HorizontalAlignment = Element.ALIGN_LEFT; // Changed from CENTER to LEFT
                    logoCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    logoCell.PaddingBottom = 5f;

                    // Try to load logo from local path
                    string logoPath = @"C:\Users\ADMIN\source\repos\SwiftFinancialsNew\SwiftFinancialsSolution\TestApis\Assets\Images\rubani-logo.jpeg";
                    if (File.Exists(logoPath))
                    {
                        try
                        {
                            Image logo = Image.GetInstance(logoPath);
                            logo.ScaleToFit(100, 100); // Increased size for better visibility
                            logoCell.AddElement(logo);
                        }
                        catch (Exception)
                        {
                            // Fallback to text if image fails to load
                            logoCell.AddElement(new Paragraph("RUBANI SACCO", companyNameFont)
                            {
                                Alignment = Element.ALIGN_LEFT // Changed from CENTER to LEFT
                            });
                        }
                    }
                    else
                    {
                        // Use text if no logo file
                        logoCell.AddElement(new Paragraph("RUBANI SACCO", companyNameFont)
                        {
                            Alignment = Element.ALIGN_LEFT // Changed from CENTER to LEFT
                        });
                    }

                    headerTable.AddCell(logoCell);

                    // Row 2: Company Info - LEFT ALIGNED
                    PdfPCell infoCell = new PdfPCell();
                    infoCell.Border = Rectangle.NO_BORDER;
                    infoCell.HorizontalAlignment = Element.ALIGN_LEFT; // Changed from CENTER to LEFT
                    infoCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    infoCell.PaddingTop = 5f;

                    // Company name - LEFT ALIGNED
                    var companyNamePara = new Paragraph("RUBANI SACCO", companyNameFont)
                    {
                        Alignment = Element.ALIGN_LEFT // Changed from CENTER to LEFT
                    };
                    infoCell.AddElement(companyNamePara);

                    // Address - LEFT ALIGNED
                    var address = new Paragraph("Rubani House, Off Airport North Embakasi", companyInfoFont)
                    {
                        Alignment = Element.ALIGN_LEFT // Changed from CENTER to LEFT
                    };
                    infoCell.AddElement(address);

                    // Email - LEFT ALIGNED
                    var email = new Paragraph("rubanisacco@gmail.com", companyInfoFont)
                    {
                        Alignment = Element.ALIGN_LEFT // Changed from CENTER to LEFT
                    };
                    infoCell.AddElement(email);

                    headerTable.AddCell(infoCell);

                    document.Add(headerTable);

                    // Add decorative line (Blue-Red-Blue) - Still centered for visual appeal
                    var lineTable = new PdfPTable(3)
                    {
                        WidthPercentage = 100,
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        SpacingAfter = 10f
                    };
                    lineTable.SetWidths(new float[] { 33, 34, 33 });

                    lineTable.AddCell(new PdfPCell()
                    {
                        BackgroundColor = SkyBlue,
                        FixedHeight = 2f,
                        Border = Rectangle.NO_BORDER
                    });

                    lineTable.AddCell(new PdfPCell()
                    {
                        BackgroundColor = Red,
                        FixedHeight = 2f,
                        Border = Rectangle.NO_BORDER
                    });

                    lineTable.AddCell(new PdfPCell()
                    {
                        BackgroundColor = SkyBlue,
                        FixedHeight = 2f,
                        Border = Rectangle.NO_BORDER
                    });

                    document.Add(lineTable);
                }
                catch (Exception)
                {
                    // Fallback header with left alignment
                    var fallbackPara = new Paragraph("RUBANI SACCO\nRubani House, Off Airport North Embakasi\nrubanisacco@gmail.com",
                        companyInfoFont)
                    {
                        Alignment = Element.ALIGN_LEFT, // Changed from CENTER to LEFT
                        SpacingAfter = 15f
                    };
                    document.Add(fallbackPara);
                }

                // ===== STATEMENT TITLE =====
                // Keep title centered for emphasis
                document.Add(new Paragraph("LOAN STATEMENT", titleFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 10f
                });

                // ===== MEMBER INFORMATION SECTION =====
                string fullName = $"{customerData.FirstName} {customerData.LastName}".Trim().ToUpper();
                string staffNo = customerData.Reference2;
                string pfNumber = customerData.Reference3;

                // Member info - LEFT ALIGNED
                Paragraph memberInfo = new Paragraph();
                memberInfo.Alignment = Element.ALIGN_LEFT; // Changed from CENTER to LEFT

                // Add member info with proper formatting
                memberInfo.Add(new Chunk("Name: ", boldFont));
                memberInfo.Add(new Chunk(fullName, normalFont));
                memberInfo.Add(new Chunk("   MemberNo: ", boldFont));
                memberInfo.Add(new Chunk(staffNo ?? "N/A", normalFont));
                memberInfo.Add(Chunk.NEWLINE);

                memberInfo.Add(new Chunk("Account No: ", boldFont));
                memberInfo.Add(new Chunk(fullAccountNumber, normalFont));
                memberInfo.Add(new Chunk("   Company: ", boldFont));
                memberInfo.Add(new Chunk("RUBANI SACCO", normalFont));

                memberInfo.SpacingAfter = 15f;
                document.Add(memberInfo);

                // ===== LOAN DETAILS SECTION =====
                // Loan details - LEFT ALIGNED
                Paragraph loanDetails = new Paragraph();
                loanDetails.Alignment = Element.ALIGN_LEFT; // Changed from CENTER to LEFT

                loanDetails.Add(new Chunk("Loan Number: ", boldFont));
                loanDetails.Add(new Chunk(loanHeader.LoanNumber, normalFont));
                loanDetails.Add(new Chunk("   Loan Product: ", boldFont));
                loanDetails.Add(new Chunk(loanHeader.LoanProductType, normalFont));
                loanDetails.Add(Chunk.NEWLINE);

                loanDetails.Add(new Chunk("Applied Amount: ", boldFont));
                loanDetails.Add(new Chunk(loanHeader.AppliedLoanAmount.ToString("N2"), normalFont));
                loanDetails.Add(new Chunk("   Monthly Repayment: ", boldFont));
                loanDetails.Add(new Chunk(loanHeader.MonthlyRepayment.ToString("N2"), normalFont));

                loanDetails.SpacingAfter = 5f;
                document.Add(loanDetails);

                // ===== ADD TOTAL OUTSTANDING BALANCE BELOW MONTHLY REPAYMENT =====
                Paragraph outstandingBalance = new Paragraph();
                outstandingBalance.Alignment = Element.ALIGN_LEFT; // Changed from CENTER to LEFT

                outstandingBalance.Add(new Chunk("TOTAL OUTSTANDING BALANCE: ",
                    FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11, DarkGray)));
                outstandingBalance.Add(new Chunk(summary.OutstandingLoanAmount.ToString("N2"),
                    FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11, Red)));

                outstandingBalance.SpacingAfter = 15f;
                document.Add(outstandingBalance);

                // ===== LOAN TRANSACTIONS SECTION =====
                if (statementRows != null && statementRows.Count > 0)
                {
                    // Section header - Keep centered for visual hierarchy
                    var sectionHeader = new Paragraph("LOAN TRANSACTIONS",
                        FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, White))
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingAfter = 5f
                    };

                    // Create a background for the header
                    PdfPTable headerBgTable = new PdfPTable(1)
                    {
                        WidthPercentage = 100,
                        SpacingAfter = 10f
                    };

                    PdfPCell headerCell = new PdfPCell(sectionHeader)
                    {
                        BackgroundColor = DarkGray,
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        Padding = 8,
                        Border = Rectangle.NO_BORDER,
                        BorderWidthBottom = 2f,
                        BorderColorBottom = SkyBlue
                    };
                    headerBgTable.AddCell(headerCell);
                    document.Add(headerBgTable);

                    // Transactions table
                    PdfPTable transTable = new PdfPTable(6)
                    {
                        WidthPercentage = 100,
                        SpacingAfter = 5f
                    };
                    // Column widths: Date, Type, Description, Debit, Credit, RunningBalance
                    transTable.SetWidths(new float[] { 12, 12, 40, 18, 18, 20 });

                    // Table headers
                    string[] headers = { "Date", "Type", "Description", "Debit", "Credit", "Running Balance" };

                    // Add header cells with NO BORDERS
                    for (int i = 0; i < headers.Length; i++)
                    {
                        PdfPCell headerCellItem = new PdfPCell(new Phrase(headers[i], headerFont))
                        {
                            BackgroundColor = LightGray,
                            HorizontalAlignment = Element.ALIGN_CENTER,
                            Padding = 5,
                            // NO BORDERS - remove all border widths
                            BorderWidthTop = 0f,
                            BorderWidthBottom = 0f,
                            BorderWidthLeft = 0f,
                            BorderWidthRight = 0f
                        };
                        transTable.AddCell(headerCellItem);
                    }

                    // Add transactions with NO BORDERS between rows
                    for (int rowIndex = 0; rowIndex < statementRows.Count; rowIndex++)
                    {
                        var row = statementRows[rowIndex];

                        // Date cell - NO BORDERS
                        PdfPCell dateCell = new PdfPCell(new Phrase(row.PostingDate, normalFont));
                        dateCell.HorizontalAlignment = Element.ALIGN_CENTER;
                        dateCell.Padding = 5f;
                        // NO BORDERS - remove all border widths
                        dateCell.BorderWidthTop = 0f;
                        dateCell.BorderWidthBottom = 0f;
                        dateCell.BorderWidthLeft = 0f;
                        dateCell.BorderWidthRight = 0f;
                        transTable.AddCell(dateCell);

                        // Type cell - NO BORDERS
                        PdfPCell typeCell = new PdfPCell(new Phrase(row.TransactionType, normalFont));
                        typeCell.HorizontalAlignment = Element.ALIGN_CENTER;
                        typeCell.Padding = 5f;
                        // NO BORDERS - remove all border widths
                        typeCell.BorderWidthTop = 0f;
                        typeCell.BorderWidthBottom = 0f;
                        typeCell.BorderWidthLeft = 0f;
                        typeCell.BorderWidthRight = 0f;
                        transTable.AddCell(typeCell);

                        // Description cell - NO BORDERS
                        PdfPCell descCell = new PdfPCell(new Phrase(row.Description ?? "", normalFont));
                        descCell.HorizontalAlignment = Element.ALIGN_LEFT;
                        descCell.Padding = 5f;
                        // NO BORDERS - remove all border widths
                        descCell.BorderWidthTop = 0f;
                        descCell.BorderWidthBottom = 0f;
                        descCell.BorderWidthLeft = 0f;
                        descCell.BorderWidthRight = 0f;
                        transTable.AddCell(descCell);

                        // Debit cell - NO BORDERS
                        PdfPCell debitCell = new PdfPCell(new Phrase(row.Debit.ToString("N2"), normalFont));
                        debitCell.HorizontalAlignment = Element.ALIGN_RIGHT;
                        debitCell.Padding = 5f;
                        // NO BORDERS - remove all border widths
                        debitCell.BorderWidthTop = 0f;
                        debitCell.BorderWidthBottom = 0f;
                        debitCell.BorderWidthLeft = 0f;
                        debitCell.BorderWidthRight = 0f;
                        transTable.AddCell(debitCell);

                        // Credit cell - NO BORDERS
                        PdfPCell creditCell = new PdfPCell(new Phrase(row.Credit.ToString("N2"), normalFont));
                        creditCell.HorizontalAlignment = Element.ALIGN_RIGHT;
                        creditCell.Padding = 5f;
                        // NO BORDERS - remove all border widths
                        creditCell.BorderWidthTop = 0f;
                        creditCell.BorderWidthBottom = 0f;
                        creditCell.BorderWidthLeft = 0f;
                        creditCell.BorderWidthRight = 0f;
                        transTable.AddCell(creditCell);

                        // Running Balance cell - NO BORDERS
                        // Handle NULL values for Interest Paid rows
                        string balanceText = row.Balance == 0 && row.TransactionType == "Interest Paid"
                            ? ""  // Empty string for Interest Paid rows
                            : row.Balance.ToString("N2");

                        PdfPCell balanceCell = new PdfPCell(new Phrase(balanceText, normalFont));
                        balanceCell.HorizontalAlignment = Element.ALIGN_RIGHT;
                        balanceCell.Padding = 5f;
                        // NO BORDERS - remove all border widths
                        balanceCell.BorderWidthTop = 0f;
                        balanceCell.BorderWidthBottom = 0f;
                        balanceCell.BorderWidthLeft = 0f;
                        balanceCell.BorderWidthRight = 0f;
                        transTable.AddCell(balanceCell);
                    }

                    document.Add(transTable);
                }
                else
                {
                    var noTransPara = new Paragraph("No transactions found", normalFont)
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingBefore = 20f,
                        SpacingAfter = 20f
                    };
                    document.Add(noTransPara);
                }

                // ===== CUSTOM FOOTER =====
                document.Add(new Paragraph("\n"));
                var footerPara = new Paragraph(
                    $"Statement Date: {DateTime.Now:dd/MM/yyyy} | Printed on: {DateTime.Now:dd/MM/yyyy HH:mm:ss} | Page: 1",
                    smallFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingBefore = 10f
                };
                document.Add(footerPara);

                // ===== FOOTER NOTES =====
                document.Add(new Paragraph("\n"));
                document.Add(new Paragraph("This is a system generated statement.", smallFont)
                {
                    Alignment = Element.ALIGN_CENTER
                });
                document.Add(new Paragraph("For any queries, contact: rubanisacco@gmail.com", smallFont)
                {
                    Alignment = Element.ALIGN_CENTER
                });

                document.Close();
                writer.Close();

                return ms.ToArray();
            }
        }

        // Helper method to add summary rows
        private void AddSummaryRow(PdfPTable table, string label, string value, Font labelFont, Font valueFont)
        {
            PdfPCell labelCell = new PdfPCell(new Phrase(label, labelFont));
            labelCell.Border = Rectangle.NO_BORDER;
            labelCell.Padding = 5f;
            table.AddCell(labelCell);

            PdfPCell valueCell = new PdfPCell(new Phrase(value, valueFont));
            valueCell.Border = Rectangle.NO_BORDER;
            valueCell.HorizontalAlignment = Element.ALIGN_RIGHT;
            valueCell.Padding = 5f;
            table.AddCell(valueCell);
        }










        [HttpGet]
        [Route("GetMemberStatement/{customerId}")]
        public async Task<HttpResponseMessage> GetMemberStatement(
     Guid customerId,
     DateTime? startDate = null,
     DateTime? endDate = null,
     bool downloadPdf = false)
        {
            try
            {
                var memberStatement = new MemberStatementResult
                {
                    CustomerId = customerId,
                    StartDate = startDate,
                    EndDate = endDate
                };

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // ===== GET LOANS INFORMATION =====
                    var allLoanStatements = new List<LoanStatementResult>();

                    // ADD BACK THE LOAN STORED PROCEDURE CALL
                    using (var loanCommand = new SqlCommand("sp_GenerateMemberLoanStatement", connection))
                    {
                        loanCommand.CommandType = CommandType.StoredProcedure;
                        loanCommand.Parameters.Add("@CustomerId", SqlDbType.UniqueIdentifier).Value = customerId;

                        if (startDate.HasValue)
                            loanCommand.Parameters.Add("@StartDate", SqlDbType.Date).Value = startDate.Value.Date;
                        else
                            loanCommand.Parameters.Add("@StartDate", SqlDbType.Date).Value = DBNull.Value;

                        if (endDate.HasValue)
                            loanCommand.Parameters.Add("@EndDate", SqlDbType.Date).Value = endDate.Value.Date;
                        else
                            loanCommand.Parameters.Add("@EndDate", SqlDbType.Date).Value = DBNull.Value;

                        using (var reader = await loanCommand.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                // First result set: Loan Header for current loan
                                var loanHeader = new
                                {
                                    LoanNumber = reader["LoanNumber"]?.ToString() ?? "",
                                    LoanProductType = reader["LoanProductType"]?.ToString() ?? "",
                                    AppliedLoanAmount = reader["AppliedLoanAmount"] != DBNull.Value ? Convert.ToDecimal(reader["AppliedLoanAmount"]) : 0m,
                                    MonthlyRepayment = reader["MonthlyRepayment"] != DBNull.Value ? Convert.ToDecimal(reader["MonthlyRepayment"]) : 0m,
                                    CustomerAccountId = reader["CustomerAccountId"] != DBNull.Value ? (Guid)reader["CustomerAccountId"] : Guid.Empty,
                                    MemberNumber = reader["MemberNumber"]?.ToString() ?? "",
                                    DisbursedDate = reader["DisbursedDate"] != DBNull.Value ?
                                        Convert.ToDateTime(reader["DisbursedDate"]).ToString("yyyy-MM-dd") : ""
                                };

                                var statementRows = new List<LoanStatementRow>();
                                var summary = new LoanSummary();
                                DateTime? statementStartDate = null;
                                DateTime? statementEndDate = null;

                                // Result Set 2: Statement rows
                                if (await reader.NextResultAsync())
                                {
                                    while (await reader.ReadAsync())
                                    {
                                        var row = new LoanStatementRow
                                        {
                                            TransDate = reader["TransDate"] != DBNull.Value ?
                                                Convert.ToDateTime(reader["TransDate"]).ToString("yyyy-MM-dd") : "",
                                            OpeningBalance = reader["OpeningBalance"] != DBNull.Value ?
                                                Convert.ToDecimal(reader["OpeningBalance"]) : 0m,
                                            Principle = reader["Principle"] != DBNull.Value ?
                                                Convert.ToDecimal(reader["Principle"]) : 0m,
                                            Interest = reader["Interest"] != DBNull.Value ?
                                                Convert.ToDecimal(reader["Interest"]) : 0m,
                                            Amount = reader["Amount"] != DBNull.Value ?
                                                Convert.ToDecimal(reader["Amount"]) : 0m,
                                            LoanBalance = reader["LoanBalance"] != DBNull.Value ?
                                                Convert.ToDecimal(reader["LoanBalance"]) : 0m,
                                            PostingDate = reader["TransDate"] != DBNull.Value ?
                                                Convert.ToDateTime(reader["TransDate"]).ToString("yyyy-MM-dd") : "",
                                            Balance = reader["LoanBalance"] != DBNull.Value ?
                                                Convert.ToDecimal(reader["LoanBalance"]) : 0m
                                        };
                                        statementRows.Add(row);
                                    }
                                }

                                // Result Set 3: Summary
                                if (await reader.NextResultAsync())
                                {
                                    if (await reader.ReadAsync())
                                    {
                                        summary = new LoanSummary
                                        {
                                            TotalDisbursed = reader["TotalDisbursed"] != DBNull.Value ? Convert.ToDecimal(reader["TotalDisbursed"]) : 0m,
                                            TotalPrincipalRepaid = reader["TotalPrincipalPaid"] != DBNull.Value ? Convert.ToDecimal(reader["TotalPrincipalPaid"]) : 0m,
                                            TotalInterestPaid = reader["TotalInterestPaid"] != DBNull.Value ? Convert.ToDecimal(reader["TotalInterestPaid"]) : 0m,
                                            TotalInterestAccrued = reader["TotalInterestCharged"] != DBNull.Value ? Convert.ToDecimal(reader["TotalInterestCharged"]) : 0m,
                                            OutstandingLoanAmount = reader["OutstandingPrincipal"] != DBNull.Value ? Convert.ToDecimal(reader["OutstandingPrincipal"]) : 0m,
                                            OutstandingLoanInterest = reader["OutstandingInterest"] != DBNull.Value ? Convert.ToDecimal(reader["OutstandingInterest"]) : 0m,
                                            TotalOutstandingBalance = reader["TotalOutstandingBalance"] != DBNull.Value ? Convert.ToDecimal(reader["TotalOutstandingBalance"]) : 0m,
                                            OpeningBalance = reader["OpeningBalance"] != DBNull.Value ? Convert.ToDecimal(reader["OpeningBalance"]) : 0m
                                        };

                                        if (reader["StartDate"] != DBNull.Value)
                                            statementStartDate = Convert.ToDateTime(reader["StartDate"]);
                                        if (reader["EndDate"] != DBNull.Value)
                                            statementEndDate = Convert.ToDateTime(reader["EndDate"]);
                                    }
                                }

                                // Get customer details for this loan
                                var customerData = await GetCustomerDetails(connection, loanHeader.CustomerAccountId, customerId);

                                // Build the full account number
                                string fullAccountNumber = string.Format("{0}-{1}-{2}-{3}",
                                    customerData.BranchCode.ToString().PadLeft(3, '0'),
                                    customerData.CustomerSerialNumber.ToString().PadLeft(7, '0'),
                                    customerData.ProductCode.ToString().PadLeft(3, '0'),
                                    customerData.TargetProductCode.ToString().PadLeft(3, '0'));

                                // Create the loan statement result
                                var loanStatementResult = new LoanStatementResult
                                {
                                    LoanNumber = loanHeader.LoanNumber,
                                    Customer = new CustomerInfo
                                    {
                                        FullName = $"{customerData.FirstName} {customerData.LastName}".Trim(),
                                        AccountNumber = fullAccountNumber,
                                        StaffNo = customerData.Reference2,
                                        PFNumber = customerData.Reference3,
                                        Mobile = customerData.Mobile,
                                        Email = customerData.Email
                                    },
                                    LoanDetails = new LoanDetails
                                    {
                                        LoanNumber = loanHeader.LoanNumber,
                                        LoanProductType = loanHeader.LoanProductType,
                                        AppliedAmount = loanHeader.AppliedLoanAmount,
                                        MonthlyRepayment = loanHeader.MonthlyRepayment,
                                        MemberNumber = loanHeader.MemberNumber,
                                        DisbursedDate = loanHeader.DisbursedDate
                                    },
                                    Statement = statementRows,
                                    Summary = summary,
                                    StartDate = statementStartDate,
                                    EndDate = statementEndDate
                                };

                                allLoanStatements.Add(loanStatementResult);

                                // Move to next loan (if any)
                                await reader.NextResultAsync();
                            }
                        }
                    }

                    // ===== GET SHARES INFORMATION =====
                    var allSharesStatements = new List<SharesStatementResult>();

                    using (var sharesCommand = new SqlCommand("sp_GenerateAllSharesStatement", connection))
                    {
                        sharesCommand.CommandType = CommandType.StoredProcedure;
                        sharesCommand.Parameters.Add("@CustomerId", SqlDbType.UniqueIdentifier).Value = customerId;

                        if (startDate.HasValue)
                            sharesCommand.Parameters.Add("@StartDate", SqlDbType.Date).Value = startDate.Value.Date;
                        else
                            sharesCommand.Parameters.Add("@StartDate", SqlDbType.Date).Value = DBNull.Value;

                        if (endDate.HasValue)
                            sharesCommand.Parameters.Add("@EndDate", SqlDbType.Date).Value = endDate.Value.Date;
                        else
                            sharesCommand.Parameters.Add("@EndDate", SqlDbType.Date).Value = DBNull.Value;

                        using (var reader = await sharesCommand.ExecuteReaderAsync())
                        {
                            // Dictionary to group transactions by account
                            var accountTransactions = new Dictionary<Guid, List<SharesTransaction>>();
                            var accountDetails = new Dictionary<Guid, (string ProductName, decimal TotalContribution)>();

                            // SKIP OUTPUT 0: Account Header (first result set)
                            if (await reader.NextResultAsync())
                            {
                                // First result set is now the Detailed Statement (OUTPUT 1)
                                while (await reader.ReadAsync())
                                {
                                    // Skip if it's a message result set
                                    if (reader.FieldCount == 1 && reader.GetName(0) == "Message")
                                        continue;

                                    var customerAccountId = reader["CustomerAccountId"] != DBNull.Value ?
                                        (Guid)reader["CustomerAccountId"] : Guid.Empty;

                                    var transaction = new SharesTransaction
                                    {
                                        TransactionDate = reader["Date"]?.ToString() ?? "",
                                        Description = reader["Description"]?.ToString() ?? "",
                                        DepositAmount = reader["Share Contribution"] != DBNull.Value ?
                                            Convert.ToDecimal(reader["Share Contribution"]) : 0m,
                                        WithdrawalAmount = 0m,
                                        RunningBalance = reader["Cumulative"] != DBNull.Value ?
                                            Convert.ToDecimal(reader["Cumulative"]) : 0m
                                    };

                                    if (!accountTransactions.ContainsKey(customerAccountId))
                                        accountTransactions[customerAccountId] = new List<SharesTransaction>();

                                    accountTransactions[customerAccountId].Add(transaction);
                                }
                            }

                            // Move to Summary result set (OUTPUT 2)
                            if (await reader.NextResultAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    // Skip if it's a message
                                    if (reader.FieldCount == 1 && reader.GetName(0) == "Message")
                                        continue;

                                    var customerAccountId = reader["CustomerAccountId"] != DBNull.Value ?
                                        (Guid)reader["CustomerAccountId"] : Guid.Empty;
                                    var productName = reader["ProductName"]?.ToString() ?? "";
                                    var totalContribution = reader["TotalContribution"] != DBNull.Value ?
                                        Convert.ToDecimal(reader["TotalContribution"]) : 0m;

                                    accountDetails[customerAccountId] = (productName, totalContribution);
                                }
                            }

                            // Skip the third result set (Summary Stats - OUTPUT 3)
                            // We don't need it, but we need to advance the reader
                            await reader.NextResultAsync();

                            // Create shares statement results for each account
                            foreach (var account in accountDetails)
                            {
                                var transactions = accountTransactions.ContainsKey(account.Key)
                                    ? accountTransactions[account.Key]
                                    : new List<SharesTransaction>();

                                // Calculate summary values from transactions
                                decimal openingBalance = 0m;
                                decimal totalDeposits = transactions.Sum(t => t.DepositAmount);
                                decimal closingBalance = transactions.Any()
                                    ? transactions.Last().RunningBalance
                                    : 0m;

                                // Use the TotalContribution from the SP for the summary
                                decimal actualTotalContribution = account.Value.TotalContribution;

                                // Create shares statement result
                                var sharesStatementResult = new SharesStatementResult
                                {
                                    StatementType = "SHARES/SAVINGS STATEMENT",
                                    ProductName = account.Value.ProductName,
                                    AccountType = "Share Account",
                                    ProductCode = 0,
                                    Period = $"{(startDate.HasValue ? startDate.Value.ToString("dd/MM/yyyy") : "Beginning")} to {(endDate.HasValue ? endDate.Value.ToString("dd/MM/yyyy") : DateTime.Now.ToString("dd/MM/yyyy"))}",
                                    OpeningBalance = openingBalance,
                                    TotalDeposits = totalDeposits,
                                    TotalWithdrawals = 0m,
                                    ClosingBalance = closingBalance,
                                    Transactions = transactions,
                                    Summary = new SharesAccountSummary
                                    {
                                        AccountName = account.Value.ProductName,
                                        AccountType = "Share Account",
                                        OpeningBalance = openingBalance,
                                        TotalDeposits = actualTotalContribution,
                                        TotalWithdrawals = 0m,
                                        ClosingBalance = closingBalance,
                                        NetMovement = actualTotalContribution
                                    }
                                };

                                allSharesStatements.Add(sharesStatementResult);
                            }
                        }
                    }

                    // Get customer info
                    var customerInfo = allLoanStatements.FirstOrDefault()?.Customer ??
                                     (allSharesStatements.Count > 0 ?
                                         new CustomerInfo
                                         {
                                             FullName = await GetCustomerName(connection, customerId),
                                             AccountNumber = "N/A",
                                             StaffNo = await GetCustomerStaffNo(connection, customerId),
                                             Mobile = await GetCustomerMobile(connection, customerId),
                                             Email = await GetCustomerEmail(connection, customerId),
                                             PFNumber = await GetCustomerPFNumber(connection, customerId)
                                         } : null);

                    if (customerInfo == null)
                    {
                        customerInfo = new CustomerInfo
                        {
                            FullName = await GetCustomerName(connection, customerId),
                            AccountNumber = "N/A",
                            StaffNo = await GetCustomerStaffNo(connection, customerId),
                            Mobile = await GetCustomerMobile(connection, customerId),
                            Email = await GetCustomerEmail(connection, customerId),
                            PFNumber = await GetCustomerPFNumber(connection, customerId)
                        };
                    }

                    // Populate member statement
                    memberStatement.Customer = customerInfo;
                    memberStatement.LoanStatements = allLoanStatements;
                    memberStatement.SharesStatements = allSharesStatements;

                    // Calculate totals
                    memberStatement.TotalLoanBalance = allLoanStatements.Sum(l => l.Summary?.TotalOutstandingBalance ?? 0);
                    memberStatement.TotalSharesBalance = allSharesStatements.Sum(s => s.ClosingBalance);
                    memberStatement.TotalAccounts = allLoanStatements.Count + allSharesStatements.Count;
                }

                // Check if we found any data
                if (memberStatement.LoanStatements.Count == 0 && memberStatement.SharesStatements.Count == 0)
                {
                    var errorResponse = Request.CreateResponse(HttpStatusCode.NotFound);
                    errorResponse.Content = new StringContent(
                        JsonConvert.SerializeObject(new ApiResponse<object>
                        {
                            Success = false,
                            Message = "No loan or shares accounts found for this customer.",
                            Data = null
                        }),
                        Encoding.UTF8,
                        "application/json");
                    return errorResponse;
                }

                // If PDF download is requested
                if (downloadPdf)
                {
                    byte[] pdfBytes = GenerateMemberStatementPdf(memberStatement, startDate, endDate);

                    var response = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new ByteArrayContent(pdfBytes)
                    };

                    response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

                    string customerName = memberStatement.Customer?.FullName?.Replace(" ", "_") ?? "Customer";
                    string dateRange = "";
                    if (startDate.HasValue && endDate.HasValue)
                        dateRange = $"{startDate.Value:yyyyMMdd}_{endDate.Value:yyyyMMdd}";
                    else if (startDate.HasValue)
                        dateRange = $"from_{startDate.Value:yyyyMMdd}";
                    else if (endDate.HasValue)
                        dateRange = $"to_{endDate.Value:yyyyMMdd}";

                    response.Content.Headers.ContentDisposition =
                        new ContentDispositionHeaderValue("attachment")
                        {
                            FileName = $"MemberStatement_{customerName}_{dateRange}_{DateTime.Now:yyyyMMdd}.pdf"
                        };

                    return response;
                }
                else
                {
                    // Return JSON response
                    var response = Request.CreateResponse(HttpStatusCode.OK);
                    response.Content = new StringContent(
                        JsonConvert.SerializeObject(new ApiResponse<object>
                        {
                            Success = true,
                            Message = $"Found {memberStatement.LoanStatements.Count} loan(s) and {memberStatement.SharesStatements.Count} shares/savings account(s).",
                            Data = memberStatement
                        }),
                        Encoding.UTF8,
                        "application/json");
                    return response;
                }
            }
            catch (Exception ex)
            {
                var response = Request.CreateResponse(HttpStatusCode.InternalServerError);
                response.Content = new StringContent(
                    JsonConvert.SerializeObject(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "An error occurred while retrieving member statement.",
                        Data = ex.Message + " | Inner: " + (ex.InnerException?.Message ?? "None")
                    }),
                    Encoding.UTF8,
                    "application/json");
                return response;
            }
        }
        private async Task<CustomerData> GetCustomerDetails(SqlConnection connection, Guid customerAccountId, Guid customerId)
        {
            string customerQuery = @"
        SELECT TOP 1
            c.Individual_FirstName,
            c.Individual_LastName,
            c.Address_MobileLine,
            c.Address_Email,
            c.Reference2,
            c.Reference3,
            ISNULL(b.Code, 0) as BranchCode,
            ISNULL(c.SerialNumber, 0) as CustomerSerialNumber,
            ISNULL(ca.CustomerAccountType_ProductCode, 0) as ProductCode,
            ISNULL(ca.CustomerAccountType_TargetProductCode, 0) as TargetProductCode
        FROM [SwiftFinancialsDB_Live].[dbo].[swiftFin_CustomerAccounts] ca
        INNER JOIN [SwiftFinancialsDB_Live].[dbo].[swiftFin_Customers] c
            ON ca.CustomerId = c.Id
        LEFT JOIN [SwiftFinancialsDB_Live].[dbo].[swiftFin_Branches] b
            ON ca.BranchId = b.Id
        WHERE (ca.Id = @CustomerAccountId OR c.Id = @CustomerId)
        ORDER BY ca.CreatedDate DESC";

            using (var cmd = new SqlCommand(customerQuery, connection))
            {
                cmd.Parameters.Add("@CustomerAccountId", SqlDbType.UniqueIdentifier).Value = customerAccountId;
                cmd.Parameters.Add("@CustomerId", SqlDbType.UniqueIdentifier).Value = customerId;

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return new CustomerData
                        {
                            FirstName = reader["Individual_FirstName"]?.ToString() ?? "",
                            LastName = reader["Individual_LastName"]?.ToString() ?? "",
                            Mobile = reader["Address_MobileLine"]?.ToString() ?? "",
                            Email = reader["Address_Email"]?.ToString() ?? "",
                            Reference2 = reader["Reference2"]?.ToString() ?? "",
                            Reference3 = reader["Reference3"]?.ToString() ?? "",
                            BranchCode = reader["BranchCode"] != DBNull.Value ? Convert.ToInt32(reader["BranchCode"]) : 0,
                            CustomerSerialNumber = reader["CustomerSerialNumber"] != DBNull.Value ? Convert.ToInt32(reader["CustomerSerialNumber"]) : 0,
                            ProductCode = reader["ProductCode"] != DBNull.Value ? Convert.ToInt32(reader["ProductCode"]) : 0,
                            TargetProductCode = reader["TargetProductCode"] != DBNull.Value ? Convert.ToInt32(reader["TargetProductCode"]) : 0
                        };
                    }
                }
            }

            // Return default if no customer found
            return new CustomerData();
        }

        private async Task<string> GetCustomerName(SqlConnection connection, Guid customerId)
        {
            string query = @"
        SELECT 
            CASE 
                WHEN Type = 1
                THEN CONCAT(ISNULL(Individual_FirstName, ''), ' ', ISNULL(Individual_LastName, ''))
                ELSE ISNULL(NonIndividual_Description, '')
            END AS FullName
        FROM swiftFin_Customers
        WHERE Id = @CustomerId";

            using (var cmd = new SqlCommand(query, connection))
            {
                cmd.Parameters.Add("@CustomerId", SqlDbType.UniqueIdentifier).Value = customerId;

                var result = await cmd.ExecuteScalarAsync();
                return result?.ToString()?.Trim() ?? "Customer";
            }
        }

        private async Task<string> GetCustomerStaffNo(SqlConnection connection, Guid customerId)
        {
            string query = @"
        SELECT Reference2 
        FROM swiftFin_Customers 
        WHERE Id = @CustomerId";

            using (var cmd = new SqlCommand(query, connection))
            {
                cmd.Parameters.Add("@CustomerId", SqlDbType.UniqueIdentifier).Value = customerId;
                return (await cmd.ExecuteScalarAsync())?.ToString() ?? "N/A";
            }
        }

        private async Task<string> GetCustomerMobile(SqlConnection connection, Guid customerId)
        {
            string query = @"
        SELECT Address_MobileLine 
        FROM swiftFin_Customers 
        WHERE Id = @CustomerId";

            using (var cmd = new SqlCommand(query, connection))
            {
                cmd.Parameters.Add("@CustomerId", SqlDbType.UniqueIdentifier).Value = customerId;
                return (await cmd.ExecuteScalarAsync())?.ToString() ?? "N/A";
            }
        }

        private async Task<string> GetCustomerEmail(SqlConnection connection, Guid customerId)
        {
            string query = @"
        SELECT Address_Email 
        FROM swiftFin_Customers 
        WHERE Id = @CustomerId";

            using (var cmd = new SqlCommand(query, connection))
            {
                cmd.Parameters.Add("@CustomerId", SqlDbType.UniqueIdentifier).Value = customerId;
                return (await cmd.ExecuteScalarAsync())?.ToString() ?? "N/A";
            }
        }

        private async Task<string> GetCustomerPFNumber(SqlConnection connection, Guid customerId)
        {
            string query = @"
        SELECT Reference3 
        FROM swiftFin_Customers 
        WHERE Id = @CustomerId";

            using (var cmd = new SqlCommand(query, connection))
            {
                cmd.Parameters.Add("@CustomerId", SqlDbType.UniqueIdentifier).Value = customerId;
                return (await cmd.ExecuteScalarAsync())?.ToString() ?? "N/A";
            }
        }

        public class MemberStatementResult
        {
            public Guid CustomerId { get; set; }
            public CustomerInfo Customer { get; set; }
            public List<LoanStatementResult> LoanStatements { get; set; } = new List<LoanStatementResult>();
            public List<SharesStatementResult> SharesStatements { get; set; } = new List<SharesStatementResult>();
            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }
            public decimal TotalLoanBalance { get; set; }
            public decimal TotalSharesBalance { get; set; }
            public int TotalAccounts { get; set; }
        }

        public class SharesStatementResult
        {
            public string StatementType { get; set; }
            public string ProductName { get; set; }
            public string AccountType { get; set; }
            public int ProductCode { get; set; }
            public string Period { get; set; }
            public decimal OpeningBalance { get; set; }
            public decimal TotalDeposits { get; set; }
            public decimal TotalWithdrawals { get; set; }
            public decimal ClosingBalance { get; set; }
            public List<SharesTransaction> Transactions { get; set; } = new List<SharesTransaction>();
            public SharesAccountSummary Summary { get; set; }
        }

        public class SharesTransaction
        {
            public string TransactionDate { get; set; }
            public string Description { get; set; }
            public decimal DepositAmount { get; set; }
            public decimal WithdrawalAmount { get; set; }
            public decimal RunningBalance { get; set; }
        }

        public class SharesAccountSummary
        {
            public string AccountName { get; set; }
            public string AccountType { get; set; }
            public decimal OpeningBalance { get; set; }
            public decimal TotalDeposits { get; set; }
            public decimal TotalWithdrawals { get; set; }
            public decimal ClosingBalance { get; set; }
            public decimal NetMovement { get; set; }
        }

        private byte[] GenerateMemberStatementPdf(MemberStatementResult memberStatement, DateTime? startDate = null, DateTime? endDate = null)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                // Create document with same margins as individual statements
                Document document = new Document(PageSize.A4, 30, 30, 50, 30);
                PdfWriter writer = PdfWriter.GetInstance(document, ms);

                document.Open();

                // ===== RUBANI SACCO COLOR THEME =====
                BaseColor SkyBlue = new BaseColor(0, 174, 239); // #00AEEF
                BaseColor Red = new BaseColor(255, 0, 0);       // #FF0000
                BaseColor Green = new BaseColor(0, 150, 0);     // #009600
                BaseColor DarkGray = new BaseColor(26, 26, 26); // #1A1A1A
                BaseColor LightGray = new BaseColor(217, 217, 217); // #D9D9D9
                BaseColor MediumGray = new BaseColor(128, 128, 128); // #808080 - Added for section headers
                BaseColor White = BaseColor.WHITE;

                // ===== FONTS - BOOK ANTIQUA FONT WITH SIZE 11 =====
                // Load Book Antiqua font (make sure it's available on your system)
                // You may need to install Book Antiqua font or use a different serif font
                string bookAntiquaFontName = "Book Antiqua";

                // Try to create Book Antiqua fonts, fallback to Times if not available
                Font titleFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 16f, Font.BOLD, DarkGray);
                Font normalFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 11f, Font.NORMAL, DarkGray);
                Font boldFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 11f, Font.BOLD, DarkGray);
                Font smallFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 9f, Font.NORMAL, DarkGray);
                Font companyNameFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 14f, Font.BOLD, SkyBlue);
                Font companyInfoFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 11f, Font.NORMAL, DarkGray);
                Font sectionHeaderFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 12f, Font.BOLD, SkyBlue);
                Font tableHeaderFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 9f, Font.BOLD, DarkGray);
                Font tableCellFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 10f, Font.NORMAL, DarkGray);

                // Fallback fonts if Book Antiqua is not available
                try
                {
                    // Test if Book Antiqua is available
                    var testFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 11f);
                }
                catch
                {
                    // Fallback to Times New Roman if Book Antiqua is not available
                    bookAntiquaFontName = "Times New Roman";
                    titleFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 16f, Font.BOLD, DarkGray);
                    normalFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 11f, Font.NORMAL, DarkGray);
                    boldFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 11f, Font.BOLD, DarkGray);
                    smallFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 9f, Font.NORMAL, DarkGray);
                    companyNameFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 14f, Font.BOLD, SkyBlue);
                    companyInfoFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 11f, Font.NORMAL, DarkGray);
                    sectionHeaderFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 12f, Font.BOLD, SkyBlue);
                    tableHeaderFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 9f, Font.BOLD, DarkGray);
                    tableCellFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 10f, Font.NORMAL, DarkGray);
                }

                // ===== CUSTOM HEADER WITH RUBANI SACCO LOGO =====
                try
                {
                    // Create a table with 1 column for left-aligned content
                    PdfPTable headerTable = new PdfPTable(1)
                    {
                        WidthPercentage = 100,
                        SpacingAfter = 8f
                    };

                    // Row 1: Logo left-aligned at top
                    PdfPCell logoCell = new PdfPCell();
                    logoCell.Border = Rectangle.NO_BORDER;
                    logoCell.HorizontalAlignment = Element.ALIGN_LEFT;
                    logoCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    logoCell.PaddingBottom = 3f;

                    // Try to load logo from local path
                    string logoPath = @"C:\Users\ADMIN\source\repos\SwiftFinancialsNew\SwiftFinancialsSolution\TestApis\Assets\Images\rubani-logo.jpeg";
                    if (File.Exists(logoPath))
                    {
                        try
                        {
                            Image logo = Image.GetInstance(logoPath);
                            logo.ScaleToFit(100, 100);
                            logoCell.AddElement(logo);
                        }
                        catch (Exception)
                        {
                            logoCell.AddElement(new Paragraph("RUBANI SACCO", companyNameFont)
                            {
                                Alignment = Element.ALIGN_LEFT
                            });
                        }
                    }
                    else
                    {
                        logoCell.AddElement(new Paragraph("RUBANI SACCO", companyNameFont)
                        {
                            Alignment = Element.ALIGN_LEFT
                        });
                    }

                    headerTable.AddCell(logoCell);

                    // Row 2: Company Info - LEFT ALIGNED
                    PdfPCell infoCell = new PdfPCell();
                    infoCell.Border = Rectangle.NO_BORDER;
                    infoCell.HorizontalAlignment = Element.ALIGN_LEFT;
                    infoCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    infoCell.PaddingTop = 3f;

                    var companyNamePara = new Paragraph("RUBANI SACCO", companyNameFont)
                    {
                        Alignment = Element.ALIGN_LEFT
                    };
                    infoCell.AddElement(companyNamePara);

                    var address = new Paragraph("Rubani House, Off Airport North Embakasi", companyInfoFont)
                    {
                        Alignment = Element.ALIGN_LEFT
                    };
                    infoCell.AddElement(address);

                    var email = new Paragraph("rubanisacco@gmail.com", companyInfoFont)
                    {
                        Alignment = Element.ALIGN_LEFT
                    };
                    infoCell.AddElement(email);

                    headerTable.AddCell(infoCell);
                    document.Add(headerTable);

                    // Add decorative line (Blue-Red-Blue)
                    var lineTable = new PdfPTable(3)
                    {
                        WidthPercentage = 100,
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        SpacingAfter = 8f
                    };
                    lineTable.SetWidths(new float[] { 33, 34, 33 });

                    lineTable.AddCell(new PdfPCell()
                    {
                        BackgroundColor = SkyBlue,
                        FixedHeight = 2f,
                        Border = Rectangle.NO_BORDER
                    });

                    lineTable.AddCell(new PdfPCell()
                    {
                        BackgroundColor = Red,
                        FixedHeight = 2f,
                        Border = Rectangle.NO_BORDER
                    });

                    lineTable.AddCell(new PdfPCell()
                    {
                        BackgroundColor = SkyBlue,
                        FixedHeight = 2f,
                        Border = Rectangle.NO_BORDER
                    });

                    document.Add(lineTable);
                }
                catch (Exception)
                {
                    var fallbackPara = new Paragraph("RUBANI SACCO\nRubani House, Off Airport North Embakasi\nrubanisacco@gmail.com",
                        companyInfoFont)
                    {
                        Alignment = Element.ALIGN_LEFT,
                        SpacingAfter = 10f
                    };
                    document.Add(fallbackPara);
                }

                // ===== MEMBER DETAILED STATEMENT TITLE =====
                string titleText = "MEMBER DETAILED STATEMENT";
                if (startDate.HasValue || endDate.HasValue)
                {
                    titleText = "MEMBER DETAILED STATEMENT";
                    string dateRangeText = "";

                    if (startDate.HasValue && endDate.HasValue)
                        dateRangeText = $"{startDate.Value:dd/MM/yyyy} to {endDate.Value:dd/MM/yyyy}";
                    else if (startDate.HasValue)
                        dateRangeText = $"From {startDate.Value:dd/MM/yyyy}";
                    else if (endDate.HasValue)
                        dateRangeText = $"To {endDate.Value:dd/MM/yyyy}";

                    if (!string.IsNullOrEmpty(dateRangeText))
                    {
                        document.Add(new Paragraph(titleText, titleFont)
                        {
                            Alignment = Element.ALIGN_CENTER,
                            SpacingAfter = 3f
                        });

                        document.Add(new Paragraph(dateRangeText,
                            FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 12f, Font.BOLD, DarkGray))
                        {
                            Alignment = Element.ALIGN_CENTER,
                            SpacingAfter = 8f
                        });
                    }
                    else
                    {
                        document.Add(new Paragraph(titleText, titleFont)
                        {
                            Alignment = Element.ALIGN_CENTER,
                            SpacingAfter = 8f
                        });
                    }
                }
                else
                {
                    document.Add(new Paragraph(titleText, titleFont)
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingAfter = 8f
                    });
                }

                // ===== MEMBER INFORMATION SECTION =====
                if (memberStatement.Customer != null)
                {
                    // Create a 2-column table for better alignment
                    PdfPTable memberInfoTable = new PdfPTable(2)
                    {
                        WidthPercentage = 100,
                        SpacingAfter = 10f
                    };
                    memberInfoTable.SetWidths(new float[] { 40, 60 });

                    // Left column: Name, Staff No, Mobile
                    Paragraph leftColumn = new Paragraph();
                    leftColumn.Add(new Chunk("Name: ", boldFont));
                    leftColumn.Add(new Chunk(memberStatement.Customer.FullName, normalFont));
                    leftColumn.Add(Chunk.NEWLINE);
                    leftColumn.Add(new Chunk("Staff No: ", boldFont));
                    leftColumn.Add(new Chunk(memberStatement.Customer.PFNumber ?? "N/A", normalFont));
                    leftColumn.Add(Chunk.NEWLINE);
                    leftColumn.Add(new Chunk("Mobile: ", boldFont));
                    leftColumn.Add(new Chunk(memberStatement.Customer.Mobile ?? "N/A", normalFont));

                    PdfPCell leftCell = new PdfPCell(leftColumn)
                    {
                        Border = Rectangle.NO_BORDER,
                        HorizontalAlignment = Element.ALIGN_LEFT,
                        Padding = 3
                    };
                    memberInfoTable.AddCell(leftCell);

                    // Right column: MemberNo, Account No, Email
                    Paragraph rightColumn = new Paragraph();
                    rightColumn.Add(new Chunk("MemberNo: ", boldFont));
                    rightColumn.Add(new Chunk(memberStatement.Customer.StaffNo ?? "N/A", normalFont));
                    rightColumn.Add(Chunk.NEWLINE);
                    rightColumn.Add(new Chunk("Account No: ", boldFont));
                    rightColumn.Add(new Chunk(memberStatement.Customer.AccountNumber, normalFont));
                    rightColumn.Add(Chunk.NEWLINE);
                    rightColumn.Add(new Chunk("Email: ", boldFont));
                    rightColumn.Add(new Chunk(memberStatement.Customer.Email ?? "N/A", normalFont));

                    PdfPCell rightCell = new PdfPCell(rightColumn)
                    {
                        Border = Rectangle.NO_BORDER,
                        HorizontalAlignment = Element.ALIGN_LEFT,
                        Padding = 3
                    };
                    memberInfoTable.AddCell(rightCell);

                    document.Add(memberInfoTable);
                }

                // ===== STATEMENT PERIOD SECTION =====
                if (startDate.HasValue || endDate.HasValue)
                {
                    string periodText = "Statement Period: ";
                    if (startDate.HasValue && endDate.HasValue)
                        periodText += $"{startDate.Value:dd/MM/yyyy} to {endDate.Value:dd/MM/yyyy}";
                    else if (startDate.HasValue)
                        periodText += $"From {startDate.Value:dd/MM/yyyy}";
                    else if (endDate.HasValue)
                        periodText += $"To {endDate.Value:dd/MM/yyyy}";

                    var periodPara = new Paragraph(periodText, boldFont)
                    {
                        Alignment = Element.ALIGN_LEFT,
                        SpacingAfter = 8f
                    };
                    document.Add(periodPara);
                }

                // In the PDF generation method, update the shares section:

                // ===== SHARES/SAVINGS DETAILED SECTION =====
                if (memberStatement.SharesStatements.Count > 0)
                {
                    var sharesHeader = new Paragraph("SHARES/SAVINGS STATEMENT",
                        FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 12f, Font.BOLD, White))
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingAfter = 3f
                    };

                    PdfPTable sharesHeaderTable = new PdfPTable(1)
                    {
                        WidthPercentage = 100,
                        SpacingAfter = 10f
                    };

                    PdfPCell sharesHeaderCell = new PdfPCell(sharesHeader)
                    {
                        BackgroundColor = MediumGray,
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        Padding = 6,
                        Border = Rectangle.NO_BORDER,
                        BorderWidthBottom = 2f,
                        BorderColorBottom = SkyBlue
                    };
                    sharesHeaderTable.AddCell(sharesHeaderCell);
                    document.Add(sharesHeaderTable);

                    int sharesCounter = 1;
                    foreach (var shares in memberStatement.SharesStatements)
                    {
                        // Account Header - Show product name only
                        var accountHeaderPara = new Paragraph($"ACCOUNT #{sharesCounter}: {shares.ProductName}", sectionHeaderFont)
                        {
                            Alignment = Element.ALIGN_LEFT,
                            SpacingAfter = 8f
                        };
                        document.Add(accountHeaderPara);

                        // Transactions Section - Show ALL transactions (no limit)
                        if (shares.Transactions.Count > 0)
                        {
                            // Use all transactions, not just first 5
                            var allTransactions = shares.Transactions;

                            PdfPTable transTable = new PdfPTable(5)
                            {
                                WidthPercentage = 100,
                                SpacingAfter = 5f
                            };
                            transTable.SetWidths(new float[] { 20, 35, 15, 15, 15 });

                            // Table headers - using tableHeaderFont
                            PdfPCell dateHeaderCell = new PdfPCell(new Phrase("Date", tableHeaderFont))
                            {
                                BackgroundColor = LightGray,
                                HorizontalAlignment = Element.ALIGN_CENTER,
                                Padding = 4,
                                PaddingTop = 6,
                                PaddingBottom = 6,
                                BorderWidthTop = 1f,
                                BorderWidthBottom = 1f,
                                BorderWidthLeft = 1f,
                                BorderWidthRight = 0f,
                                BorderColorTop = DarkGray,
                                BorderColorBottom = DarkGray,
                                BorderColorLeft = DarkGray
                            };
                            transTable.AddCell(dateHeaderCell);

                            PdfPCell descHeaderCell = new PdfPCell(new Phrase("Description", tableHeaderFont))
                            {
                                BackgroundColor = LightGray,
                                HorizontalAlignment = Element.ALIGN_CENTER,
                                Padding = 4,
                                PaddingTop = 6,
                                PaddingBottom = 6,
                                BorderWidthTop = 1f,
                                BorderWidthBottom = 1f,
                                BorderWidthLeft = 0f,
                                BorderWidthRight = 0f,
                                BorderColorTop = DarkGray,
                                BorderColorBottom = DarkGray
                            };
                            transTable.AddCell(descHeaderCell);

                            PdfPCell depositHeaderCell = new PdfPCell(new Phrase("Deposit", tableHeaderFont))
                            {
                                BackgroundColor = LightGray,
                                HorizontalAlignment = Element.ALIGN_RIGHT,
                                Padding = 4,
                                PaddingTop = 6,
                                PaddingBottom = 6,
                                BorderWidthTop = 1f,
                                BorderWidthBottom = 1f,
                                BorderWidthLeft = 0f,
                                BorderWidthRight = 0f,
                                BorderColorTop = DarkGray,
                                BorderColorBottom = DarkGray
                            };
                            transTable.AddCell(depositHeaderCell);

                            // Withdrawal header - kept for consistency but will be empty for shares
                            PdfPCell withdrawalHeaderCell = new PdfPCell(new Phrase("Withdrawal", tableHeaderFont))
                            {
                                BackgroundColor = LightGray,
                                HorizontalAlignment = Element.ALIGN_RIGHT,
                                Padding = 4,
                                PaddingTop = 6,
                                PaddingBottom = 6,
                                BorderWidthTop = 1f,
                                BorderWidthBottom = 1f,
                                BorderWidthLeft = 0f,
                                BorderWidthRight = 0f,
                                BorderColorTop = DarkGray,
                                BorderColorBottom = DarkGray
                            };
                            transTable.AddCell(withdrawalHeaderCell);

                            PdfPCell balanceHeaderCell = new PdfPCell(new Phrase("Balance", tableHeaderFont))
                            {
                                BackgroundColor = LightGray,
                                HorizontalAlignment = Element.ALIGN_RIGHT,
                                Padding = 4,
                                PaddingTop = 6,
                                PaddingBottom = 6,
                                BorderWidthTop = 1f,
                                BorderWidthBottom = 1f,
                                BorderWidthLeft = 0f,
                                BorderWidthRight = 1f,
                                BorderColorTop = DarkGray,
                                BorderColorBottom = DarkGray,
                                BorderColorRight = DarkGray
                            };
                            transTable.AddCell(balanceHeaderCell);

                            // Add ALL transactions - using tableCellFont
                            for (int i = 0; i < allTransactions.Count; i++)
                            {
                                var transaction = allTransactions[i];
                                bool isLastRow = (i == allTransactions.Count - 1);

                                PdfPCell dateCell = new PdfPCell(new Phrase(transaction.TransactionDate, tableCellFont))
                                {
                                    HorizontalAlignment = Element.ALIGN_CENTER,
                                    Padding = 3,
                                    PaddingTop = 6,
                                    PaddingBottom = 6,
                                    BorderWidthLeft = 1f,
                                    BorderWidthRight = 0f,
                                    BorderWidthTop = 0f,
                                    BorderWidthBottom = isLastRow ? 1f : 0f,
                                    BorderColorLeft = DarkGray,
                                    BorderColorBottom = isLastRow ? DarkGray : BaseColor.WHITE
                                };
                                transTable.AddCell(dateCell);

                                PdfPCell descCell = new PdfPCell(new Phrase(transaction.Description ?? "", tableCellFont))
                                {
                                    HorizontalAlignment = Element.ALIGN_LEFT,
                                    Padding = 3,
                                    PaddingTop = 6,
                                    PaddingBottom = 6,
                                    BorderWidthLeft = 0f,
                                    BorderWidthRight = 0f,
                                    BorderWidthTop = 0f,
                                    BorderWidthBottom = isLastRow ? 1f : 0f,
                                    BorderColorBottom = isLastRow ? DarkGray : BaseColor.WHITE
                                };
                                transTable.AddCell(descCell);

                                PdfPCell depositCell = new PdfPCell(new Phrase(transaction.DepositAmount.ToString("N2"), tableCellFont))
                                {
                                    HorizontalAlignment = Element.ALIGN_RIGHT,
                                    Padding = 3,
                                    PaddingTop = 6,
                                    PaddingBottom = 6,
                                    BorderWidthLeft = 0f,
                                    BorderWidthRight = 0f,
                                    BorderWidthTop = 0f,
                                    BorderWidthBottom = isLastRow ? 1f : 0f,
                                    BorderColorBottom = isLastRow ? DarkGray : BaseColor.WHITE
                                };
                                transTable.AddCell(depositCell);

                                // Withdrawal cell - always empty for shares
                                PdfPCell withdrawalCell = new PdfPCell(new Phrase("", tableCellFont))
                                {
                                    HorizontalAlignment = Element.ALIGN_RIGHT,
                                    Padding = 3,
                                    PaddingTop = 6,
                                    PaddingBottom = 6,
                                    BorderWidthLeft = 0f,
                                    BorderWidthRight = 0f,
                                    BorderWidthTop = 0f,
                                    BorderWidthBottom = isLastRow ? 1f : 0f,
                                    BorderColorBottom = isLastRow ? DarkGray : BaseColor.WHITE
                                };
                                transTable.AddCell(withdrawalCell);

                                PdfPCell balanceCell = new PdfPCell(new Phrase(transaction.RunningBalance.ToString("N2"), tableCellFont))
                                {
                                    HorizontalAlignment = Element.ALIGN_RIGHT,
                                    Padding = 3,
                                    PaddingTop = 6,
                                    PaddingBottom = 6,
                                    BorderWidthLeft = 0f,
                                    BorderWidthRight = 1f,
                                    BorderWidthTop = 0f,
                                    BorderWidthBottom = isLastRow ? 1f : 0f,
                                    BorderColorRight = DarkGray,
                                    BorderColorBottom = isLastRow ? DarkGray : BaseColor.WHITE
                                };
                                transTable.AddCell(balanceCell);
                            }

                            document.Add(transTable);

                        }

                        // Account Summary - Show net movement only
                        if (shares.Summary != null)
                        {
                            var summaryPara = new Paragraph();
                            summaryPara.Alignment = Element.ALIGN_LEFT;

                            string accountTypeName = shares.ProductName ?? shares.AccountType;
                            string totalLabel = accountTypeName.Contains("Share", StringComparison.OrdinalIgnoreCase) ?
                                               "Total Share Capital" : $"Total {accountTypeName}";

                            summaryPara.Add(new Chunk($"{totalLabel}: ", boldFont));
                            summaryPara.Add(new Chunk(shares.Summary.NetMovement.ToString("N2"),
                                FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 11f, Font.BOLD, shares.Summary.NetMovement >= 0 ? Green : Red)));

                            summaryPara.SpacingAfter = 8f;
                            document.Add(summaryPara);
                        }

                        sharesCounter++;
                    }
                }
                // ===== LOANS DETAILED SECTION =====
                if (memberStatement.LoanStatements.Count > 0)
                {
                    var loansHeader = new Paragraph("LOANS STATEMENT",
                        FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 12f, Font.BOLD, White))
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingAfter = 3f
                    };

                    PdfPTable loansHeaderTable = new PdfPTable(1)
                    {
                        WidthPercentage = 100,
                        SpacingAfter = 10f
                    };

                    PdfPCell loansHeaderCell = new PdfPCell(loansHeader)
                    {
                        BackgroundColor = MediumGray,
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        Padding = 6,
                        Border = Rectangle.NO_BORDER,
                        BorderWidthBottom = 2f,
                        BorderColorBottom = SkyBlue
                    };
                    loansHeaderTable.AddCell(loansHeaderCell);
                    document.Add(loansHeaderTable);

                    int loanCounter = 1;
                    foreach (var loan in memberStatement.LoanStatements)
                    {
                        // Loan Header
                        var loanHeaderPara = new Paragraph($"LOAN #{loanCounter}: {loan.LoanNumber}", sectionHeaderFont)
                        {
                            Alignment = Element.ALIGN_LEFT,
                            SpacingAfter = 3f
                        };
                        document.Add(loanHeaderPara);

                        // Loan Details - 3 column layout with Disbursed Date centered
                        PdfPTable loanDetailsTable = new PdfPTable(3)
                        {
                            WidthPercentage = 100,
                            SpacingAfter = 3f  // Reduced from 5f
                        };
                        loanDetailsTable.SetWidths(new float[] { 33, 34, 33 });

                        // Format the disbursed date
                        string mainDisbursedDateDisplay = "N/A";
                        if (!string.IsNullOrEmpty(loan.LoanDetails.DisbursedDate))
                        {
                            DateTime disbursedDate;
                            if (DateTime.TryParse(loan.LoanDetails.DisbursedDate, out disbursedDate))
                            {
                                mainDisbursedDateDisplay = disbursedDate.ToString("dd/MM/yyyy");
                            }
                            else
                            {
                                mainDisbursedDateDisplay = loan.LoanDetails.DisbursedDate;
                            }
                        }

                        // Column 1: Loan Product - Left aligned
                        Paragraph col1Details = new Paragraph();
                        col1Details.Add(new Chunk("Loan Product: ", boldFont));
                        col1Details.Add(new Chunk(loan.LoanDetails.LoanProductType, normalFont));

                        PdfPCell col1Cell = new PdfPCell(col1Details)
                        {
                            Border = Rectangle.NO_BORDER,
                            Padding = 3,
                            HorizontalAlignment = Element.ALIGN_LEFT,
                            VerticalAlignment = Element.ALIGN_MIDDLE
                        };
                        loanDetailsTable.AddCell(col1Cell);

                        // Column 2: Disbursed Date - CENTERED
                        Paragraph col2Details = new Paragraph();
                        col2Details.Add(new Chunk("Disbursed Date: ", boldFont));
                        col2Details.Add(new Chunk(mainDisbursedDateDisplay, normalFont));

                        PdfPCell col2Cell = new PdfPCell(col2Details)
                        {
                            Border = Rectangle.NO_BORDER,
                            Padding = 3,
                            HorizontalAlignment = Element.ALIGN_CENTER,
                            VerticalAlignment = Element.ALIGN_MIDDLE
                        };
                        loanDetailsTable.AddCell(col2Cell);

                        // Column 3: Issued Amount - Right aligned
                        Paragraph col3Details = new Paragraph();
                        col3Details.Add(new Chunk("Issued Amount: ", boldFont));
                        col3Details.Add(new Chunk(loan.LoanDetails.AppliedAmount.ToString("N0"), normalFont));

                        PdfPCell col3Cell = new PdfPCell(col3Details)
                        {
                            Border = Rectangle.NO_BORDER,
                            Padding = 3,
                            HorizontalAlignment = Element.ALIGN_RIGHT,
                            VerticalAlignment = Element.ALIGN_MIDDLE
                        };
                        loanDetailsTable.AddCell(col3Cell);

                        document.Add(loanDetailsTable);

                        // CURRENT OUTSTANDING - Single line, left and right aligned
                        if (loan.Summary != null)
                        {
                            PdfPTable outstandingTable = new PdfPTable(2)
                            {
                                WidthPercentage = 100,
                                SpacingAfter = 5f  // Reduced from 10f
                            };
                            outstandingTable.SetWidths(new float[] { 50, 50 });

                            // Left cell: "CURRENT OUTSTANDING:" label
                            PdfPCell labelCell = new PdfPCell(new Paragraph("CURRENT OUTSTANDING:",
                                FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 11f, Font.BOLD, DarkGray)))
                            {
                                Border = Rectangle.NO_BORDER,
                                HorizontalAlignment = Element.ALIGN_LEFT,
                                VerticalAlignment = Element.ALIGN_MIDDLE,
                                Padding = 3,
                                PaddingTop = 0
                            };
                            outstandingTable.AddCell(labelCell);

                            // Right cell: Value
                            PdfPCell valueCell = new PdfPCell(new Paragraph(loan.Summary.TotalOutstandingBalance.ToString("N0"),
                                FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 11f, Font.BOLD, Red)))
                            {
                                Border = Rectangle.NO_BORDER,
                                HorizontalAlignment = Element.ALIGN_RIGHT,
                                VerticalAlignment = Element.ALIGN_MIDDLE,
                                Padding = 3,
                                PaddingTop = 0
                            };
                            outstandingTable.AddCell(valueCell);

                            document.Add(outstandingTable);
                        }

                        // Transaction Table
                        PdfPTable transTable = new PdfPTable(6)
                        {
                            WidthPercentage = 100,
                            SpacingAfter = 5f  // Reduced from 10f
                        };
                        transTable.SetWidths(new float[] { 15, 18, 15, 15, 15, 22 });

                        // Table headers - using tableHeaderFont
                        PdfPCell dateHeaderCell = new PdfPCell(new Phrase("Date", tableHeaderFont))
                        {
                            BackgroundColor = LightGray,
                            HorizontalAlignment = Element.ALIGN_CENTER,
                            Padding = 4,
                            PaddingTop = 6,
                            PaddingBottom = 6,
                            BorderWidthTop = 1f,
                            BorderWidthBottom = 1f,
                            BorderWidthLeft = 1f,
                            BorderWidthRight = 0f,
                            BorderColorTop = DarkGray,
                            BorderColorBottom = DarkGray,
                            BorderColorLeft = DarkGray
                        };
                        transTable.AddCell(dateHeaderCell);

                        PdfPCell openingBalanceHeaderCell = new PdfPCell(new Phrase("Opening Balance", tableHeaderFont))
                        {
                            BackgroundColor = LightGray,
                            HorizontalAlignment = Element.ALIGN_RIGHT,
                            Padding = 4,
                            PaddingTop = 6,
                            PaddingBottom = 6,
                            BorderWidthTop = 1f,
                            BorderWidthBottom = 1f,
                            BorderWidthLeft = 0f,
                            BorderWidthRight = 0f,
                            BorderColorTop = DarkGray,
                            BorderColorBottom = DarkGray
                        };
                        transTable.AddCell(openingBalanceHeaderCell);

                        PdfPCell principleHeaderCell = new PdfPCell(new Phrase("Principle", tableHeaderFont))
                        {
                            BackgroundColor = LightGray,
                            HorizontalAlignment = Element.ALIGN_RIGHT,
                            Padding = 4,
                            PaddingTop = 6,
                            PaddingBottom = 6,
                            BorderWidthTop = 1f,
                            BorderWidthBottom = 1f,
                            BorderWidthLeft = 0f,
                            BorderWidthRight = 0f,
                            BorderColorTop = DarkGray,
                            BorderColorBottom = DarkGray
                        };
                        transTable.AddCell(principleHeaderCell);

                        PdfPCell interestHeaderCell = new PdfPCell(new Phrase("Interest", tableHeaderFont))
                        {
                            BackgroundColor = LightGray,
                            HorizontalAlignment = Element.ALIGN_RIGHT,
                            Padding = 4,
                            PaddingTop = 6,
                            PaddingBottom = 6,
                            BorderWidthTop = 1f,
                            BorderWidthBottom = 1f,
                            BorderWidthLeft = 0f,
                            BorderWidthRight = 0f,
                            BorderColorTop = DarkGray,
                            BorderColorBottom = DarkGray
                        };
                        transTable.AddCell(interestHeaderCell);

                        PdfPCell amountHeaderCell = new PdfPCell(new Phrase("Amount", tableHeaderFont))
                        {
                            BackgroundColor = LightGray,
                            HorizontalAlignment = Element.ALIGN_RIGHT,
                            Padding = 4,
                            PaddingTop = 6,
                            PaddingBottom = 6,
                            BorderWidthTop = 1f,
                            BorderWidthBottom = 1f,
                            BorderWidthLeft = 0f,
                            BorderWidthRight = 0f,
                            BorderColorTop = DarkGray,
                            BorderColorBottom = DarkGray
                        };
                        transTable.AddCell(amountHeaderCell);

                        PdfPCell loanBalanceHeaderCell = new PdfPCell(new Phrase("Loan Balance", tableHeaderFont))
                        {
                            BackgroundColor = LightGray,
                            HorizontalAlignment = Element.ALIGN_RIGHT,
                            Padding = 4,
                            PaddingTop = 6,
                            PaddingBottom = 6,
                            BorderWidthTop = 1f,
                            BorderWidthBottom = 1f,
                            BorderWidthLeft = 0f,
                            BorderWidthRight = 1f,
                            BorderColorTop = DarkGray,
                            BorderColorBottom = DarkGray,
                            BorderColorRight = DarkGray
                        };
                        transTable.AddCell(loanBalanceHeaderCell);

                        // Add transactions if they exist - using tableCellFont
                        if (loan.Statement != null && loan.Statement.Count > 0)
                        {
                            for (int i = 0; i < loan.Statement.Count; i++)
                            {
                                var row = loan.Statement[i];
                                bool isLastRow = (i == loan.Statement.Count - 1);

                                // Format date
                                string transDate = "";
                                if (!string.IsNullOrEmpty(row.TransDate))
                                {
                                    DateTime date;
                                    if (DateTime.TryParse(row.TransDate, out date))
                                        transDate = date.ToString("dd/MM/yyyy");
                                    else
                                        transDate = row.TransDate;
                                }

                                // Date cell
                                PdfPCell dateCell = new PdfPCell(new Phrase(transDate, tableCellFont))
                                {
                                    HorizontalAlignment = Element.ALIGN_CENTER,
                                    Padding = 3,
                                    PaddingTop = 6,
                                    PaddingBottom = 6,
                                    BorderWidthLeft = 1f,
                                    BorderWidthRight = 0f,
                                    BorderWidthTop = 0f,
                                    BorderWidthBottom = isLastRow ? 1f : 0f,
                                    BorderColorLeft = DarkGray,
                                    BorderColorBottom = isLastRow ? DarkGray : BaseColor.WHITE
                                };
                                transTable.AddCell(dateCell);

                                // Opening Balance cell
                                PdfPCell openingBalanceCell = new PdfPCell(new Phrase(row.OpeningBalance.ToString("N0"), tableCellFont))
                                {
                                    HorizontalAlignment = Element.ALIGN_RIGHT,
                                    Padding = 3,
                                    PaddingTop = 6,
                                    PaddingBottom = 6,
                                    BorderWidthLeft = 0f,
                                    BorderWidthRight = 0f,
                                    BorderWidthTop = 0f,
                                    BorderWidthBottom = isLastRow ? 1f : 0f,
                                    BorderColorBottom = isLastRow ? DarkGray : BaseColor.WHITE
                                };
                                transTable.AddCell(openingBalanceCell);

                                // Principle cell
                                PdfPCell principleCell = new PdfPCell(new Phrase(row.Principle.ToString("N0"), tableCellFont))
                                {
                                    HorizontalAlignment = Element.ALIGN_RIGHT,
                                    Padding = 3,
                                    PaddingTop = 6,
                                    PaddingBottom = 6,
                                    BorderWidthLeft = 0f,
                                    BorderWidthRight = 0f,
                                    BorderWidthTop = 0f,
                                    BorderWidthBottom = isLastRow ? 1f : 0f,
                                    BorderColorBottom = isLastRow ? DarkGray : BaseColor.WHITE
                                };
                                transTable.AddCell(principleCell);

                                // Interest cell
                                PdfPCell interestCell = new PdfPCell(new Phrase(row.Interest.ToString("N0"), tableCellFont))
                                {
                                    HorizontalAlignment = Element.ALIGN_RIGHT,
                                    Padding = 3,
                                    PaddingTop = 6,
                                    PaddingBottom = 6,
                                    BorderWidthLeft = 0f,
                                    BorderWidthRight = 0f,
                                    BorderWidthTop = 0f,
                                    BorderWidthBottom = isLastRow ? 1f : 0f,
                                    BorderColorBottom = isLastRow ? DarkGray : BaseColor.WHITE
                                };
                                transTable.AddCell(interestCell);

                                // Amount cell - with color coding
                                Font amountCellFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 10f, Font.BOLD, row.Amount > 0 ? Green : DarkGray);
                                PdfPCell amountCell = new PdfPCell(new Phrase(row.Amount.ToString("N0"), amountCellFont))
                                {
                                    HorizontalAlignment = Element.ALIGN_RIGHT,
                                    Padding = 3,
                                    PaddingTop = 6,
                                    PaddingBottom = 6,
                                    BorderWidthLeft = 0f,
                                    BorderWidthRight = 0f,
                                    BorderWidthTop = 0f,
                                    BorderWidthBottom = isLastRow ? 1f : 0f,
                                    BorderColorBottom = isLastRow ? DarkGray : BaseColor.WHITE
                                };
                                transTable.AddCell(amountCell);

                                // Loan Balance cell
                                PdfPCell balanceCell = new PdfPCell(new Phrase(row.LoanBalance.ToString("N0"), tableCellFont))
                                {
                                    HorizontalAlignment = Element.ALIGN_RIGHT,
                                    Padding = 3,
                                    PaddingTop = 6,
                                    PaddingBottom = 6,
                                    BorderWidthLeft = 0f,
                                    BorderWidthRight = 1f,
                                    BorderWidthTop = 0f,
                                    BorderWidthBottom = isLastRow ? 1f : 0f,
                                    BorderColorRight = DarkGray,
                                    BorderColorBottom = isLastRow ? DarkGray : BaseColor.WHITE
                                };
                                transTable.AddCell(balanceCell);
                            }

                            //if (loan.Statement.Count > 10)
                            //{
                            //    var moreTransPara = new Paragraph($"... and {loan.Statement.Count - 10} more transactions", smallFont)
                            //    {
                            //        Alignment = Element.ALIGN_CENTER,
                            //        SpacingBefore = 3f,
                            //        SpacingAfter = 3f  // Reduced from 8f
                            //    };
                            //    document.Add(moreTransPara);
                            //}
                        }
                        else
                        {
                            // No transactions - show initial disbursement
                            decimal issuedAmount = loan.LoanDetails.AppliedAmount;

                            // Use a different variable name for the "else" block
                            string noTransactionsDateDisplay = "N/A";
                            if (!string.IsNullOrEmpty(loan.LoanDetails.DisbursedDate))
                            {
                                DateTime noTransactionsDate;
                                if (DateTime.TryParse(loan.LoanDetails.DisbursedDate, out noTransactionsDate))
                                {
                                    noTransactionsDateDisplay = noTransactionsDate.ToString("dd/MM/yyyy");
                                }
                                else
                                {
                                    noTransactionsDateDisplay = loan.LoanDetails.DisbursedDate;
                                }
                            }

                            // Date cell
                            PdfPCell dateCell = new PdfPCell(new Phrase(noTransactionsDateDisplay, tableCellFont))
                            {
                                HorizontalAlignment = Element.ALIGN_CENTER,
                                Padding = 3,
                                PaddingTop = 6,
                                PaddingBottom = 6,
                                BorderWidthLeft = 1f,
                                BorderWidthRight = 0f,
                                BorderWidthTop = 0f,
                                BorderWidthBottom = 1f,
                                BorderColorLeft = DarkGray,
                                BorderColorBottom = DarkGray
                            };
                            transTable.AddCell(dateCell);

                            // Opening Balance cell
                            PdfPCell openingBalanceCell = new PdfPCell(new Phrase(issuedAmount.ToString("N0"), tableCellFont))
                            {
                                HorizontalAlignment = Element.ALIGN_RIGHT,
                                Padding = 3,
                                PaddingTop = 6,
                                PaddingBottom = 6,
                                BorderWidthLeft = 0f,
                                BorderWidthRight = 0f,
                                BorderWidthTop = 0f,
                                BorderWidthBottom = 1f,
                                BorderColorBottom = DarkGray
                            };
                            transTable.AddCell(openingBalanceCell);

                            // Principle cell
                            PdfPCell principleCell = new PdfPCell(new Phrase("0", tableCellFont))
                            {
                                HorizontalAlignment = Element.ALIGN_RIGHT,
                                Padding = 3,
                                PaddingTop = 6,
                                PaddingBottom = 6,
                                BorderWidthLeft = 0f,
                                BorderWidthRight = 0f,
                                BorderWidthTop = 0f,
                                BorderWidthBottom = 1f,
                                BorderColorBottom = DarkGray
                            };
                            transTable.AddCell(principleCell);

                            // Interest cell
                            PdfPCell interestCell = new PdfPCell(new Phrase("0", tableCellFont))
                            {
                                HorizontalAlignment = Element.ALIGN_RIGHT,
                                Padding = 3,
                                PaddingTop = 6,
                                PaddingBottom = 6,
                                BorderWidthLeft = 0f,
                                BorderWidthRight = 0f,
                                BorderWidthTop = 0f,
                                BorderWidthBottom = 1f,
                                BorderColorBottom = DarkGray
                            };
                            transTable.AddCell(interestCell);

                            // Amount cell
                            PdfPCell amountCell = new PdfPCell(new Phrase("0", tableCellFont))
                            {
                                HorizontalAlignment = Element.ALIGN_RIGHT,
                                Padding = 3,
                                PaddingTop = 6,
                                PaddingBottom = 6,
                                BorderWidthLeft = 0f,
                                BorderWidthRight = 0f,
                                BorderWidthTop = 0f,
                                BorderWidthBottom = 1f,
                                BorderColorBottom = DarkGray
                            };
                            transTable.AddCell(amountCell);

                            // Loan Balance cell
                            PdfPCell balanceCell = new PdfPCell(new Phrase(issuedAmount.ToString("N0"), tableCellFont))
                            {
                                HorizontalAlignment = Element.ALIGN_RIGHT,
                                Padding = 3,
                                PaddingTop = 6,
                                PaddingBottom = 6,
                                BorderWidthLeft = 0f,
                                BorderWidthRight = 1f,
                                BorderWidthTop = 0f,
                                BorderWidthBottom = 1f,
                                BorderColorRight = DarkGray,
                                BorderColorBottom = DarkGray
                            };
                            transTable.AddCell(balanceCell);
                        }

                        document.Add(transTable);

                        // Loan Summary - Moved closer to table
                        //if (loan.Summary != null)
                        //{
                        //    // Removed document.Add(new Paragraph("\n")); to eliminate blank line

                        //    var summaryPara = new Paragraph();
                        //    summaryPara.Alignment = Element.ALIGN_LEFT;

                        //    string periodText = "";
                        //    if (startDate.HasValue && endDate.HasValue)
                        //        periodText = $"for period {startDate.Value:dd/MM/yyyy} - {endDate.Value:dd/MM/yyyy}";
                        //    else if (startDate.HasValue)
                        //        periodText = $"from {startDate.Value:dd/MM/yyyy}";
                        //    else if (endDate.HasValue)
                        //        periodText = $"up to {endDate.Value:dd/MM/yyyy}";

                        //    if (!string.IsNullOrEmpty(periodText))
                        //    {
                        //        summaryPara.Add(new Chunk($"Summary {periodText}: ", boldFont));
                        //    }
                        //    else
                        //    {
                        //        summaryPara.Add(new Chunk("Loan Summary: ", boldFont));
                        //    }

                        //    summaryPara.Add(new Chunk($"Principal Paid: {loan.Summary.TotalPrincipalRepaid:N0}", normalFont));
                        //    summaryPara.Add(new Chunk(" | ", normalFont));
                        //    summaryPara.Add(new Chunk($"Interest Accrued: {loan.Summary.TotalInterestAccrued:N0}", normalFont));
                        //    summaryPara.Add(new Chunk(" | ", normalFont));
                        //    summaryPara.Add(new Chunk($"Interest Paid: {loan.Summary.TotalInterestPaid:N0}", normalFont));

                        //    summaryPara.SpacingAfter = 8f;  // Reduced from 15f
                        //    document.Add(summaryPara);
                        //}

                        loanCounter++;
                    }
                }

                // ===== FOOTER =====
                document.Add(new Paragraph("\n"));
                string footerText = $"Statement Generated on: {DateTime.Now:dd/MM/yyyy HH:mm:ss}";

                if (startDate.HasValue || endDate.HasValue)
                {
                    string dateRangeInfo = "";
                    if (startDate.HasValue && endDate.HasValue)
                        dateRangeInfo = $" | Period: {startDate.Value:dd/MM/yyyy} - {endDate.Value:dd/MM/yyyy}";
                    else if (startDate.HasValue)
                        dateRangeInfo = $" | From: {startDate.Value:dd/MM/yyyy}";
                    else if (endDate.HasValue)
                        dateRangeInfo = $" | Up to: {endDate.Value:dd/MM/yyyy}";

                    footerText += dateRangeInfo;
                }

                footerText += $" | Total Accounts: {memberStatement.TotalAccounts}";

                var footerPara = new Paragraph(footerText, smallFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingBefore = 8f
                };
                document.Add(footerPara);

                // ===== FOOTER NOTES =====
                document.Add(new Paragraph("\n"));
                document.Add(new Paragraph("This is a system generated detailed statement for all member accounts.", smallFont)
                {
                    Alignment = Element.ALIGN_CENTER
                });
                document.Add(new Paragraph("For any queries, contact: rubanisacco@gmail.com", smallFont)
                {
                    Alignment = Element.ALIGN_CENTER
                });

                document.Close();
                writer.Close();

                return ms.ToArray();
            }
        }

        // Helper method to create cells with NO BORDERS (not used anymore, but kept for backward compatibility)
        private PdfPCell CreateStyledCell(string text, Font font, int alignment = Element.ALIGN_LEFT)
        {
            PdfPCell cell = new PdfPCell(new Phrase(text ?? "", font));
            cell.HorizontalAlignment = alignment;
            cell.Padding = 4f;

            // REMOVE ALL BORDERS
            cell.BorderWidthLeft = 0f;
            cell.BorderWidthRight = 0f;
            cell.BorderWidthTop = 0f;
            cell.BorderWidthBottom = 0f;

            return cell;
        }


        public class LoanPaymentRow
        {
            public string TransactionDate { get; set; }
            public decimal OpeningBalance { get; set; }
            public decimal Principal { get; set; }
            public decimal Interest { get; set; }
            public decimal Amount { get; set; }
            public decimal LoanBalance { get; set; }
            public string TransactionType { get; set; }
            public string Description { get; set; }
        }

        public class LoanDetails
        {
            public string LoanNumber { get; set; }
            public string LoanProductType { get; set; }
            public decimal AppliedAmount { get; set; }
            public decimal MonthlyRepayment { get; set; }
            public string MemberNumber { get; set; }
            public string DisbursedDate { get; set; }
        }

        public class LoanStatementResult
        {
            public string LoanNumber { get; set; }
            public CustomerInfo Customer { get; set; }
            public LoanDetails LoanDetails { get; set; }
            public List<LoanStatementRow> Statement { get; set; }
            public LoanSummary Summary { get; set; }
            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }
        }
        public class CustomerData
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Mobile { get; set; }
            public string Email { get; set; }
            public string Reference2 { get; set; }
            public string Reference3 { get; set; }
            public int BranchCode { get; set; }
            public int CustomerSerialNumber { get; set; }
            public int ProductCode { get; set; }
            public int TargetProductCode { get; set; }
        }

        public class CustomerInfo
        {
            public string FullName { get; set; }
            public string AccountNumber { get; set; }
            public string StaffNo { get; set; }
            public string PFNumber { get; set; }
            public string Mobile { get; set; }
            public string Email { get; set; }
        }



        // Helper method to create cells WITH BORDERS for loan table alignment
        private PdfPCell CreateTableCell(string text, Font font, int alignment = Element.ALIGN_LEFT, BaseColor borderColor = null, bool showBorders = false)
        {
            PdfPCell cell = new PdfPCell(new Phrase(text ?? "", font));
            cell.HorizontalAlignment = alignment;
            cell.Padding = 5f;
            cell.PaddingTop = 8f;
            cell.PaddingBottom = 8f;

            if (showBorders && borderColor != null)
            {
                // Add borders for better alignment
                cell.BorderWidthLeft = 1f;
                cell.BorderWidthRight = 1f;
                cell.BorderWidthTop = 1f;
                cell.BorderWidthBottom = 1f;
                cell.BorderColor = borderColor;
            }
            else
            {
                // No borders for shares table
                cell.BorderWidthLeft = 0f;
                cell.BorderWidthRight = 0f;
                cell.BorderWidthTop = 0f;
                cell.BorderWidthBottom = 0f;
            }

            return cell;
        }




        #endregion

        public class CustomerShareStatementRow
        {
            public string Date { get; set; }
            public decimal ShareContribution { get; set; }
            public decimal Cumulative { get; set; }
            public string Description { get; set; }
        }

        public class CustomerShareStatementResult
        {
            public List<CustomerShareStatementRow> Statement { get; set; }
            public decimal TotalContribution { get; set; }
        }

        public class LoanStatementRow
        {
            public string PostingDate { get; set; }
            public string TransactionType { get; set; }
            public string DocumentNo { get; set; }
            public string Description { get; set; }
            public decimal Debit { get; set; }
            public decimal Credit { get; set; }
            public decimal Balance { get; set; }
            public decimal Amount { get; set; }

            public string TransDate { get; set; }
            public decimal OpeningBalance { get; set; }
            public decimal Principle { get; set; }
            public decimal Interest { get; set; }
            public decimal LoanBalance { get; set; }
        }

        public class LoanSummary
        {
            public decimal TotalDisbursed { get; set; }
            public decimal TotalPrincipalRepaid { get; set; }
            public decimal TotalInterestPaid { get; set; }
            public decimal TotalInterestAccrued { get; set; }
            public decimal OutstandingLoanAmount { get; set; }
            public decimal OutstandingLoanInterest { get; set; }
            public decimal TotalOutstandingBalance { get; set; }
            public decimal OpeningBalance { get; set; }

        }

        public class RecentTransaction
        {
            public string PostingDate { get; set; }
            public string TransactionType { get; set; }
            public string DocumentNo { get; set; }
            public string Description { get; set; }
            public decimal Debit { get; set; }
            public decimal Credit { get; set; }
            public decimal Balance { get; set; }
        }


        public class CreditBatchImportRequest
        {
            public string FileName { get; set; }
        }


    }


}

