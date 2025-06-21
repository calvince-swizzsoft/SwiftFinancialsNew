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
                var cmd = new SqlCommand(@"
    SELECT 
        Id, 
        Name, 
        Email, 
        ChartOfAccountId, 
        ChartofAccountName, 
        PhoneNumber, 
        Address
    FROM Suppliers", conn);
                await conn.OpenAsync();
                var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    suppliers.Add(new SupplierDTO
                    {
                        Id = reader["Id"] != DBNull.Value ? (Guid)reader["Id"] : Guid.Empty,
                        Name = reader["Name"]?.ToString(),
                        Email = reader["Email"]?.ToString(),
                        ChartOfAccountId = reader["ChartOfAccountId"] != DBNull.Value ? (Guid)reader["ChartOfAccountId"] : Guid.Empty,
                        ChartofAccountName = reader["ChartofAccountName"]?.ToString(),
                        LandLine = reader["PhoneNumber"]?.ToString(),
                        AddressLine1 = reader["Address"]?.ToString()
                    });
                }

            }

            ViewBag.BatchStatus = suppliers;
            return View(suppliers);
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(SupplierDTO supplier)
        {
            await ServeNavigationMenus();

            supplier.Id = Guid.NewGuid();
            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand("INSERT INTO Suppliers (Id, Name, Email,ChartofAccountName,ChartofAccountId) VALUES (@Id, @Name, @Email,@ChartofAccountName,@ChartofAccountId)", conn);
                cmd.Parameters.AddWithValue("@Id", supplier.Id);
                cmd.Parameters.AddWithValue("@Name", supplier.Name);
                cmd.Parameters.AddWithValue("@Email", supplier.Email);
                cmd.Parameters.AddWithValue("@ChartofAccountName", supplier.ChartofAccountName);

                cmd.Parameters.AddWithValue("@ChartOfAccountId", supplier.ChartOfAccountId);

                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
            }

            return RedirectToAction("Index");
        }
        public async Task<ActionResult> Edit(Guid? id)
        {
            await ServeNavigationMenus();

            if (id == null)
                return HttpNotFound();

            SupplierDTO supplier = null;
            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand(@"
            SELECT 
                Id, 
                Name, 
                Email, 
                ChartOfAccountId, 
                ChartofAccountName, 
                PhoneNumber, 
                Address,
                AddressLine2,
                Street,
                PostalCode,
                LandLine
            FROM Suppliers 
            WHERE Id = @Id", conn);

                cmd.Parameters.AddWithValue("@Id", id.Value);
                await conn.OpenAsync();
                var reader = await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    supplier = new SupplierDTO
                    {
                        Id = (Guid)reader["Id"],
                        Name = reader["Name"]?.ToString(),
                        Email = reader["Email"]?.ToString(),
                        ChartOfAccountId = reader["ChartOfAccountId"] != DBNull.Value ? (Guid)reader["ChartOfAccountId"] : Guid.Empty,
                        ChartofAccountName = reader["ChartofAccountName"]?.ToString(),
                        LandLine = reader["PhoneNumber"]?.ToString(),
                        AddressLine1 = reader["Address"]?.ToString(),
                        AddressLine2 = reader["AddressLine2"]?.ToString(),
                        Street = reader["Street"]?.ToString(),
                        PostalCode = reader["PostalCode"]?.ToString(),
                        MobileLine = reader["LandLine"]?.ToString(),
                    };
                }
            }

            if (supplier == null)
                return HttpNotFound();

            return View(supplier);
        }


        public async Task<ActionResult> Details(Guid? id)
        {
            await ServeNavigationMenus();

            if (id == null)
                return HttpNotFound();

            SupplierDTO supplier = null;
            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand(@"
            SELECT 
                Id, 
                Name, 
                Email, 
                ChartOfAccountId, 
                ChartofAccountName, 
                PhoneNumber, 
                Address,
                AddressLine2,
                Street,
                PostalCode,
                LandLine
            FROM Suppliers 
            WHERE Id = @Id", conn);

                cmd.Parameters.AddWithValue("@Id", id.Value);
                await conn.OpenAsync();
                var reader = await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    supplier = new SupplierDTO
                    {
                        Id = (Guid)reader["Id"],
                        Name = reader["Name"]?.ToString(),
                        Email = reader["Email"]?.ToString(),
                        ChartOfAccountId = reader["ChartOfAccountId"] != DBNull.Value ? (Guid)reader["ChartOfAccountId"] : Guid.Empty,
                        ChartofAccountName = reader["ChartofAccountName"]?.ToString(),
                        LandLine = reader["PhoneNumber"]?.ToString(),
                        AddressLine1 = reader["Address"]?.ToString(),
                        AddressLine2 = reader["AddressLine2"]?.ToString(),
                        Street = reader["Street"]?.ToString(),
                        PostalCode = reader["PostalCode"]?.ToString(),
                        MobileLine = reader["LandLine"]?.ToString(),
                    };
                }
            }

            if (supplier == null)
                return HttpNotFound();

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
