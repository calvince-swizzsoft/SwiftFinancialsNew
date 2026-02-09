using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.RegistryModule;
using DistributedServices.MainBoundedContext.Identity;
using Infrastructure.Crosscutting.Framework.Utils;
using Microsoft.AspNet.Identity;
using SwiftFinancials.Presentation.Infrastructure.Services;
using SwiftFinancials.Presentation.Infrastructure.Util;
using TestApis.Controllers;
using TestApis.Models;

[RoutePrefix("api/CustomerApi")]
public class CustomerApiController : ApiController
{
    private readonly IChannelService _channelService;
    private readonly ApplicationUserManager _userManager;
    private readonly MasterController _master;
    private readonly string _connectionString;

    public CustomerApiController(
        IChannelService channelService,
        ApplicationUserManager userManager,
        MasterController master)
    {
        _channelService = channelService ?? throw new ArgumentNullException(nameof(channelService));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _master = master ?? throw new ArgumentNullException(nameof(master));

        _connectionString = System.Configuration.ConfigurationManager
            .ConnectionStrings["SwiftFin_Dev"].ConnectionString;
    }
    public CustomerApiController()
    {
        _master = new MasterController();
        _connectionString = System.Configuration.ConfigurationManager
            .ConnectionStrings["SwiftFin_Dev"].ConnectionString;
    }

    private ServiceHeader GetServiceHeader() => _master.GetServiceHeader();

    // ===========================================================
    // 1. QUERY CUSTOMERS
    // ===========================================================
    [HttpGet]
    [Route("")]
    public async Task<IHttpActionResult> GetCustomers(string search = null, int page = 0, int pageSize = 50)
    {
        var serviceHeader = _master.GetServiceHeader();

        var result = await _master._channelService.FindCustomersInPageAsync(page, pageSize, serviceHeader);
        var sortedData = result.PageCollection
                   .OrderByDescending(loanCase => loanCase.CreatedDate)
                   .ToList();
        return Ok(sortedData);
    }

    [HttpGet]
    [Route("{id:guid}")]
    public async Task<IHttpActionResult> GetCustomer(Guid id)
    {
        var dto = await _channelService.FindCustomerAsync(id, GetServiceHeader());
        return dto == null ? (IHttpActionResult)NotFound() : Ok(dto);
    }

    [HttpGet]
    [Route("by-id-number")]
    public async Task<IHttpActionResult> GetByIdentity(string idNumber, bool exact = true)
    {
        var list = await _channelService
            .FindCustomersByIdentityCardNumberAsync(idNumber, exact, GetServiceHeader());

        return Ok(list);
    }

    // ===========================================================
    // 2. CREATE CUSTOMER
    // ===========================================================
    [HttpPost]
    [Route("create")]
    public async Task<IHttpActionResult> CreateCustomer([FromBody] CustomerBindingModel model)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var dto = model.MapTo<CustomerDTO>();
            var header = GetServiceHeader();

            // 1. Resolve branch
            var branch = await _master._channelService.FindBranchAsync(dto.BranchId, header);
            if (branch == null)
                return Content(HttpStatusCode.BadRequest, "Branch information not found.");

            // 2. Resolve company
            var company = await _master._channelService.FindCompanyAsync(branch.CompanyId, header);
            if (company == null)
                return Content(HttpStatusCode.BadRequest, "Company information not found.");

            // 3. Resolve company products
            var attachedProducts = await _master._channelService.FindAttachedProductsByCompanyIdAsync(company.Id, header);
            var mandatoryDebitTypes = await _master._channelService.FindDebitTypesByCompanyIdAsync(company.Id, header);

            if (attachedProducts?.InvestmentProductCollection == null ||
                attachedProducts.SavingsProductCollection == null ||
                mandatoryDebitTypes == null)
            {
                return Content(HttpStatusCode.BadRequest,
                    "Company does not contain mandatory products. Setup is required.");
            }

