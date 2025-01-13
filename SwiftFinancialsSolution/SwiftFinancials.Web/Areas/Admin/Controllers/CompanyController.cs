using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AdministrationModule;
using SwiftFinancials.Web.Attributes;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using SwiftFinancials.Presentation.Infrastructure.Util;
using Application.MainBoundedContext.DTO.AccountsModule;
using System.Collections.ObjectModel;
using System.Web;
using System.Configuration;
using SwiftFinancials.Web.Areas.Admin.DocumentsModel;
using System.Data.SqlClient;
using System.IO;

namespace SwiftFinancials.Web.Areas.Admin.Controllers
{
    [RoleBasedAccessControl]
    public class CompanyController : MasterController
    {

        private readonly string _connectionString;
        private HttpPostedFileBase uploadedPassportPhoto;

        public CompanyController()
        {
            // Get connection string from Web.config
            _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;
        }
        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public async Task<JsonResult> Index(JQueryDataTablesModel jQueryDataTablesModel)
        {
            int totalRecordCount = 0;

            int searchRecordCount = 0;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;

            var pageCollectionInfo = await _channelService.FindCompaniesByFilterInPageAsync(jQueryDataTablesModel.sSearch, pageIndex, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;
                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(company => company.CreatedDate).ToList();
                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<CompanyDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var CompanyDTO = await _channelService.FindCompanyAsync(id, GetServiceHeader());

            // Retrieve all available debit, investment, and savings products
            var debitTypes = await _channelService.FindDebitTypesAsync(GetServiceHeader());
            var creditTypes = await _channelService.FindCreditTypesAsync(GetServiceHeader());
            var allInvestmentProducts = await _channelService.FindInvestmentProductsAsync(GetServiceHeader());
            var allSavingsProducts = await _channelService.FindMandatorySavingsProductsAsync(true, GetServiceHeader());

            // Retrieve company and its linked products
            var debitTypeDTOs = await _channelService.FindDebitTypesByCompanyIdAsync(CompanyDTO.Id, GetServiceHeader());
            var linkedProducts = await _channelService.FindAttachedProductsByCompanyIdAsync(CompanyDTO.Id, GetServiceHeader());

            // Pass the full list and linked items to ViewBag
            ViewBag.SelectedDebitTypes = debitTypeDTOs.Select(d => d.Id).ToList();
            if (linkedProducts != null)
            {
                ViewBag.SelectedInvestmentProducts = linkedProducts.InvestmentProductCollection.Select(i => i.Id).ToList();
                ViewBag.SelectedSavingsProducts = linkedProducts.SavingsProductCollection.Select(s => s.Id).ToList();
            }
            var debitType = await _channelService.FindDebitTypesAsync(GetServiceHeader());
            var creditType = await _channelService.FindCreditTypesAsync(GetServiceHeader());
            var allInvestmentProduct = await _channelService.FindInvestmentProductsAsync(GetServiceHeader());
            var allSavingsProduct = await _channelService.FindSavingsProductsAsync(GetServiceHeader());
            // Pass the full list and linked items to ViewBag
            ViewBag.SelectedDebitTypes = debitTypes;
            ViewBag.SelectedInvestmentProducts = allInvestmentProducts;
            ViewBag.SelectedSavingsProducts = allSavingsProducts;

            // Pass all products to the view
            ViewBag.allInvestments = allInvestmentProducts;
            ViewBag.allSavings = allSavingsProducts;
            ViewBag.debit = debitTypes;
            return View(CompanyDTO.MapTo<CompanyBindingModel>());
        }


        private ActionResult NotFound()
        {
            throw new NotImplementedException();
        }


        // Get Documents ...........................
        private async Task<List<Document>> GetDocumentsAsync(Guid id)
        {
            var documents = new List<Document>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var query = "SELECT PassportPhoto, SignaturePhoto, IDCardFrontPhoto, IDCardBackPhoto FROM swiftFin_SpecimenCapture WHERE CompanyId = @CompanyId";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@CompanyId", id);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            documents.Add(new Document
                            {
                                PassportPhoto = reader.IsDBNull(0) ? null : (byte[])reader[0],
                                SignaturePhoto = reader.IsDBNull(1) ? null : (byte[])reader[1],
                                IDCardFrontPhoto = reader.IsDBNull(2) ? null : (byte[])reader[2],
                                IDCardBackPhoto = reader.IsDBNull(3) ? null : (byte[])reader[3]
                            });
                        }
                    }
                }
            }

            return documents;
        }

        private Document ProcessDocumentUpload(Guid CompanyId, HttpPostedFileBase signaturePhoto, HttpPostedFileBase idCardFrontPhoto, HttpPostedFileBase idCardBackPhoto, string passportPhotoDataUrl)
        {
            var document = new Document
            {
                Id = Guid.NewGuid(),
                CompanyId = CompanyId
            };

            // Process the passport photo from the data URL
            if (!string.IsNullOrEmpty(passportPhotoDataUrl))
            {
                var base64Data = passportPhotoDataUrl.Split(',')[1];
                document.PassportPhoto = Convert.FromBase64String(base64Data);
            }

            // Process other uploaded files
            document.SignaturePhoto = ConvertFileToByteArray(signaturePhoto);
            document.IDCardFrontPhoto = ConvertFileToByteArray(idCardFrontPhoto);
            document.IDCardBackPhoto = ConvertFileToByteArray(idCardBackPhoto);

            return document;
        }

        private byte[] ConvertFileToByteArray(HttpPostedFileBase file)
        {
            if (file != null && file.ContentLength > 0)
            {
                using (var memoryStream = new MemoryStream())
                {
                    file.InputStream.CopyTo(memoryStream);
                    return memoryStream.ToArray();
                }
            }
            return null;
        }



        // Save Documents ......................
        private async Task SaveDocumentAsync(Document document)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var query = "INSERT INTO swiftFin_SpecimenCapture (Id, CompanyId, CreatedDate) " +
                            "VALUES (@Id, @CompanyId, @CreatedDate)";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", document.Id);
                    command.Parameters.AddWithValue("@CompanyId", document.CompanyId);
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

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();
            var debitTypes = await _channelService.FindMandatoryDebitTypesAsync(true, GetServiceHeader());
            var creditTypes = await _channelService.FindCreditTypesAsync(GetServiceHeader());
            var investmentProducts = await _channelService.FindMandatoryInvestmentProductsAsync(true, GetServiceHeader());
            var savingsProducts = await _channelService.FindMandatorySavingsProductsAsync(true, GetServiceHeader());


            var savingsProductDTOs = await _channelService.FindSavingsProductsAsync(GetServiceHeader());
            var investment = await _channelService.FindInvestmentProductsAsync(GetServiceHeader());
            var debitypes = await _channelService.FindDebitTypesAsync(GetServiceHeader());
            ViewBag.investment = investment;
            ViewBag.savings = savingsProductDTOs;
            ViewBag.debit = debitypes;
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(CompanyBindingModel companyBindingModel, string[] debittypes, string[] savingsproducts, string[] investmentproducts, HttpPostedFileBase Companylogo)
        {
            //cheat
            var mandatoryInvestmentProducts = new List<InvestmentProductDTO>();
            var mandatorySavingsProducts = new List<SavingsProductDTO>();
            var mandatoryDebitTypes = new ObservableCollection<DebitTypeDTO>();
            var mandatoryProducts = new ProductCollectionInfo();

            SavingsProductDTO j = new SavingsProductDTO();
            foreach (var k in savingsproducts)
            {
                j.Id = Guid.Parse(k);
                mandatorySavingsProducts.Add(j);
            }
            mandatoryProducts.SavingsProductCollection = mandatorySavingsProducts;



            InvestmentProductDTO investmentProductDTO = new InvestmentProductDTO();
            foreach (var invest in investmentproducts)
            {
                investmentProductDTO.Id = Guid.Parse(invest);
                mandatoryInvestmentProducts.Add(investmentProductDTO);
            }
            mandatoryProducts.InvestmentProductCollection = mandatoryInvestmentProducts;


            DebitTypeDTO debitTypeDTO = new DebitTypeDTO();
            foreach (var debit in debittypes)
            {
                debitTypeDTO.Id = Guid.Parse(debit);
                mandatoryDebitTypes.Add(debitTypeDTO);
            }


            companyBindingModel.ValidateAll();
            companyBindingModel.RecoveryPriority = "DirectDebits";
            if (!companyBindingModel.HasErrors)
            {
                await ServeNavigationMenus();

                var companies = await _channelService.AddCompanyAsync(companyBindingModel.MapTo<CompanyDTO>(), GetServiceHeader());
                await _channelService.UpdateAttachedProductsByCompanyIdAsync(companies.Id, mandatoryProducts, GetServiceHeader());

                await _channelService.UpdateDebitTypesByCompanyIdAsync(companies.Id, mandatoryDebitTypes, GetServiceHeader());


                TempData["Successmasssage"] = "Company Registered Successfully";
                var savingsProductDTOs = await _channelService.FindSavingsProductsAsync(GetServiceHeader());
                var investment = await _channelService.FindInvestmentProductsAsync(GetServiceHeader());
                var debitypes = await _channelService.FindDebitTypesAsync(GetServiceHeader());
                ViewBag.investment = investment;
                ViewBag.savings = savingsProductDTOs;
                ViewBag.debit = debitypes;
                return View("Index");

            }
            else
            {
                var errorMessages = companyBindingModel.ErrorMessages;
                var savingsProductDTOs = await _channelService.FindSavingsProductsAsync(GetServiceHeader());
                var investment = await _channelService.FindInvestmentProductsAsync(GetServiceHeader());
                var debitypes = await _channelService.FindDebitTypesAsync(GetServiceHeader());
                ViewBag.investment = investment;
                ViewBag.savings = savingsProductDTOs;
                ViewBag.debit = debitypes;
                return View(companyBindingModel);
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            // Retrieve all available debit, investment, and savings products
            var debitTypes = await _channelService.FindDebitTypesAsync(GetServiceHeader());
            var creditTypes = await _channelService.FindCreditTypesAsync(GetServiceHeader());
            var allInvestmentProducts = await _channelService.FindInvestmentProductsAsync(GetServiceHeader());
            var allSavingsProducts = await _channelService.FindSavingsProductsAsync(GetServiceHeader());

            // Retrieve company and its linked products
            var CompanyDTO = await _channelService.FindCompanyAsync(id, GetServiceHeader());
            var debitTypeDTOs = await _channelService.FindDebitTypesByCompanyIdAsync(CompanyDTO.Id, GetServiceHeader());
            var linkedProducts = await _channelService.FindAttachedProductsByCompanyIdAsync(CompanyDTO.Id, GetServiceHeader());
            if (linkedProducts != null)
            {
         

                // Pass the full list and linked items to ViewBag
                ViewBag.SelectedDebitTypes = debitTypeDTOs.Select(d => d.Id).ToList();
                ViewBag.SelectedInvestmentProducts = linkedProducts.InvestmentProductCollection.Select(i => i.Id).ToList();
                ViewBag.SelectedSavingsProducts = linkedProducts.SavingsProductCollection.Select(s => s.Id).ToList();

                // Pass all products to the view
                ViewBag.allInvestments = allInvestmentProducts;
                ViewBag.allSavings = allSavingsProducts;
                ViewBag.debit = debitTypes;
            }
            else
            {
                var debitType = await _channelService.FindDebitTypesAsync(GetServiceHeader());
                var creditType = await _channelService.FindCreditTypesAsync(GetServiceHeader());
                var allInvestmentProduct = await _channelService.FindInvestmentProductsAsync(GetServiceHeader());
                var allSavingsProduct = await _channelService.FindSavingsProductsAsync(GetServiceHeader());
                // Pass the full list and linked items to ViewBag
                ViewBag.SelectedDebitTypes = debitTypes;
                ViewBag.SelectedInvestmentProducts = allInvestmentProducts;
                ViewBag.SelectedSavingsProducts = allSavingsProducts;
                
                // Pass all products to the view
                ViewBag.allInvestments = allInvestmentProducts;
                ViewBag.allSavings = allSavingsProducts;
                ViewBag.debit = debitTypes;

            }
            return View(CompanyDTO.MapTo<CompanyBindingModel>());
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, CompanyBindingModel companyBindingModel, string[] debittypes, string[] savingsproducts, string[] investmentproducts)
        {
            //cheat
            var mandatoryInvestmentProducts = new List<InvestmentProductDTO>();
            var mandatorySavingsProducts = new List<SavingsProductDTO>();
            var mandatoryDebitTypes = new ObservableCollection<DebitTypeDTO>();
            var mandatoryProducts = new ProductCollectionInfo();

            SavingsProductDTO j = new SavingsProductDTO();
            foreach (var k in savingsproducts)
            {
                j.Id = Guid.Parse(k);
                mandatorySavingsProducts.Add(j);
            }
            mandatoryProducts.SavingsProductCollection = mandatorySavingsProducts;



            InvestmentProductDTO investmentProductDTO = new InvestmentProductDTO();
            foreach (var invest in investmentproducts)
            {
                investmentProductDTO.Id = Guid.Parse(invest);
                mandatoryInvestmentProducts.Add(investmentProductDTO);
            }
            mandatoryProducts.InvestmentProductCollection = mandatoryInvestmentProducts;


            DebitTypeDTO debitTypeDTO = new DebitTypeDTO();
            foreach (var debit in debittypes)
            {
                debitTypeDTO.Id = Guid.Parse(debit);
                mandatoryDebitTypes.Add(debitTypeDTO);
            }

            if (ModelState.IsValid)
            {
                await _channelService.UpdateCompanyAsync(companyBindingModel.MapTo<CompanyDTO>(), GetServiceHeader());
                await _channelService.UpdateAttachedProductsByCompanyIdAsync(id, mandatoryProducts, GetServiceHeader());

                await _channelService.UpdateDebitTypesByCompanyIdAsync(id, mandatoryDebitTypes, GetServiceHeader());

                TempData["Success"] = "Company Successfully Edited";

                return RedirectToAction("Index");
            }
            else
            {
                return View(companyBindingModel);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetCompaniesAsync()
        {
            var companiesDTOs = await _channelService.FindCompaniesAsync(GetServiceHeader());

            return Json(companiesDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}