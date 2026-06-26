using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;

namespace SwiftFinancials.Web
{
    static class GlobalVariable
    {
        //public static string ReportPath = ConfigurationManager.AppSettings["ReportPath"].ToString();
        public static string ReportName = null;
    }
}
