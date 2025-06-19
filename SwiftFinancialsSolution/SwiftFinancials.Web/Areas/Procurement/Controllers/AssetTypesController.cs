using Application.MainBoundedContext.DTO;
using SwiftFinancials.Web.Controllers;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.Procurement.Controllers
{
    public class AssetTypesController : MasterController
    {
        private readonly string _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;

        public ActionResult Index()
        {
            var assetTypes = new List<AssetTypeDTO>();

            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand("SELECT Id, Name, Description FROM AssetTypes", conn);
                conn.Open();
                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    assetTypes.Add(new AssetTypeDTO
                    {
                        Id = reader.GetGuid(reader.GetOrdinal("Id")),
                        Name = reader["Name"] as string
                    });
                }
            }

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

            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand(
                    "INSERT INTO AssetTypes (Id, Name, Description, CreatedDate) VALUES (@Id, @Name, @Description, GETDATE())",
                    conn
                );

                cmd.Parameters.AddWithValue("@Id", assetType.Id);
                cmd.Parameters.AddWithValue("@Name", assetType.Name ?? (object)DBNull.Value);

                conn.Open();
                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Index");
        }

        public ActionResult Edit(Guid id)
        {
            AssetTypeDTO assetType = null;

            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand("SELECT Id, Name, Description FROM AssetTypes WHERE Id = @Id", conn);
                cmd.Parameters.AddWithValue("@Id", id);

                conn.Open();
                var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    assetType = new AssetTypeDTO
                    {
                        Id = reader.GetGuid(reader.GetOrdinal("Id")),
                        Name = reader["Name"] as string,
                    };
                }
            }

            if (assetType == null)
                return HttpNotFound();

            return View(assetType);
        }

        [HttpPost]
        public ActionResult Edit(AssetTypeDTO assetType)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand(
                    "UPDATE AssetTypes SET Name = @Name, Description = @Description WHERE Id = @Id",
                    conn
                );

                cmd.Parameters.AddWithValue("@Id", assetType.Id);
                cmd.Parameters.AddWithValue("@Name", assetType.Name ?? (object)DBNull.Value);

                conn.Open();
                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Index");
        }
    }
}
