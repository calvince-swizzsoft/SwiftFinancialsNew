using Application.MainBoundedContext.DTO.RegistryModule;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.IO;

namespace TestApis.Services
{
    public class CustomerStatementPdfService
    {
        // Color palette for Rubani Sacco
        private static readonly BaseColor SkyBlue = new BaseColor(0, 174, 239); // #00AEEF
        private static readonly BaseColor Red = new BaseColor(255, 0, 0);       // #FF0000
        private static readonly BaseColor DarkGray = new BaseColor(26, 26, 26); // #1A1A1A
        private static readonly BaseColor LightGray = new BaseColor(217, 217, 217); // #D9D9D9
        private static readonly BaseColor White = BaseColor.WHITE;

        public byte[] GenerateCustomerStatementPdf(
            List<CustomerStatementDTO> transactions,
            CustomerStatementSummaryDTO summary,
            object customerInfo,
            DateTime startDate,
            DateTime endDate,
            decimal openingBalance,
            decimal closingBalance,
            List<ProductTransactionSummary> productSummaries = null)
        {
            using (var ms = new MemoryStream())
            {
                var document = new Document(PageSize.A4, 30, 30, 50, 30);
                PdfWriter writer = PdfWriter.GetInstance(document, ms);

                document.Open();

                // Fonts
                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16, SkyBlue);
                var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, White);
                var subHeaderFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11, DarkGray);
                var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 9);
                var boldFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9);
                var smallFont = FontFactory.GetFont(FontFactory.HELVETICA, 8, DarkGray);
                var redFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9, Red);
                var blueFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9, SkyBlue);

                // ===== CUSTOM HEADER WITH LOGO =====
                AddCustomHeaderWithLogo(document);

                // ===== STATEMENT TITLE =====
                document.Add(new Paragraph("MEMBER DETAILED STATEMENT", titleFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 10f
                });

                document.Add(new Paragraph(
                    $"Statement Period: {startDate:dd MMMM yyyy} to {endDate:dd MMMM yyyy}",
                    boldFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 15f
                });

                // ===== MEMBER INFORMATION =====
                var memberInfoSection = CreateMemberInfoSection(customerInfo, boldFont, normalFont);
                document.Add(memberInfoSection);

                document.Add(new Paragraph("\n"));

                // ===== TRANSACTIONS BY PRODUCT TYPE =====
                if (productSummaries != null && productSummaries.Count > 0)
                {
                    foreach (var productSummary in productSummaries)
                    {
                        var productTransactions = transactions.FindAll(t => t.Product == productSummary.ProductName);
                        var productSection = CreateProductTransactionSection(
                            productSummary,
                            productTransactions,
                            subHeaderFont, headerFont, normalFont, boldFont);
                        document.Add(productSection);
                        document.Add(new Paragraph("\n"));
                    }
                }
                else
                {
                    // Group transactions by product
                    var groupedTransactions = GroupTransactionsByProduct(transactions);
                    foreach (var group in groupedTransactions)
                    {
                        var productSection = CreateProductTransactionSection(
                            new ProductTransactionSummary
                            {
                                ProductName = group.Key,
                                Transactions = group.Value
                            },
                            group.Value,
                            subHeaderFont, headerFont, normalFont, boldFont);
                        document.Add(productSection);
                        document.Add(new Paragraph("\n"));
                    }
                }

                // ===== CUSTOM FOOTER =====
                AddCustomFooter(document);

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
                return ms.ToArray();
            }
        }

       private void AddCustomHeaderWithLogo(Document document)
{
    try
    {
        // Create a table with 2 columns: Logo | Company Info
        PdfPTable headerTable = new PdfPTable(2)
        {
            WidthPercentage = 100,
            SpacingAfter = 15f
        };
        headerTable.SetWidths(new float[] { 25, 75 });

        // Column 1: Logo
        PdfPCell logoCell = new PdfPCell();
        logoCell.Border = Rectangle.NO_BORDER;
        logoCell.HorizontalAlignment = Element.ALIGN_LEFT;
        logoCell.VerticalAlignment = Element.ALIGN_MIDDLE;

        string logoPath = @"C:\Users\ADMIN\source\repos\SwiftFinancialsNew\SwiftFinancialsSolution\TestApis\Assets\Images\rubani-logo.jpeg";
        if (File.Exists(logoPath))
        {
            try
            {
                Image logo = Image.GetInstance(logoPath);
                logo.ScaleToFit(80, 80); // Resize logo
                logoCell.AddElement(logo);
            }
            catch (Exception)
            {
                // If logo loading fails, add text placeholder
                logoCell.AddElement(new Paragraph("RUBANI SACCO",
                    FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, SkyBlue)));
            }
        }
        else
        {
            // If logo file doesn't exist, add text placeholder
            logoCell.AddElement(new Paragraph("RUBANI SACCO",
                FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, SkyBlue)));
        }

        headerTable.AddCell(logoCell);

        // Column 2: Company Info (Moved left by reducing padding and changing alignment)
        PdfPCell infoCell = new PdfPCell();
        infoCell.Border = Rectangle.NO_BORDER;
        infoCell.HorizontalAlignment = Element.ALIGN_LEFT;  // Changed from ALIGN_CENTER to ALIGN_LEFT
        infoCell.VerticalAlignment = Element.ALIGN_MIDDLE;
        infoCell.PaddingLeft = 10f;  // Reduced from 20f to 10f to move text left

        // Company name with sky blue color - LEFT ALIGNED
        var companyName = new Paragraph("RUBANI SACCO",
            FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14, SkyBlue))
        {
            Alignment = Element.ALIGN_LEFT,  // Changed from ALIGN_CENTER to ALIGN_LEFT
            SpacingAfter = 3f
        };
        infoCell.AddElement(companyName);

        // Address - LEFT ALIGNED
        var address = new Paragraph("Rubani House, Off Airport North Embakasi",
            FontFactory.GetFont(FontFactory.HELVETICA, 10))
        {
            Alignment = Element.ALIGN_LEFT,  // Changed from ALIGN_CENTER to ALIGN_LEFT
            SpacingAfter = 2f
        };
        infoCell.AddElement(address);

        // Email - LEFT ALIGNED
        var email = new Paragraph("rubanisacco@gmail.com",
            FontFactory.GetFont(FontFactory.HELVETICA, 10))
        {
            Alignment = Element.ALIGN_LEFT  // Changed from ALIGN_CENTER to ALIGN_LEFT
        };
        infoCell.AddElement(email);

        headerTable.AddCell(infoCell);

        document.Add(headerTable);

        // Add decorative line below header
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
    catch (Exception ex)
    {
        // Fallback header if logo loading fails
        var fallbackPara = new Paragraph("RUBANI SACCO\nRubani House, Off Airport North Embakasi\nrubanisacco@gmail.com",
            FontFactory.GetFont(FontFactory.HELVETICA, 10))
        {
            Alignment = Element.ALIGN_LEFT,  // Changed from ALIGN_CENTER to ALIGN_LEFT
            SpacingAfter = 15f,
            IndentationLeft = 10f  // Added indentation to move left
        };
        document.Add(fallbackPara);
    }
}

        private PdfPTable CreateMemberInfoSection(object customerInfo, Font boldFont, Font normalFont)
        {
            PdfPTable table = new PdfPTable(4)
            {
                WidthPercentage = 100,
                SpacingAfter = 15f
            };
            table.SetWidths(new float[] { 20, 30, 20, 30 });

            // Extract customer info
            string memberNo = "";
            string memberName = "";
            string idNumber = "";
            string phoneNo = "";
            string employer = "N/A";
            string registrationDate = "N/A";
            string reference1 = "";
            string reference2 = "";
            string reference3 = "";

            if (customerInfo != null)
            {
                var customerType = customerInfo.GetType();

                // Use Reference2 as Member No (preserving leading zeros)
                memberNo = customerType.GetProperty("Reference2")?.GetValue(customerInfo)?.ToString() ?? "";

                // Use AccountName as Member Name
                memberName = customerType.GetProperty("FullName")?.GetValue(customerInfo)?.ToString() ?? "";

                idNumber = customerType.GetProperty("IndividualIdentityCardNumber")?.GetValue(customerInfo)?.ToString() ?? "";

                var mobile = customerType.GetProperty("AddressMobileLine")?.GetValue(customerInfo)?.ToString() ?? "";
                phoneNo = string.IsNullOrEmpty(mobile) ? "N/A" : mobile;

                reference1 = customerType.GetProperty("Reference1")?.GetValue(customerInfo)?.ToString() ?? "";
                reference2 = customerType.GetProperty("Reference2")?.GetValue(customerInfo)?.ToString() ?? "";
                reference3 = customerType.GetProperty("Reference3")?.GetValue(customerInfo)?.ToString() ?? "";

                var regDate = customerType.GetProperty("RegistrationDate")?.GetValue(customerInfo);
                if (regDate != null && DateTime.TryParse(regDate.ToString(), out DateTime dt))
                {
                    registrationDate = dt.ToString("d/M/yyyy");
                }
            }

            // Member Info Rows
            AddMemberInfoRow(table, "Member No:", memberNo, boldFont, normalFont);
            AddMemberInfoRow(table, "Member Name:", memberName, boldFont, normalFont);
            AddMemberInfoRow(table, "ID No:", idNumber, boldFont, normalFont);
            AddMemberInfoRow(table, "Phone No:", phoneNo, boldFont, normalFont);
            //AddMemberInfoRow(table, "Reference 1:", reference1, boldFont, normalFont);
            //AddMemberInfoRow(table, "Reference 2:", reference2, boldFont, normalFont);
            //AddMemberInfoRow(table, "Reference 3:", reference3, boldFont, normalFont);
            AddMemberInfoRow(table, "Employer:", employer, boldFont, normalFont);
            AddMemberInfoRow(table, "Registration Date:", registrationDate, boldFont, normalFont);

            return table;
        }

        private PdfPTable CreateProductTransactionSection(ProductTransactionSummary productSummary,
            List<CustomerStatementDTO> transactions, Font subHeaderFont, Font headerFont,
            Font normalFont, Font boldFont)
        {
            PdfPTable sectionTable = new PdfPTable(1)
            {
                WidthPercentage = 100,
                SpacingAfter = 15f
            };

            // Product header with light gray background
            var productHeader = new PdfPCell(new Phrase(productSummary.ProductName.ToUpper(), subHeaderFont))
            {
                BackgroundColor = LightGray,
                HorizontalAlignment = Element.ALIGN_LEFT,
                Padding = 6,
                Border = Rectangle.NO_BORDER
            };
            sectionTable.AddCell(productHeader);

            // Transactions table
            if (transactions != null && transactions.Count > 0)
            {
                PdfPTable transTable = new PdfPTable(7)
                {
                    WidthPercentage = 100
                };
                transTable.SetWidths(new float[] { 12, 20, 15, 18, 12, 12, 11 });

                // Table headers with sky blue background
                string[] headers = { "Posting date", "Transaction", "Doc No.", "Description", "Debit", "Credit", "Balance" };
                foreach (var h in headers)
                {
                    transTable.AddCell(new PdfPCell(new Phrase(h, headerFont))
                    {
                        BackgroundColor = SkyBlue,
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        Padding = 5,
                        BorderWidth = 0.5f
                    });
                }

                // Add transactions
                decimal productBalance = 0;
                decimal totalDebit = 0;
                decimal totalCredit = 0;

                foreach (var trx in transactions)
                {
                    // Calculate product-specific balance
                    productBalance += trx.Credit - trx.Debit;
                    totalDebit += trx.Debit;
                    totalCredit += trx.Credit;

                    transTable.AddCell(Cell(trx.TransactionDate.ToString("d/M/yyyy"), normalFont));
                    transTable.AddCell(Cell(trx.Product ?? "", normalFont));
                    transTable.AddCell(Cell(trx.Reference ?? "", normalFont));
                    transTable.AddCell(Cell(trx.Description ?? "", normalFont));
                    transTable.AddCell(Cell(trx.Debit == 0 ? "-" : trx.Debit.ToString("N2"), normalFont, Element.ALIGN_RIGHT));
                    transTable.AddCell(Cell(trx.Credit == 0 ? "-" : trx.Credit.ToString("N2"), normalFont, Element.ALIGN_RIGHT));
                    transTable.AddCell(Cell(productBalance.ToString("N2"), normalFont, Element.ALIGN_RIGHT));
                }

                // Totals row with light gray background
                var totalLabelCell = new PdfPCell(new Phrase($"{productSummary.ProductName} Total", boldFont))
                {
                    BackgroundColor = LightGray,
                    HorizontalAlignment = Element.ALIGN_RIGHT,
                    Padding = 5,
                    BorderWidthTop = 1f,
                    BorderWidthBottom = 0.5f,
                    BorderWidthLeft = 0.5f,
                    BorderWidthRight = 0.5f,
                    Colspan = 3
                };
                transTable.AddCell(totalLabelCell);

                // Empty description cell
                transTable.AddCell(new PdfPCell(new Phrase("", boldFont))
                {
                    BackgroundColor = LightGray,
                    BorderWidthTop = 1f,
                    BorderWidthBottom = 0.5f,
                    BorderWidthLeft = 0.5f,
                    BorderWidthRight = 0.5f
                });

                // Debit total
                transTable.AddCell(new PdfPCell(new Phrase(totalDebit.ToString("N2"), boldFont))
                {
                    BackgroundColor = LightGray,
                    HorizontalAlignment = Element.ALIGN_RIGHT,
                    Padding = 5,
                    BorderWidthTop = 1f,
                    BorderWidthBottom = 0.5f,
                    BorderWidthLeft = 0.5f,
                    BorderWidthRight = 0.5f
                });

                // Credit total
                transTable.AddCell(new PdfPCell(new Phrase(totalCredit.ToString("N2"), boldFont))
                {
                    BackgroundColor = LightGray,
                    HorizontalAlignment = Element.ALIGN_RIGHT,
                    Padding = 5,
                    BorderWidthTop = 1f,
                    BorderWidthBottom = 0.5f,
                    BorderWidthLeft = 0.5f,
                    BorderWidthRight = 0.5f
                });

                // Balance total
                transTable.AddCell(new PdfPCell(new Phrase(productBalance.ToString("N2"), boldFont))
                {
                    BackgroundColor = LightGray,
                    HorizontalAlignment = Element.ALIGN_RIGHT,
                    Padding = 5,
                    BorderWidthTop = 1f,
                    BorderWidthBottom = 0.5f,
                    BorderWidthLeft = 0.5f,
                    BorderWidthRight = 0.5f
                });

                var tableCell = new PdfPCell(transTable)
                {
                    Border = Rectangle.NO_BORDER,
                    Padding = 0
                };
                sectionTable.AddCell(tableCell);
            }
            else
            {
                sectionTable.AddCell(new PdfPCell(new Phrase("No transactions", normalFont))
                {
                    Border = Rectangle.NO_BORDER,
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    Padding = 10
                });
            }

            return sectionTable;
        }

        private void AddCustomFooter(Document document)
        {
            document.Add(new Paragraph("\n"));

            // Print date and page info
            var footerPara = new Paragraph(
                $"Printed on: {DateTime.Now:MMMM d, yyyy}   |   Page: 1   |   User ID: SYSTEM",
                FontFactory.GetFont(FontFactory.HELVETICA, 8, DarkGray))
            {
                Alignment = Element.ALIGN_CENTER,
                SpacingBefore = 10f
            };
            document.Add(footerPara);
        }

        private Dictionary<string, List<CustomerStatementDTO>> GroupTransactionsByProduct(List<CustomerStatementDTO> transactions)
        {
            var grouped = new Dictionary<string, List<CustomerStatementDTO>>();

            foreach (var trx in transactions)
            {
                var productName = trx.Product ?? "General Account";
                if (!grouped.ContainsKey(productName))
                {
                    grouped[productName] = new List<CustomerStatementDTO>();
                }
                grouped[productName].Add(trx);
            }

            return grouped;
        }

        // Helper methods for adding rows
        private static void AddMemberInfoRow(PdfPTable table, string label, string value, Font labelFont, Font valueFont)
        {
            table.AddCell(new PdfPCell(new Phrase(label, labelFont))
            {
                Border = Rectangle.NO_BORDER,
                Padding = 3
            });
            table.AddCell(new PdfPCell(new Phrase(value ?? "N/A", valueFont))
            {
                Border = Rectangle.NO_BORDER,
                Padding = 3
            });
        }

        private static PdfPCell Cell(string text, Font font, int align = Element.ALIGN_LEFT)
        {
            return new PdfPCell(new Phrase(text ?? "", font))
            {
                Padding = 5,
                HorizontalAlignment = align,
                BorderWidth = 0.5f
            };
        }
    }

    // Helper class for product summaries
    public class ProductTransactionSummary
    {
        public string ProductName { get; set; }
        public string ProductType { get; set; }
        public List<CustomerStatementDTO> Transactions { get; set; }
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public decimal NetBalance { get; set; }
    }
}