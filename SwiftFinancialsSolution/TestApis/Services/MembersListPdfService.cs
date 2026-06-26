using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static TestApis.Controllers.ReportingController;

namespace TestApis.Services
{
    public class MembersListPdfService
    {
        // Color palette for Rubani Sacco
        private static readonly BaseColor SkyBlue = new BaseColor(0, 174, 239); // #00AEEF
        private static readonly BaseColor Red = new BaseColor(255, 0, 0);       // #FF0000
        private static readonly BaseColor DarkGray = new BaseColor(26, 26, 26); // #1A1A1A
        private static readonly BaseColor LightGray = new BaseColor(217, 217, 217); // #D9D9D9
        private static readonly BaseColor White = BaseColor.WHITE;

        public byte[] GenerateMembersListPdf(List<MemberSummaryDTO> members)
        {
            using (var ms = new MemoryStream())
            {
                var document = new Document(PageSize.A4.Rotate(), 30, 30, 50, 30); // Landscape orientation
                PdfWriter.GetInstance(document, ms);
                document.Open();

                // Fonts
                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16, SkyBlue);
                var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, White);
                var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 9);
                var boldFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9);
                var smallFont = FontFactory.GetFont(FontFactory.HELVETICA, 8, DarkGray);

                // ===== CUSTOM HEADER WITH LOGO =====
                AddCustomHeaderWithLogo(document, "MEMBERS LIST REPORT");

                // ===== REPORT SUMMARY =====
                AddReportSummary(document, members.Count, boldFont, normalFont);

                document.Add(new Paragraph("\n"));

                // ===== MEMBERS TABLE =====
                var membersTable = CreateMembersTable(members, headerFont, normalFont);
                document.Add(membersTable);

                document.Add(new Paragraph("\n"));

                // ===== FOOTER NOTES =====
                document.Add(new Paragraph("This is a system generated report.", smallFont)
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

        private void AddCustomHeaderWithLogo(Document document, string reportTitle)
        {
            try
            {
                // Table with 2 columns: Logo | Info
                PdfPTable headerTable = new PdfPTable(2)
                {
                    WidthPercentage = 100,
                    SpacingAfter = 5f
                };
                headerTable.SetWidths(new float[] { 20, 80 });

                // Logo cell (left)
                PdfPCell logoCell = new PdfPCell();
                logoCell.Border = Rectangle.NO_BORDER;
                logoCell.HorizontalAlignment = Element.ALIGN_LEFT;
                logoCell.VerticalAlignment = Element.ALIGN_MIDDLE;

                string logoPath = @"C:\Users\Karenju\Desktop\testapidebug\Assets\Images\logo.png";
                if (File.Exists(logoPath))
                {
                    Image logo = Image.GetInstance(logoPath);
                    logo.ScaleToFit(60, 60);
                    logoCell.AddElement(logo);
                }
                else
                {
                    logoCell.AddElement(new Paragraph("RUBANI SACCO",
                        FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, SkyBlue)));
                }

                headerTable.AddCell(logoCell);

                // Info cell (centered)
                PdfPCell infoCell = new PdfPCell();
                infoCell.Border = Rectangle.NO_BORDER;
                infoCell.HorizontalAlignment = Element.ALIGN_CENTER;
                infoCell.VerticalAlignment = Element.ALIGN_MIDDLE;

                infoCell.AddElement(new Paragraph("RUBANI SACCO",
                    FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14, SkyBlue))
                { Alignment = Element.ALIGN_CENTER, SpacingAfter = 2f });

                infoCell.AddElement(new Paragraph("Rubani House, Off Airport North Embakasi",
                    FontFactory.GetFont(FontFactory.HELVETICA, 10))
                { Alignment = Element.ALIGN_CENTER, SpacingAfter = 1f });

                infoCell.AddElement(new Paragraph("rubanisacco@gmail.com",
                    FontFactory.GetFont(FontFactory.HELVETICA, 10))
                { Alignment = Element.ALIGN_CENTER, SpacingAfter = 3f });

                infoCell.AddElement(new Paragraph(reportTitle,
                    FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, DarkGray))
                { Alignment = Element.ALIGN_CENTER });

                headerTable.AddCell(infoCell);
                document.Add(headerTable);

                // Decorative line
                PdfPTable lineTable = new PdfPTable(3)
                {
                    WidthPercentage = 100,
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    SpacingAfter = 10f
                };
                lineTable.SetWidths(new float[] { 33, 34, 33 });
                lineTable.AddCell(new PdfPCell() { BackgroundColor = SkyBlue, FixedHeight = 2f, Border = Rectangle.NO_BORDER });
                lineTable.AddCell(new PdfPCell() { BackgroundColor = Red, FixedHeight = 2f, Border = Rectangle.NO_BORDER });
                lineTable.AddCell(new PdfPCell() { BackgroundColor = SkyBlue, FixedHeight = 2f, Border = Rectangle.NO_BORDER });
                document.Add(lineTable);
            }
            catch
            {
                var fallbackPara = new Paragraph("RUBANI SACCO\nRubani House, Off Airport North Embakasi\nrubanisacco@gmail.com\n" + reportTitle,
                    FontFactory.GetFont(FontFactory.HELVETICA, 10))
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 10f
                };
                document.Add(fallbackPara);
            }
        }

        private void AddReportSummary(Document document, int currentCount, Font boldFont, Font normalFont)
        {
            PdfPTable summaryTable = new PdfPTable(4)
            {
                WidthPercentage = 60,
                HorizontalAlignment = Element.ALIGN_CENTER,
                SpacingAfter = 15f
            };
            summaryTable.SetWidths(new float[] { 25, 25, 25, 25 });

            AddSummaryCell(summaryTable, "Report Date:", DateTime.Now.ToString("dd/MM/yyyy HH:mm"), boldFont, normalFont);
            AddSummaryCell(summaryTable, "Total Members:", currentCount.ToString("N0"), boldFont, normalFont);
            AddSummaryCell(summaryTable, "Generated By:", "System", boldFont, normalFont);
            AddSummaryCell(summaryTable, "Status:", "Active Members", boldFont, normalFont);
        }

        private void AddSummaryCell(PdfPTable table, string title, string value, Font titleFont, Font valueFont)
        {
            PdfPCell cell = new PdfPCell();
            cell.Border = Rectangle.NO_BORDER;
            cell.HorizontalAlignment = Element.ALIGN_CENTER;
            cell.VerticalAlignment = Element.ALIGN_MIDDLE;
            cell.Padding = 2f;

            var p = new Paragraph();
            p.Add(new Chunk(title + " ", titleFont));
            p.Add(new Chunk(value, valueFont));
            p.Alignment = Element.ALIGN_CENTER;

            cell.AddElement(p);
            table.AddCell(cell);
        }

        private PdfPTable CreateMembersTable(List<MemberSummaryDTO> members, Font headerFont, Font normalFont)
        {
            // 7 columns: No., Member Name, Member No., ID Number, Mobile, Nationality, Payroll No., Email, Reg. Date
            PdfPTable table = new PdfPTable(9)
            {
                WidthPercentage = 100,
                SpacingAfter = 10f
            };

            // Set column widths (adjust based on content)
            table.SetWidths(new float[] { 5, 15, 10, 10, 10, 8, 10, 15, 10 });

            // Table headers with sky blue background
            string[] headers = {
                "No.",
                "Member Name",
                "Member No.",
                "ID Number",
                "Mobile",
                "Nationality",
                "Payroll No.",
                "Email",
                "Reg. Date"
            };

            foreach (var h in headers)
            {
                table.AddCell(new PdfPCell(new Phrase(h, headerFont))
                {
                    BackgroundColor = SkyBlue,
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    Padding = 5,
                    BorderWidth = 0.5f
                });
            }

            // Add member rows with alternating colors
            bool alternateRow = false;
            int rowNumber = 1;

            // Sort members by MemberNo (Reference2)
            var sortedMembers = members.OrderBy(m => m.MembershipNumber).ToList();

            foreach (var member in sortedMembers)
            {
                var rowColor = alternateRow ? new BaseColor(245, 245, 245) : White;

                // Row Number
                table.AddCell(CellStyled(rowNumber.ToString(), normalFont, rowColor, Element.ALIGN_CENTER));

                // Member Name
                table.AddCell(CellStyled(member.FullName ?? "N/A", normalFont, rowColor));

                // Member No (Reference2)
                table.AddCell(CellStyled(member.MembershipNumber ?? "N/A", normalFont, rowColor, Element.ALIGN_CENTER));

                // ID Number
                table.AddCell(CellStyled(member.IdNumber ?? "N/A", normalFont, rowColor, Element.ALIGN_CENTER));

                // Mobile
                table.AddCell(CellStyled(member.Mobile ?? "N/A", normalFont, rowColor, Element.ALIGN_CENTER));

                // Nationality
                table.AddCell(CellStyled(member.Nationality ?? "N/A", normalFont, rowColor));

                // Payroll Number
                table.AddCell(CellStyled(member.PayrollNumber ?? "N/A", normalFont, rowColor, Element.ALIGN_CENTER));

                // Email
                table.AddCell(CellStyled(member.Email ?? "N/A", normalFont, rowColor));

                // Registration Date
                string regDate = member.RegistrationDate.HasValue ?
                    member.RegistrationDate.Value.ToString("dd/MM/yyyy") : "N/A";
                table.AddCell(CellStyled(regDate, normalFont, rowColor, Element.ALIGN_CENTER));

                rowNumber++;
                alternateRow = !alternateRow;
            }

            return table;
        }

        private static PdfPCell CellStyled(string text, Font font, BaseColor backgroundColor, int align = Element.ALIGN_LEFT)
        {
            return new PdfPCell(new Phrase(text ?? "", font))
            {
                Padding = 5,
                HorizontalAlignment = align,
                BackgroundColor = backgroundColor,
                BorderWidth = 0.5f
            };
        }
    }
}