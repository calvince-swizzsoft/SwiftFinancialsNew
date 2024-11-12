using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.RegistryModule;
using Infrastructure.Crosscutting.Framework.Adapter;
using Infrastructure.Crosscutting.Framework.Utils;
using Microsoft.AspNet.Identity;
using SwiftFinancials.Presentation.Infrastructure.Services;
using SwiftFinancials.Presentation.Infrastructure.Util;
using SwiftFinancials.Web.Areas.Registry.DocumentsModel;
using SwiftFinancials.Web.Attributes;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.Registry.Controllers
{
    [RoleBasedAccessControl]
    public class CustomerController : MasterController
    {
        private readonly string _connectionString;
        private HttpPostedFileBase uploadedPassportPhoto;

        public CustomerController()
        {
            // Get connection string from Web.config
            _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;
        }

        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();

            ViewBag.recordStatus = GetRecordStatusSelectList(string.Empty);
            ViewBag.customerFilter = GetCustomerFilterSelectList(string.Empty);

            return View();
        }

        [HttpPost]
        public async Task<JsonResult> Index(JQueryDataTablesModel jQueryDataTablesModel, int? recordStatus, int? customerFilter)
        {
            int totalRecordCount = 0;

            int searchRecordCount = 0;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;

            var pageCollectionInfo = new PageCollectionInfo<CustomerDTO>();

            if (recordStatus != null && customerFilter != null)
            {
                pageCollectionInfo = await _channelService.FindCustomersByRecordStatusAndFilterInPageAsync((int)recordStatus, jQueryDataTablesModel.sSearch, (int)customerFilter, pageIndex, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());
            }
            else if (recordStatus == null && customerFilter != null)
            {
                pageCollectionInfo = await _channelService.FindCustomersByFilterInPageAsync(jQueryDataTablesModel.sSearch, (int)customerFilter, pageIndex, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());
            }
            else if (recordStatus != null && customerFilter == null)
            {
                pageCollectionInfo = await _channelService.FindCustomersByRecordStatusAndFilterInPageAsync((int)recordStatus, jQueryDataTablesModel.sSearch, (int)CustomerFilter.FirstName, pageIndex, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());
            }
            else
            {
                pageCollectionInfo = await _channelService.FindCustomersByFilterInPageAsync(jQueryDataTablesModel.sSearch, (int)CustomerFilter.NonIndividual_Description, pageIndex, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());
            }

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;
                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(postingPeriod => postingPeriod.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<CustomerDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();
            ViewBag.CustomerTypeSelectList = GetCustomerTypeSelectList(string.Empty);
            ViewBag.IndividualTypeSelectList = GetIndividualTypeSelectList(string.Empty);
            ViewBag.IdentityCardSelectList = GetIdentityCardTypeSelectList(string.Empty);
            ViewBag.SalutationSelectList = GetSalutationSelectList(string.Empty);
            ViewBag.GenderSelectList = GetGenderSelectList(string.Empty);
            ViewBag.MaritalStatusSelectList = GetMaritalStatusSelectList(string.Empty);
            ViewBag.IndividualNationalitySelectList = GetNationalitySelectList(string.Empty);
            ViewBag.IndividualEmploymentTermsOfServiceSelectList = GetTermsOfServiceSelectList(string.Empty);
            ViewBag.IndividualClassificationSelectList = GetCustomerClassificationSelectList(string.Empty);

            ViewBag.PartnershipRelationships = GetPartnershipRelationshipsSelectList(string.Empty);
            var debitTypes = await _channelService.FindDebitTypesAsync(GetServiceHeader());
            var creditTypes = await _channelService.FindCreditTypesAsync(GetServiceHeader());
            var investmentProducts = await _channelService.FindInvestmentProductsAsync(GetServiceHeader());
            var savingsProducts = await _channelService.FindSavingsProductsAsync(GetServiceHeader());
            var savingsProductDTOs = await _channelService.FindSavingsProductsAsync(GetServiceHeader());
            var investment = await _channelService.FindInvestmentProductsAsync(GetServiceHeader());
            var debitypes = await _channelService.FindDebitTypesAsync(GetServiceHeader());
            ViewBag.investment = investment;
            ViewBag.savings = savingsProductDTOs;
            ViewBag.debit = debitypes;
            ViewBag.creditTypes = creditTypes;
            var customerDTO = await _channelService.FindCustomerAsync(id, GetServiceHeader());

            return View(customerDTO.MapTo<CustomerBindingModel>());

            //var documents = await GetDocumentsAsync(id);

            //if (documents == null || documents.Count == 0)
            //{
            //    return View();
            //}

            //return View(documents);
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
                var query = "SELECT PassportPhoto, SignaturePhoto, IDCardFrontPhoto, IDCardBackPhoto FROM swiftFin_SpecimenCapture WHERE CustomerId = @CustomerId";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@CustomerId", id);

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



        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();


            ViewBag.CustomerTypeSelectList = GetCustomerTypeSelectList(string.Empty);
            ViewBag.IndividualTypeSelectList = GetIndividualTypeSelectList(string.Empty);
            ViewBag.IdentityCardSelectList = GetIdentityCardTypeSelectList(string.Empty);
            ViewBag.SalutationSelectList = GetSalutationSelectList(string.Empty);
            ViewBag.GenderSelectList = GetGenderSelectList(string.Empty);
            ViewBag.MaritalStatusSelectList = GetMaritalStatusSelectList(string.Empty);
            ViewBag.IndividualNationalitySelectList = GetNationalitySelectList(string.Empty);
            ViewBag.IndividualEmploymentTermsOfServiceSelectList = GetTermsOfServiceSelectList(string.Empty);
            ViewBag.IndividualClassificationSelectList = GetCustomerClassificationSelectList(string.Empty);

            ViewBag.PartnershipRelationships = GetPartnershipRelationshipsSelectList(string.Empty);

            var debitTypes = await _channelService.FindDebitTypesAsync(GetServiceHeader());
            var creditTypes = await _channelService.FindCreditTypesAsync(GetServiceHeader());
            var investmentProducts = await _channelService.FindInvestmentProductsAsync(GetServiceHeader());
            var savingsProducts = await _channelService.FindSavingsProductsAsync(GetServiceHeader());
            var savingsProductDTOs = await _channelService.FindSavingsProductsAsync(GetServiceHeader());
            var investment = await _channelService.FindInvestmentProductsAsync(GetServiceHeader());
            var debitypes = await _channelService.FindDebitTypesAsync(GetServiceHeader());
            ViewBag.investment = investment;
            ViewBag.savings = savingsProductDTOs;
            ViewBag.debit = debitypes;
            ViewBag.creditTypes = creditTypes;



            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Query to get the next reference by incrementing the current max
                var getNextReferenceQuery = string.Format("SELECT ISNULL(MAX(Reference1), 0) + 1 AS NextReference FROM {0}Customers", DefaultSettings.Instance.TablePrefix);

                int newReference;

                using (var getCommand = new SqlCommand(getNextReferenceQuery, connection))
                {
                    var result = await getCommand.ExecuteScalarAsync();
                    newReference = (result != DBNull.Value) ? Convert.ToInt32(result) : 1; // Start at 1 if there are no rows
                }

            }
            return View();
        }

        [HttpPost]
        public async Task<JsonResult> add(CustomerBindingModel customerBindingModel)
        {
            PartnershipMemberDTO partnershipMemberDTO = new PartnershipMemberDTO();
            ObservableCollection<PartnershipMemberDTO> partnerships = ViewBag.partnershipMemberDTO ?? new ObservableCollection<PartnershipMemberDTO>();

            foreach (var partner in customerBindingModel.partnershipMemberCollection)
            {
                partnershipMemberDTO = new PartnershipMemberDTO
                {
                    FirstName = partner.FullName,
                    CreatedDate = partner.CreatedDate,
                    IdentityCardNumber = partner.IdentityCardNumber,
                    AddressAddressLine1 = partner.AddressAddressLine1,
                    AddressAddressLine2 = partner.AddressAddressLine2,
                    AddressStreet = partner.AddressStreet,
                    AddressPostalCode = partner.AddressPostalCode,
                    AddressCity = partner.AddressCity,
                    AddressEmail = partner.AddressEmail,
                    AddressLandLine = partner.AddressLandLine,
                    AddressMobileLine = partner.AddressMobileLine
                };
                TempData["h"] = customerBindingModel;
                partnerships.Add(partnershipMemberDTO);
            }

            // Store the updated collection in ViewBag
            ViewBag.partnershipMemberDTO = partnerships;

            // Return the updated list as JSON
            return Json(new { partnerships = partnerships });
        }


        [HttpPost]
        public async Task<ActionResult> Create(CustomerBindingModel customerBindingModel, string[] debittypes, string[] savingsproducts, string[] investmentproducts, string typedescription, HttpPostedFileBase signaturePhoto, HttpPostedFileBase idCardFrontPhoto, HttpPostedFileBase idCardBackPhoto, string passportPhotoDataUrl, string type)
        {

            if (typedescription == "Individual")
            {
                customerBindingModel.Type = 0;
            }

            if (typedescription == "Partnership")
            {
                customerBindingModel.Type = 1;
            }
            if (typedescription == "Corporation")
            {
                customerBindingModel.Type = 2;
            }
            if (typedescription == "MicroCredit")
            {
                customerBindingModel.Type =3 ;
            }

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

            // Initialize ViewBag Select Lists based on Customer Type
            InitializeViewBagSelectLists(customerBindingModel);

            // Retrieve and parse dates from the request
            ParseCustomerSpecificDates(customerBindingModel);

            // Retrieve user information
            var userDTO = await _applicationUserManager.FindByIdAsync(User.Identity.GetUserId());
            if (userDTO.BranchId != null)
            {
                customerBindingModel.BranchId = (Guid)userDTO.BranchId;
            }

            ViewBag.CustomerTypeSelectList = GetCustomerTypeSelectList(string.Empty);
            ViewBag.IndividualTypeSelectList = GetIndividualTypeSelectList(string.Empty);
            ViewBag.IdentityCardSelectList = GetIdentityCardTypeSelectList(string.Empty);
            ViewBag.SalutationSelectList = GetSalutationSelectList(string.Empty);
            ViewBag.GenderSelectList = GetGenderSelectList(string.Empty);
            ViewBag.MaritalStatusSelectList = GetMaritalStatusSelectList(string.Empty);
            ViewBag.IndividualNationalitySelectList = GetNationalitySelectList(string.Empty);
            ViewBag.IndividualEmploymentTermsOfServiceSelectList = GetTermsOfServiceSelectList(string.Empty);
            ViewBag.IndividualClassificationSelectList = GetCustomerClassificationSelectList(string.Empty);

            ViewBag.PartnershipRelationships = GetPartnershipRelationshipsSelectList(string.Empty);

            customerBindingModel.ValidateAll();

            // Prepare mandatory products and services
            var mandatoryProduct = await PrepareMandatoryProductCollection();

            // Add customer and process errors
            if (!customerBindingModel.HasErrors)
            {
                try
                {
                    //customerBindingModel.Type = 2;
                    var customerDTO = customerBindingModel.MapTo<CustomerDTO>();

                    var result = await _channelService.AddCustomerAsync(
                        customerDTO,
                        mandatoryDebitTypes.ToList(),
                        mandatoryProducts.InvestmentProductCollection,
                        mandatoryProducts.SavingsProductCollection,
                        mandatoryProducts,
                        1,
                        GetServiceHeader()
                    );
                   // await SaveDocumentAsync(ProcessDocumentUpload(result.Id, signaturePhoto, idCardFrontPhoto, idCardBackPhoto, passportPhotoDataUrl));
                    
                    TempData["SuccessMessage"] = "customer created successfully";

                    if (result == null || !string.IsNullOrEmpty(result.ErrorMessageResult))
                    {
                        TempData["DefaultError"] = result?.ErrorMessageResult ?? "An error occurred while creating the customer.";
                        await ServeNavigationMenus();
                        return View("Create", customerBindingModel);
                    }

                    // customerBindingModel.Type = 3;
                    //// Process based on customer type
                    //switch ((CustomerType)customerBindingModel.Type)
                    //{
                    //    case CustomerType.Individual:
                    //        TempData["SuccessMessage"] = !string.IsNullOrEmpty(customerBindingModel.FullName)
                    //            ? $"Successfully Created Customer {customerBindingModel.FullName}"
                    //            : "Successfully created customer, but invalid data provided.";
                    //        return RedirectToAction("index", customerBindingModel);

                    //    case CustomerType.Partnership:
                    //        await _channelService.UpdatePartnershipMemberCollectionByPartnershipIdAsync(result.Id, customerBindingModel.partnershipMemberCollection, GetServiceHeader());
                    //        TempData["SuccessMessage"] = $"Partnership customer '{customerBindingModel.FullName}' created successfully.";
                    //        return RedirectToAction("index", customerBindingModel);

                    //    case CustomerType.Corporation:
                    //        await _channelService.UpdateCorporationMemberCollectionByCorporationIdAsync(result.Id, customerBindingModel.corporationMemberDTO, GetServiceHeader());
                    //        TempData["SuccessMessage"] = $"Corporation customer '{customerBindingModel.FullName}' created successfully.";
                    //        return RedirectToAction("index", customerBindingModel);
                    //    case CustomerType.MicroCredit:
                    //        await _channelService.AddMicroCreditGroupMemberAsync(customerBindingModel.microCreditGroupMemberDTOs[0], GetServiceHeader());
                    //        TempData["SuccessMessage"] = $"Corporation customer '{customerBindingModel.FullName}' created successfully.";
                    //        return RedirectToAction("index", customerBindingModel);
                    //    default:
                    //        TempData["DefaultError"] = "Unknown customer type.";
                    //        return RedirectToAction("index", customerBindingModel);
                    //}
                    //Handle Document Upload to DB

                    //// Refresh ViewBag Select Lists
                    //InitializeViewBagSelectLists(customerBindingModel);

                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    // Log the exception and return an error view
                    TempData["DefaultError"] = $"An unexpected error occurred: {ex.Message}";
                    await ServeNavigationMenus();
                    return View("Create", customerBindingModel);
                }
            }
            else
            {
                TempData["Error2"] = customerBindingModel.ErrorMessages;
                await ServeNavigationMenus();
                return View("Create", customerBindingModel);
            }

        }

        private void InitializeViewBagSelectLists(CustomerBindingModel customerBindingModel)
        {
            ViewBag.CustomerTypeSelectList = GetCustomerTypeSelectList(customerBindingModel.TypeDescription.ToString());

            // Try to parse the TypeDescription to CustomerType
            if (Enum.TryParse(customerBindingModel.TypeDescription, true, out CustomerType customerType))
            {
                // Specific to Individual Customers
                if (customerType == CustomerType.Individual)
                {
                    ViewBag.IndividualTypeSelectList = GetIndividualTypeSelectList(customerBindingModel.IndividualType.ToString());
                    ViewBag.IdentityCardSelectList = GetIdentityCardTypeSelectList(customerBindingModel.IndividualIdentityCardType.ToString());
                    ViewBag.SalutationSelectList = GetSalutationSelectList(customerBindingModel.IndividualSalutation.ToString());
                    ViewBag.GenderSelectList = GetGenderSelectList(customerBindingModel.IndividualGender.ToString());
                    ViewBag.MaritalStatusSelectList = GetMaritalStatusSelectList(customerBindingModel.IndividualMaritalStatus.ToString());
                    ViewBag.IndividualNationalitySelectList = GetNationalitySelectList(customerBindingModel.IndividualNationality.ToString());
                    ViewBag.IndividualEmploymentTermsOfServiceSelectList = GetTermsOfServiceSelectList(customerBindingModel.IndividualEmploymentTermsOfService.ToString());
                    ViewBag.IndividualClassificationSelectList = GetCustomerClassificationSelectList(customerBindingModel.IndividualClassification.ToString());
                }
                else if (customerType == CustomerType.Partnership)
                {
                    // Add ViewBag initialization specific to Partnership customers
                }
                else if (customerType == CustomerType.Corporation)
                {
                    // Add ViewBag initialization specific to Corporation customers
                }
                else if (customerType == CustomerType.MicroCredit)
                {
                    // Add ViewBag initialization specific to MicroCredit customers
                }
            }
        }

        private void ParseCustomerSpecificDates(CustomerBindingModel customerBindingModel)
        {
            // Parse and assign dates based on customer type
            var registrationdate = Request["registrationdate"];
            var birthdate = Request["birthdate"];
            var employmentdate = Request["employmentdate"];

            if (Enum.TryParse(customerBindingModel.TypeDescription, true, out CustomerType customerType))
            {
                if (customerType == CustomerType.Individual)
                {
                    if (DateTime.TryParseExact(birthdate, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedBirthDate))
                    {
                        customerBindingModel.IndividualBirthDate = parsedBirthDate;
                    }
                    if (DateTime.TryParseExact(employmentdate, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedEmploymentDate))
                    {
                        customerBindingModel.IndividualEmploymentDate = parsedEmploymentDate;
                    }
                }
            }

            if (DateTime.TryParseExact(registrationdate, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedRegistrationDate))
            {
                customerBindingModel.RegistrationDate = parsedRegistrationDate;
            }
        }

        private async Task<ProductCollectionInfo> PrepareMandatoryProductCollection()
        {
            var investmentProducts = await _channelService.FindMandatoryInvestmentProductsAsync(true, GetServiceHeader());
            var savingsProducts = await _channelService.FindMandatorySavingsProductsAsync(false, GetServiceHeader());

            return new ProductCollectionInfo
            {
                InvestmentProductCollection = investmentProducts.ToList(),
                SavingsProductCollection = savingsProducts.ToList()
            };
        }

        private Document ProcessDocumentUpload(Guid customerId, HttpPostedFileBase signaturePhoto, HttpPostedFileBase idCardFrontPhoto, HttpPostedFileBase idCardBackPhoto, string passportPhotoDataUrl)
        {
            var document = new Document
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId
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



        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();
            ViewBag.CustomerTypeSelectList = GetCustomerTypeSelectList(string.Empty);
            var customerDTO = await _channelService.FindCustomerAsync(id, GetServiceHeader());
            ViewBag.CustomerTypeSelectList = GetCustomerTypeSelectList(string.Empty);
            ViewBag.IndividualTypeSelectList = GetIndividualTypeSelectList(string.Empty);
            ViewBag.IdentityCardSelectList = GetIdentityCardTypeSelectList(string.Empty);
            ViewBag.SalutationSelectList = GetSalutationSelectList(string.Empty);
            ViewBag.GenderSelectList = GetGenderSelectList(string.Empty);
            ViewBag.MaritalStatusSelectList = GetMaritalStatusSelectList(string.Empty);
            ViewBag.IndividualNationalitySelectList = GetNationalitySelectList(string.Empty);
            ViewBag.IndividualEmploymentTermsOfServiceSelectList = GetTermsOfServiceSelectList(string.Empty);
            ViewBag.IndividualClassificationSelectList = GetCustomerClassificationSelectList(string.Empty);
            ViewBag.recordstatus = GetRecordStatusSelectList(string.Empty);
            return View(customerDTO.MapTo<CustomerBindingModel>());
        }

        [HttpPost]
        public async Task<ActionResult> Edit(CustomerBindingModel customerBindingModel, string[] debittypes, string[] savingsproducts, string[] investmentproducts, string typedescription, HttpPostedFileBase signaturePhoto, HttpPostedFileBase idCardFrontPhoto, HttpPostedFileBase idCardBackPhoto, string passportPhotoDataUrl, string type)
        {

            if (typedescription == "Individual")
            {
                customerBindingModel.Type = 0;
            }

            if (typedescription == "Partnership")
            {
                customerBindingModel.Type = 1;
            }
            if (typedescription == "Corporation")
            {
                customerBindingModel.Type = 2;
            }
            if (typedescription == "MicroCredit")
            {
                customerBindingModel.Type = 3;
            }

            ////cheat
            //var mandatoryInvestmentProducts = new List<InvestmentProductDTO>();
            //var mandatorySavingsProducts = new List<SavingsProductDTO>();
            //var mandatoryDebitTypes = new ObservableCollection<DebitTypeDTO>();
            //var mandatoryProducts = new ProductCollectionInfo();

            //SavingsProductDTO j = new SavingsProductDTO();
            //foreach (var k in savingsproducts)
            //{
            //    j.Id = Guid.Parse(k);
            //    mandatorySavingsProducts.Add(j);
            //}
            //mandatoryProducts.SavingsProductCollection = mandatorySavingsProducts;



            //InvestmentProductDTO investmentProductDTO = new InvestmentProductDTO();
            //foreach (var invest in investmentproducts)
            //{
            //    investmentProductDTO.Id = Guid.Parse(invest);
            //    mandatoryInvestmentProducts.Add(investmentProductDTO);
            //}
            //mandatoryProducts.InvestmentProductCollection = mandatoryInvestmentProducts;


            //DebitTypeDTO debitTypeDTO = new DebitTypeDTO();
            //foreach (var debit in debittypes)
            //{
            //    debitTypeDTO.Id = Guid.Parse(debit);
            //    mandatoryDebitTypes.Add(debitTypeDTO);
            //}

            // Initialize ViewBag Select Lists based on Customer Type
            InitializeViewBagSelectLists(customerBindingModel);

            // Retrieve and parse dates from the request
            ParseCustomerSpecificDates(customerBindingModel);

            // Retrieve user information
            var userDTO = await _applicationUserManager.FindByIdAsync(User.Identity.GetUserId());
            if (userDTO.BranchId != null)
            {
                customerBindingModel.BranchId = (Guid)userDTO.BranchId;
            }

            ViewBag.CustomerTypeSelectList = GetCustomerTypeSelectList(string.Empty);
            ViewBag.IndividualTypeSelectList = GetIndividualTypeSelectList(string.Empty);
            ViewBag.IdentityCardSelectList = GetIdentityCardTypeSelectList(string.Empty);
            ViewBag.SalutationSelectList = GetSalutationSelectList(string.Empty);
            ViewBag.GenderSelectList = GetGenderSelectList(string.Empty);
            ViewBag.MaritalStatusSelectList = GetMaritalStatusSelectList(string.Empty);
            ViewBag.IndividualNationalitySelectList = GetNationalitySelectList(string.Empty);
            ViewBag.IndividualEmploymentTermsOfServiceSelectList = GetTermsOfServiceSelectList(string.Empty);
            ViewBag.IndividualClassificationSelectList = GetCustomerClassificationSelectList(string.Empty);

            ViewBag.PartnershipRelationships = GetPartnershipRelationshipsSelectList(string.Empty);

            customerBindingModel.ValidateAll();

            // Prepare mandatory products and services
            var mandatoryProduct = await PrepareMandatoryProductCollection();

            // Add customer and process errors
            if (!customerBindingModel.HasErrors)
            {
                try
                {
                    //customerBindingModel.Type = 2;
                    var customerDTO = customerBindingModel.MapTo<CustomerDTO>();

                    var result = await _channelService.UpdateCustomerAsync(customerDTO,
                        GetServiceHeader()
                    );
                    TempData["SuccessMessage"] = "customer created successfully";

                  
                    // customerBindingModel.Type = 3;
                    //// Process based on customer type
                    //switch ((CustomerType)customerBindingModel.Type)
                    //{
                    //    case CustomerType.Individual:
                    //        TempData["SuccessMessage"] = !string.IsNullOrEmpty(customerBindingModel.FullName)
                    //            ? $"Successfully Created Customer {customerBindingModel.FullName}"
                    //            : "Successfully created customer, but invalid data provided.";
                    //        return RedirectToAction("index", customerBindingModel);

                    //    case CustomerType.Partnership:
                    //        await _channelService.UpdatePartnershipMemberCollectionByPartnershipIdAsync(result.Id, customerBindingModel.partnershipMemberCollection, GetServiceHeader());
                    //        TempData["SuccessMessage"] = $"Partnership customer '{customerBindingModel.FullName}' created successfully.";
                    //        return RedirectToAction("index", customerBindingModel);

                    //    case CustomerType.Corporation:
                    //        await _channelService.UpdateCorporationMemberCollectionByCorporationIdAsync(result.Id, customerBindingModel.corporationMemberDTO, GetServiceHeader());
                    //        TempData["SuccessMessage"] = $"Corporation customer '{customerBindingModel.FullName}' created successfully.";
                    //        return RedirectToAction("index", customerBindingModel);
                    //    case CustomerType.MicroCredit:
                    //        await _channelService.AddMicroCreditGroupMemberAsync(customerBindingModel.microCreditGroupMemberDTOs[0], GetServiceHeader());
                    //        TempData["SuccessMessage"] = $"Corporation customer '{customerBindingModel.FullName}' created successfully.";
                    //        return RedirectToAction("index", customerBindingModel);
                    //    default:
                    //        TempData["DefaultError"] = "Unknown customer type.";
                    //        return RedirectToAction("index", customerBindingModel);
                    //}
                    // Handle Document Upload to DB
                    //await SaveDocumentAsync(ProcessDocumentUpload(result.Id, signaturePhoto, idCardFrontPhoto, idCardBackPhoto, passportPhotoDataUrl));

                    //// Refresh ViewBag Select Lists
                    //InitializeViewBagSelectLists(customerBindingModel);

                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    // Log the exception and return an error view
                    TempData["DefaultError"] = $"An unexpected error occurred: {ex.Message}";
                    await ServeNavigationMenus();
                    return View("Create", customerBindingModel);
                }
            }
            else
            {
                TempData["Error2"] = customerBindingModel.ErrorMessages;
                await ServeNavigationMenus();
                return View("Create", customerBindingModel);
            }

        }
        [HttpGet]
        public async Task<JsonResult> GetCustomersAsync()
        {
            var customersDTOs = await _channelService.FindCustomersAsync(GetServiceHeader());

            return Json(customersDTOs, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public async Task<JsonResult> GetCustomerByIdentityNumberAsync(string individualIdentityCardNumber, bool matchExtact)
        {
            var customersDTOs = await _channelService.FindCustomersByIdentityCardNumberAsync(individualIdentityCardNumber, matchExtact, GetServiceHeader());

            return Json(customersDTOs, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public async Task<JsonResult> GetCustomersByIDyNumberAsync(string individualIdentityCardNumber)
        {
            var customersDTOs = await _channelService.FindCustomersByIDNumberAsync(individualIdentityCardNumber, GetServiceHeader());

            return Json(customersDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}