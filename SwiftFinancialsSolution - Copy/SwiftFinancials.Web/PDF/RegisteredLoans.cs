using System;
using System.Collections.Generic;
using System.IO;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.Web;
using Application.MainBoundedContext.DTO.BackOfficeModule;

namespace SwiftFinancials.Web.PDF
{
    public class RegisteredLoans
    {
        public bool WritePdf(string fpath, string address, List<LoanCaseDTO> trans)
        {
            string imageFilePath = HttpContext.Current.Server.MapPath("~/Images/MIA.JPG");
            string datetime = DateTime.Now.ToString("yyyyMMddHHmmss");
            var filename = "Registered_Loans_" + datetime;
            string fullFilePath = Path.Combine(address, filename + ".pdf");

            try
            {
                // Ensure directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(fullFilePath));

                // Font Definition
                var headerfont = FontFactory.GetFont(FontFactory.TIMES_ROMAN, 8);
                var normalFont = FontFactory.GetFont(FontFactory.TIMES_ROMAN, 5);
                var boldFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 6);
                var underLine = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 6, Font.UNDERLINE);

                // Initialize Document and FileStream
                using (var fs = new FileStream(fullFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var doc = new Document())
                {
                    // Adjust margins to provide space for header and footer
                    doc.SetMargins(doc.LeftMargin, doc.RightMargin, doc.TopMargin + 60, doc.BottomMargin + 40);

                    // Initialize PdfWriter
                    using (var writer = PdfWriter.GetInstance(doc, fs))
                    {
                        writer.PageEvent = new PdfHeaderFooter(imageFilePath); // Assuming PdfHeaderFooter is a custom class
                        writer.AddViewerPreference(PdfName.HIDEMENUBAR, new PdfBoolean(true));
                        writer.ViewerPreferences = PdfWriter.HideMenubar;
                        doc.Open();

                        // Add content to the document
                        string date = DateTime.Now.ToString();

                        var pr1 = new Paragraph("Registered Loans", headerfont);
                        var pr2 = new Paragraph("Date : " + date, headerfont);
                        var pr4 = new Paragraph("", headerfont);

                        var glnodesTable = new PdfPTable(5) { WidthPercentage = 100 };
                        var widths = new float[] { 5f, 10f, 5f, 10f, 10f };
                        glnodesTable.SetWidths(widths);

                        glnodesTable.AddCell(new PdfPCell(new Phrase("Case Number", boldFont)) { BackgroundColor = BaseColor.LIGHT_GRAY });
                        glnodesTable.AddCell(new PdfPCell(new Phrase("Customer", boldFont)) { BackgroundColor = BaseColor.LIGHT_GRAY });
                        glnodesTable.AddCell(new PdfPCell(new Phrase("Amount Applied", boldFont)) { BackgroundColor = BaseColor.LIGHT_GRAY });
                        glnodesTable.AddCell(new PdfPCell(new Phrase("Created By", boldFont)) { BackgroundColor = BaseColor.LIGHT_GRAY });
                        glnodesTable.AddCell(new PdfPCell(new Phrase("Created Date", boldFont)) { BackgroundColor = BaseColor.LIGHT_GRAY });

                        foreach (var n in trans)
                        {
                            glnodesTable.AddCell(new PdfPCell(new Phrase(n.CaseNumber.ToString(), normalFont)));
                            glnodesTable.AddCell(new PdfPCell(new Phrase(n.CustomerFullName.ToString(), normalFont)));
                            glnodesTable.AddCell(new PdfPCell(new Phrase(n.AmountApplied.ToString(), normalFont)));
                            glnodesTable.AddCell(new PdfPCell(new Phrase(n.CreatedBy.ToString(), normalFont)));
                            glnodesTable.AddCell(new PdfPCell(new Phrase(n.CreatedDate.ToString(), normalFont)));
                        }

                        doc.Add(pr1);
                        doc.Add(pr2);
                        doc.Add(pr4);

                        if (trans.Count > 0)
                        {
                            doc.Add(glnodesTable);
                        }

                        var outroTable = new PdfPTable(1) { WidthPercentage = 100 };
                        var cell = new PdfPCell(new Paragraph("Thank You", normalFont))
                        {
                            HorizontalAlignment = Element.ALIGN_LEFT,
                            Border = PdfPCell.NO_BORDER
                        };                        
                        outroTable.AddCell(cell);
                        doc.Add(outroTable);

                        doc.Close();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating PDF: {ex.Message}");
                return false; // Indicate failure
            }
        }
    }
}
