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
    public class AssetTypesController : MasterController
    {
        private readonly string _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;

        public async Task<ActionResult> Index()
        {
            var assetTypes = new List<AssetTypeDTO>();

            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand("SELECT Id, Name, DepreciationMethod, UsefulLife, IsTangible, CreatedDate FROM AssetTypes", conn);
                conn.Open();
                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    assetTypes.Add(new AssetTypeDTO
                    {
                        Id = reader.GetGuid(reader.GetOrdinal("Id")),
                        Name = reader["Name"] as string,
                        DepreciationMethod = reader["DepreciationMethod"] as string,
                        UsefulLife = Convert.ToInt32(reader["UsefulLife"]),
                        IsTangible = Convert.ToBoolean(reader["IsTangible"]),
                        CreatedDate = Convert.ToDateTime(reader["CreatedDate"])
                    });
                }
            }
            await ServeNavigationMenus();

            return View(assetTypes);
        }
        public async Task<ActionResult> Details(Guid ?id)
        {
            AssetTypeDTO assetType = null;

            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand("SELECT Id, Name, DepreciationMethod, UsefulLife, IsTangible, CreatedDate FROM AssetTypes WHERE Id = @Id", conn);
                cmd.Parameters.AddWithValue("@Id", id);

                conn.Open();
                var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    assetType = new AssetTypeDTO
                    {
                        Id = reader.GetGuid(reader.GetOrdinal("Id")),
                        Name = reader["Name"] as string,
                        DepreciationMethod = reader["DepreciationMethod"] as string,
                        UsefulLife = Convert.ToInt32(reader["UsefulLife"]),
                        IsTangible = Convert.ToBoolean(reader["IsTangible"]),
                        CreatedDate = Convert.ToDateTime(reader["CreatedDate"])
                    };
                }
            }
            await ServeNavigationMenus();

            if (assetType == null)
                return HttpNotFound();

            return View(assetType);
        }
        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(AssetTypeDTO assetType)
        {
            assetType.Id = Guid.NewGuid();
            assetType.CreatedDate = DateTime.Now;

            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand(@"
                    INSERT INTO AssetTypes (Id, Name, DepreciationMethod, UsefulLife, IsTangible, CreatedDate)
                    VALUES (@Id, @Name, @DepreciationMethod, @UsefulLife, @IsTangible, @CreatedDate)", conn);

                cmd.Parameters.AddWithValue("@Id", assetType.Id);
                cmd.Parameters.AddWithValue("@Name", assetType.Name ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@DepreciationMethod", assetType.DepreciationMethod ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@UsefulLife", assetType.UsefulLife);
                cmd.Parameters.AddWithValue("@IsTangible", assetType.IsTangible);
                cmd.Parameters.AddWithValue("@CreatedDate", assetType.CreatedDate);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
            await ServeNavigationMenus();

            return RedirectToAction("Index");
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            AssetTypeDTO assetType = null;

            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand("SELECT Id, Name, DepreciationMethod, UsefulLife, IsTangible, CreatedDate FROM AssetTypes WHERE Id = @Id", conn);
                cmd.Parameters.AddWithValue("@Id", id);

                conn.Open();
                var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    assetType = new AssetTypeDTO
                    {
                        Id = reader.GetGuid(reader.GetOrdinal("Id")),
                        Name = reader["Name"] as string,
                        DepreciationMethod = reader["DepreciationMethod"] as string,
                        UsefulLife = Convert.ToInt32(reader["UsefulLife"]),
                        IsTangible = Convert.ToBoolean(reader["IsTangible"]),
                        CreatedDate = Convert.ToDateTime(reader["CreatedDate"])
                    };
                }
            }
            await ServeNavigationMenus();

            if (assetType == null)
                return HttpNotFound();

            return View(assetType);
        }

        [HttpPost]
        public async Task<ActionResult> Edit(AssetTypeDTO assetType)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand(@"
                    UPDATE AssetTypes 
                    SET Name = @Name, 
                        DepreciationMethod = @DepreciationMethod, 
                        UsefulLife = @UsefulLife, 
                        IsTangible = @IsTangible
                    WHERE Id = @Id", conn);

                cmd.Parameters.AddWithValue("@Id", assetType.Id);
                cmd.Parameters.AddWithValue("@Name", assetType.Name ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@DepreciationMethod", assetType.DepreciationMethod ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@UsefulLife", assetType.UsefulLife);
                cmd.Parameters.AddWithValue("@IsTangible", assetType.IsTangible);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
            await ServeNavigationMenus();

            return RedirectToAction("Index");
        }
    }
}
