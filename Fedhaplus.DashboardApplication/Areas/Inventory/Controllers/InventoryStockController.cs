using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Fedhaplus.DashboardApplication.Areas.Inventory.Controllers
{
    public class InventoryStockController : Controller
    {
        // GET: Inventory/InventoryStock
        public ActionResult Index()
        {
            return View();
        }

        // GET: Inventory/InventoryStock/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: Inventory/InventoryStock/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Inventory/InventoryStock/Create
        [HttpPost]
        public ActionResult Create(FormCollection collection)
        {
            try
            {
                // TODO: Add insert logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: Inventory/InventoryStock/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: Inventory/InventoryStock/Edit/5
        [HttpPost]
        public ActionResult Edit(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add update logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: Inventory/InventoryStock/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: Inventory/InventoryStock/Delete/5
        [HttpPost]
        public ActionResult Delete(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }
    }
}
