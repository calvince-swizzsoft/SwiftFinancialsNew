//using SwiftFinancials.Web.Areas.Reports.Models;
//using System;
//using System.Collections.Generic;
//using System.Data.SqlClient;
//using System.Linq;
//using System.Web;
//using System.Web.Mvc;

//namespace SwiftFinancials.Web.Areas.Reports.Controllers
//{
//    public class StoredProcedureController : Controller
//    {
//        public ActionResult Index()
//        {
//            string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;
//            var storedProcedures = new List<StoredProcedureInfo>();

//            using (SqlConnection connection = new SqlConnection(connectionString))
//            {
//                string query = @"SELECT 
//                                p.name AS ProcedureName,
//                                m.definition AS ProcedureDefinition,
//                                p.create_date AS CreatedDate,
//                                p.modify_date AS ModifiedDate,
//                                param.name AS ParameterName,
//                                t.name AS ParameterType,
//                                param.max_length AS ParameterLength,
//                                param.is_output AS IsOutputParameter
//                            FROM 
//                                sys.procedures p
//                            JOIN 
//                                sys.sql_modules m ON p.object_id = m.object_id
//                            LEFT JOIN 
//                                sys.parameters param ON p.object_id = param.object_id
//                            LEFT JOIN 
//                                sys.types t ON param.system_type_id = t.system_type_id
//                            ORDER BY 
//                                p.name, param.parameter_id;";

//                SqlCommand command = new SqlCommand(query, connection);
//                connection.Open();

//                using (SqlDataReader reader = command.ExecuteReader())
//                {
//                    string currentProcedureName = null;
//                    StoredProcedureInfo currentProcedure = null;

//                    while (reader.Read())
//                    {
//                        var procedureName = reader["ProcedureName"].ToString();

//                        // Check if we are still on the same procedure or if we have a new one
//                        if (currentProcedureName != procedureName)
//                        {
//                            currentProcedureName = procedureName;
//                            currentProcedure = new StoredProcedureInfo
//                            {
//                                ProcedureName = procedureName,
//                                ProcedureDefinition = reader["ProcedureDefinition"].ToString(),
//                                CreatedDate = Convert.ToDateTime(reader["CreatedDate"]),
//                                ModifiedDate = Convert.ToDateTime(reader["ModifiedDate"]),
//                            };
//                            storedProcedures.Add(currentProcedure);
//                        }

//                        // Add parameters if available
//                        if (!reader.IsDBNull(reader.GetOrdinal("ParameterName")))
//                        {
//                            currentProcedure.Parameters.Add(new StoredProcedureParameter
//                            {
//                                ParameterName = reader["ParameterName"].ToString(),
//                                ParameterType = reader["ParameterType"].ToString(),
//                                ParameterLength = Convert.ToInt32(reader["ParameterLength"]),
//                                IsOutputParameter = Convert.ToBoolean(reader["IsOutputParameter"])
//                            });
//                        }
//                    }
//                }

//                connection.Close();
//            }

//            return View(storedProcedures);
//        }

        
//        [HttpGet]
//        public ActionResult Create()
//        {
//            return View(new StoredProcedureInfo());
//        }

        
//        [HttpPost]
//        public ActionResult Create(StoredProcedureInfo procedure)
//        {
//            string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

//            using (SqlConnection connection = new SqlConnection(connectionString))
//            {
//                connection.Open();
//                using (SqlCommand command = new SqlCommand(procedure.ProcedureDefinition, connection))
//                {
//                    command.ExecuteNonQuery();
//                }

//                connection.Close();
//            }

//            return RedirectToAction("Index");
//        }
//    }
//}