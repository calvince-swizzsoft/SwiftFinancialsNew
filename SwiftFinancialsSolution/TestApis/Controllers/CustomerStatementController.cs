using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using TestApis.Models;
using TestApis.Services;

namespace TestApis.Controllers
{
    [RoutePrefix("api/customer-statement")]
    public class CustomerStatementController : ApiController
    {
        private readonly CustomerStatementService _statementService = new CustomerStatementService();
        private readonly CustomerService _customerService = new CustomerService();

        private IHttpActionResult ApiResponse(bool success, string message, object data = null)
        {
            return Ok(new { success, message, data });
        }
        [HttpGet, Route("")]
        public IHttpActionResult GetStatement(
            [FromUri] DateTime startDate,
            [FromUri] DateTime endDate,
            [FromUri] string searchBy = null,
            [FromUri] string searchString = null,
            [FromUri] bool includeProductBreakdown = false)
        {
            try
            {
                if (startDate > endDate)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Start date cannot be after end date" });

                if (string.IsNullOrEmpty(searchString) && string.IsNullOrEmpty(searchBy))
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Search criteria is required" });

                // Get statement transactions
                var statement = _statementService.GetCustomerStatement(startDate, endDate, searchBy, searchString);

                // Get summary
                var summary = _statementService.GetStatementSummary(searchBy, searchString, startDate, endDate);

                // Get opening balance
                var openingBalance = _statementService.GetOpeningBalance(searchBy, searchString, startDate);
                summary.OpeningBalance = openingBalance;
                summary.ClosingBalance = openingBalance + summary.NetBalance;

                // Get product breakdown if requested
                if (includeProductBreakdown && !string.IsNullOrEmpty(searchString))
                {
                    // Determine which reference to use based on searchBy
                    string reference1 = searchBy == "Reference1" ? searchString : null;
                    string reference2 = searchBy == "Reference2" ? searchString : null;
                    string reference3 = searchBy == "Reference3" ? searchString : null;

                    // Use GetByReference with the appropriate reference
                    var customer = _customerService.GetByReference(reference1, reference2, reference3);

                    if (customer != null)
                    {
                        summary.ProductBreakdown = new System.Collections.Generic.List<CustomerProductStatementDTO>(
                            _statementService.GetStatementByProduct(customer.Id, startDate, endDate)
                        );
                    }
                }

                return ApiResponse(true, "Customer statement retrieved successfully", new
                {
                    statement = statement,
                    summary = summary,
                    transactionCount = ((System.Collections.Generic.List<CustomerStatementDTO>)statement).Count
                });
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpGet, Route("by-customer/{customerId:guid}")]
        public IHttpActionResult GetStatementByCustomerId(
            Guid customerId,
            [FromUri] DateTime startDate,
            [FromUri] DateTime endDate,
            [FromUri] bool includeProductBreakdown = true)
        {
            try
            {
                if (startDate > endDate)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Start date cannot be after end date" });

                // Get customer details
                var customer = _customerService.GetById(customerId);

                if (customer == null)
                    return Content(System.Net.HttpStatusCode.NotFound,
                                   new { success = false, message = "Customer not found" });

                // Get statement
                var statement = _statementService.GetCustomerStatementByCustomerId(customerId, startDate, endDate);
                var statementList = (System.Collections.Generic.List<CustomerStatementDTO>)statement;

                // Calculate opening balance
                var openingBalance = _statementService.GetCustomerBalanceAsOfDate(customerId, startDate.AddSeconds(-1));

                // Calculate running totals including opening balance
                decimal runningTotal = openingBalance;
                foreach (var transaction in statementList)
                {
                    runningTotal += transaction.Credit - transaction.Debit;
                    transaction.RunningTotal = runningTotal;
                }

                // Calculate summary
                var summary = new CustomerStatementSummaryDTO
                {
                    CustomerName = customer.FullName,
                    SerialNumber = customer.SerialNumber.ToString(),
                    FirstTransactionDate = startDate,
                    LastTransactionDate = endDate,
                    OpeningBalance = openingBalance,
                    ClosingBalance = runningTotal
                };

                // Calculate totals
                decimal totalDebit = 0, totalCredit = 0;
                foreach (var transaction in statementList)
                {
                    totalDebit += transaction.Debit;
                    totalCredit += transaction.Credit;
                }

                summary.TotalTransactions = statementList.Count;
                summary.TotalDebit = totalDebit;
                summary.TotalCredit = totalCredit;
                summary.NetBalance = totalCredit - totalDebit;

                // Get product breakdown if requested
                if (includeProductBreakdown)
                {
                    summary.ProductBreakdown = new System.Collections.Generic.List<CustomerProductStatementDTO>(
                        _statementService.GetStatementByProduct(customerId, startDate, endDate)
                    );
                }

                return ApiResponse(true, "Customer statement retrieved successfully", new
                {
                    customer = new
                    {
                        customer.FullName,
                        customer.SerialNumber,
                        customer.Reference1,
                        customer.Reference2,
                        customer.Reference3,
                        customer.AddressAddressLine1,
                        customer.AddressAddressLine2,
                        customer.AddressMobileLine,
                        customer.IndividualIdentityCardNumber
                    },
                    statement = statement,
                    summary = summary,
                    openingBalance = openingBalance,
                    closingBalance = runningTotal
                });
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpGet, Route("balance/{customerId:guid}")]
        public IHttpActionResult GetCurrentBalance(Guid customerId)
        {
            try
            {
                var customer = _customerService.GetById(customerId);

                if (customer == null)
                    return Content(System.Net.HttpStatusCode.NotFound,
                                   new { success = false, message = "Customer not found" });

                var balance = _statementService.GetCustomerBalanceAsOfDate(customerId, DateTime.Now);

                return ApiResponse(true, "Customer balance retrieved successfully", new
                {
                    customerId = customerId,
                    customerName = customer.FullName,
                    currentBalance = balance,
                    asOfDate = DateTime.Now,
                    serialNumber = customer.SerialNumber,
                    references = new
                    {
                        customer.Reference1,
                        customer.Reference2,
                        customer.Reference3
                    }
                });
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpGet, Route("mini-statement/{customerId:guid}")]
        public IHttpActionResult GetMiniStatement(Guid customerId, [FromUri] int lastNTransactions = 10)
        {
            try
            {
                var endDate = DateTime.Now;
                var startDate = endDate.AddMonths(-3); // Last 3 months for mini statement

                var statement = _statementService.GetCustomerStatementByCustomerId(customerId, startDate, endDate);
                var statementList = (System.Collections.Generic.List<CustomerStatementDTO>)statement;

                // Get current balance
                var currentBalance = _statementService.GetCustomerBalanceAsOfDate(customerId, DateTime.Now);

                // Take only last N transactions
                if (statementList.Count > lastNTransactions)
                {
                    statementList = statementList.GetRange(statementList.Count - lastNTransactions, lastNTransactions);
                }

                // Calculate running totals backwards from current balance
                decimal runningBalance = currentBalance;
                for (int i = statementList.Count - 1; i >= 0; i--)
                {
                    statementList[i].RunningTotal = runningBalance;
                    runningBalance -= statementList[i].Credit - statementList[i].Debit;
                }

                return ApiResponse(true, "Mini statement retrieved successfully", new
                {
                    transactions = statementList,
                    currentBalance = currentBalance,
                    transactionCount = statementList.Count,
                    period = $"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}",
                    asOfDate = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpGet, Route("product-breakdown/{customerId:guid}")]
        public IHttpActionResult GetProductBreakdown(
            Guid customerId,
            [FromUri] DateTime? startDate = null,
            [FromUri] DateTime? endDate = null)
        {
            try
            {
                var customer = _customerService.GetById(customerId);

                if (customer == null)
                    return Content(System.Net.HttpStatusCode.NotFound,
                                   new { success = false, message = "Customer not found" });

                // Use default dates if not provided
                if (!startDate.HasValue) startDate = DateTime.Now.AddMonths(-1);
                if (!endDate.HasValue) endDate = DateTime.Now;

                if (startDate > endDate)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Start date cannot be after end date" });

                var productBreakdown = _statementService.GetStatementByProduct(customerId, startDate.Value, endDate.Value);

                return ApiResponse(true, "Product breakdown retrieved successfully", new
                {
                    customer = new
                    {
                        customer.FullName,
                        customer.SerialNumber
                    },
                    period = new
                    {
                        startDate = startDate.Value,
                        endDate = endDate.Value
                    },
                    productBreakdown = productBreakdown,
                    productCount = ((System.Collections.Generic.List<CustomerProductStatementDTO>)productBreakdown).Count
                });
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }


        [HttpGet, Route("pdf/{customerId:guid}")]
        public IHttpActionResult DownloadStatementPdf(
Guid customerId,
DateTime startDate,
DateTime endDate)
        {
            try
            {
                if (startDate > endDate)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                    new { success = false, message = "Start date cannot be after end date" });
                var customer = _customerService.GetById(customerId);
                if (customer == null)
                    return Content(System.Net.HttpStatusCode.NotFound,
                    new { success = false, message = "Customer not found" });
                // Get transactions
                var statement = _statementService.GetCustomerStatementByCustomerId(customerId, startDate, endDate)
    .ToList();
                // Calculate balances
                var openingBalance = _statementService.GetCustomerBalanceAsOfDate(customerId, startDate.AddSeconds(-1));
                var closingBalance = _statementService.GetCustomerBalanceAsOfDate(customerId, endDate);
                // Calculate totals
                decimal totalDebit = 0, totalCredit = 0;
                foreach (var transaction in statement)
                {
                    totalDebit += transaction.Debit;
                    totalCredit += transaction.Credit;
                }
                // Get product breakdown
                var productBreakdown = _statementService.GetStatementByProduct(customerId, startDate, endDate)
    .ToList();
                // Prepare summary
                var summary = new CustomerStatementSummaryDTO
                {
                    CustomerName = customer.FullName,
                    SerialNumber = customer.SerialNumber.ToString(),
                    FirstTransactionDate = startDate,
                    LastTransactionDate = endDate,
                    OpeningBalance = openingBalance,
                    ClosingBalance = closingBalance,
                    TotalTransactions = statement.Count,
                    TotalDebit = totalDebit,
                    TotalCredit = totalCredit,
                    NetBalance = totalCredit - totalDebit,
                    ProductBreakdown = productBreakdown,
                    FullAccount = statement.FirstOrDefault()?.FullAccount
                };
                // Prepare customer info with AccountName
                var customerInfo = new
                {
                    // For Member Name - get from first transaction's AccountName
                    AccountName = statement.FirstOrDefault()?.AccountName ?? customer.FullName,
                    FullName = customer.FullName,
                    SerialNumber = customer.SerialNumber,
                    Reference1 = customer.Reference1,  // Will be used as Payroll No
                    Reference2 = customer.Reference2,  // Will be used as Member No (with zeros)
                    Reference3 = customer.Reference3,
                    AddressAddressLine1 = customer.AddressAddressLine1,
                    AddressAddressLine2 = customer.AddressAddressLine2,
                    AddressMobileLine = customer.AddressMobileLine,
                    IndividualIdentityCardNumber = customer.IndividualIdentityCardNumber,
                    RegistrationDate = customer.CreatedDate
                };
                // Prepare product summaries
                var productSummaries = new List<ProductTransactionSummary>();
                foreach (var product in productBreakdown)
                {
                    var productTransactions = statement
                    .Where(t => t.Product == product.ProductName)
                    .ToList();
                    productSummaries.Add(new ProductTransactionSummary
                    {
                        ProductName = product.ProductName,
                        ProductType = product.ProductType,
                        Transactions = productTransactions,
                        TotalDebit = product.TotalDebit,
                        TotalCredit = product.TotalCredit,
                        NetBalance = product.NetBalance
                    });
                }
                var pdfService = new CustomerStatementPdfService();
                var pdfBytes = pdfService.GenerateCustomerStatementPdf(
                statement, summary, customerInfo, startDate, endDate,
                openingBalance, closingBalance, productSummaries);
                return new System.Web.Http.Results.ResponseMessageResult(
                new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new System.Net.Http.ByteArrayContent(pdfBytes)
                    {
                        Headers =
                {
ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf"),
ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment")
{
FileName = $"Rubani_Statement_{customer.Reference2}_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.pdf"
}
                }
                    }
                });
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                new { success = false, message = ex.Message });
            }
        }
    }
}