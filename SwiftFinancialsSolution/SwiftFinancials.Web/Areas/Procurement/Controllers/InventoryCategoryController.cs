using Application.MainBoundedContext.DTO;
using SwiftFinancials.Web.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.Procurement.Controllers
{
    public class InventoryCategoryController : MasterController
    {
        private static List<InventoryCategoryDTO> inventoryCategories = new List<InventoryCategoryDTO>();

        public ActionResult Index()
        {
            return View(inventoryCategories);
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Create(InventoryCategoryDTO inventoryCategory)
        {
            inventoryCategory.Id = Guid.NewGuid();
            inventoryCategories.Add(inventoryCategory);
            return RedirectToAction("Index");
        }
    }

}