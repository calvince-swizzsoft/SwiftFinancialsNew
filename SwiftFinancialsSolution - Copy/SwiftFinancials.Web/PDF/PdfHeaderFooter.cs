using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace SwiftFinancials.Web.PDF
{
    public class PdfHeaderFooter : PdfPageEventHelper
    {
        public iTextSharp.text.Image _headerImage { get; set; }
        public iTextSharp.text.Image ImageFooter { get; set; }

        private readonly float _imageHeight;

        public PdfHeaderFooter(string headerImagePath)
        {
            _headerImage = Image.GetInstance(headerImagePath);
            _headerImage.ScaleToFit(100f, 100f);
            _imageHeight = _headerImage.ScaledHeight;
        }

        public override void OnStartPage(PdfWriter writer, Document document)
        {
            if (writer.PageNumber == 1)
            {
                PdfContentByte canvas = writer.DirectContent;

                // Set the position of the header image
                float headerX = document.LeftMargin;
                float headerY = document.PageSize.Height - _headerImage.ScaledHeight - document.TopMargin;

                _headerImage.SetAbsolutePosition(headerX, headerY);
                canvas.AddImage(_headerImage);

                document.SetMargins(document.LeftMargin, document.RightMargin, document.TopMargin + _headerImage.ScaledHeight, document.BottomMargin);
            }
        }
    }
}