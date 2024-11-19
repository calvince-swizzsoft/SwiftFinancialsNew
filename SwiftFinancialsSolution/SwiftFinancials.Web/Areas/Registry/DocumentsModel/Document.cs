using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SwiftFinancials.Web.Areas.Admin.DocumentsModel
{
    public class Document
    {
        public Guid Id { get; set; }  // Unique identifier for the document
        public Guid CompanyId { get; set; }  // Reference to the associated customer
        public byte[] PassportPhoto { get; set; }
        public byte[] SignaturePhoto { get; set; }
        public byte[] IDCardFrontPhoto { get; set; }
        public byte[] IDCardBackPhoto { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public string Type { get; internal set; }
        public object DataUrl { get; internal set; }
    }
}