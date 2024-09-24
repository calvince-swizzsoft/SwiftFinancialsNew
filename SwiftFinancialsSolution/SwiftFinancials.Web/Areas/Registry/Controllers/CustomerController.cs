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
using System.Web.Configuration;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.Registry.Controllers
{
    [RoleBasedAccessControl]
    public class CustomerController : MasterController
    {
        private readonly string _connectionString;

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
        public async Task<JsonResult> Index(JQueryDataTablesModel jQueryDataTablesModel,int? recordStatus,int? customerFilter)
        {
            int totalRecordCount = 0;

            int searchRecordCount = 0;



            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;

            var pageCollectionInfo = new PageCollectionInfo<CustomerDTO>();

            if(recordStatus != null && customerFilter != null)
            {
                pageCollectionInfo = await _channelService.FindCustomersByRecordStatusAndFilterInPageAsync((int)recordStatus, jQueryDataTablesModel.sSearch, (int)customerFilter, pageIndex, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());
            }
            else if(recordStatus == null && customerFilter != null)
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
        public async Task<ActionResult> Create(CustomerBindingModel customerBindingModel, HttpPostedFileBase signaturePhoto, HttpPostedFileBase idCardFrontPhoto, HttpPostedFileBase idCardBackPhoto, string passportPhotoDataUrl)
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


            var registrationdate = Request["registrationdate"];
            var birthdate = Request["birthdate"];
            var employmentdate = Request["employmentdate"];


            var filesCount = Request.Files.Count;
            var signatureFile = Request.Files["signaturePhoto"];
            var idFrontFile = Request.Files["idCardFrontPhoto"];
            var idBackFile = Request.Files["idCardBackPhoto"];

            if (signaturePhoto == null || idCardFrontPhoto == null || idCardBackPhoto == null)
            {
                var errorMsg = $"Missing files: {nameof(signaturePhoto)} is null: {signaturePhoto == null}, {nameof(idCardFrontPhoto)} is null: {idCardFrontPhoto == null}, {nameof(idCardBackPhoto)} is null: {idCardBackPhoto == null}.";
                throw new Exception(errorMsg);
            }


            customerBindingModel.IndividualBirthDate = DateTime.ParseExact((Request["birthdate"].ToString()), "dd/MM/yyyy", CultureInfo.InvariantCulture);

            customerBindingModel.RegistrationDate = DateTime.ParseExact((Request["registrationdate"].ToString()), "dd/MM/yyyy", CultureInfo.InvariantCulture);

            customerBindingModel.IndividualEmploymentDate = DateTime.ParseExact((Request["employmentdate"].ToString()), "dd/MM/yyyy", CultureInfo.InvariantCulture);

            var userDTO = await _applicationUserManager.FindByIdAsync(User.Identity.GetUserId());
            var roles= userDTO.Roles;


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
                    TempData["DefaultError"] = result.ErrorMessageResult.ToString();

                    return View();
                }

                // Handle Document Upload to DB.......
                var document = new Document
                {
                    Id = Guid.NewGuid(),
                    CustomerId = result.Id
                };

                // Process the passport photo from the data URL
                if (!string.IsNullOrEmpty(passportPhotoDataUrl))
                {
                    var base64Data = passportPhotoDataUrl.Split(',')[1];
                    var imageBytes = Convert.FromBase64String(base64Data);
                    document.PassportPhoto = imageBytes;
                }

                // Process other uploaded files
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

                await SaveDocumentAsync(document);

                ViewBag.CustomerTypeSelectList = GetCustomerTypeSelectList(customerBindingModel.TypeDescription.ToString());
                ViewBag.IndividualTypeSelectList = GetIndividualTypeSelectList(customerBindingModel.IndividualType.ToString());
                ViewBag.IdentityCardSelectList = GetIdentityCardTypeSelectList(customerBindingModel.IndividualIdentityCardType.ToString());
                ViewBag.SalutationSelectList = GetSalutationSelectList(customerBindingModel.IndividualSalutation.ToString());
                ViewBag.GenderSelectList = GetGenderSelectList(customerBindingModel.IndividualGender.ToString());
                ViewBag.MaritalStatusSelectList = GetMaritalStatusSelectList(customerBindingModel.IndividualMaritalStatus.ToString());
                ViewBag.IndividualNationalitySelectList = GetNationalitySelectList(customerBindingModel.IndividualNationality.ToString());
                ViewBag.IndividualEmploymentTermsOfServiceSelectList = GetTermsOfServiceSelectList(customerBindingModel.IndividualEmploymentTermsOfService.ToString());
                ViewBag.IndividualClassificationSelectList = GetCustomerClassificationSelectList(customerBindingModel.IndividualClassification.ToString());

                // await _channelService.UpdateRefereeCollectionByCustomerIdAsync(result.Id,)

                TempData["SuccessMessage"] = "Successfully Created Customer " + customerBindingModel.FullName;

                //return View("Index");

                //TempData["SuccessMessage"] = "Successfully Created Customer " + customerBindingModel.FullName;
                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = customerBindingModel.ErrorMessages;
                TempData["Error2"] = customerBindingModel.ErrorMessages;

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

                return View("Create", customerBindingModel);
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
                    await command.ExecuteNonQueryAsync();
                }
            }
        }



        public async Task<ActionResult> Directors()
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
        public async Task<ActionResult> Directors(CustomerBindingModel customerBindingModel)
        {
            var registrationdate = Request["registrationdate"];
            var birthdate = Request["birthdate"];
            var employmentdate = Request["employmentdate"];

            customerBindingModel.IndividualBirthDate = DateTime.ParseExact((Request["birthdate"].ToString()), "dd/MM/yyyy", CultureInfo.InvariantCulture);

            customerBindingModel.RegistrationDate = DateTime.ParseExact((Request["registrationdate"].ToString()), "dd/MM/yyyy", CultureInfo.InvariantCulture);

            customerBindingModel.IndividualEmploymentDate = DateTime.ParseExact((Request["employmentdate"].ToString()), "dd/MM/yyyy", CultureInfo.InvariantCulture);

            customerBindingModel.ValidateAll();


            //cheat
            var mandatoryInvestmentProducts = new List<InvestmentProductDTO>();
            var mandatorySavingsProducts = new List<SavingsProductDTO>();
            var mandatoryDebitTypes = new List<DebitTypeDTO>();
            var mandatoryProducts = new ProductCollectionInfo();


            var debitTypes = await _channelService.FindMandatoryDebitTypesAsync(true, GetServiceHeader());
            var investmentProducts = await _channelService.FindMandatoryInvestmentProductsAsync(true, GetServiceHeader());
            var savingsProducts = await _channelService.FindMandatorySavingsProductsAsync(true, GetServiceHeader());

            mandatoryProducts.InvestmentProductCollection = investmentProducts.ToList();
            mandatoryProducts.SavingsProductCollection = savingsProducts.ToList();

            if (!customerBindingModel.HasErrors)
            {
                await _channelService.AddCustomerAsync(customerBindingModel.MapTo<CustomerDTO>(), debitTypes.ToList(), investmentProducts.ToList(), savingsProducts.ToList(), mandatoryProducts, 1, GetServiceHeader());
                TempData["SuccessMessage"] = "Successfully Created Customer " + customerBindingModel.FullName;

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = customerBindingModel.ErrorMessages;

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





        public async Task<ActionResult> Delegates(Guid id)
        {
            await ServeNavigationMenus();
            var customer = await _channelService.FindCustomerAsync(id, GetServiceHeader());

            DelegateDTO withdrawalNotificationDTO = new DelegateDTO();

            if (customer != null)
            {

                withdrawalNotificationDTO.CustomerId = customer.Id;
                withdrawalNotificationDTO.CustomerIndividualFirstName = customer.FullName;
                withdrawalNotificationDTO.CustomerIndividualPayrollNumbers = customer.IndividualPayrollNumbers;
                withdrawalNotificationDTO.CustomerSerialNumber = customer.SerialNumber;
                withdrawalNotificationDTO.CustomerIndividualIdentityCardNumber = customer.IndividualIdentityCardNumber;
                withdrawalNotificationDTO.CustomerStationZoneDescription = customer.StationZoneDescription;
                withdrawalNotificationDTO.ZoneDivisionEmployerDescription = customer.StationZoneDivisionEmployerDescription;
            }

            return View(withdrawalNotificationDTO);

        }

        [HttpPost]
        public async Task<ActionResult> Delegates(DelegateDTO customerBindingModel)
        {
            if (!customerBindingModel.HasErrors)
            {
                await _channelService.AddDelegateAsync(customerBindingModel, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = customerBindingModel.ErrorMessages;
                return View(customerBindingModel);
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

        public async Task<ActionResult> BranchLinkage(Guid id)
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
        public async Task<ActionResult> BranchLinkage(Guid id, CustomerBindingModel customerBindingModel)
        {
            customerBindingModel.ValidateAll();

            if (!customerBindingModel.HasErrors)
            {
                await _channelService.UpdateCustomerAsync(customerBindingModel.MapTo<CustomerDTO>(), GetServiceHeader());
                TempData["SuccessMessage"] = $"Successfully {customerBindingModel.RecordStatusDescription} Customer {customerBindingModel.FullName}";
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




        public async Task<ActionResult> Documents(Guid id)
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
        public async Task<ActionResult> Documents(Guid id, CustomerBindingModel customerBindingModel)
        {
            customerBindingModel.ValidateAll();

            if (!customerBindingModel.HasErrors)
            {
                await _channelService.UpdateCustomerAsync(customerBindingModel.MapTo<CustomerDTO>(), GetServiceHeader());
                TempData["SuccessMessage"] = $"Successfully {customerBindingModel.RecordStatusDescription} Customer {customerBindingModel.FullName}";
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