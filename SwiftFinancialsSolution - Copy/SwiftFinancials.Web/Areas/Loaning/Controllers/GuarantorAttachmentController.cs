using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Windows.Forms;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.BackOfficeModule;
using Application.MainBoundedContext.DTO.RegistryModule;
using Infrastructure.Crosscutting.Framework.Utils;
using Newtonsoft.Json;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;



namespace SwiftFinancials.Web.Areas.Loaning.Controllers
{
    public class GuarantorAttachmentController : MasterController
    {
        string connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;
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

            var pageCollectionInfo = await _channelService.FindLoanGuarantorAttachmentHistoryByStatusAndFilterInPageAsync((int)LoanGuarantorAttachmentHistoryStatus.Attached, DateTime.Now.AddDays(-30), DateTime.Now.AddDays(-30), string.Empty, 0, 300);

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(LoanCase => LoanCase.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<LoanGuarantorDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }


        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var dataCapture = await _channelService.FindLoanGuarantorAttachmentHistoryEntriesByLoanGuarantorAttachmentHistoryIdAsync(id, GetServiceHeader());

            return View(dataCapture);
        }

        private async Task<List<LoanGuarantorDTO>> GetLoanGuarantorsAsync(Guid loaneeCustomerId)
        {
            var loanguarantors = new List<LoanGuarantorDTO>();

            using (var connection = new SqlConnection(connectionString))
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

            using (var connection = new SqlConnection(connectionString))
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

        public async Task<ActionResult> CustomerAccountLookUp(Guid? id, LoanGuarantorDTO loanGuarantorDTO)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View("create");
            }

            var accounts = await _channelService.FindCustomerAccountAsync(parseId, true, true, true, true, GetServiceHeader());

            if (accounts != null)
            {
                loanGuarantorDTO.CustomerAccountFullAccountNumber = accounts.FullAccountNumber;
                loanGuarantorDTO.CustomerAccountAccountId = accounts.Id;
                loanGuarantorDTO.CustomerAccountAccountStatusDescription = accounts.StatusDescription;
                loanGuarantorDTO.CustomerAccountAccountRemarks = accounts.Remarks;
                loanGuarantorDTO.BookBalance = accounts.BookBalance;
                loanGuarantorDTO.CustomerId = accounts.CustomerId;
                loanGuarantorDTO.CustomerIndividualFirstName = accounts.CustomerFullName;
                loanGuarantorDTO.CustomerAccounntCustomerTypeDescription = accounts.CustomerTypeDescription;
                loanGuarantorDTO.CustomerIndividualPayrollNumbers = accounts.CustomerIndividualPayrollNumbers;
                loanGuarantorDTO.CustomerPersonalIdentificationNumber = accounts.CustomerPersonalIdentificationNumber;
                loanGuarantorDTO.CustomerReference1 = accounts.CustomerReference1;
                loanGuarantorDTO.CustomerReference2 = accounts.CustomerReference2;
                loanGuarantorDTO.CustomerReference3 = accounts.CustomerReference3;
                loanGuarantorDTO.EmployerDescription = accounts.CustomerStationZoneDivisionEmployerDescription;

                var productId = accounts.CustomerAccountTypeTargetProductId;

                var findLoanCaseId = await _channelService.FindLoanCasesByCustomerIdInProcessAsync(loanGuarantorDTO.CustomerId, GetServiceHeader());

                // Loan Guarantors
                List<LoanGuarantorDTO> loaneeLoanGuarantors = await GetLoanGuarantorsAsync(loanGuarantorDTO.CustomerId);
                for (int i = 0; i < loaneeLoanGuarantors.Count; i++)
                {
                    var customerDetails = loaneeLoanGuarantors[i];
                    var customername = await _channelService.FindCustomerAsync(customerDetails.CustomerId, GetServiceHeader());

                    loaneeLoanGuarantors[i].CustomerIndividualFirstName = customername.IndividualFirstName;
                    loaneeLoanGuarantors[i].CustomerIndividualLastName = customername.IndividualLastName;

                    loaneeLoanGuarantors[i].CreatedDate = Convert.ToDateTime(loaneeLoanGuarantors[i].CreatedDate);
                }


                return Json(new
                {
                    success = true,
                    data = new
                    {
                        CustomerAccountFullAccountNumber = loanGuarantorDTO.CustomerAccountFullAccountNumber,
                        CustomerAccountAccountId = loanGuarantorDTO.CustomerAccountAccountId,
                        CustomerAccountAccountStatusDescription = loanGuarantorDTO.CustomerAccountAccountStatusDescription,
                        CustomerAccountAccountRemarks = loanGuarantorDTO.CustomerAccountAccountRemarks,
                        BookBalance = loanGuarantorDTO.BookBalance,
                        CustomerId = loanGuarantorDTO.CustomerId,
                        CustomerIndividualFirstName = loanGuarantorDTO.CustomerIndividualFirstName,
                        CustomerAccounntCustomerTypeDescription = loanGuarantorDTO.CustomerAccounntCustomerTypeDescription,
                        CustomerIndividualPayrollNumbers = loanGuarantorDTO.CustomerIndividualPayrollNumbers,
                        CustomerPersonalIdentificationNumber = loanGuarantorDTO.CustomerPersonalIdentificationNumber,
                        CustomerReference1 = loanGuarantorDTO.CustomerReference1,
                        CustomerReference2 = loanGuarantorDTO.CustomerReference2,
                        CustomerReference3 = loanGuarantorDTO.CustomerReference3,
                        EmployerDescription = loanGuarantorDTO.EmployerDescription,

                        LoanGuarantors = loaneeLoanGuarantors
                    }
                });
            }

