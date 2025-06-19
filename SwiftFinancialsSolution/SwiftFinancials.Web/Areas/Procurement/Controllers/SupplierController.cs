using Application.MainBoundedContext.DTO;
using SwiftFinancials.Web.Controllers;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Mvc;
using System.Threading.Tasks;

namespace SwiftFinancials.Web.Areas.Procurement.Controllers
{
    public class SupplierController : MasterController
    {
        private readonly string _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;

        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();

            var suppliers = new List<SupplierDTO>();
            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand("SELECT Id, Name, Email FROM Suppliers", conn); // Adjust columns accordingly
                await conn.OpenAsync();
                var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    suppliers.Add(new SupplierDTO
                    {
                        Id = reader.GetGuid(0),
                        Name = reader.GetString(1),
                        Email = reader.GetString(2)
                    });
                }
            }

            ViewBag.BatchStatus = suppliers;
            return View();
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> CreateAsync(SupplierDTO supplier)
        {
            await ServeNavigationMenus();

            supplier.Id = Guid.NewGuid();
            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand("INSERT INTO Suppliers (Id, Name, Email) VALUES (@Id, @Name, @Email)", conn);
                cmd.Parameters.AddWithValue("@Id", supplier.Id);
                cmd.Parameters.AddWithValue("@Name", supplier.Name);
                cmd.Parameters.AddWithValue("@Email", supplier.Email);
                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
            }

            return RedirectToAction("Index");
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            SupplierDTO supplier = null;
            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand("SELECT Id, Name, Email FROM Suppliers WHERE Id = @Id", conn);
                cmd.Parameters.AddWithValue("@Id", id);
                await conn.OpenAsync();
                var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    supplier = new SupplierDTO
                    {
                        Id = reader.GetGuid(0),
                        Name = reader.GetString(1),
                        Email = reader.GetString(2)
                    };
                }
            }

            return View(supplier);
        }

        [HttpPost]
        public async Task<ActionResult> Edit(SupplierDTO supplier)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand("UPDATE Suppliers SET Name = @Name, Email = @Email WHERE Id = @Id", conn);
                cmd.Parameters.AddWithValue("@Id", supplier.Id);
                cmd.Parameters.AddWithValue("@Name", supplier.Name);
                cmd.Parameters.AddWithValue("@Email", supplier.Email);
                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
            }

            return RedirectToAction("Index");
        }
    }
}
