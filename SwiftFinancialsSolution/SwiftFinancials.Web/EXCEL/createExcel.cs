using Application.MainBoundedContext.DTO.BackOfficeModule;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SwiftFinancials.Web.EXCEL
{
    public class createExcel
    {
        public byte[] GenerateExcelFile(IEnumerable<LoanCaseDTO> loanCases)
        {
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Loan Cases");

                // Add headers
                worksheet.Cells[1, 1].Value = "Case Number";
                worksheet.Cells[1, 2].Value = "Customer Name";
                worksheet.Cells[1, 3].Value = "Amount Applied";
                worksheet.Cells[1, 4].Value = "Created By";
                worksheet.Cells[1, 5].Value = "Created Date";
                

                
                int row = 2;
                foreach (var loanCase in loanCases)
                {
                    worksheet.Cells[row, 1].Value = loanCase.CaseNumber;
                    worksheet.Cells[row, 2].Value = loanCase.CustomerIndividualFirstName;
                    worksheet.Cells[row, 3].Value = loanCase.AmountApplied;
                    worksheet.Cells[row, 4].Value = loanCase.CreatedBy;
                    worksheet.Cells[row, 5].Value = loanCase.CreatedDate.ToString("yyyy-MM-dd");
                    // Map more fields as necessary

                    row++;
                }

                
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                return package.GetAsByteArray();
            }
        }

    }
}