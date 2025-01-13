using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.RegistryModule;
using Infrastructure.Crosscutting.Framework.Utils;
using Microsoft.AspNet.Identity;
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
using System.Web.Mvc;
using System.Data;
using System.Linq;
using System.Data.SqlClient;
using System.Text.RegularExpressions;

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
            ViewBag.CustomerTypeSelectList = GetCustomerTypeSelectList(string.Empty);
            return View();
        }

        [HttpPost]
        public async Task<JsonResult> Index(JQueryDataTablesModel jQueryDataTablesModel, int? recordStatus, int? customerFilter, int? customerType)
        {
            int totalRecordCount = 0;

            int searchRecordCount = 0;

            //var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            //var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            //int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;

            var pageCollectionInfo = new PageCollectionInfo<CustomerDTO>();

            if (recordStatus != null && customerFilter != null)
            {
                pageCollectionInfo = await _channelService.FindCustomersByRecordStatusAndFilterInPageAsync((int)recordStatus, jQueryDataTablesModel.sSearch, (int)customerFilter, 0, int.MaxValue, GetServiceHeader());
            }
            else if (recordStatus == null && customerFilter != null)
            {
                pageCollectionInfo = await _channelService.FindCustomersByFilterInPageAsync(jQueryDataTablesModel.sSearch, (int)customerFilter, 0, int.MaxValue, GetServiceHeader());
            }
            else if (customerType != null && customerFilter == null)
            {

                if (customerType == 0)
                {
                    pageCollectionInfo = await _channelService.FindCustomersByRecordStatusAndFilterInPageAsync((int)recordStatus, jQueryDataTablesModel.sSearch, (int)CustomerFilter.FirstName, 0, int.MaxValue, GetServiceHeader());
                }

                if (customerType == 1)
                {
                    pageCollectionInfo = await _channelService.FindCustomersByFilterInPageAsync(jQueryDataTablesModel.sSearch, (int)CustomerFilter.NonIndividual_Description, 0, int.MaxValue, GetServiceHeader());
                }
                if (customerType == 2)
                {
                    pageCollectionInfo = await _channelService.FindCustomersByFilterInPageAsync(jQueryDataTablesModel.sSearch, (int)CustomerFilter.NonIndividual_Description, 0, int.MaxValue, GetServiceHeader());
                }
                if (customerType == 3)
                {
                    pageCollectionInfo = await _channelService.FindCustomersByFilterInPageAsync(jQueryDataTablesModel.sSearch, (int)CustomerFilter.NonIndividual_Description, 0, int.MaxValue, GetServiceHeader());
                }
            }
            else if (recordStatus != null && customerFilter == null)
            {
                pageCollectionInfo = await _channelService.FindCustomersByTypeAndFilterInPageAsync((int)customerType, jQueryDataTablesModel.sSearch, (int)CustomerFilter.NonIndividual_Description, 0, int.MaxValue, GetServiceHeader());
            }
            else if (recordStatus == null && customerFilter == null)
            {
                pageCollectionInfo = await _channelService.FindCustomersByRecordStatusAndFilterInPageAsync((int)RecordStatus.New, jQueryDataTablesModel.sSearch, (int)CustomerFilter.FirstName, 0, int.MaxValue, GetServiceHeader());

                if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
                {

                    var sortedData = pageCollectionInfo.PageCollection
                        .OrderByDescending(loanCase => loanCase.CreatedDate)
                        .ToList();

                    totalRecordCount = sortedData.Count;

                    var paginatedData = sortedData
                        .Skip(jQueryDataTablesModel.iDisplayStart)
                        .Take(jQueryDataTablesModel.iDisplayLength)
                        .ToList();

                    searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch)
                        ? sortedData.Count
                        : totalRecordCount;

                    return this.DataTablesJson(
                        items: paginatedData,
                        totalRecords: totalRecordCount,
                        totalDisplayRecords: searchRecordCount,
                        sEcho: jQueryDataTablesModel.sEcho
                    );
                }

                return this.DataTablesJson(
                    items: new List<CustomerDTO>(),
                    totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount,
                    sEcho: jQueryDataTablesModel.sEcho
            );
            }


            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {

                var sortedData = pageCollectionInfo.PageCollection
                    .OrderByDescending(loanCase => loanCase.CreatedDate)
                    .ToList();

                totalRecordCount = sortedData.Count;

                var paginatedData = sortedData
                    .Skip(jQueryDataTablesModel.iDisplayStart)
                    .Take(jQueryDataTablesModel.iDisplayLength)
                    .ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch)
                    ? sortedData.Count
                    : totalRecordCount;

                return this.DataTablesJson(
                    items: paginatedData,
                    totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount,
                    sEcho: jQueryDataTablesModel.sEcho
                );
            }

            return this.DataTablesJson(
                items: new List<CustomerDTO>(),
                totalRecords: totalRecordCount,
                totalDisplayRecords: searchRecordCount,
                sEcho: jQueryDataTablesModel.sEcho);
        }


        public async Task<ActionResult> Details(Guid id, CustomerBindingModel customerBindingModel)
        {
            await ServeNavigationMenus();
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

            ViewBag.PartnershipRelationships = GetPartnershipRelationshipsSelectList(string.Empty);
            var debitTypes = await _channelService.FindDebitTypesAsync(GetServiceHeader());
            var creditTypes = await _channelService.FindCreditTypesAsync(GetServiceHeader());
            var investmentProducts = await _channelService.FindInvestmentProductsAsync(GetServiceHeader());
            var savingsProducts = await _channelService.FindSavingsProductsAsync(GetServiceHeader());
            var savingsProductDTOs = await _channelService.FindSavingsProductsAsync(GetServiceHeader());
            var investment = await _channelService.FindInvestmentProductsAsync(GetServiceHeader());
            var debitypes = await _channelService.FindDebitTypesAsync(GetServiceHeader());

            var nextofkin = await _channelService.FindNextOfKinCollectionByCustomerIdAsync(customerDTO.Id, GetServiceHeader());
            ViewBag.nextofkin = nextofkin;
            ViewBag.type = customerDTO.Type;

            ViewBag.investment = investment;
            ViewBag.savings = savingsProductDTOs;
            ViewBag.debit = debitypes;
            ViewBag.creditTypes = creditTypes;

            // Define a mapping dictionary
            var typeMapping = new Dictionary<string, string>
{
    { "0", "Individual" },
    { "1", "Partnership" },
    { "2", "Corporation" },
    { "3", "MicroCredit" }
};

            // Map ViewBag.type if it exists in the dictionary
            if (typeMapping.ContainsKey(ViewBag.type?.ToString()))
            {
                ViewBag.type = typeMapping[ViewBag.type.ToString()];
            }
            else
            {
                ViewBag.type = "Individual"; // Default to Individual if no match
            }
            var documents = await GetDocumentsAsync(customerDTO.Id);
            if (documents.Any())
            {
                var document = documents.First();

                TempData["PassportPhoto"] = document.PassportPhoto;
                TempData["SignaturePhoto"] = document.SignaturePhoto;
                TempData["idCardFront"] = document.IDCardFrontPhoto;
                TempData["idCardBack"] = document.IDCardBackPhoto;

                // Sending the images as Base64 encoded strings to be used in AJAX
                ViewBag.PassportPhoto = document.PassportPhoto != null ? Convert.ToBase64String(document.PassportPhoto) : null;
                ViewBag.SignaturePhoto = document.SignaturePhoto != null ? Convert.ToBase64String(document.SignaturePhoto) : null;
                ViewBag.IDCardFrontPhoto = document.IDCardFrontPhoto != null ? Convert.ToBase64String(document.IDCardFrontPhoto) : null;
                ViewBag.IDCardBackPhoto = document.IDCardBackPhoto != null ? Convert.ToBase64String(document.IDCardBackPhoto) : null;
            }

            return View(customerDTO.MapTo<CustomerBindingModel>());
        }
        //[HttpGet]
        //public async Task<JsonResult> GetImage(string imageType)
        //{
        //    return await GetImage(imageType); // Existing method from earlier
        //}

        //public async Task<JsonResult> GetImage(string imageType)
        //{
        //    var documents = await GetDocumentsAsync(customerId); // Get documents
        //    var imageData = imageType switch
        //    {
        //        "passportPhoto" => documents?.FirstOrDefault()?.PassportPhoto,
        //        "signaturePhoto" => documents?.FirstOrDefault()?.SignaturePhoto,
        //        "idCardFront" => documents?.FirstOrDefault()?.IDCardFrontPhoto,
        //        "idCardBack" => documents?.FirstOrDefault()?.IDCardBackPhoto,
        //        _ => null
        //    };

        //    if (imageData != null)
        //    {
        //        return Json(new { imageData = Convert.ToBase64String(imageData) });
        //    }

        //    return Json(new { imageData = string.Empty });
        //}

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
                                IDCardBackPhoto = reader.IsDBNull(3) ? null : (byte[])reader[3],

                            }
                            );

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
            CustomerDTO customerDTO = new CustomerDTO();
            var userDTO = await _applicationUserManager.FindByIdAsync(User.Identity.GetUserId());
            if (userDTO.BranchId != null)
            {
                customerDTO.BranchId = (Guid)userDTO.BranchId;
            }
            var companies = await _channelService.FindBranchAsync(customerDTO.BranchId, GetServiceHeader());
            var j = await _channelService.FindCompanyAsync(companies.CompanyId, GetServiceHeader());

            var attached = await _channelService.FindAttachedProductsByCompanyIdAsync(companies.CompanyId, GetServiceHeader());

            var mandatorydebitTypes = await _channelService.FindDebitTypesByCompanyIdAsync(companies.CompanyId, GetServiceHeader());

            var mandatorycreditTypes = await _channelService.FindCreditTypesAsync(GetServiceHeader());
            var mandatoryinvestmentProducts = await _channelService.FindMandatoryInvestmentProductsAsync(true, GetServiceHeader());
            var mandatorysavingsProducts = await _channelService.FindMandatorySavingsProductsAsync(true, GetServiceHeader());

            CustomerBindingModel customerBindingModel = new CustomerBindingModel();


            //ViewBag.investment = investment;
            //ViewBag.savings = savingsProductDTOs;
            //ViewBag.debit = debitypes;
            //ViewBag.creditTypes = creditTypes;


            //ViewBag.investment = mandatoryinvestmentProducts;
            //ViewBag.savings = mandatorysavingsProducts;
            //ViewBag.debit = mandatorydebitTypes;
            //ViewBag.creditTypes = mandatorycreditTypes;



            ViewBag.investment = investment; // Original investment products
            ViewBag.savings = savingsProductDTOs; // Original savings products
            ViewBag.debit = debitypes; // Original debit types
            ViewBag.creditTypes = creditTypes; // Original credit types
            if (attached != null)
            {
                // Separate ViewBag properties for mandatory items
                ViewBag.mandatoryInvestment = attached.InvestmentProductCollection.Select(m => m.Id).ToList();
                ViewBag.mandatorySavings = attached.SavingsProductCollection.Select(m => m.Id).ToList();
                ViewBag.mandatoryDebit = mandatorydebitTypes.Select(m => m.Id).ToList();
                ViewBag.mandatoryCreditTypes = mandatorydebitTypes.Select(m => m.Id).ToList();
            }
            else if (attached == null)
            {
                ViewBag.mandatoryInvestment = mandatoryinvestmentProducts;
                ViewBag.mandatorySavings = mandatorysavingsProducts;
                ViewBag.mandatoryDebit = mandatorydebitTypes;
                //ViewBag.mandatoryCreditTypes = mandatorydebitTypes.Select(m => m.Id).ToList();
            }
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Query to get the next reference by incrementing the current max
                var getNextReferenceQuery = string.Format("SELECT ISNULL(MAX(Reference1), 0) + 1 AS NextReference FROM {0}Customers", DefaultSettings.Instance.TablePrefix);

                int newReference;

                using (var getCommand = new SqlCommand(getNextReferenceQuery, connection))
                {
                    //var result = await getCommand.ExecuteScalarAsync();
                    // newReference = (result != DBNull.Value) ? Convert.ToInt32(result) : 1; // Start at 1 if there are no rows
                }

            }
            return View();
        }

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
        public async Task<ActionResult> Search(Guid? id)
        {
            //string Remarks = "";
            await ServeNavigationMenus();

            ViewBag.WithdrawalNotificationCategorySelectList = GetWithdrawalNotificationCategorySelectList(string.Empty);

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            var customer = await _channelService.FindCustomerAsync(parseId, GetServiceHeader());


            RefereeDTO withdrawalNotificationDTO = new RefereeDTO();


            if (customer != null)
            {

                withdrawalNotificationDTO.CustomerId = customer.Id;
                withdrawalNotificationDTO.WitnessIndividualFirstName = customer.FullName;
                withdrawalNotificationDTO.WitnessIndividualPayrollNumbers = customer.IndividualPayrollNumbers;
                withdrawalNotificationDTO.WitnessIndividualIdentityCardSerialNumber = customer.IndividualIdentityCardSerialNumber;
                withdrawalNotificationDTO.WitnessIndividualIdentityCardNumber = customer.IndividualIdentityCardNumber;
                withdrawalNotificationDTO.WitnessAddressMobileLine = customer.AddressMobileLine;
                //Session["Test"] =Request.Form["h"] + "";
                //string mimi = Session["Test"].ToString();
            }
            //
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

            //TempData["WithdrawalNotificationDTOs"] = withdrawalNotificationDTO;

            return View("Create", customer.MapTo<CustomerBindingModel>());
        }



        [HttpPost]
        public async Task<ActionResult> Create(
            CustomerBindingModel customerBindingModel,
            string[] debittypes,
            string[] savingsproductIds,
            string[] investmentproductIds,
            string typedescription,
            string passportPhotoDataUrl,
            HttpPostedFileBase signaturePhoto,
            HttpPostedFileBase idCardFrontPhoto,
            HttpPostedFileBase idCardBackPhoto,
            HttpPostedFileBase passportPhoto)
        {

            var startDate = Request["registrationdate"];
            var endDate = Request["birthdate"];

            // Parse and set dates
            customerBindingModel.IndividualBirthDate = new DateTime(1990, DateTime.Today.Month, DateTime.Today.Day);
            customerBindingModel.RegistrationDate = DateTime.Parse(endDate).Date;

            // Set customer type based on description
            switch (typedescription)
            {
                case "Individual":
                    customerBindingModel.Type = 0;
                    break;
                case "Partnership":
                    customerBindingModel.NonIndividualDescription = customerBindingModel.RefereeFirstName;

                    customerBindingModel.Type = 1;
                    break;
                case "Corporation":
                    customerBindingModel.NonIndividualDescription = customerBindingModel.RefereeFirstName;
                    customerBindingModel.Type = 2;
                    break;
                case "MicroCredit":
                    customerBindingModel.NonIndividualDescription = customerBindingModel.RefereeFirstName;

                    customerBindingModel.Type = 3;
                    break;
            }

            // Process mandatory products
            var mandatoryInvestmentProducts = new List<InvestmentProductDTO>();
            var mandatorySavingsProducts = new List<SavingsProductDTO>();
            var mandatoryDebitTypes = new ObservableCollection<DebitTypeDTO>();
            var mandatoryProducts = new ProductCollectionInfo();

            if (savingsproductIds != null && investmentproductIds != null && debittypes != null)
            {
                foreach (var savingsProductID in savingsproductIds)
                {
                    var savingsDTO = await _channelService.FindSavingsProductAsync(Guid.Parse(savingsProductID), GetServiceHeader());

                    mandatorySavingsProducts.Add(savingsDTO);
                }
                mandatoryProducts.SavingsProductCollection = mandatorySavingsProducts;

                foreach (var investmentId in investmentproductIds)
                {
                    var investmentDTO = await _channelService.FindInvestmentProductAsync(Guid.Parse(investmentId), GetServiceHeader());
                    mandatoryInvestmentProducts.Add(investmentDTO);
                }
                mandatoryProducts.InvestmentProductCollection = mandatoryInvestmentProducts;

                foreach (var debit in debittypes)
                {
                    var debitTypeDTO = await _channelService.FindDebitTypeAsync(Guid.Parse(debit), GetServiceHeader());
                    mandatoryDebitTypes.Add(debitTypeDTO);
                }
            }



            var userDTO = await _applicationUserManager.FindByIdAsync(User.Identity.GetUserId());
            //if (userDTO.BranchId != null)
            //{
            //    customerBindingModel.BranchId = (Guid)userDTO.BranchId;
            //}
            //var companies = await _channelService.FindBranchAsync(customerBindingModel.BranchId, GetServiceHeader());
            //var j = await _channelService.FindCompanyAsync(companies.CompanyId, GetServiceHeader());
            //var mandatorydebitTypes = await _channelService.FindDebitTypesByCompanyIdAsync(companies.CompanyId, GetServiceHeader());
            //var attached = await _channelService.FindAttachedProductsByCompanyIdAsync(companies.CompanyId, GetServiceHeader());

            //mandatoryInvestmentProducts = attached.InvestmentProductCollection;
            //mandatorySavingsProducts = attached.SavingsProductCollection;
            //mandatoryDebitTypes = mandatorydebitTypes;
            //mandatoryProducts.InvestmentProductCollection = mandatoryInvestmentProducts;
            //mandatoryProducts.SavingsProductCollection = mandatorySavingsProducts;

            // Initialize ViewBag Select Lists based on Customer Type
            InitializeViewBagSelectLists(customerBindingModel);
            // Retrieve user information
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

            // Validate and process the customer data
            if (!customerBindingModel.HasErrors)
            {
                try
                {
                    var customerDTO = customerBindingModel.MapTo<CustomerDTO>();
                    var result = await _channelService.AddCustomerAsync(
     customerDTO,
     mandatoryDebitTypes.ToList(),
     mandatoryInvestmentProducts,
    mandatorySavingsProducts,
     mandatoryProducts,
     1,
     GetServiceHeader()
 );
                    await SaveDocumentAsync(ProcessDocumentUpload(result.Id, signaturePhoto, idCardFrontPhoto, idCardBackPhoto, passportPhotoDataUrl));

                    if (result.ErrorMessages == null)
                    {
                        // TempData["SuccessMessage"] = "Teller created successfully.";
                        TempData["SuccessMessage"] = "Customer " + result.FullName + " created successfully.";

                        //System.Windows.Forms.MessageBox.Show(
                        //    "Customer " + result.FullName + " created successfully.",
                        //    "Success",
                        //    System.Windows.Forms.MessageBoxButtons.OK,
                        //    System.Windows.Forms.MessageBoxIcon.Information,
                        //    System.Windows.Forms.MessageBoxDefaultButton.Button1,
                        //    System.Windows.Forms.MessageBoxOptions.ServiceNotification
                        //);
                        ViewBag.recordStatus = GetRecordStatusSelectList(string.Empty);
                        ViewBag.customerFilter = GetCustomerFilterSelectList(string.Empty);
                        ViewBag.CustomerTypeSelectList = GetCustomerTypeSelectList(string.Empty);
                        await ServeNavigationMenus();
                        return RedirectToAction("Index", customerBindingModel);
                    }

                    else
                    {
                        //TempData["Error"] = "Sorry, teller creation failed.";

                        System.Windows.Forms.MessageBox.Show(
                                     "Sorry, Teller creation failed.",
                                     "Error",
                                     System.Windows.Forms.MessageBoxButtons.OK,
                                     System.Windows.Forms.MessageBoxIcon.Error,
                                     System.Windows.Forms.MessageBoxDefaultButton.Button1,
                                     System.Windows.Forms.MessageBoxOptions.ServiceNotification
                                     );


                        return Json(new { success = false, message = "Operation Failed" });

                    }
                    // Document document = new Document();

                    //// Convert Base64 strings to byte arrays with error handling
                    //document.SignaturePhoto = SafeConvertFromBase64String(signaturePhoto);
                    //document.IDCardBackPhoto = SafeConvertFromBase64String(idCardBackPhoto);
                    //document.IDCardFrontPhoto = SafeConvertFromBase64String(idCardFrontPhoto);
                    //byte[] ka = SafeConvertFromBase64String(passportPhotoDataUrl);
                    //string p = Convert.ToString(passportPhotoDataUrl);

                    //// Convert byte arrays to HttpPostedFileBase if not null
                    //HttpPostedFileBase signatureFile = document.SignaturePhoto != null ? ConvertToHttpPostedFileBase(document.SignaturePhoto, "signaturePhoto.jpg") : null;
                    //HttpPostedFileBase idCardBackFile = document.IDCardBackPhoto != null ? ConvertToHttpPostedFileBase(document.IDCardBackPhoto, "idCardBackPhoto.jpg") : null;
                    //HttpPostedFileBase idCardFrontFile = document.IDCardFrontPhoto != null ? ConvertToHttpPostedFileBase(document.IDCardFrontPhoto, "idCardFrontPhoto.jpg") : null;
                    //HttpPostedFileBase passportFile = document.PassportPhoto != null ? ConvertToHttpPostedFileBase(document.PassportPhoto, "passportPhoto.jpg") : null;

                    // Proceed with your document upload process
                    //string j= document.PassportPhoto != null ? Convert.ToString(document.PassportPhoto) : null;
                    // await SaveDocumentAsync(ProcessDocumentUpload(result.Id, signaturePhoto, idCardFrontPhoto, idCardBackPhoto, j));




                    if (result == null || !string.IsNullOrEmpty(result.ErrorMessageResult))
                    {
                        TempData["DefaultError"] = result?.ErrorMessageResult ?? "An error occurred while creating the customer.";
                        await ServeNavigationMenus();
                        return View("Create", customerBindingModel);
                    }

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
        public byte[] SafeConvertFromBase64String(string base64String)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(base64String))
                {
                    return null;
                }

                // Remove any unwanted characters that may have been added accidentally
                base64String = base64String.Trim();

                // Check if the string is a valid Base64
                if (IsBase64String(base64String))
                {
                    return Convert.FromBase64String(base64String);
                }
            }
            catch (FormatException)
            {
                // Handle invalid Base64 string format
            }

            // Return null if it's invalid
            return null;
        }

        private bool IsBase64String(string base64String)
        {
            // Check if the Base64 string matches the regex pattern for Base64 encoding
            var regex = new Regex(@"^([0-9a-zA-Z+/=])*$");
            return regex.IsMatch(base64String);
        }

        public class MockHttpPostedFile : HttpPostedFileBase
        {
            private readonly byte[] _fileData;
            private readonly string _fileName;

            public MockHttpPostedFile(byte[] fileData, string fileName)
            {
                _fileData = fileData;
                _fileName = fileName;
            }

            public override string FileName => _fileName;
            public override int ContentLength => _fileData.Length;
            public override Stream InputStream => new MemoryStream(_fileData);
        }


        public HttpPostedFileBase ConvertToHttpPostedFileBase(byte[] fileData, string fileName)
        {
            return new MockHttpPostedFile(fileData, fileName);
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

        private Document ProcessDocumentUpload(Guid customerId, HttpPostedFileBase signaturePhoto, HttpPostedFileBase idCardFrontPhoto, HttpPostedFileBase idCardBackPhoto, string passportFile)
        {
            var document = new Document
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId
            };

            // Process the passport photo from the data URL
            if (!string.IsNullOrEmpty(passportFile))
            {
                var base64Data = passportFile.Split(',')[1];
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
            ViewBag.type = customerDTO.Type;
            TempData["Reference2"] = customerDTO.Reference2;
            // Define a mapping dictionary
            var typeMapping = new Dictionary<string, string>
{
    { "0", "Individual" },
    { "1", "Partnership" },
    { "2", "Corporation" },
    { "3", "MicroCredit" }
};

            // Map ViewBag.type if it exists in the dictionary
            if (typeMapping.ContainsKey(ViewBag.type?.ToString()))
            {
                ViewBag.type = typeMapping[ViewBag.type.ToString()];
            }
            else
            {
                ViewBag.type = "Individual"; // Default to Individual if no match
            }


            var documents = await GetDocumentsAsync(customerDTO.Id);
            if (documents.Any())
            {
                var document = documents.First();

                TempData["PassportPhoto"] = document.PassportPhoto;
                TempData["SignaturePhoto"] = document.SignaturePhoto;
                TempData["idCardFront"] = document.IDCardFrontPhoto;
                TempData["idCardBack"] = document.IDCardBackPhoto;

                // Sending the images as Base64 encoded strings to be used in AJAX
                ViewBag.PassportPhoto = document.PassportPhoto != null ? Convert.ToBase64String(document.PassportPhoto) : null;
                ViewBag.SignaturePhoto = document.SignaturePhoto != null ? Convert.ToBase64String(document.SignaturePhoto) : null;
                ViewBag.IDCardFrontPhoto = document.IDCardFrontPhoto != null ? Convert.ToBase64String(document.IDCardFrontPhoto) : null;
                ViewBag.IDCardBackPhoto = document.IDCardBackPhoto != null ? Convert.ToBase64String(document.IDCardBackPhoto) : null;

            }
            return View(customerDTO.MapTo<CustomerBindingModel>());



        }

        [HttpPost]
        public async Task<ActionResult> Edit(CustomerBindingModel customerBindingModel, string typedescription, HttpPostedFileBase signaturePhoto, HttpPostedFileBase idCardFrontPhoto, HttpPostedFileBase idCardBackPhoto, string passportPhotoDataUrl, string type)
        {

            if (typedescription == "Individual")
            {
                customerBindingModel.Type = 0;
            }

            if (typedescription == "Partnership")
            {
                customerBindingModel.Type = 1;
                customerBindingModel.NonIndividualDescription = customerBindingModel.RefereeFirstName;

            }
            if (typedescription == "Corporation")
            {
                customerBindingModel.Type = 2;
                customerBindingModel.NonIndividualDescription = customerBindingModel.RefereeFirstName;

            }
            if (typedescription == "MicroCredit")
            {
                customerBindingModel.Type = 3;
                customerBindingModel.NonIndividualDescription = customerBindingModel.RefereeFirstName;
            }
            customerBindingModel.IndividualBirthDate = new DateTime(1990, DateTime.Today.Month, DateTime.Today.Day);
            if (TempData["Reference2"] != null)
            {
                customerBindingModel.Reference2 = TempData["Reference2"].ToString();
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

            //customerBindingModel.ValidateAll();

            // Prepare mandatory products and services
            //  var mandatoryProduct = await PrepareMandatoryProductCollection();
            customerBindingModel.IndividualBirthDate = new DateTime(1990, DateTime.Today.Month, DateTime.Today.Day);

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

                    if (result == true)
                    {
                        // TempData["SuccessMessage"] = "Teller created successfully.";

                        System.Windows.Forms.MessageBox.Show(
                            "Customer " + customerDTO.FullName + " Edited successfully.",
                            "Success",
                            System.Windows.Forms.MessageBoxButtons.OK,
                            System.Windows.Forms.MessageBoxIcon.Information,
                            System.Windows.Forms.MessageBoxDefaultButton.Button1,
                            System.Windows.Forms.MessageBoxOptions.ServiceNotification
                        );
                        ViewBag.recordStatus = GetRecordStatusSelectList(string.Empty);
                        ViewBag.customerFilter = GetCustomerFilterSelectList(string.Empty);
                        ViewBag.CustomerTypeSelectList = GetCustomerTypeSelectList(string.Empty);
                        await ServeNavigationMenus();
                        return RedirectToAction("Index", customerBindingModel);
                    }
                    if (signaturePhoto != null)
                    {
                        await SaveDocumentAsync(ProcessDocumentUpload(customerDTO.Id, signaturePhoto, idCardFrontPhoto, idCardBackPhoto, passportPhotoDataUrl));
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