using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.BackOfficeModule;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using TestApis.Models;

namespace TestApis.Controllers
{
    [RoutePrefix("api/Loansetups")]
    public class LoansetupsController : ApiController
    {
        private MasterController master = new MasterController();
        private readonly string _conn = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;
        public LoansetupsController()
        {
            master = new MasterController();
        }
        public class LoanSectorDTO
        {
            public int Id { get; set; }
            public string SectorCode { get; set; }
            public string SectorName { get; set; }
            public bool IsActive { get; set; }
        }
        public class LoanSubSectorDTO
        {
            public int Id { get; set; }
            public string SubSectorCode { get; set; }
            public string SubSectorName { get; set; }
            public bool IsActive { get; set; }
        }
       

        // GET: api/loan-subsectors
        [HttpGet]
        [Route("GetAllLoanSubSector")]
        public IHttpActionResult GetAllLoanSubSectorDTO()
        {
            var list = new List<LoanSubSectorDTO>();

            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(
                "SELECT Id, SubSectorCode, SubSectorName, IsActive FROM LoanSubSectors", conn))
            {
                conn.Open();
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        list.Add(new LoanSubSectorDTO
                        {
                            Id = rd.GetInt32(0),
                            SubSectorCode = rd.GetString(1),
                            SubSectorName = rd.GetString(2),
                            IsActive = rd.GetBoolean(3)
                        });
                    }
                }
            }

            return Ok(list);
        }

        // POST: api/loan-subsectors
        [HttpPost]
        [Route("")]
        public IHttpActionResult Create([FromBody]LoanSubSectorDTO dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.SubSectorCode))
                return BadRequest("Invalid subsector payload");

            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(
                @"INSERT INTO LoanSubSectors (SubSectorCode, SubSectorName, IsActive)
              VALUES (@SubSectorCode, @SubSectorName, @IsActive)", conn))
            {
                cmd.Parameters.AddWithValue("@SubSectorCode", dto.SubSectorCode.Trim());
                cmd.Parameters.AddWithValue("@SubSectorName", dto.SubSectorName.Trim());
                cmd.Parameters.AddWithValue("@IsActive", dto.IsActive);

                conn.Open();
                cmd.ExecuteNonQuery();
            }

            return Ok(new { success = true });
        }

        // PUT: api/loan-subsectors/{id}/toggle

        [HttpGet]
        [Route("GetAllloanSector")]
        public IHttpActionResult GetAllloanSector()
        {
            var sectors = new List<LoanSectorDTO>();

            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(
                "SELECT Id, SectorCode, SectorName, IsActive FROM LoanSectors", conn))
            {
                conn.Open();
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        sectors.Add(new LoanSectorDTO
                        {
                            Id = rd.GetInt32(0),
                            SectorCode = rd.GetString(1),
                            SectorName = rd.GetString(2),
                            IsActive = rd.GetBoolean(3)
                        });
                    }
                }
            }

            return Ok(sectors);
        }

        // POST: api/loan-sectors
        [HttpPost]
        [Route("LoanSector")]
        public IHttpActionResult Create([FromBody]LoanSectorDTO dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.SectorCode))
                return BadRequest("Invalid sector payload");

            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(
                @"INSERT INTO LoanSectors (SectorCode, SectorName, IsActive)
              VALUES (@SectorCode, @SectorName, @IsActive)", conn))
            {
                cmd.Parameters.AddWithValue("@SectorCode", dto.SectorCode.Trim());
                cmd.Parameters.AddWithValue("@SectorName", dto.SectorName.Trim());
                cmd.Parameters.AddWithValue("@IsActive", dto.IsActive);

                conn.Open();
                cmd.ExecuteNonQuery();
            }

            return Ok(new { success = true });
        }


        [HttpGet]
        [Route("GetLoanProducts")]
        public async Task<IHttpActionResult> GetLoanProducts([FromUri] string search = null, [FromUri] int pageIndex = 0, [FromUri] int pageSize = 20)
        {
            if (pageIndex < 0 || pageSize <= 0)
                return BadRequest("Invalid paging parameters.");

            try
            {
                var serviceHeader = master.GetServiceHeader();

                var loanProducts = await master._channelService.FindLoanProductsByFilterInPageAsync(search, pageIndex, pageSize, serviceHeader);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = loanProducts.PageCollection != null && loanProducts.PageCollection.Any()
                        ? $"{loanProducts.ItemsCount} loan products retrieved."
                        : "No loan products found.",
                    Data = loanProducts.PageCollection
                });
            }
            catch (Exception ex)
            {
                // log ex here (Serilog / AppInsights / ELK — non-negotiable)
                return InternalServerError(new Exception("Failed to retrieve loan products."));
            }
        }


        [HttpPost]
        [Route("AddLoanproducts")]
        public async Task<IHttpActionResult> AddLoanproducts([FromBody] LoanProductDTO loanProductDTO)
        {
            var serviceHeader = master.GetServiceHeader();

            if (loanProductDTO == null)
                return Json(new ApiResponse<object> { Success = false, Message = "Invalid data.", Data = null });

         
            var result = await master._channelService.AddLoanProductAsync(loanProductDTO, serviceHeader);


            return Json(new ApiResponse<object>
            {
                Success = true,
                Message = "loan ProductDTO created successfully."
            });


            return Json(new ApiResponse<object>
            {
                Success = false,
                Message = "Validation failed.",
                Data = loanProductDTO.ErrorMessages
            });
        }


        [HttpPut]
        [Route("UpdateLoanproducts")]
        public async Task<IHttpActionResult> UpdateLoanproducts([FromBody] LoanProductDTO loanProductDTO)
        {
            var serviceHeader = master.GetServiceHeader();

            if (loanProductDTO == null)
                return Json(new ApiResponse<object> { Success = false, Message = "Invalid data.", Data = null });



            loanProductDTO.ValidateAll();

            if (!loanProductDTO.HasErrors)
            {
                var result = await master._channelService.UpdateLoanProductAsync(loanProductDTO, serviceHeader);

                if (result == false)
                {
                    return Json(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "failed. " + loanProductDTO.ErrorMessages,
                        Data = null
                    });
                }

                return Json(new ApiResponse<object>
                {
                    Success = true,
                    Message = "loan ProductDTO updated successfully.",
                    Data = result
                });
            }

            return Json(new ApiResponse<object>
            {
                Success = false,
                Message = "Validation failed.",
                Data = loanProductDTO.ErrorMessages
            });
        }


        [HttpGet]
        [Route("GetLoanpurpose")]
        public async Task<IHttpActionResult> GetLoanpurpose()
        {
            try
            {
                var serviceHeader = master.GetServiceHeader();
                var loanPurposeDTOs = await master._channelService.FindLoanPurposesAsync(serviceHeader);

                return Json(new ApiResponse<object>
                {
                    Success = true,
                    Message = loanPurposeDTOs?.Count > 0 ? $"{loanPurposeDTOs.Count} loan Purpose retrieved." : "No loan Purpose  found.",
                    Data = loanPurposeDTOs
                });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while retrieving loan Purpose.",
                    Data = ex.Message
                });
            }
        }

        [HttpPost]
        [Route("Addloanpurpose")]
        public async Task<IHttpActionResult> Addloanpurpose([FromBody] LoanPurposeDTO loanPurposeDTO)
        {
            var serviceHeader = master.GetServiceHeader();

            if (loanPurposeDTO == null)
                return Json(new ApiResponse<object> { Success = false, Message = "Invalid data.", Data = null });



            loanPurposeDTO.ValidateAll();

            if (!loanPurposeDTO.HasErrors)
            {
                var result = await master._channelService.AddLoanPurposeAsync(loanPurposeDTO, serviceHeader);

                if (result.ErrorMessages != null)
                {
                    return Json(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "failed. " + result.ErrorMessages.ToString(),
                        Data = null
                    });
                }

                return Json(new ApiResponse<object>
                {
                    Success = true,
                    Message = "loan Purpose created successfully.",
                    Data = result
                });
            }

            return Json(new ApiResponse<object>
            {
                Success = false,
                Message = "Validation failed.",
                Data = loanPurposeDTO.ErrorMessages
            });
        }


        [HttpPut]
        [Route("UpdateloanPurpose")]
        public async Task<IHttpActionResult> UpdateloanPurpose([FromBody] LoanPurposeDTO loanPurposeDTO)
        {
            var serviceHeader = master.GetServiceHeader();

            if (loanPurposeDTO == null)
                return Json(new ApiResponse<object> { Success = false, Message = "Invalid data.", Data = null });



            loanPurposeDTO.ValidateAll();

            if (!loanPurposeDTO.HasErrors)
            {
                var result = await master._channelService.UpdateLoanPurposeAsync(loanPurposeDTO, serviceHeader);

                if (result == false)
                {
                    return Json(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "failed. " + loanPurposeDTO.ErrorMessages,
                        Data = null
                    });
                }

                return Json(new ApiResponse<object>
                {
                    Success = true,
                    Message = "loan loan Purpose updated successfully.",
                    Data = result
                });
            }

            return Json(new ApiResponse<object>
            {
                Success = false,
                Message = "Validation failed.",
                Data = loanPurposeDTO.ErrorMessages
            });
        }


        [HttpGet]
        [Route("GetLoanRemarks")]
        public async Task<IHttpActionResult> GetLoanRemarks()
        {
            try
            {
                var serviceHeader = master.GetServiceHeader();

                var pageCollectionInfo = await master._channelService
                    .FindLoaningRemarksByFilterInPageAsync("", pageIndex: 0, pageSize: 50, serviceHeader);

                var items = pageCollectionInfo?.PageCollection ?? new List<LoaningRemarkDTO>();
                var count = items.Count;

                return Json(new ApiResponse<object>
                {
                    Success = true,
                    Message = count > 0
                        ? $"{count} loan remarks retrieved."
                        : "No loan remarks found.",
                    Data = new
                    {
                        pageCollectionInfo.PageIndex,
                        pageCollectionInfo.PageSize,
                        pageCollectionInfo.ItemsCount,
                        Items = items,
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while retrieving loan remarks.",
                    Data = ex.Message
                });
            }
        }

        [HttpPost]
        [Route("AddLoanRemarks")]
        public async Task<IHttpActionResult> AddLoanRemarks([FromBody] LoaningRemarkDTO loaningRemarkDTO)
        {
            var serviceHeader = master.GetServiceHeader();

            if (loaningRemarkDTO == null)
                return Json(new ApiResponse<object> { Success = false, Message = "Invalid data.", Data = null });



            loaningRemarkDTO.ValidateAll();

            if (!loaningRemarkDTO.HasErrors)
            {
                var result = await master._channelService.AddLoaningRemarkAsync(loaningRemarkDTO, serviceHeader);

                if (result.ErrorMessages != null)
                {
                    return Json(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "failed. " + result.ErrorMessages.ToString(),
                        Data = null
                    });
                }

                return Json(new ApiResponse<object>
                {
                    Success = true,
                    Message = "loaning Remark created successfully.",
                    Data = result
                });
            }

            return Json(new ApiResponse<object>
            {
                Success = false,
                Message = "Validation failed.",
                Data = loaningRemarkDTO.ErrorMessages
            });
        }

    }
}