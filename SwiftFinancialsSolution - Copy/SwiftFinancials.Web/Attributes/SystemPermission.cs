using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Web;
using System.Web.Http;

namespace SwiftFinancials.Web.Attributes
{
    public class SystemPermission: AuthorizeAttribute
    {
        // Get the current Windows identity
        WindowsIdentity identity = WindowsIdentity.GetCurrent();


    }
}