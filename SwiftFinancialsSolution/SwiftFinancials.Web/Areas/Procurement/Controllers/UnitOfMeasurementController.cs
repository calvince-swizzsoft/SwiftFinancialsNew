using Application.MainBoundedContext.DTO;
using SwiftFinancials.Web.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.Procurement.Controllers
{
    public class UnitOfMeasurementController : Controller
    {
        private static List<UnitOfMeasurementDTO> unitOfMeasurements = new List<UnitOfMeasurementDTO>();

        public ActionResult Index()
        {
            return View(unitOfMeasurements);
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Create(UnitOfMeasurementDTO unitOfMeasurement)
        {
            unitOfMeasurement.Id = Guid.NewGuid();
            unitOfMeasurements.Add(unitOfMeasurement);
            return RedirectToAction("Index");
        }
    }
}