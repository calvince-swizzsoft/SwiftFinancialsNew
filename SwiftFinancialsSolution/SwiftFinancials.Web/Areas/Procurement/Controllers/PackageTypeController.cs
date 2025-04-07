using Application.MainBoundedContext.DTO;
using SwiftFinancials.Web.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.Procurement.Controllers
{
    public class PackageTypeController : MasterController
    {
        private static List<PackageTypeDTO> packageTypes = new List<PackageTypeDTO>();

        public ActionResult Index()
        {
            return View(packageTypes);
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Create(PackageTypeDTO packageType)
        {
            packageType.Id = Guid.NewGuid();
            packageTypes.Add(packageType);
            return RedirectToAction("Index");
        }
    }

}