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
        private static List<AssetTypeDTO> assetTypes = new List<AssetTypeDTO>();

        public ActionResult Index()
        {
            return View(assetTypes);
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Create(AssetTypeDTO assetType)
        {
            assetType.Id = Guid.NewGuid();
            assetTypes.Add(assetType);
            return RedirectToAction("Index");
        }
        public ActionResult Edit(Guid id)
        {
            var Assets = assetTypes.FirstOrDefault(s => s.Id == id);
            return View(Assets);
        }

        [HttpPost]
        public ActionResult Edit(AssetTypeDTO assetType)
        {
            var existing = assetTypes.FirstOrDefault(s => s.Id == assetType.Id);
            if (existing != null)
            {
                assetTypes.Remove(existing);
                assetTypes.Add(assetType);
            }
            return RedirectToAction("Index");
        }

    }
}