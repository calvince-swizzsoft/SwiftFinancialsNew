using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.Accounts.Controllers
{
    public class CommissionController : Controller
    {
        // GET: Accounts/Commission
        public ActionResult Index()
        {
            return View();
        }

        // GET: Accounts/Commission/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: Accounts/Commission/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Accounts/Commission/Create
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

        // GET: Accounts/Commission/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: Accounts/Commission/Edit/5
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

        // GET: Accounts/Commission/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: Accounts/Commission/Delete/5
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
