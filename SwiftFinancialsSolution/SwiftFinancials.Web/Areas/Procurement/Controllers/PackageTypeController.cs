using Application.MainBoundedContext.DTO;
using SwiftFinancials.Web.Controllers;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.Procurement.Controllers
{
    public class PackageTypeController : MasterController
    {
        private readonly string _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;

        public ActionResult Index()
        {
            var packageTypes = new List<PackageTypeDTO>();

            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand("SELECT Id, Name, Description FROM PackageType", conn);
                conn.Open();
                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    packageTypes.Add(new PackageTypeDTO
                    {
                        Id = reader.GetGuid(0),
                        Name = reader.GetString(1),
                        Description = reader.IsDBNull(2) ? null : reader.GetString(2)
                    });
                }
            }

            return View(packageTypes);
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Create(PackageTypeDTO packageType)
        {
            packageType.Id = Guid.NewGuid();

            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand("INSERT INTO PackageType (Id, Name, Description, CreatedDate) VALUES (@Id, @Name, @Description, GETDATE())", conn);
                cmd.Parameters.AddWithValue("@Id", packageType.Id);
                cmd.Parameters.AddWithValue("@Name", packageType.Name ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Description", packageType.Description ?? (object)DBNull.Value);

                conn.Open();
                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Index");
        }

        public ActionResult Details(Guid id)
        {
            PackageTypeDTO packageType = null;

            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand("SELECT Id, Name, Description FROM PackageType WHERE Id = @Id", conn);
                cmd.Parameters.AddWithValue("@Id", id);

                conn.Open();
                var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    packageType = new PackageTypeDTO
                    {
                        Id = reader.GetGuid(0),
                        Name = reader.GetString(1),
                        Description = reader.IsDBNull(2) ? null : reader.GetString(2)
                    };
                }
            }

            if (packageType == null)
                return HttpNotFound();

            return View(packageType);
        }

        public ActionResult Edit(Guid id)
        {
            PackageTypeDTO packageType = null;

            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand("SELECT Id, Name, Description FROM PackageType WHERE Id = @Id", conn);
                cmd.Parameters.AddWithValue("@Id", id);

                conn.Open();
                var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    packageType = new PackageTypeDTO
                    {
                        Id = reader.GetGuid(0),
                        Name = reader.GetString(1),
                        Description = reader.IsDBNull(2) ? null : reader.GetString(2)
                    };
                }
            }

            if (packageType == null)
                return HttpNotFound();

            return View(packageType);
        }

        [HttpPost]
        public ActionResult Edit(PackageTypeDTO packageType)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand("UPDATE PackageType SET Name = @Name, Description = @Description WHERE Id = @Id", conn);
                cmd.Parameters.AddWithValue("@Id", packageType.Id);
                cmd.Parameters.AddWithValue("@Name", packageType.Name ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Description", packageType.Description ?? (object)DBNull.Value);

                conn.Open();
                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Index");
        }
    }
}
