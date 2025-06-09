using Application.MainBoundedContext.DTO;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.Procurement.Controllers
{
    public class UnitOfMeasurementController : Controller
    {
        private readonly string _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;

        // INDEX
        public ActionResult Index()
        {
            var list = new List<UnitOfMeasurementDTO>();

            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand("SELECT Id, Symbol, Description FROM UnitOfMeasurement", conn);
                conn.Open();
                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    list.Add(new UnitOfMeasurementDTO
                    {
                        Id = reader.GetGuid(0),
                        Symbol = reader.GetString(1),
                        Description = reader.IsDBNull(2) ? null : reader.GetString(2)
                    });
                }
            }

            return View(list);
        }

        // CREATE (GET)
        public ActionResult Create()
        {
            return View();
        }

        // CREATE (POST)
        [HttpPost]
        public ActionResult Create(UnitOfMeasurementDTO unitOfMeasurement)
        {
            unitOfMeasurement.Id = Guid.NewGuid();

            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand("INSERT INTO UnitOfMeasurement (Id, Symbol, Description, CreatedDate) VALUES (@Id, @Symbol, @Description, GETDATE())", conn);
                cmd.Parameters.AddWithValue("@Id", unitOfMeasurement.Id);
                var symbolValue = unitOfMeasurement.Symbol != null
                    ? string.Join(",", unitOfMeasurement.Symbol)
                    : (object)DBNull.Value;

                cmd.Parameters.AddWithValue("@Symbol", symbolValue);
                cmd.Parameters.AddWithValue("@Description", unitOfMeasurement.Description ?? (object)DBNull.Value);

                conn.Open();
                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Index");
        }

        // DETAILS / VIEW
        public ActionResult Details(Guid id)
        {
            UnitOfMeasurementDTO unit = null;

            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand("SELECT Id, Symbol, Description FROM UnitOfMeasurement WHERE Id = @Id", conn);
                cmd.Parameters.AddWithValue("@Id", id);

                conn.Open();
                var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    unit = new UnitOfMeasurementDTO
                    {
                        Id = reader.GetGuid(0),
                        Symbol = reader.GetString(1),
                        Description = reader.IsDBNull(2) ? null : reader.GetString(2)
                    };
                }
            }

            if (unit == null)
                return HttpNotFound();

            return View(unit);
        }

        // EDIT (GET)
        public ActionResult Edit(Guid id)
        {
            UnitOfMeasurementDTO unit = null;

            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand("SELECT Id, Symbol, Description FROM UnitOfMeasurement WHERE Id = @Id", conn);
                cmd.Parameters.AddWithValue("@Id", id);

                conn.Open();
                var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    unit = new UnitOfMeasurementDTO
                    {
                        Id = reader.GetGuid(0),
                        Symbol = reader.GetString(1),
                        Description = reader.IsDBNull(2) ? null : reader.GetString(2)
                    };
                }
            }

            if (unit == null)
                return HttpNotFound();

            return View(unit);
        }

        // EDIT (POST)
        [HttpPost]
        public ActionResult Edit(UnitOfMeasurementDTO unit)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand("UPDATE UnitOfMeasurement SET Symbol = @Symbol, Description = @Description WHERE Id = @Id", conn);
                cmd.Parameters.AddWithValue("@Id", unit.Id);
                cmd.Parameters.AddWithValue("@Symbol", unit.Symbol ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Description", unit.Description ?? (object)DBNull.Value);

                conn.Open();
                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Index");
        }
    }
}
