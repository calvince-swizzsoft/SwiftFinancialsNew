using SwiftFinancials.Web.Areas.Reports.Models;
using SwiftFinancials.Web.Controllers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.Reports.Controllers
{
    public class spDynamicReportingController : MasterController
    {
        private string connectionString = "Data Source=(local);Initial Catalog=SwiftFinancialsDB_Live;Persist Security Info=true; User ID=sa;Password=pass123; Pooling=True";


        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();

            var storedProcedures = GetStoredProcedures();
            ViewBag.StoredProcedures = new SelectList((System.Collections.IEnumerable)storedProcedures);

            var storedProceduresCount = await GetStoredProceduresCount(); 
            ViewBag.StoredProcedureCount = storedProceduresCount;  

            return View();
        }


        private async Task<int> GetStoredProceduresCount()
        {
            int count = 0;
            string query = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.ROUTINES WHERE ROUTINE_TYPE = 'PROCEDURE'";

            using (var connection = new SqlConnection(connectionString))
            {
                using (var command = new SqlCommand(query, connection))
                {
                    await connection.OpenAsync();
                    count = (int)await command.ExecuteScalarAsync(); 
                }
            }

            return count;
        }



        [HttpPost]
        public async Task<ActionResult> FetchParameters(string storedProcedureName)
        {
            await ServeNavigationMenus();

            var parameters = new List<StoredProcedureParameter>();

            using (var connection = new SqlConnection(connectionString))
            {
                using (var command = new SqlCommand(storedProcedureName, connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    connection.Open();
                    SqlCommandBuilder.DeriveParameters(command);
                    connection.Close();

                    foreach (SqlParameter param in command.Parameters)
                    {
                        if (param.Direction == ParameterDirection.Input || param.Direction == ParameterDirection.InputOutput)
                        {
                            parameters.Add(new StoredProcedureParameter
                            {
                                Name = param.ParameterName,
                                DataType = param.SqlDbType.ToString()
                            });
                        }
                    }
                }
            }

            return PartialView("_StoredProcedureParameters", parameters);
        }


        private async Task<List<string>> GetStoredProcedures()
        {
            await ServeNavigationMenus();

            var storedProcedures = new List<string>();

            using (var connection = new SqlConnection(connectionString))
            {
                var command = new SqlCommand("SELECT SPECIFIC_NAME FROM INFORMATION_SCHEMA.ROUTINES WHERE ROUTINE_TYPE = 'PROCEDURE'", connection);
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        storedProcedures.Add(reader["SPECIFIC_NAME"].ToString());
                    }
                }
            }

            return storedProcedures;
        }

        private async Task<List<StoredProcedureParameter>> GetStoredProcedureParameters(string storedProcedureName)
        {
            await ServeNavigationMenus();

            var parameters = new List<StoredProcedureParameter>();

            using (var connection = new SqlConnection(connectionString))
            {
                var command = new SqlCommand(storedProcedureName, connection);
                command.CommandType = CommandType.StoredProcedure;

                SqlCommandBuilder.DeriveParameters(command);

                foreach (SqlParameter param in command.Parameters)
                {
                    parameters.Add(new StoredProcedureParameter
                    {
                        Name = param.ParameterName,
                        DataType = param.SqlDbType.ToString()
                    });
                }
            }

            return parameters;
        }
    }
}