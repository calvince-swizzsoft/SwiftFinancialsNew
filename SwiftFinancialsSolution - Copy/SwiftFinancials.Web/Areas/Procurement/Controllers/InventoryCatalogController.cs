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
    public class InventoryCatalogController : MasterController
    {
        private readonly string _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;

        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();
            var items = new List<InventoryCatalogDTO>();

            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand("SELECT * FROM InventoryCatalog", conn);
                conn.Open();
                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    items.Add(new InventoryCatalogDTO
                    {
                        Id = reader.GetGuid(reader.GetOrdinal("Id")),
                        Name = reader["Name"] as string,
                        Category = reader["Category"] as string,
                        BaseUOM = reader["BaseUOM"] as string,
                        PackageType = reader["PackageType"] as string,
                        GLAccount = reader["GLAccount"] as string,
                        MainSupplier = reader["MainSupplier"] as string,

                        ReorderPoint = reader.GetInt32(reader.GetOrdinal("ReorderPoint")),
                        MaximumOrder = reader.GetInt32(reader.GetOrdinal("MaximumOrder")),
                        UnitsPerPack = reader.GetInt32(reader.GetOrdinal("UnitsPerPack")),
                        PaletteTI = reader.GetInt32(reader.GetOrdinal("PaletteTI")),
                        PaletteHI = reader.GetInt32(reader.GetOrdinal("PaletteHI")),

                        UnitNetWeight = reader.GetDecimal(reader.GetOrdinal("UnitNetWeight")),
                        UnitGrossWeight = reader.GetDecimal(reader.GetOrdinal("UnitGrossWeight")),
                        LeadDays = reader.GetInt32(reader.GetOrdinal("LeadDays")),
                        EconomicOrder = reader.GetInt32(reader.GetOrdinal("EconomicOrder")),

                        UnitCost = reader.GetDecimal(reader.GetOrdinal("UnitCost")),
                        UnitPrice = reader.GetDecimal(reader.GetOrdinal("UnitPrice")),
                        MonthlyDemand = reader.GetInt32(reader.GetOrdinal("MonthlyDemand")),

                        Remarks = reader["Remarks"] as string
                    });
                }
            }

            return View(items);
        }

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(InventoryCatalogDTO item)
        {
            await ServeNavigationMenus();

            item.Id = Guid.NewGuid();

            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand(@"
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                    INSERT INTO InventoryCatalog (
                        Id, Name, Category, BaseUOM, PackageType, GLAccount, MainSupplier,
                        ReorderPoint, MaximumOrder, UnitsPerPack, PaletteTI, PaletteHI,
                        UnitNetWeight, UnitGrossWeight, LeadDays, EconomicOrder,
                        UnitCost, UnitPrice, MonthlyDemand, Remarks, CreatedDate
                    ) VALUES (
                        @Id, @Name, @Category, @BaseUOM, @PackageType, @GLAccount, @MainSupplier,
                        @ReorderPoint, @MaximumOrder, @UnitsPerPack, @PaletteTI, @PaletteHI,
                        @UnitNetWeight, @UnitGrossWeight, @LeadDays, @EconomicOrder,
                        @UnitCost, @UnitPrice, @MonthlyDemand, @Remarks, GETDATE()
                    )", conn);

                cmd.Parameters.AddWithValue("@Id", item.Id);
                cmd.Parameters.AddWithValue("@Name", item.Name ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Category", item.Category ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@BaseUOM", item.BaseUOM ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@PackageType", item.PackageType ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@GLAccount", item.GLAccount ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@MainSupplier", item.MainSupplier ?? (object)DBNull.Value);

                cmd.Parameters.AddWithValue("@ReorderPoint", item.ReorderPoint);
                cmd.Parameters.AddWithValue("@MaximumOrder", item.MaximumOrder);
                cmd.Parameters.AddWithValue("@UnitsPerPack", item.UnitsPerPack);
                cmd.Parameters.AddWithValue("@PaletteTI", item.PaletteTI);
                cmd.Parameters.AddWithValue("@PaletteHI", item.PaletteHI);

                cmd.Parameters.AddWithValue("@UnitNetWeight", item.UnitNetWeight);
                cmd.Parameters.AddWithValue("@UnitGrossWeight", item.UnitGrossWeight);
                cmd.Parameters.AddWithValue("@LeadDays", item.LeadDays);
                cmd.Parameters.AddWithValue("@EconomicOrder", item.EconomicOrder);

                cmd.Parameters.AddWithValue("@UnitCost", item.UnitCost);
                cmd.Parameters.AddWithValue("@UnitPrice", item.UnitPrice);
                cmd.Parameters.AddWithValue("@MonthlyDemand", item.MonthlyDemand);

                cmd.Parameters.AddWithValue("@Remarks", item.Remarks ?? (object)DBNull.Value);

                conn.Open();
                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Index");
        }


        public async Task<ActionResult> Details(Guid? id)
        {
            InventoryCatalogDTO item = null;
            await ServeNavigationMenus();

            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand("SELECT * FROM InventoryCatalog WHERE Id = @Id", conn);
                cmd.Parameters.AddWithValue("@Id", id);

                conn.Open();
                var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    item = new InventoryCatalogDTO
                    {
                        Id = reader.GetGuid(reader.GetOrdinal("Id")),
                        Name = reader["Name"] as string,
                        Category = reader["Category"] as string,
                        BaseUOM = reader["BaseUOM"] as string,
                        PackageType = reader["PackageType"] as string,
                        GLAccount = reader["GLAccount"] as string,
                        MainSupplier = reader["MainSupplier"] as string,

                        ReorderPoint = reader.GetInt32(reader.GetOrdinal("ReorderPoint")),
                        MaximumOrder = reader.GetInt32(reader.GetOrdinal("MaximumOrder")),
                        UnitsPerPack = reader.GetInt32(reader.GetOrdinal("UnitsPerPack")),
                        PaletteTI = reader.GetInt32(reader.GetOrdinal("PaletteTI")),
                        PaletteHI = reader.GetInt32(reader.GetOrdinal("PaletteHI")),

                        UnitNetWeight = reader.GetDecimal(reader.GetOrdinal("UnitNetWeight")),
                        UnitGrossWeight = reader.GetDecimal(reader.GetOrdinal("UnitGrossWeight")),
                        LeadDays = reader.GetInt32(reader.GetOrdinal("LeadDays")),
                        EconomicOrder = reader.GetInt32(reader.GetOrdinal("EconomicOrder")),

                        UnitCost = reader.GetDecimal(reader.GetOrdinal("UnitCost")),
                        UnitPrice = reader.GetDecimal(reader.GetOrdinal("UnitPrice")),
                        MonthlyDemand = reader.GetInt32(reader.GetOrdinal("MonthlyDemand")),

                        Remarks = reader["Remarks"] as string
                    };
                }
            }

            if (item == null)
                return HttpNotFound();

            return View(item);
        }


        public async Task<ActionResult> Edit(Guid? id)
        {
            InventoryCatalogDTO item = null;
            await ServeNavigationMenus();

            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand("SELECT * FROM InventoryCatalog WHERE Id = @Id", conn);
                cmd.Parameters.AddWithValue("@Id", id);

                conn.Open();
                var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    item = new InventoryCatalogDTO
                    {
                        Id = reader.GetGuid(reader.GetOrdinal("Id")),
                        Name = reader["Name"] as string,
                        Category = reader["Category"] as string,
                        BaseUOM = reader["BaseUOM"] as string,
                        PackageType = reader["PackageType"] as string,
                        GLAccount = reader["GLAccount"] as string,
                        MainSupplier = reader["MainSupplier"] as string,

                        ReorderPoint = reader.GetInt32(reader.GetOrdinal("ReorderPoint")),
                        MaximumOrder = reader.GetInt32(reader.GetOrdinal("MaximumOrder")),
                        UnitsPerPack = reader.GetInt32(reader.GetOrdinal("UnitsPerPack")),
                        PaletteTI = reader.GetInt32(reader.GetOrdinal("PaletteTI")),
                        PaletteHI = reader.GetInt32(reader.GetOrdinal("PaletteHI")),

                        UnitNetWeight = reader.GetDecimal(reader.GetOrdinal("UnitNetWeight")),
                        UnitGrossWeight = reader.GetDecimal(reader.GetOrdinal("UnitGrossWeight")),
                        LeadDays = reader.GetInt32(reader.GetOrdinal("LeadDays")),
                        EconomicOrder = reader.GetInt32(reader.GetOrdinal("EconomicOrder")),

                        UnitCost = reader.GetDecimal(reader.GetOrdinal("UnitCost")),
                        UnitPrice = reader.GetDecimal(reader.GetOrdinal("UnitPrice")),
                        MonthlyDemand = reader.GetInt32(reader.GetOrdinal("MonthlyDemand")),

                        Remarks = reader["Remarks"] as string
                    };
                }
            }

            if (item == null)
                return HttpNotFound();

            return View(item);
        }

        [HttpPost]
        public async Task<ActionResult> Edit(InventoryCatalogDTO item)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand(@"
                    UPDATE InventoryCatalog SET
                        Name = @Name,
                        Category = @Category,
                        BaseUOM = @BaseUOM,
                        PackageType = @PackageType,
                        GLAccount = @GLAccount,
                        MainSupplier = @MainSupplier,
                        ReorderPoint = @ReorderPoint,
                        MaximumOrder = @MaximumOrder,
                        UnitsPerPack = @UnitsPerPack,
                        PaletteTI = @PaletteTI,
                        PaletteHI = @PaletteHI,
                        UnitNetWeight = @UnitNetWeight,
                        UnitGrossWeight = @UnitGrossWeight,
                        LeadDays = @LeadDays,
                        EconomicOrder = @EconomicOrder,
                        UnitCost = @UnitCost,
                        UnitPrice = @UnitPrice,
                        MonthlyDemand = @MonthlyDemand,
                        Remarks = @Remarks
                    WHERE Id = @Id", conn);

                cmd.Parameters.AddWithValue("@Id", item.Id);
                cmd.Parameters.AddWithValue("@Name", item.Name ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Category", item.Category ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@BaseUOM", item.BaseUOM ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@PackageType", item.PackageType ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@GLAccount", item.GLAccount ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@MainSupplier", item.MainSupplier ?? (object)DBNull.Value);

                cmd.Parameters.AddWithValue("@ReorderPoint", item.ReorderPoint);
                cmd.Parameters.AddWithValue("@MaximumOrder", item.MaximumOrder);
                cmd.Parameters.AddWithValue("@UnitsPerPack", item.UnitsPerPack);
                cmd.Parameters.AddWithValue("@PaletteTI", item.PaletteTI);
                cmd.Parameters.AddWithValue("@PaletteHI", item.PaletteHI);

                cmd.Parameters.AddWithValue("@UnitNetWeight", item.UnitNetWeight);
                cmd.Parameters.AddWithValue("@UnitGrossWeight", item.UnitGrossWeight);
                cmd.Parameters.AddWithValue("@LeadDays", item.LeadDays);
                cmd.Parameters.AddWithValue("@EconomicOrder", item.EconomicOrder);

                cmd.Parameters.AddWithValue("@UnitCost", item.UnitCost);
                cmd.Parameters.AddWithValue("@UnitPrice", item.UnitPrice);
                cmd.Parameters.AddWithValue("@MonthlyDemand", item.MonthlyDemand);
                cmd.Parameters.AddWithValue("@Remarks", item.Remarks ?? (object)DBNull.Value);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
            await ServeNavigationMenus();

            return RedirectToAction("Index");
        }
    }
}
