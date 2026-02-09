using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using TestApis.Models;

namespace TestApis.Services
{
    public class FixedAssetService
    {
        private readonly string _connectionString;

        public FixedAssetService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;
        }

        // Get all
        public List<FixedAsset> GetAll()
        {
            var list = new List<FixedAsset>();

            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"
                    SELECT fa.Id, fa.No, fa.SerialNo, fa.AssetName, fa.ResponsibleEmployee,
                           fa.FASubClassId, sc.Description AS FASubClassDescription,
                           fa.LocationId, loc.Description AS LocationDescription,
                           fa.BookValue, fa.IsInactive, fa.DepreciationMethod,
                           fa.DepreciationStartDate, fa.NoOfDepreciationYears,
                           fa.DepreciationEndingDate, fa.ReducingBalancePercentage,
                           fa.FAGroup, fa.CreatedDate, fa.CreatedBy
                    FROM FixedAsset fa
                    INNER JOIN FASubClass sc ON fa.FASubClassId = sc.Id
                    INNER JOIN FALocation loc ON fa.LocationId = loc.Id";

                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();
                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    list.Add(new FixedAsset
                    {
                        Id = reader.GetGuid(reader.GetOrdinal("Id")),
                        No = reader["No"].ToString(),
                        SerialNo = reader["SerialNo"].ToString(),
                        AssetName = reader["AssetName"].ToString(),
                        ResponsibleEmployee = reader["ResponsibleEmployee"].ToString(),
                        FASubClassId = reader.GetGuid(reader.GetOrdinal("FASubClassId")),
                        FASubClassDescription = reader["FASubClassDescription"].ToString(),
                        LocationId = reader.GetGuid(reader.GetOrdinal("LocationId")),
                        LocationDescription = reader["LocationDescription"].ToString(),
                        BookValue = Convert.ToDecimal(reader["BookValue"]),
                        IsInactive = Convert.ToBoolean(reader["IsInactive"]),
                        DepreciationMethod = reader["DepreciationMethod"].ToString(),
                        DepreciationStartDate = Convert.ToDateTime(reader["DepreciationStartDate"]),
                        NoOfDepreciationYears = Convert.ToInt32(reader["NoOfDepreciationYears"]),
                        DepreciationEndingDate = Convert.ToDateTime(reader["DepreciationEndingDate"]),
                        ReducingBalancePercentage = Convert.ToDecimal(reader["ReducingBalancePercentage"]),
                        FAGroup = reader["FAGroup"].ToString(),
                        CreatedDate = Convert.ToDateTime(reader["CreatedDate"]),
                        CreatedBy = reader["CreatedBy"].ToString()
                    });
                }
            }

            return list;
        }

        // Get by Id
        public FixedAsset GetById(Guid id)
        {
            FixedAsset asset = null;

            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"
                    SELECT fa.Id, fa.No, fa.SerialNo, fa.AssetName, fa.ResponsibleEmployee,
                           fa.FASubClassId, sc.Description AS FASubClassDescription,
                           fa.LocationId, loc.Description AS LocationDescription,
                           fa.BookValue, fa.IsInactive, fa.DepreciationMethod,
                           fa.DepreciationStartDate, fa.NoOfDepreciationYears,
                           fa.DepreciationEndingDate, fa.ReducingBalancePercentage,
                           fa.FAGroup, fa.CreatedDate, fa.CreatedBy
                    FROM FixedAsset fa
                    INNER JOIN FASubClass sc ON fa.FASubClassId = sc.Id
                    INNER JOIN FALocation loc ON fa.LocationId = loc.Id
                    WHERE fa.Id = @Id";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);

                conn.Open();
                var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    asset = new FixedAsset
                    {
                        Id = reader.GetGuid(reader.GetOrdinal("Id")),
                        No = reader["No"].ToString(),
                        SerialNo = reader["SerialNo"].ToString(),
                        AssetName = reader["AssetName"].ToString(),
                        ResponsibleEmployee = reader["ResponsibleEmployee"].ToString(),
                        FASubClassId = reader.GetGuid(reader.GetOrdinal("FASubClassId")),
                        FASubClassDescription = reader["FASubClassDescription"].ToString(),
                        LocationId = reader.GetGuid(reader.GetOrdinal("LocationId")),
                        LocationDescription = reader["LocationDescription"].ToString(),
                        BookValue = Convert.ToDecimal(reader["BookValue"]),
                        IsInactive = Convert.ToBoolean(reader["IsInactive"]),
                        DepreciationMethod = reader["DepreciationMethod"].ToString(),
                        DepreciationStartDate = Convert.ToDateTime(reader["DepreciationStartDate"]),
                        NoOfDepreciationYears = Convert.ToInt32(reader["NoOfDepreciationYears"]),
                        DepreciationEndingDate = Convert.ToDateTime(reader["DepreciationEndingDate"]),
                        ReducingBalancePercentage = Convert.ToDecimal(reader["ReducingBalancePercentage"]),
                        FAGroup = reader["FAGroup"].ToString(),
                        CreatedDate = Convert.ToDateTime(reader["CreatedDate"]),
                        CreatedBy = reader["CreatedBy"].ToString()
                    };
                }
            }

            return asset;
        }

        // Add
        public void Add(FixedAsset asset)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"
                    INSERT INTO FixedAsset
                    (Id, No, SerialNo, AssetName, ResponsibleEmployee, 
                     FASubClassId, LocationId, BookValue, IsInactive, 
                     DepreciationMethod, DepreciationStartDate, NoOfDepreciationYears, 
                     DepreciationEndingDate, ReducingBalancePercentage, FAGroup, 
                     CreatedDate, CreatedBy)
                    VALUES
                    (@Id, @No, @SerialNo, @AssetName, @ResponsibleEmployee,
                     @FASubClassId, @LocationId, @BookValue, @IsInactive,
                     @DepreciationMethod, @DepreciationStartDate, @NoOfDepreciationYears,
                     @DepreciationEndingDate, @ReducingBalancePercentage, @FAGroup,
                     @CreatedDate, @CreatedBy)";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", asset.Id);
                cmd.Parameters.AddWithValue("@No", asset.No ?? "");
                cmd.Parameters.AddWithValue("@SerialNo", asset.SerialNo ?? "");
                cmd.Parameters.AddWithValue("@AssetName", asset.AssetName ?? "");
                cmd.Parameters.AddWithValue("@ResponsibleEmployee", asset.ResponsibleEmployee ?? "");
                cmd.Parameters.AddWithValue("@FASubClassId", asset.FASubClassId);
                cmd.Parameters.AddWithValue("@LocationId", asset.LocationId);
                cmd.Parameters.AddWithValue("@BookValue", asset.BookValue);
                cmd.Parameters.AddWithValue("@IsInactive", asset.IsInactive);
                cmd.Parameters.AddWithValue("@DepreciationMethod", asset.DepreciationMethod ?? "");
                cmd.Parameters.AddWithValue("@DepreciationStartDate", asset.DepreciationStartDate);
                cmd.Parameters.AddWithValue("@NoOfDepreciationYears", asset.NoOfDepreciationYears);
                cmd.Parameters.AddWithValue("@DepreciationEndingDate", asset.DepreciationEndingDate);
                cmd.Parameters.AddWithValue("@ReducingBalancePercentage", asset.ReducingBalancePercentage);
                cmd.Parameters.AddWithValue("@FAGroup", asset.FAGroup ?? "");
                cmd.Parameters.AddWithValue("@CreatedDate", asset.CreatedDate);
                cmd.Parameters.AddWithValue("@CreatedBy", asset.CreatedBy ?? "System");

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // Update
        public bool Update(FixedAsset asset)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"
                    UPDATE FixedAsset
                    SET No=@No, SerialNo=@SerialNo, AssetName=@AssetName,
                        ResponsibleEmployee=@ResponsibleEmployee,
                        FASubClassId=@FASubClassId, LocationId=@LocationId,
                        BookValue=@BookValue, IsInactive=@IsInactive,
                        DepreciationMethod=@DepreciationMethod, 
                        DepreciationStartDate=@DepreciationStartDate,
                        NoOfDepreciationYears=@NoOfDepreciationYears,
                        DepreciationEndingDate=@DepreciationEndingDate,
                        ReducingBalancePercentage=@ReducingBalancePercentage,
                        FAGroup=@FAGroup
                    WHERE Id=@Id";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", asset.Id);
                cmd.Parameters.AddWithValue("@No", asset.No ?? "");
                cmd.Parameters.AddWithValue("@SerialNo", asset.SerialNo ?? "");
                cmd.Parameters.AddWithValue("@AssetName", asset.AssetName ?? "");
                cmd.Parameters.AddWithValue("@ResponsibleEmployee", asset.ResponsibleEmployee ?? "");
                cmd.Parameters.AddWithValue("@FASubClassId", asset.FASubClassId);
                cmd.Parameters.AddWithValue("@LocationId", asset.LocationId);
                cmd.Parameters.AddWithValue("@BookValue", asset.BookValue);
                cmd.Parameters.AddWithValue("@IsInactive", asset.IsInactive);
                cmd.Parameters.AddWithValue("@DepreciationMethod", asset.DepreciationMethod ?? "");
                cmd.Parameters.AddWithValue("@DepreciationStartDate", asset.DepreciationStartDate);
                cmd.Parameters.AddWithValue("@NoOfDepreciationYears", asset.NoOfDepreciationYears);
                cmd.Parameters.AddWithValue("@DepreciationEndingDate", asset.DepreciationEndingDate);
                cmd.Parameters.AddWithValue("@ReducingBalancePercentage", asset.ReducingBalancePercentage);
                cmd.Parameters.AddWithValue("@FAGroup", asset.FAGroup ?? "");

                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        // Delete
        public bool Delete(Guid id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"DELETE FROM FixedAsset WHERE Id=@Id";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);

                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }
    }
}
