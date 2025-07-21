using Application.MainBoundedContext.DTO;
using System.Collections.Generic;
using System.Web.Mvc;

namespace TestApis.Helpers
{
    public static class ControllerExtensions
    {
        public static JsonResult DataTablesJson<T>(this Controller controller, IEnumerable<T> items, int totalRecords, int totalDisplayRecords, int sEcho)
        {
            JsonResult result = new JsonResult
            {
                Data = new JQueryDataTablesResponse<T>(items ?? new List<T> { }, totalRecords, totalDisplayRecords, sEcho),
                MaxJsonLength = int.MaxValue
            };

            return result;
        }

        public static int GetPageIndex(this JQueryDataTablesModel jQueryDataTablesModel)
        {
            return jQueryDataTablesModel.iDisplayStart == 0 ? jQueryDataTablesModel.iDisplayStart : jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;
        }
    }
}