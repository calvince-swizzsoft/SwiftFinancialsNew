using Application.MainBoundedContext.DTO;
using SwiftFinancials.Web.Controllers;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.Procurement.Controllers
{
    public class InventoryCategoryController : MasterController
    {
        private readonly string _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;

        public ActionResult Index()
        {
            var inventoryCategories = new List<InventoryCategoryDTO>();

            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand("SELECT Id, Name, Remarks FROM InventoryCategory", conn);
                conn.Open();
                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    inventoryCategories.Add(new InventoryCategoryDTO
                    {
                        Id = reader.GetGuid(reader.GetOrdinal("Id")),
                        Name = reader["Name"] as string,
                        Remarks = reader["Remarks"] as string
                    });
                }
            }

            return View(inventoryCategories);
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Create(InventoryCategoryDTO inventoryCategory)
        {
            inventoryCategory.Id = Guid.NewGuid();

            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand(
                    "INSERT INTO InventoryCategory (Id, Name, Remarks, CreatedDate) VALUES (@Id, @Name, @Remarks, GETDATE())",
                    conn
                );

                cmd.Parameters.AddWithValue("@Id", inventoryCategory.Id);
                cmd.Parameters.AddWithValue("@Name", inventoryCategory.Name ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Remarks", inventoryCategory.Remarks ?? (object)DBNull.Value);

                conn.Open();
                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Index");
        }

        public ActionResult Edit(Guid id)
        {
            InventoryCategoryDTO category = null;

            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand("SELECT Id, Name, Remarks FROM InventoryCategory WHERE Id = @Id", conn);
                cmd.Parameters.AddWithValue("@Id", id);
                conn.Open();
                var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    category = new InventoryCategoryDTO
                    {
                        Id = reader.GetGuid(reader.GetOrdinal("Id")),
                        Name = reader["Name"] as string,
                        Remarks = reader["Remarks"] as string
                    };
                }
            }

            return View(category);
        }

        [HttpPost]
        public ActionResult Edit(InventoryCategoryDTO inventoryCategory)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand(
                    "UPDATE InventoryCategory SET Name = @Name, Remarks = @Remarks WHERE Id = @Id",
                    conn
                );

                cmd.Parameters.AddWithValue("@Id", inventoryCategory.Id);
                cmd.Parameters.AddWithValue("@Name", inventoryCategory.Name ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Remarks", inventoryCategory.Remarks ?? (object)DBNull.Value);

                conn.Open();
                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Index");
        }

        public ActionResult Details(Guid? id)
        {
            InventoryCategoryDTO category = null;

            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand("SELECT Id, Name, Remarks FROM InventoryCategory WHERE Id = @Id", conn);
                cmd.Parameters.AddWithValue("@Id", id);
                conn.Open();
                var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    category = new InventoryCategoryDTO
                    {
                        Id = reader.GetGuid(reader.GetOrdinal("Id")),
                        Name = reader["Name"] as string,
                        Remarks = reader["Remarks"] as string
                    };
                }
            }

            return View(category);
        }
    }
}
