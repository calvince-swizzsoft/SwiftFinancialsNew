using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Cors;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.AdministrationModule;
using Application.MainBoundedContext.DTO.RegistryModule;
using Infrastructure.Crosscutting.Framework.Utils;
using SwiftFinancials.Presentation.Infrastructure.Models;
using SwiftFinancials.Presentation.Infrastructure.Services;
using SwiftFinancials.Presentation.Infrastructure.Util;
using SwiftFinancials.TextAlertDispatcher.Celcom.Configuration;
using TestApis.Models;
using TestApis.Services;

namespace TestApis.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    [AllowAnonymous]
    [RoutePrefix("api/values")]
    public class ValuesController : ApiController
    {
        private readonly MasterController master;
        private readonly string _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;

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
        public async Task<IHttpActionResult> getBanks(int? pagesize, int? pageindex)
        {
            try
            {
                var serviceHeader = master.GetServiceHeader();
                var pageCollectionInfo = await master._channelService.FindBanksInPageAsync((int)pagesize, (int)pageindex, serviceHeader);
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


        [HttpPost]
        [Route("AddBanks")]
        public async Task<IHttpActionResult> AddBanks([FromBody] BankDTO bankDTO)
        {
            var serviceHeader = master.GetServiceHeader();

            if (bankDTO == null)
                return Json(new ApiResponse<object> { Success = false, Message = "Invalid data.", Data = null });

            bankDTO.ValidateAll();

            if (!bankDTO.HasErrors)
            {
                var result = await master._channelService.AddBankAsync(bankDTO, serviceHeader);

                await master._channelService.UpdateBankBranchesByBankIdAsync(bankDTO.Id, bankDTO.BankBranchesDTO, serviceHeader);

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
                    Data = bankDTO
                });
            }

            return Json(new ApiResponse<object>
            {
                Success = false,
                Message = "Validation failed.",
                Data = bankDTO.ErrorMessages
            });
        }

        [HttpPost]
        [Route("AddBankLinkages")]
        public async Task<IHttpActionResult> AddBankLinkages([FromBody] BankLinkageDTO bankLinkageDTO)
        {
            var serviceHeader = master.GetServiceHeader();

            if (bankLinkageDTO == null)
                return Json(new ApiResponse<object> { Success = false, Message = "Invalid data.", Data = null });

            bankLinkageDTO.ValidateAll();

            if (!bankLinkageDTO.HasErrors)
            {
                var result = await master._channelService.AddBankLinkageAsync(bankLinkageDTO, serviceHeader);

                if (result.ErrorMessages != null)
                {
                    return Json(new ApiResponse<object>
                    {
                        Success = false,
                        Message = result.ErrorMessages.ToString(),
                        Data = null
                    });
                }

                return Json(new ApiResponse<object>
                {
                    Success = true,
                    Message = "bank Linkage created successfully.",
                    Data = bankLinkageDTO
                });
            }

            return Json(new ApiResponse<object>
            {
                Success = false,
                Message = "Validation failed.",
                Data = bankLinkageDTO.ErrorMessages
            });
        }


        [HttpGet]
        [Route("GetGeneralLeadgersBalances")]
        public async Task<IHttpActionResult> GetGeneralLeadgers()
        {
            try
            {
                var serviceHeader = master.GetServiceHeader();
                var chartOfAccountDTOs = await master._channelService.FindGeneralLedgerAccountsAsync(true, true, serviceHeader);

                return Json(new ApiResponse<object>
                {
                    Success = true,
                    Message = chartOfAccountDTOs?.Count > 0 ? $"{chartOfAccountDTOs.Count} Transactions Found." : "No Transaction  found.",
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

        [Route("addPostingPeriod")]
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

        [HttpPost]
        [Route("addJournals")]
        public async Task<IHttpActionResult> addJournals([FromBody] JournalDTO journalDTO)
        {
            var serviceHeader = master.GetServiceHeader();
            CustomerTransactionModel transactionModel = new CustomerTransactionModel();

            journalDTO.MapTo<TransactionModel>();
            transactionModel.CreditChartOfAccountId = new Guid("8E87F619-592A-C077-EDE8-08D38C554F2E");
            transactionModel.CreditCustomerAccountId = new Guid("CC04EC57-A535-E911-A2B8-000C2914209C");
            transactionModel.DebitCustomerAccountId = new Guid("15C4DA29-BF35-E911-A2B8-000C2914209C");
            transactionModel.DebitChartOfAccountId = new Guid("ADEC18B6-6EB9-C271-1501-08D38C554F2F");
            transactionModel.CreditCustomerAccount = await master._channelService.FindCustomerAccountAsync(transactionModel.CreditCustomerAccountId, true, true, true, true, serviceHeader);
            transactionModel.DebitCustomerAccount = await master._channelService.FindCustomerAccountAsync(transactionModel.DebitCustomerAccountId, true, true, true, true, serviceHeader);
            transactionModel.TotalValue = journalDTO.TotalValue;
            transactionModel.BranchId = journalDTO.BranchId;
            ObservableCollection<TariffWrapper> tariffWrappers = new ObservableCollection<TariffWrapper>();
            TariffWrapper tariffWrapper = new TariffWrapper();
            transactionModel.MapTo<TariffWrapper>();
            tariffWrapper.Amount = journalDTO.TotalValue;

            tariffWrapper.DebitCustomerAccount = transactionModel.DebitCustomerAccount;
            tariffWrapper.CreditCustomerAccount = transactionModel.CreditCustomerAccount;
            tariffWrapper.CreditGLAccountId = transactionModel.CreditChartOfAccountId;
            tariffWrapper.DebitGLAccountId = transactionModel.CreditChartOfAccountId;
            transactionModel.TransactionCode = (int)SystemTransactionCode.CashDeposit;

            tariffWrappers.Add(tariffWrapper);
            var result = await master._channelService.AddJournalWithCustomerAccountAsync(transactionModel, serviceHeader);

            return Json(new ApiResponse<object>
            {
                Success = true,
                Message = "Journal added successfully.",
                Data = result
            });
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

    }
}
