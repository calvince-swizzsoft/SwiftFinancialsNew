using SwiftFinancials.Web.Areas.Accounts.Models.GLAccountsDetermination;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.Accounts.Controllers
{
    public class GLAccountDeterminationController : Controller
    {
        // GET: Accounts/GLAccountDetermination
        public ActionResult GetDetails()
        {
            var data = new  GLAccountsDet();
            
            return View(data);
        }

        
        //public  GLADRply(GLAccountsDet glad)
        //{
        //    string systemGLAccountCode2 = glad.systemGLAccountCode, mappedGLAccountName2 = glad.mappedGLAccountName,
        //        mappedGLAccountcostCenter2 = glad.mappedGLAccountcostCenter, createdDate2 = glad.createdDate;

           
        //}
    }
}