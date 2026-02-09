using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TestApis.Models
{
public class DocumentModel
    {
        public int DocumentID { get; set; }
        public string FileName { get; set; }
        public string FileType { get; set; }
        public string FileBase64 { get; set; }
        public string Uploadedby { get; set; }
        public Guid UploadedByID { get; set; }
        public string UploadedByRole { get; set; }
        public Guid? UploadedForID { get; set; }
        public string VisibilityLevel { get; set; }
        public string Status { get; set; }
        public string Remarks { get; set; }
        public DateTime UploadedDate { get; set; }
    }
}


