using Application.MainBoundedContext.DTO;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Web.Mvc;
using SwiftFinancials.Presentation.Infrastructure.Util;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.Procurement.Controllers
{
    public class UnitOfMeasurementController : MasterController
    {
        private readonly string _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;

        // INDEX
        public async Task<ActionResult> Index()
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
            await ServeNavigationMenus();
            return View(list);
        }

        // CREATE (GET)
        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();
            return View();
        }

        // CREATE (POST)
        [HttpPost]
        public async Task<ActionResult> Create(UnitOfMeasurementDTO unitOfMeasurement)
        {
            await ServeNavigationMenus();
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
        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();
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
        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();
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
        public async Task<ActionResult> Edit(UnitOfMeasurementDTO unit)
        {
            await ServeNavigationMenus();
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
