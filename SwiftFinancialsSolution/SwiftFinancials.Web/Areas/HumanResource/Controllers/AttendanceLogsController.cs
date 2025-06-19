using SwiftFinancials.Web.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Threading.Tasks;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.HumanResourcesModule;
using SwiftFinancials.Web.Helpers;
using System.Windows.Forms;

namespace SwiftFinancials.Web.Areas.HumanResource.Controllers
{
    public class AttendanceLogsController : MasterController
    {
        private static List<Attendancelog> Attendancelogs = new List<Attendancelog>();

        public ActionResult Index()
        {
            return View(Attendancelogs);
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Create(Attendancelog attendancelog)
        {
            attendancelog.Id = Guid.NewGuid();
            Attendancelogs.Add(attendancelog);
            return RedirectToAction("Index");
        }
        public ActionResult Edit(Guid id)
        {
            var Assets = Attendancelogs.FirstOrDefault(s => s.Id == id);
            return View(Assets);
        }

        [HttpPost]
        public ActionResult Edit(Attendancelog attendancelog)
        {
            var existing = Attendancelogs.FirstOrDefault(s => s.Id == attendancelog.Id);
            if (existing != null)
            {
                Attendancelogs.Remove(existing);
                Attendancelogs.Add(attendancelog);
            }
            return RedirectToAction("Index");
        }

    }
}