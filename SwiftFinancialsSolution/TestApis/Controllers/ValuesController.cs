using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.Entity.Validation;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Cors;
//using System.Web.Http.Cors;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.AdministrationModule;
using Application.MainBoundedContext.DTO.RegistryModule;
using DistributedServices.MainBoundedContext;
using Infrastructure.Crosscutting.Framework.Utils;
//using Microsoft.AspNetCore.Cors;
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
        [Route("GetGeneralLedgers")]
        public async Task<IHttpActionResult> GetGeneralLedgers([FromUri] string text = null, int? accountCategory = null, bool updateDepth = false)
        {
            try
            {
                var serviceHeader = master.GetServiceHeader();

             
                var gls = await master._channelService.FindGeneralLedgerAccountsWithCategoryAndTextAsync(
                    accountCategory,
                    text,
            
                    updateDepth,
                    serviceHeader
                );

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

                var effectiveStartDate =  new DateTime(1900, 1, 1);
                                                                         
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
                    return BadRequest("Failed to create journal voucher" );
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

                foreach (var gl in purchaseInvoiceDTO.PurchaseInvoiceLines) {

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


            else {

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
        public async Task<IHttpActionResult> GetPurchaseCreditMemos()
        {
            var serviceHeader = master.GetServiceHeader();

            var creditMemos = await master._channelService.FindPurchaseCreditMemosAsync(serviceHeader);

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
        public async Task<IHttpActionResult> GetSalesInvoices()
        {
            var serviceHeader = master.GetServiceHeader();

            var salesInvoices = await master._channelService.FindSalesInvoicesAsync(serviceHeader);

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





        [HttpPost]
        [Route("AddSalesCreditMemo")]
        public async Task<IHttpActionResult> AddSalesCreditMemo([FromBody] SalesCreditMemoDTO salesCreditMemoDTO)
        {

            var serviceHeader = master.GetServiceHeader();

            var totalLines = 0.00m;



            if (salesCreditMemoDTO != null)
            {


                var targetSalesInvoice = await master._channelService.FindSalesInvoiceAsync(salesCreditMemoDTO.SalesInvoiceId, serviceHeader);

                if (targetSalesInvoice == null)
                {

                    return Json(new
                    {

                        success = false,
                        message = "target invoice does not exist" //or doesnt exist??
                    });
                }

                foreach (var gl in salesCreditMemoDTO.SalesCreditMemoLines)
                {

                    totalLines += gl.Amount;

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



               

                if (totalLines != salesCreditMemoDTO.TotalAmount)
                    {

                        return Json(new
                        {

                            success = false,
                            mesSage = "amount in lines do no equal totalamount"
                        });
                    }

                salesCreditMemoDTO.ValidateAll();


                if (salesCreditMemoDTO.ErrorMessages.Count > 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = salesCreditMemoDTO.ErrorMessages
                    });
                }

                var result = await master._channelService.AddNewSalesCreditMemoAsync(salesCreditMemoDTO, serviceHeader);


                if (result != null)
                {


                    return Json(new
                    {
                        success = true,
                        message = "Successfully added Sales CREDIT MEMO with lines."
                    });
                }


                else
                {
                    return Json(new
                    {
                        success = false,
                        message = "Failed to add SALES CREDIT MEMO with lines."
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
        public async Task<IHttpActionResult> GetSalesCreditMemos()
        {
            var serviceHeader = master.GetServiceHeader();

            var creditMemos = await master._channelService.FindSalesCreditMemosAsync(serviceHeader);

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
        [Route("PostReceipt")]
        public async Task<IHttpActionResult> ReceiveCustomerPayment(ReceiptDTO receiptDTO)
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






    }
}