            return Json(new { success = false, message = "Customer account not found" });
        }

        public async Task<ActionResult> AttachToLookUp(Guid? id, LoanGuarantorDTO loanGuarantorDTO)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View("create");
            }

            var loanProduct = await _channelService.FindLoanProductAsync(parseId, GetServiceHeader());

            if (loanProduct != null)
            {
                loanGuarantorDTO.AttachTo = loanProduct.Description;
                loanGuarantorDTO.AttachToId = loanProduct.Id;

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        AttachTo = loanGuarantorDTO.AttachTo,
                        AttachToId = loanGuarantorDTO.AttachToId,
                    }
                });
            }

            return Json(new { success = false, message = "Customer account not found" });
        }


        public async Task<ActionResult> Create(Guid? id)
        {
            await ServeNavigationMenus();

            ViewBag.MonthSelectList = GetMonthsAsync(string.Empty);

            ViewBag.LoanProductSection = GetLoanRegistrationLoanProductSectionsSelectList(string.Empty);

            ViewBag.CustomerFilter = GetCustomerFilterSelectList(string.Empty);
            ViewBag.customerFilter = GetCustomerFilterSelectList(string.Empty);
            ViewBag.ProductCode = GetProductCodeSelectList(string.Empty);
            ViewBag.RecordStatus = GetRecordStatusSelectList(string.Empty);

            return View();
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(LoanGuarantorDTO loanGuarantorDTO, string TableDataJson)
        {
            if (string.IsNullOrEmpty(TableDataJson))
            {
                ModelState.AddModelError("", "No table data received.");
                return View();
            }

            var guarantors = JsonConvert.DeserializeObject<List<Guarantor>>(TableDataJson);

            if (guarantors == null || !guarantors.Any())
            {
                ModelState.AddModelError("", "Invalid table data.");
                return View();
            }

            var LoanGuarantorDetails = new ObservableCollection<LoanGuarantorDTO>();
            foreach (var guarantor in guarantors)
            {
                var L = new LoanGuarantorDTO
                {
                    AmountGuaranteed = Convert.ToDecimal(guarantor.AmountGuaranteed),
                    CommittedShares = Convert.ToDecimal(guarantor.CommittedShares),
                    Id = Guid.Parse(guarantor.Id),
                    CustomerId = Guid.Parse(guarantor.CustomerId),
                    InterestAttached = Convert.ToDecimal(guarantor.InterestAttached),
                    PrincipalAttached = Convert.ToDecimal(guarantor.PrincipalAttached),
                    TotalShares = Convert.ToDecimal(guarantor.TotalShares)
                };

                LoanGuarantorDetails.Add(L);
            }


            loanGuarantorDTO.ValidateAll();

            if (!loanGuarantorDTO.HasErrors)
            {
                await ServeNavigationMenus();

                ViewBag.MonthSelectList = GetMonthsAsync(string.Empty);

                ViewBag.LoanProductSection = GetLoanRegistrationLoanProductSectionsSelectList(string.Empty);

                ViewBag.CustomerFilter = GetCustomerFilterSelectList(string.Empty);
                ViewBag.customerFilter = GetCustomerFilterSelectList(string.Empty);
                ViewBag.ProductCode = GetProductCodeSelectList(string.Empty);
                ViewBag.RecordStatus = GetRecordStatusSelectList(string.Empty);

                var loanguarantorAttachmentHistory = await _channelService.AttachLoanGuarantorsAsync(loanGuarantorDTO.CustomerAccountAccountId, loanGuarantorDTO.AttachToId, LoanGuarantorDetails, 1234, GetServiceHeader());
                TempData["Success"] = "Operation Completed Successfully.";
                return RedirectToAction("Create");
            }
            else
            {
                await ServeNavigationMenus();

                var errorMessages = loanGuarantorDTO.ErrorMessages;
                string errorMessage = string.Join("\n", errorMessages.Where(msg => !string.IsNullOrWhiteSpace(msg)));

                ViewBag.LoanProductSection = GetLoanRegistrationLoanProductSectionsSelectList(loanGuarantorDTO.LoanRegistrationLoanProductSectionDescription.ToString());
                ViewBag.customerFilter = GetCustomerFilterSelectList(loanGuarantorDTO.CustomerFilterDescription.ToString());

                TempData["Failed"] = $"Operation Failed!\n{errorMessage}";

                return View(loanGuarantorDTO);
            }
        }
    }
}
