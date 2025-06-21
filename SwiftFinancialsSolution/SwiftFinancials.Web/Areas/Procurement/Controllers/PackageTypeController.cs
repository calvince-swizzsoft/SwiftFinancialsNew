using Application.MainBoundedContext.DTO;
using SwiftFinancials.Web.Controllers;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.Procurement.Controllers
{
    public class PackageTypeController : MasterController
    {
        private readonly string _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;

        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();

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

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(PackageTypeDTO packageType)
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
            await ServeNavigationMenus();

            return RedirectToAction("Index");
        }

        public async Task<ActionResult> Details(Guid ?id)
        {
            await ServeNavigationMenus();

            PackageTypeDTO packageType = null;

            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand("select * from packageType WHERE Id = @Id", conn);
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

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

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
        public async Task<ActionResult> Edit(PackageTypeDTO packageType)
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
            await ServeNavigationMenus();

            return RedirectToAction("Index");
        }
    }
}
