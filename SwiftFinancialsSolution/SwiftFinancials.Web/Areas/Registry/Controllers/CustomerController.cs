using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.RegistryModule;
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
                pageCollectionInfo = await _channelService.FindCustomersByFilterInPageAsync(jQueryDataTablesModel.sSearch, (int)CustomerFilter.FirstName, pageIndex, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());
            }

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<CustomerDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            //var customerDTO = await _channelService.FindCustomerAsync(id, GetServiceHeader());

            //return View(customerDTO.ProjectedAs<CustomerDTO>());

            var documents = await GetDocumentsAsync(id);

            if (documents == null || documents.Count == 0)
            {
                return View();
            }

            return View(documents);
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

            var debitTypes = await _channelService.FindMandatoryDebitTypesAsync(false, GetServiceHeader());
            var creditTypes = await _channelService.FindCreditTypesAsync(GetServiceHeader());
            var investmentProducts = await _channelService.FindMandatoryInvestmentProductsAsync(false, GetServiceHeader());
            var savingsProducts = await _channelService.FindMandatorySavingsProductsAsync(false, GetServiceHeader());
            ViewBag.investment = investmentProducts;
            ViewBag.savings = savingsProducts;
            ViewBag.debit = debitTypes;
            ViewBag.credit = creditTypes;

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(CustomerBindingModel customerBindingModel, HttpPostedFileBase signaturePhoto, HttpPostedFileBase idCardFrontPhoto, HttpPostedFileBase idCardBackPhoto, string passportPhotoDataUrl, HttpPostedFileBase uploadedPassportPhoto)
        {
            switch (customerBindingModel.Type)
            {
                case (int)CustomerType.Individual:
                    // Populate the dropdown lists for the Individual type
                    PopulateIndividualDropdownLists(customerBindingModel);

                    // Validate file uploads
                    if (signaturePhoto == null || idCardFrontPhoto == null || idCardBackPhoto == null)
                    {
                        TempData["ErrorMessage"] = "Please provide all required files.";
                        return View("Create", customerBindingModel);
                    }

                    // Parse dates from form data
                    ParseIndividualDates(customerBindingModel);

                    // Set BranchId if available
                    var userDTO = await _applicationUserManager.FindByIdAsync(User.Identity.GetUserId());
                    if (userDTO.BranchId != null)
                    {
                        customerBindingModel.BranchId = (Guid)userDTO.BranchId;
                    }

                    // Validate the customer model
                    customerBindingModel.ValidateAll();

                    if (!customerBindingModel.HasErrors)
                    {
                        // Add the customer and handle success/error messages
                        var result = await AddIndividualCustomerAsync(customerBindingModel, passportPhotoDataUrl, uploadedPassportPhoto, signaturePhoto, idCardFrontPhoto, idCardBackPhoto);
                        if (result)
                        {
                            TempData["SuccessMessage"] = $"Successfully Created Customer {customerBindingModel.FullName}";
                            return RedirectToAction("Index");
                        }
                        else
                        {
                            TempData["ErrorMessage"] = "Failed to create customer. Invalid data provided.";
                            return View("Create", customerBindingModel);
                        }
                    }
                    else
                    {
                        TempData["Error2"] = customerBindingModel.ErrorMessages;
                        return View("Create", customerBindingModel);
                    }

                case (int)CustomerType.Corporation:
                    if (ModelState.IsValid)
                    {
                        // Logic for Corporation type
                        return RedirectToAction("Index");
                    }
                    break;

                case (int)CustomerType.MicroCredit:
                    customerBindingModel.ValidateAll();
                    if (!customerBindingModel.HasErrors)
                    {
                        // Logic for MicroCredit type
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        AddModelErrors(customerBindingModel.ErrorMessages);
                        return View(customerBindingModel);
                    }
                    break;

                case (int)CustomerType.Partnership:
                    if (ModelState.IsValid)
                    {
                        // Logic for Partnership type
                        return RedirectToAction("Index");
                    }
                    break;

                default:
                    TempData["ErrorMessage"] = "Unsupported customer type.";
                    return View("Index", customerBindingModel);
            }

            return View("Index", customerBindingModel);
        }

        // Helper methods to clean up code and improve readability
        private void PopulateIndividualDropdownLists(CustomerBindingModel customerBindingModel)
        {
            ViewBag.CustomerTypeSelectList = GetCustomerTypeSelectList(customerBindingModel.TypeDescription.ToString());
            ViewBag.IndividualTypeSelectList = GetIndividualTypeSelectList(customerBindingModel.IndividualType.ToString());
            ViewBag.IdentityCardSelectList = GetIdentityCardTypeSelectList(customerBindingModel.IndividualIdentityCardType.ToString());
            ViewBag.SalutationSelectList = GetSalutationSelectList(customerBindingModel.IndividualSalutation.ToString());
            ViewBag.GenderSelectList = GetGenderSelectList(customerBindingModel.IndividualGender.ToString());
            ViewBag.MaritalStatusSelectList = GetMaritalStatusSelectList(customerBindingModel.IndividualMaritalStatus.ToString());
            ViewBag.IndividualNationalitySelectList = GetNationalitySelectList(customerBindingModel.IndividualNationality.ToString());
            ViewBag.IndividualEmploymentTermsOfServiceSelectList = GetTermsOfServiceSelectList(customerBindingModel.IndividualEmploymentTermsOfService.ToString());
            ViewBag.IndividualClassificationSelectList = GetCustomerClassificationSelectList(customerBindingModel.IndividualClassification.ToString());
        }

        private void ParseIndividualDates(CustomerBindingModel customerBindingModel)
        {
            customerBindingModel.IndividualBirthDate = DateTime.ParseExact(Request["birthdate"].ToString(), "dd/MM/yyyy", CultureInfo.InvariantCulture);
            customerBindingModel.RegistrationDate = DateTime.ParseExact(Request["registrationdate"].ToString(), "dd/MM/yyyy", CultureInfo.InvariantCulture);
            customerBindingModel.IndividualEmploymentDate = DateTime.ParseExact(Request["employmentdate"].ToString(), "dd/MM/yyyy", CultureInfo.InvariantCulture);
        }

        private async Task<bool> AddIndividualCustomerAsync(CustomerBindingModel customerBindingModel, string passportPhotoDataUrl, HttpPostedFileBase uploadedPassportPhoto, HttpPostedFileBase signaturePhoto, HttpPostedFileBase idCardFrontPhoto, HttpPostedFileBase idCardBackPhoto)
        {
            var mandatoryProducts = new ProductCollectionInfo
            {
                InvestmentProductCollection = (await _channelService.FindMandatoryInvestmentProductsAsync(true, GetServiceHeader())).ToList(),
                SavingsProductCollection = (await _channelService.FindMandatorySavingsProductsAsync(false, GetServiceHeader())).ToList()
            };

            var result = await _channelService.AddCustomerAsync(customerBindingModel.MapTo<CustomerDTO>(),
                (await _channelService.FindMandatoryDebitTypesAsync(true, GetServiceHeader())).ToList(),
                mandatoryProducts.InvestmentProductCollection,
                mandatoryProducts.SavingsProductCollection,
                mandatoryProducts, 1, GetServiceHeader());

            if (result.ErrorMessageResult == null)
            {
                var document = new Document { Id = Guid.NewGuid(), CustomerId = result.Id };
                string passportImageURL = passportPhotoDataUrl;
                HttpPostedFileBase uploadedPassportPhotoImage = uploadedPassportPhoto;

                if (string.IsNullOrEmpty(passportImageURL) && uploadedPassportPhotoImage != null)
                {
                    var passportFilePath = Path.Combine(Server.MapPath("~/Uploads/PassportPhotos"), Guid.NewGuid() + Path.GetExtension(uploadedPassportPhotoImage.FileName));
                    uploadedPassportPhotoImage.SaveAs(passportFilePath);
                    passportImageURL = passportFilePath;
                }

                ProcessDocumentUploads(document, passportImageURL, uploadedPassportPhoto, signaturePhoto, idCardFrontPhoto, idCardBackPhoto);
                await SaveDocumentAsync(document);
                return true;
            }

            TempData["DefaultError"] = result.ErrorMessageResult.ToString();
            return false;
        }

        private void ProcessDocumentUploads(Document document, string passportPhotoDataUrl, HttpPostedFileBase uploadedPassportPhoto, HttpPostedFileBase signaturePhoto, HttpPostedFileBase idCardFrontPhoto, HttpPostedFileBase idCardBackPhoto)
        {
            byte[] passportPhotoData = null;

            if (!string.IsNullOrEmpty(passportPhotoDataUrl))
            {
                var base64Data = passportPhotoDataUrl.Split(',')[1];
                passportPhotoData = Convert.FromBase64String(base64Data);
            }
            else if (uploadedPassportPhoto != null && uploadedPassportPhoto.ContentLength > 0)
            {
                using (var memoryStream = new MemoryStream())
                {
                    uploadedPassportPhoto.InputStream.CopyTo(memoryStream);
                    passportPhotoData = memoryStream.ToArray();
                }
            }

            if (passportPhotoData != null)
            {
                document.PassportPhoto = passportPhotoData;
            }

            if (signaturePhoto != null && signaturePhoto.ContentLength > 0)
            {
                using (var memoryStream = new MemoryStream())
                {
                    signaturePhoto.InputStream.CopyTo(memoryStream);
                    document.SignaturePhoto = memoryStream.ToArray();
                }
            }

            if (idCardFrontPhoto != null && idCardFrontPhoto.ContentLength > 0)
            {
                using (var memoryStream = new MemoryStream())
                {
                    idCardFrontPhoto.InputStream.CopyTo(memoryStream);
                    document.IDCardFrontPhoto = memoryStream.ToArray();
                }
            }

            if (idCardBackPhoto != null && idCardBackPhoto.ContentLength > 0)
            {
                using (var memoryStream = new MemoryStream())
                {
                    idCardBackPhoto.InputStream.CopyTo(memoryStream);
                    document.IDCardBackPhoto = memoryStream.ToArray();
                }
            }
        }

        private void AddModelErrors(IEnumerable<string> errorMessages)
        {
            foreach (var error in errorMessages)
            {
                ModelState.AddModelError(string.Empty, error);
            }
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
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, CustomerBindingModel customerBindingModel)
        {
            customerBindingModel.ValidateAll();

            if (!customerBindingModel.HasErrors)
            {
                await _channelService.UpdateCustomerAsync(customerBindingModel.MapTo<CustomerDTO>(), GetServiceHeader());
                TempData["SuccessMessage"] = $"Successfully Edited {customerBindingModel.RecordStatusDescription} Customer {customerBindingModel.FullName}";
                ViewBag.recordstatus = GetRecordStatusSelectList(customerBindingModel.RecordStatus.ToString());
                ViewBag.CustomerTypeSelectList = GetCustomerTypeSelectList(customerBindingModel.Type.ToString());
                return RedirectToAction("Index");
            }
            else
            {
                ViewBag.recordstatus = GetRecordStatusSelectList(customerBindingModel.RecordStatus.ToString());
                ViewBag.CustomerTypeSelectList = GetCustomerTypeSelectList(customerBindingModel.Type.ToString());
                ViewBag.IndividualTypeSelectList = GetIndividualTypeSelectList(customerBindingModel.IndividualType.ToString());
                ViewBag.IdentityCardSelectList = GetIdentityCardTypeSelectList(customerBindingModel.IndividualIdentityCardType.ToString());
                ViewBag.SalutationSelectList = GetSalutationSelectList(customerBindingModel.IndividualSalutation.ToString());
                ViewBag.GenderSelectList = GetGenderSelectList(customerBindingModel.IndividualGender.ToString());
                ViewBag.MaritalStatusSelectList = GetMaritalStatusSelectList(customerBindingModel.IndividualMaritalStatus.ToString());
                ViewBag.IndividualNationalitySelectList = GetNationalitySelectList(customerBindingModel.IndividualNationality.ToString());
                ViewBag.IndividualEmploymentTermsOfServiceSelectList = GetTermsOfServiceSelectList(customerBindingModel.IndividualEmploymentTermsOfService.ToString());
                ViewBag.IndividualClassificationSelectList = GetCustomerClassificationSelectList(customerBindingModel.IndividualClassification.ToString());

                return View(customerBindingModel);
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

        public async Task<ActionResult> NextofKin(Guid id, NextOfKinDTO nextOfKinDTO)
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

            Session["customerDTO"] = customerDTO;
            return View(nextOfKinDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> NextofKin(NextOfKinDTO nextOfKinDTO, ObservableCollection<NextOfKinDTO> nextOfKinCollection)
        {


            nextOfKinCollection.Add(nextOfKinDTO);

            var customer = Session["customerDTO"] as CustomerDTO;

            await _channelService.UpdateNextOfKinCollectionAsync(customer, nextOfKinCollection, GetServiceHeader());

            return RedirectToAction("Index");

        }

        public async Task<ActionResult> AccountAlerts()
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

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> AccountAlerts(CustomerBindingModel customerBindingModel)
        {
            var registrationdate = Request["registrationdate"];
            var birthdate = Request["birthdate"];
            var employmentdate = Request["employmentdate"];

            customerBindingModel.IndividualBirthDate = DateTime.ParseExact((Request["birthdate"].ToString()), "dd/MM/yyyy", CultureInfo.InvariantCulture);

            customerBindingModel.RegistrationDate = DateTime.ParseExact((Request["registrationdate"].ToString()), "dd/MM/yyyy", CultureInfo.InvariantCulture);

            customerBindingModel.IndividualEmploymentDate = DateTime.ParseExact((Request["employmentdate"].ToString()), "dd/MM/yyyy", CultureInfo.InvariantCulture);

            var userDTO = await _applicationUserManager.FindByIdAsync(User.Identity.GetUserId());

            if (userDTO.BranchId != null)
            {

                customerBindingModel.BranchId = (Guid)userDTO.BranchId;

            }


            customerBindingModel.ValidateAll();

            //cheat
            var mandatoryInvestmentProducts = new List<InvestmentProductDTO>();
            var mandatorySavingsProducts = new List<SavingsProductDTO>();
            var mandatoryDebitTypes = new List<DebitTypeDTO>();
            var mandatoryProducts = new ProductCollectionInfo();


            var debitTypes = await _channelService.FindMandatoryDebitTypesAsync(true, GetServiceHeader());
            var investmentProducts = await _channelService.FindMandatoryInvestmentProductsAsync(true, GetServiceHeader());
            var savingsProducts = await _channelService.FindMandatorySavingsProductsAsync(false, GetServiceHeader());

            mandatoryProducts.InvestmentProductCollection = investmentProducts.ToList();
            mandatoryProducts.SavingsProductCollection = savingsProducts.ToList();

            if (!customerBindingModel.HasErrors)
            {

                var result = await _channelService.AddCustomerAsync(customerBindingModel.MapTo<CustomerDTO>(), debitTypes.ToList(), investmentProducts.ToList(), savingsProducts.ToList(), mandatoryProducts, 1, GetServiceHeader());
                if (result.ErrorMessageResult != null)
                {
                    TempData["Error"] = result.ErrorMessageResult + result.ErrorMessages;
                    await ServeNavigationMenus();
                    ViewBag.CustomerTypeSelectList = GetCustomerTypeSelectList(customerBindingModel.Type.ToString());
                    ViewBag.IndividualTypeSelectList = GetIndividualTypeSelectList(customerBindingModel.IndividualType.ToString());
                    ViewBag.IdentityCardSelectList = GetIdentityCardTypeSelectList(customerBindingModel.IndividualIdentityCardType.ToString());
                    ViewBag.SalutationSelectList = GetSalutationSelectList(customerBindingModel.IndividualSalutation.ToString());
                    ViewBag.GenderSelectList = GetGenderSelectList(customerBindingModel.IndividualGender.ToString());
                    ViewBag.MaritalStatusSelectList = GetMaritalStatusSelectList(customerBindingModel.IndividualMaritalStatus.ToString());
                    ViewBag.IndividualNationalitySelectList = GetNationalitySelectList(customerBindingModel.IndividualNationality.ToString());
                    ViewBag.IndividualEmploymentTermsOfServiceSelectList = GetTermsOfServiceSelectList(customerBindingModel.IndividualEmploymentTermsOfService.ToString());
                    ViewBag.IndividualClassificationSelectList = GetCustomerClassificationSelectList(customerBindingModel.IndividualClassification.ToString());



                    return View("create", customerBindingModel);
                }
                TempData["SuccessMessage"] = "Successfully Created Account Alert " + customerBindingModel.FullName;
                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = customerBindingModel.ErrorMessages;
                TempData["Error2"] = customerBindingModel.ErrorMessages;

                ViewBag.CustomerTypeSelectList = GetCustomerTypeSelectList(customerBindingModel.Type.ToString());
                ViewBag.IndividualTypeSelectList = GetIndividualTypeSelectList(customerBindingModel.IndividualType.ToString());
                ViewBag.IdentityCardSelectList = GetIdentityCardTypeSelectList(customerBindingModel.IndividualIdentityCardType.ToString());
                ViewBag.SalutationSelectList = GetSalutationSelectList(customerBindingModel.IndividualSalutation.ToString());
                ViewBag.GenderSelectList = GetGenderSelectList(customerBindingModel.IndividualGender.ToString());
                ViewBag.MaritalStatusSelectList = GetMaritalStatusSelectList(customerBindingModel.IndividualMaritalStatus.ToString());
                ViewBag.IndividualNationalitySelectList = GetNationalitySelectList(customerBindingModel.IndividualNationality.ToString());
                ViewBag.IndividualEmploymentTermsOfServiceSelectList = GetTermsOfServiceSelectList(customerBindingModel.IndividualEmploymentTermsOfService.ToString());
                ViewBag.IndividualClassificationSelectList = GetCustomerClassificationSelectList(customerBindingModel.IndividualClassification.ToString());

                return View("Create", customerBindingModel);
            }
        }

    }
}