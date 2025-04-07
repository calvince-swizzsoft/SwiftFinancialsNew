using Application.MainBoundedContext.DTO;
using SwiftFinancials.Web.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.Procurement.Controllers
{
    public class SupplierController : MasterController
    {
        private static List<SupplierDTO> suppliers = new List<SupplierDTO>();

        public ActionResult Index()
        {
            return View(suppliers);
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Create(SupplierDTO supplier)
        {
            supplier.Id = Guid.NewGuid();
            suppliers.Add(supplier);
            return RedirectToAction("Index");
        }

        public ActionResult Edit(Guid id)
        {
            var supplier = suppliers.FirstOrDefault(s => s.Id == id);
            return View(supplier);
        }

        [HttpPost]
        public ActionResult Edit(SupplierDTO supplier)
        {
            var existing = suppliers.FirstOrDefault(s => s.Id == supplier.Id);
            if (existing != null)
            {
                suppliers.Remove(existing);
                suppliers.Add(supplier);
            }
            return RedirectToAction("Index");
        }
    }
}