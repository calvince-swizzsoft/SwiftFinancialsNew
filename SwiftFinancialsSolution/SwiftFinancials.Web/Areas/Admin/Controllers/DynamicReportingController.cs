using SwiftFinancials.Web.Areas.Admin.Data;
using SwiftFinancials.Web.Areas.Admin.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.Admin.Controllers
{
    public class DynamicReportingController : Controller
    {
        private readonly ReportRepository _repository;

        public DynamicReportingController()
        {
            _repository = new ReportRepository();
        }

        
        public ActionResult Index()
        {
            var reports = _repository.GetAllReports();
            return View(reports);
        }

        [HttpGet]
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Create(DynamicReport report)
        {
            if (ModelState.IsValid)
            {
                string storedProcedure = _repository.GetStoredProcedureName(report.Name);

                if (string.IsNullOrEmpty(storedProcedure))
                {
                    ModelState.AddModelError("Name", "The report name does not match any stored procedure.");
                    return View(report);
                }

                report.StoredProcedureName = storedProcedure;

                
                if (_repository.ReportExists(report.Name, report.StoredProcedureName))
                {
                    TempData["ErrorMessage"] = "A report with this name and stored procedure already exists.";
                    return View(report); 
                }

                _repository.AddReport(report);
                TempData["SuccessMessage"] = "Report created successfully!";
                return RedirectToAction("Index");
            }

            return View(report);
        }

        [HttpGet]
        public ActionResult Edit(int id)
        {
            var report = _repository.GetReportById(id);
            if (report == null)
            {
                return HttpNotFound();
            }
            return View(report);
        }

        [HttpPost]
        public ActionResult Edit(DynamicReport report)
        {
            if (ModelState.IsValid)
            {
                string storedProcedure = _repository.GetStoredProcedureName(report.Name);

                if (!string.IsNullOrEmpty(storedProcedure))
                {
                    report.StoredProcedureName = storedProcedure;
                }

                _repository.UpdateReport(report);
                return RedirectToAction("Index");
            }

            return View(report);
        }

        [HttpPost]
        public ActionResult Delete(int id)
        {
            _repository.DeleteReport(id);
            return RedirectToAction("Index");
        }
    }
}