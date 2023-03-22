using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using System;
using System.IO;

namespace SwiftFinancials.Web.Reports
{
    public partial class Report : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            //Create object for crystal report Export option class    
            ExportOptions exopt = default(ExportOptions);

            //create object for destination option - for set path of your pdf file save     
            DiskFileDestinationOptions dfdopt = new DiskFileDestinationOptions();
            
            ReportDocument RptDoc = new ReportDocument();

            //Map your crystal report path    
            RptDoc.Load("C:\\Program Files (x86)\\EasyBim Insurance\\dashboard_client\\Reports\\AssetRegister.rpt");

            //Report pfd name    
            string fname = "AssetRegister.pdf";
            dfdopt.DiskFileName = Server.MapPath("~/Reports/" + fname);

            exopt = RptDoc.ExportOptions;
            exopt.ExportDestinationType = ExportDestinationType.DiskFile;

            //for PDF select PortableDocFormat for excel select ExportFormatType.Excel    
            exopt.ExportFormatType = ExportFormatType.PortableDocFormat;
            exopt.DestinationOptions = dfdopt;

            //finally export your report document    
            RptDoc.Export();

            //To open your PDF after save it from crystal report    

            string Path = Server.MapPath(fname);

            FileInfo file = new FileInfo(Path);

            Response.ClearContent();

            Response.AddHeader("Content-Disposition", "attachment; filename=" + file.Name);
            Response.AddHeader("Content-Length", file.Length.ToString());
            Response.ContentType = "application/pdf";
            Response.TransmitFile(file.FullName);
            Response.Flush();

            RptDoc.Dispose();
            RptDoc.Close();
            RptDoc = null;
            ////End    

        }


        protected void Export(object sender, EventArgs e)
        {

        }
    }
}