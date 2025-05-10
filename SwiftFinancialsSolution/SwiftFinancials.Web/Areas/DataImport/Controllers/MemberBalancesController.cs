using Application.MainBoundedContext.DTO.AccountsModule;
using Microsoft.AspNetCore.Http;
using OfficeOpenXml;
using SwiftFinancials.Presentation.Infrastructure.Models;
using SwiftFinancials.Web.Controllers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;


namespace SwiftFinancials.Web.Areas.DataImport.Controllers
{
    public class MemberBalancesController : MasterController
    {
        // GET: DataImport/MemberBalances
        //public ActionResult Index()
        //{
        //    return View();
        //}


        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();
            
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(HttpPostedFileBase uploadedFile)
        {
            if (uploadedFile == null || uploadedFile.ContentLength == 0)
            {
                TempData["Error"] = "Please select a valid Excel file.";
                return RedirectToAction("Create");
            }

            var stream = new MemoryStream();
            uploadedFile.InputStream.CopyTo(stream);
            stream.Position = 0;

            var package = new ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets[1];
            var rowCount = worksheet.Dimension.Rows;

            for (int row = 2; row <= rowCount; row++)
            {
                try
                {
                    string customerAccountId = worksheet.Cells[row, 1].Text;
                    string debitChartOfAccountId = worksheet.Cells[row, 2].Text;
                    string creditChartOfAccountId = worksheet.Cells[row, 3].Text;
                    string amountStr = worksheet.Cells[row, 4].Text;
                    //string amount2Str = worksheet.Cells[row, 5].Text;
                    string transactionCode = worksheet.Cells[row, 5].Text;
                    string productCode = worksheet.Cells[row, 6].Text;
                    string primaryDescription = worksheet.Cells[row, 7].Text;
                    string secondaryDescription = worksheet.Cells[row, 8].Text;
                    string reference = worksheet.Cells[row, 9].Text;
                    string branchId = worksheet.Cells[row, 10].Text;
                    string valueDateStr = worksheet.Cells[row, 11].Text;

                    if (!Guid.TryParse(customerAccountId, out Guid customerGuid) ||
                        !Guid.TryParse(creditChartOfAccountId, out Guid creditGuid) ||
                        !Guid.TryParse(debitChartOfAccountId, out Guid debitGuid) ||
                        !Guid.TryParse(branchId, out Guid branchGuid) ||
                        !decimal.TryParse(amountStr, out decimal amount) ||
                        //!decimal.TryParse(amount2Str, out decimal amount2) ||
                        !int.TryParse(transactionCode, out int txnCode))
                        //!DateTime.TryParse(valueDateStr, out DateTime valueDate))
                    {
                        TempData["Error"] = $"Row {row} has invalid data.";
                        continue;
                    }


                    //var CustomerAccount = await _channelService.GetCustomerAccountAsync(creditChartOfAccountId, GetServiceHeader()); 

                    var CustomerAccount = new CustomerAccountDTO();

                    try
                    {

                        CustomerAccount = await _channelService.FindCustomerAccountAsync(customerGuid, false, false, false, false, GetServiceHeader());

                    }

                    catch (Exception ex)
                    {

                        TempData["Error"] = ex.Message;
                        RedirectToAction("Create");
                    }
                   

                    var entry = new CustomerTransactionModel
                    {
                        CreditCustomerAccountId = customerGuid,
                        CreditChartOfAccountId = creditGuid,
                        DebitChartOfAccountId = debitGuid,
                        TotalValue = amount,
                        PrimaryDescription = primaryDescription,
                        SecondaryDescription = secondaryDescription,
                        Reference = reference,
                        //ValueDate = valueDate,
                        BranchId = branchGuid,
                        TransactionCode = txnCode,
                        CreditCustomerAccount = CustomerAccount,
                        DebitCustomerAccount = CustomerAccount
                    };

                    await _channelService.AddJournalWithCustomerAccountAsync(entry, GetServiceHeader());
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"Error on row {row}: {ex.Message}";
                    continue;
                }
            }

            TempData["Success"] = "Excel data imported successfully!";
            return RedirectToAction("Create");
        }

    }
}