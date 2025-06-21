using SwiftFinancials.Web.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using System.Configuration;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace SwiftFinancials.Web.Areas.Procurement.Controllers
{
    public class CatalogueController : MasterController
    {
        private readonly string _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;

        public async Task<ActionResult> Index()
        {
            var assets = new List<AssetDTO>();

            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand("SELECT * FROM Assets", conn);
                conn.Open();
                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    assets.Add(new AssetDTO
                    {
                        Id = reader.GetGuid(reader.GetOrdinal("Id")),
                        Name = reader.GetString(reader.GetOrdinal("Name")),
                        Remarks = reader["Remarks"] as string,
                        AssetType = reader["AssetType"] as string,
                        Supplier = reader["Supplier"] as string,
                        Department = reader["Department"] as string,
                        PicturePath = reader["PicturePath"] as string,
                        GLAccount = reader["GLAccount"] as string,
                        SerialNumber = reader["SerialNumber"] as string,
                        Manufacturer = reader["Manufacturer"] as string,
                        Model = reader["Model"] as string,
                        TagNumber = reader["TagNumber"] as string,
                        Location = reader["Location"] as string,
                        PurchasePrice = reader.GetDecimal(reader.GetOrdinal("PurchasePrice")),
                        ResidualValue = reader.GetDecimal(reader.GetOrdinal("ResidualValue"))
                    });
                }
            }
            await ServeNavigationMenus();

            return View(assets);
        }

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(AssetDTO asset)
        {
            asset.Id = Guid.NewGuid();

            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand(@"
                    INSERT INTO Assets (
                        Id, Name, Remarks, AssetType, Supplier, Department,
                        PicturePath, GLAccount, SerialNumber, Manufacturer, Model,
                        TagNumber, Location, PurchasePrice, ResidualValue, CreatedDate
                    ) VALUES (
                        @Id, @Name, @Remarks, @AssetType, @Supplier, @Department,
                        @PicturePath, @GLAccount, @SerialNumber, @Manufacturer, @Model,
                        @TagNumber, @Location, @PurchasePrice, @ResidualValue, GETDATE()
                    )", conn);

                cmd.Parameters.AddWithValue("@Id", asset.Id);
                cmd.Parameters.AddWithValue("@Name", asset.Name ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Remarks", asset.Remarks ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@AssetType", asset.AssetType ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Supplier", asset.Supplier ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Department", asset.Department ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@PicturePath", asset.PicturePath ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@GLAccount", asset.GLAccount ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@SerialNumber", asset.SerialNumber ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Manufacturer", asset.Manufacturer ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Model", asset.Model ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@TagNumber", asset.TagNumber ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Location", asset.Location ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@PurchasePrice", asset.PurchasePrice);
                cmd.Parameters.AddWithValue("@ResidualValue", asset.ResidualValue);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
            await ServeNavigationMenus();

            return RedirectToAction("Index");
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            AssetDTO asset = null;

            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand("SELECT * FROM Assets WHERE Id = @Id", conn);
                cmd.Parameters.AddWithValue("@Id", id);

                conn.Open();
                var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    asset = new AssetDTO
                    {
                        Id = reader.GetGuid(reader.GetOrdinal("Id")),
                        Name = reader.GetString(reader.GetOrdinal("Name")),
                        Remarks = reader["Remarks"] as string,
                        AssetType = reader["AssetType"] as string,
                        Supplier = reader["Supplier"] as string,
                        Department = reader["Department"] as string,
                        PicturePath = reader["PicturePath"] as string,
                        GLAccount = reader["GLAccount"] as string,
                        SerialNumber = reader["SerialNumber"] as string,
                        Manufacturer = reader["Manufacturer"] as string,
                        Model = reader["Model"] as string,
                        TagNumber = reader["TagNumber"] as string,
                        Location = reader["Location"] as string,
                        PurchasePrice = reader.GetDecimal(reader.GetOrdinal("PurchasePrice")),
                        ResidualValue = reader.GetDecimal(reader.GetOrdinal("ResidualValue"))
                    };
                }
            }

            if (asset == null)
                return HttpNotFound();
            await ServeNavigationMenus();

            return View(asset);
        }

        [HttpPost]
        public async Task<ActionResult> Edit(AssetDTO asset)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand(@"
                    UPDATE Assets SET
                        Name = @Name,
                        Remarks = @Remarks,
                        AssetType = @AssetType,
                        Supplier = @Supplier,
                        Department = @Department,
                        PicturePath = @PicturePath,
                        GLAccount = @GLAccount,
                        SerialNumber = @SerialNumber,
                        Manufacturer = @Manufacturer,
                        Model = @Model,
                        TagNumber = @TagNumber,
                        Location = @Location,
                        PurchasePrice = @PurchasePrice,
                        ResidualValue = @ResidualValue
                    WHERE Id = @Id", conn);

                cmd.Parameters.AddWithValue("@Id", asset.Id);
                cmd.Parameters.AddWithValue("@Name", asset.Name ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Remarks", asset.Remarks ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@AssetType", asset.AssetType ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Supplier", asset.Supplier ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Department", asset.Department ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@PicturePath", asset.PicturePath ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@GLAccount", asset.GLAccount ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@SerialNumber", asset.SerialNumber ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Manufacturer", asset.Manufacturer ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Model", asset.Model ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@TagNumber", asset.TagNumber ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Location", asset.Location ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@PurchasePrice", asset.PurchasePrice);
                cmd.Parameters.AddWithValue("@ResidualValue", asset.ResidualValue);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
            await ServeNavigationMenus();

            return RedirectToAction("Index");
        }

        public async Task<ActionResult> Details(Guid id)
        {
            AssetDTO asset = null;
            await ServeNavigationMenus();

            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand("SELECT * FROM Assets WHERE Id = @Id", conn);
                cmd.Parameters.AddWithValue("@Id", id);

                conn.Open();
                var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    asset = new AssetDTO
                    {
                        Id = reader.GetGuid(reader.GetOrdinal("Id")),
                        Name = reader.GetString(reader.GetOrdinal("Name")),
                        Remarks = reader["Remarks"] as string,
                        AssetType = reader["AssetType"] as string,
                        Supplier = reader["Supplier"] as string,
                        Department = reader["Department"] as string,
                        PicturePath = reader["PicturePath"] as string,
                        GLAccount = reader["GLAccount"] as string,
                        SerialNumber = reader["SerialNumber"] as string,
                        Manufacturer = reader["Manufacturer"] as string,
                        Model = reader["Model"] as string,
                        TagNumber = reader["TagNumber"] as string,
                        Location = reader["Location"] as string,
                        PurchasePrice = reader.GetDecimal(reader.GetOrdinal("PurchasePrice")),
                        ResidualValue = reader.GetDecimal(reader.GetOrdinal("ResidualValue"))
                    };
                }
            }

            if (asset == null)
                return HttpNotFound();

            return View(asset);
        }
    }
}
