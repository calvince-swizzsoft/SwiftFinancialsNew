using System;
using System.Collections.Generic;
using System.Linq;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.Web;
using System.IO;
using Application.MainBoundedContext.DTO.BackOfficeModule;

namespace SwiftFinancials.Web.PDF
{
    public class CreatePdf
    {
        public bool WritePdf(string fpath, string address, List<LoanCaseDTO> trans)
        {
            string imageFilePath = HttpContext.Current.Server.MapPath(".") + "/Images/MIA.JPG";
            string image3 = HttpContext.Current.Server.MapPath(".") + "/Images/MIA.JPG";

            string datetime = DateTime.Now.ToString("yyyyMMddHHmmss");

            var filename = "CustomerReport" + datetime;

            string f_name = filename + ".pdf";

            var appRootDir = address + f_name;
            try
            {
                //if (!Directory.Exists(directory))
                //{
                //    Directory.CreateDirectory(directory);
                //}
                if (System.IO.File.Exists(appRootDir))
                    System.IO.File.Delete(appRootDir);

                //Font Definition 
                var headerfont = FontFactory.GetFont(FontFactory.TIMES_ROMAN, 8);
                var normalFont = FontFactory.GetFont(FontFactory.TIMES_ROMAN, 5);
                var boldFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 6);
                var underLine = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 6, Font.UNDERLINE);

                //Fetching Needed Data
                using (var fs = new FileStream(appRootDir, FileMode.Create, FileAccess.Write, FileShare.None))
                // Creating iTextSharp.text.Document object
                using (var doc = new Document())
                {
                    doc.SetMargins(doc.LeftMargin, doc.RightMargin, doc.TopMargin + 20, doc.BottomMargin + 40);
                    // Creating iTextSharp.text.pdf.PdfWriter object
                    // It helps to write the Document to the Specified FileStream
                    using (var writer = PdfWriter.GetInstance(doc, fs))
                    {
                        //Set Header & Footer Using Page Events with the above class
                        writer.PageEvent = new PdfHeaderFooter(image3);
                        writer.AddViewerPreference(PdfName.HIDEMENUBAR, new PdfBoolean(true));
                        writer.ViewerPreferences = PdfWriter.HideMenubar;
                        // Openning the Document
                        doc.Open();
                        // Adding a paragraph
                        // NOTE: When we want to insert text, then we've to do it through creating paragraph
                        //doc.NewPage();
                        DateTime datet = DateTime.Now;

                        string date = datet.ToString();
                        var prSpace = new Paragraph("\n");
                        //var prSpace2 = new Paragraph("SMS REOSRTS");
                        //var prSpace3 = new Paragraph("\n");
                        var pr1 = new Paragraph("Customer Statement", headerfont);
                        var pr2 = new Paragraph("Date : " + date, headerfont);
                        var pr3 = new Paragraph("", headerfont);
                        var pr33 = new Paragraph("", headerfont);
                        var pr4 = new Paragraph("", headerfont);
                        var glnodesTable = new PdfPTable(2) { WidthPercentage = 100 };
                        var widths = new float[] { 10f, 20f};
                        glnodesTable.SetWidths(widths);

                        var cell1 = new PdfPCell(new Phrase("E-mail", boldFont))
                        {
                            BackgroundColor = BaseColor.LIGHT_GRAY,
                            Colspan = 1
                        };
                        glnodesTable.AddCell(cell1);

                        var cell2 = new PdfPCell(new Phrase("Branch", boldFont))
                        {
                            BackgroundColor = BaseColor.LIGHT_GRAY,
                            Colspan = 1
                        };
                        glnodesTable.AddCell(cell2);
                        foreach (var n in trans)
                        {
                            //decimal paidin = Math.Round(Convert.ToDecimal(n.PaidIn), 2);

                            //decimal withdrawn = Math.Round(Convert.ToDecimal(n.Widthrawn), 2);

                            //decimal balanc = Math.Round(Convert.ToDecimal(n.Balance), 2);

                            var cell10 = new PdfPCell(new Phrase(n.BranchAddressEmail.ToString(), normalFont))
                            {
                                Colspan = 1
                            };
                            glnodesTable.AddCell(cell10);

                            var cell11 = new PdfPCell(new Phrase(n.BranchDescription.ToString(), normalFont))
                            {
                                Colspan = 1
                            };
                            glnodesTable.AddCell(cell11);
                        }

                        //Letter Head 

                        doc.Add(pr1);
                        doc.Add(pr2);
                        doc.Add(pr3);
                        doc.Add(pr33);
                        doc.Add(pr1);
                        doc.Add(pr2);
                        doc.Add(pr3);
                        doc.Add(pr33);
                        doc.Add(pr1);
                        doc.Add(pr2);
                        doc.Add(pr3);
                        doc.Add(pr33);
                        doc.Add(pr1);
                        doc.Add(pr2);
                        doc.Add(pr3);
                        doc.Add(pr33);
                        doc.Add(pr3);
                        doc.Add(pr33);
                        doc.Add(pr3);
                        doc.Add(pr33);
                        //doc.Add(pr3);
                        if (trans.Count > 0)
                        {
                            doc.Add(pr4);
                            doc.Add(prSpace);
                            doc.Add(glnodesTable);
                            doc.Add(prSpace);
                        }

                        var outroTable = new PdfPTable(1) { WidthPercentage = 100 };
                        PdfPCell cell;
                        cell = new PdfPCell(new Paragraph("Thank You", normalFont));
                        cell.HorizontalAlignment = Element.ALIGN_LEFT;
                        cell.Border = PdfPCell.NO_BORDER;
                        outroTable.AddCell(cell);
                        doc.Close();
                    }
                }


            }
            // Catching iTextSharp.text.DocumentException if any
            catch (DocumentException de)
            {
            }
            // Catching System.IO.IOException if any
            catch (IOException ioe)
            {
            }
            catch (Exception ex)
            {
                string exe = "";
            }
            return false;
        }
    }
}