            // 4. Build ProductCollectionInfo from company setup
            var mandatoryProducts = new ProductCollectionInfo
            {
                InvestmentProductCollection = attachedProducts.InvestmentProductCollection.ToList(),
                SavingsProductCollection = attachedProducts.SavingsProductCollection.ToList()
            };

            var debitTypeDTOs = mandatoryDebitTypes.ToList();

            // 5. Create customer
            var result = await _master._channelService.AddCustomerAsync(
                dto,
                debitTypeDTOs,
                attachedProducts.InvestmentProductCollection.ToList(),
                attachedProducts.SavingsProductCollection.ToList(),
                mandatoryProducts,
                1,
                header
            );

            //var customerDocument = new CustomerDocument
            //{
            //    IDCardBackPhoto = dto.IdentityCardBackSideBuffer,
            //    SignaturePhoto = dto.SignatureBuffer,
            //    IDCardFrontPhoto = dto.IdentityCardFrontSideBuffer,
            //    PassportPhoto = dto.PassportBuffer
            //};

            ////await SaveDocumentAsync(customerDocument);

            //if (!string.IsNullOrWhiteSpace(result.ErrorMessageResult))
            //    return Content(HttpStatusCode.BadRequest, result.ErrorMessageResult);

            return Ok(result);
        }
        catch (SqlException ex)
        {
            // Database-layer volatility — fail fast with telemetry hooks
            return Content(HttpStatusCode.InternalServerError,
                $"Database failure encountered. Details: {ex.Message}");
        }
        catch (TimeoutException ex)
        {
            return Content(HttpStatusCode.RequestTimeout,
                $"Operation timed out. Details: {ex.Message}");
        }
        catch (Exception ex)
        {
            // Top-level exception guardrail — keeps API deterministic
            return Content(HttpStatusCode.InternalServerError,
                $"Unexpected server error. Details: {ex.Message}");
        }
    }

    // Save Documents ......................
    private async Task SaveDocumentAsync(CustomerDocument document)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            var query = "INSERT INTO swiftFin_SpecimenCapture (Id, CustomerId, PassportPhoto, SignaturePhoto, IDCardFrontPhoto, IDCardBackPhoto, CreatedDate) " +
                        "VALUES (@Id, @CustomerId, @PassportPhoto, @SignaturePhoto, @IDCardFrontPhoto, @IDCardBackPhoto, @CreatedDate)";

            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Id", document.Id);
                command.Parameters.AddWithValue("@CustomerId", document.CustomerId);
                command.Parameters.AddWithValue("@PassportPhoto", document.PassportPhoto ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@SignaturePhoto", document.SignaturePhoto ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@IDCardFrontPhoto", document.IDCardFrontPhoto ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@IDCardBackPhoto", document.IDCardBackPhoto ?? (object)DBNull.Value);

                command.Parameters.AddWithValue("@CreatedDate", document.CreatedDate);

                // Execute the query
                await command.ExecuteNonQueryAsync();
            }
        }
    }
    // ===========================================================
    // 3. UPDATE CUSTOMER
    // ===========================================================
    [HttpPut]
    [Route("{id:guid}")]
    public async Task<IHttpActionResult> UpdateCustomer(Guid id, [FromBody] CustomerBindingModel model)
    {
        model.Id = id;
        var dto = model.MapTo<CustomerDTO>();

        var success = await _channelService.UpdateCustomerAsync(dto, GetServiceHeader());
        return success ? Ok(new { updated = true }) : (IHttpActionResult)BadRequest("Update failed");
    }

    // ===========================================================
    // 4. DOCUMENTS
    // ===========================================================
    [HttpPost]
    [Route("{customerId:guid}/documents/upload")]
    public async Task<IHttpActionResult> UploadDocuments(Guid customerId)
    {
        if (!Request.Content.IsMimeMultipartContent())
            return BadRequest("Must be multipart/form-data");

        var provider = await Request.Content.ReadAsMultipartAsync();

        var passport = await GetFileAsBytes(provider, "passportPhoto");
        var signature = await GetFileAsBytes(provider, "signaturePhoto");
        var idFront = await GetFileAsBytes(provider, "idCardFrontPhoto");
        var idBack = await GetFileAsBytes(provider, "idCardBackPhoto");

        await SaveDocumentAsync(customerId, passport, signature, idFront, idBack);

        return Ok(new { uploaded = true });
    }

    [HttpGet]
    [Route("{customerId:guid}/documents")]
    public async Task<IHttpActionResult> GetDocuments(Guid customerId)
    {
        var docs = await GetDocumentsAsync(customerId);
        return Ok(docs);
    }

    // ===========================================================
    // 5. LOOKUPS
    // ===========================================================
    [HttpGet]
    [Route("lookups")]
    public async Task<IHttpActionResult> GetAllLookups()
    {
        return Ok(new
        {
            customerTypes = new[] { "Individual", "Partnership", "Corporation", "MicroCredit" },
            genders = GetGenderSelectList(),
            debitTypes = await _channelService.FindDebitTypesAsync(GetServiceHeader()),
            creditTypes = await _channelService.FindCreditTypesAsync(GetServiceHeader()),
            investmentProducts = await _channelService.FindInvestmentProductsAsync(GetServiceHeader()),
            savingsProducts = await _channelService.FindSavingsProductsAsync(GetServiceHeader())
        });
    }

    // ===========================================================
    // PRIVATE HELPERS
    // ===========================================================
    private async Task<byte[]> GetFileAsBytes(MultipartMemoryStreamProvider provider, string key)
    {
        var file = provider.Contents.FirstOrDefault(c =>
            c.Headers.ContentDisposition.Name?.Trim('"') == key);

        return file != null ? await file.ReadAsByteArrayAsync() : null;
    }

    private object GetGenderSelectList()
    {
        return new[]
        {
            new { id = 0, text = "Male" },
            new { id = 1, text = "Female" }
        };
    }

    private async Task<List<CustomerDocument>> GetDocumentsAsync(Guid id)
    {
        var docs = new List<CustomerDocument>();
        using (var conn = new SqlConnection(_connectionString))
        {
            await conn.OpenAsync();

            var cmd = new SqlCommand(@"
                SELECT PassportPhoto, SignaturePhoto, IDCardFrontPhoto, IDCardBackPhoto
                FROM swiftFin_SpecimenCapture
                WHERE CustomerId=@Id", conn);

            cmd.Parameters.AddWithValue("@Id", id);

            using (var r = await cmd.ExecuteReaderAsync())
            {
                while (await r.ReadAsync())
                {
                    docs.Add(new CustomerDocument
                    {
                        PassportPhoto = r.IsDBNull(0) ? null : (byte[])r[0],
                        SignaturePhoto = r.IsDBNull(1) ? null : (byte[])r[1],
                        IDCardFrontPhoto = r.IsDBNull(2) ? null : (byte[])r[2],
                        IDCardBackPhoto = r.IsDBNull(3) ? null : (byte[])r[3]
                    });
                }
            }
        }
        return docs;
    }

    private async Task SaveDocumentAsync(Guid custId, byte[] pass, byte[] sign, byte[] front, byte[] back)
    {
        using (var conn = new SqlConnection(_connectionString))
        {
            await conn.OpenAsync();

            var cmd = new SqlCommand(@"
                INSERT INTO swiftFin_SpecimenCapture 
                (Id, CustomerId, PassportPhoto, SignaturePhoto, IDCardFrontPhoto, IDCardBackPhoto, CreatedDate)
                VALUES (@Id, @CustId, @Pass, @Sign, @Front, @Back, GETDATE())", conn);

            cmd.Parameters.AddWithValue("@Id", Guid.NewGuid());
            cmd.Parameters.AddWithValue("@CustId", custId);
            cmd.Parameters.AddWithValue("@Pass", (object)pass ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Sign", (object)sign ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Front", (object)front ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Back", (object)back ?? DBNull.Value);

            await cmd.ExecuteNonQueryAsync();
        }
    }




}
