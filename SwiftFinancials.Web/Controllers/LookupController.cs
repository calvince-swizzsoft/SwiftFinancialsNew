using System.Threading.Tasks;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Controllers
{
    public class LookupController : MasterController
    {
        // GET: Lookup
        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<JsonResult> GetCompaniesAsync()
        {
            var companiesDTOs = await _channelService.FindCompaniesAsync(GetServiceHeader());

            return Json(companiesDTOs, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public async Task<JsonResult> GetStationsAsync()
        {
            var stationsDTOs = await _channelService.FindStationsAsync(GetServiceHeader());

            return Json(stationsDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}