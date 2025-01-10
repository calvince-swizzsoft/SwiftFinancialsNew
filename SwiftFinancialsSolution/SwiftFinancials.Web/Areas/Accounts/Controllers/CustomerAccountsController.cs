using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.RegistryModule;
using Infrastructure.Crosscutting.Framework.Utils;
using SwiftFinancials.Presentation.Infrastructure.Util;
using SwiftFinancials.Web.Areas.Registry.DocumentsModel;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;


namespace SwiftFinancials.Web.Areas.Accounts.Controllers
{
    public class CustomerAccountsController : MasterController
    {

        private readonly string _connectionString;
        private HttpPostedFileBase uploadedPassportPhoto;

        public CustomerAccountsController()
        {
            // Get connection string from Web.config
            _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;
        }
        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();
            ViewBag.ProductCode = GetProductCodeSelectList(string.Empty);
            ViewBag.RecordStatus = GetRecordStatusSelectList(string.Empty);
            ViewBag.customerFilter = GetCustomerFilterSelectList(string.Empty);
            ViewBag.CustomerTypeSelectList = GetCustomerTypeSelectList(string.Empty);

            return View();
        }

        [HttpPost]
        public async Task<JsonResult> Index(JQueryDataTablesModel jQueryDataTablesModel, int? productCode, int? recordStatus)
        {
            int totalRecordCount = 0;

            int searchRecordCount = 0;

            int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = new PageCollectionInfo<CustomerAccountDTO>();

            if (productCode != null && recordStatus != null)
            {
                pageCollectionInfo = await _channelService.FindCustomerAccountsByProductCodeAndRecordStatusAndFilterInPageAsync((int)productCode, (int)recordStatus, jQueryDataTablesModel.sSearch, (int)CustomerFilter.FirstName, 0, int.MaxValue, false, false, false, false, GetServiceHeader());

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
                    items: new List<CustomerAccountDTO>(),
                    totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount,
                    sEcho: jQueryDataTablesModel.sEcho
            );
            }

            else if (productCode != null && recordStatus == null)
            {
                pageCollectionInfo = await _channelService.FindCustomerAccountsByProductCodeAndFilterInPageAsync((int)productCode, jQueryDataTablesModel.sSearch, (int)CustomerFilter.FirstName, pageIndex, jQueryDataTablesModel.iDisplayLength, false, false, false, false, GetServiceHeader());
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
            }
            else if (productCode == null && recordStatus == null)
            {
                pageCollectionInfo = await _channelService.FindCustomerAccountsInPageAsync(pageIndex, jQueryDataTablesModel.iDisplayLength, false, false, false, false, GetServiceHeader());
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
            }
            else
            {
                pageCollectionInfo = await _channelService.FindCustomerAccountsInPageAsync(pageIndex, jQueryDataTablesModel.iDisplayLength, false, false, false, false, GetServiceHeader());
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
            }



            return this.DataTablesJson(
                items: new List<CustomerDTO>(),
                totalRecords: totalRecordCount,
                totalDisplayRecords: searchRecordCount,
                sEcho: jQueryDataTablesModel.sEcho);
        }


        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();
            bool includeInterestBalanceForLoanAccounts = false;
            bool includeBalances = false;
            bool includeProductDescription = false;
            bool considerMaturityPeriodForInvestmentAccounts = false;
            var CustomerAccount = await _channelService.FindCustomerAccountAsync(id, includeInterestBalanceForLoanAccounts, includeBalances, includeProductDescription, considerMaturityPeriodForInvestmentAccounts, GetServiceHeader());

            return View();
        }
        [HttpPost]
        public async Task<ActionResult> Add(Guid? id, CustomerAccountDTO customerAccountDTO)
        {
            await ServeNavigationMenus();

            ViewBag.ProductCode = GetProductCodeSelectList(string.Empty);
            ViewBag.RecordStatus = GetRecordStatusSelectList(string.Empty);
            ViewBag.customerFilter = GetCustomerFilterSelectList(string.Empty);
            ViewBag.CustomerTypeSelectList = GetCustomerTypeSelectList(string.Empty);


            if (Session["BranchId"] != null)
            {
                customerAccountDTO.BranchId = (Guid)Session["BranchId"];
            }
            if (Session["BranchDescription"] != null)
            {
                customerAccountDTO.BranchDescription = Session["BranchDescription"].ToString();
            }
            savingsProductDTOs = TempData["savingsProductDTOs"] as ObservableCollection<SavingsProductDTO>;

            if (savingsProductDTOs == null)
                savingsProductDTOs = new ObservableCollection<SavingsProductDTO>();

            foreach (var expensePayableEntryDTO in customerAccountDTO.savingsProducts)
            {
                expensePayableEntryDTO.Id = customerAccountDTO.savingsProducts[0].Id;
                expensePayableEntryDTO.ChartOfAccountId = customerAccountDTO.CustomerAccountTypeTargetProductId;
                expensePayableEntryDTO.ChartOfAccountAccountName = expensePayableEntryDTO.ChartOfAccountAccountName;
                expensePayableEntryDTO.Description = expensePayableEntryDTO.Description;
                //expensePayableEntryDTO.ChartOfAccountAccountName = expensePayableEntryDTO.ChartOfAccountName;
                //expensePayableEntryDTO.TotalValue = expensePayableEntryDTO.TotalValue;
                //expensePayableEntryDTO.PrimaryDescription = expensePayableEntryDTO.PrimaryDescription;
                //expensePayableEntryDTO.SecondaryDescription = expensePayableEntryDTO.SecondaryDescription;
                //expensePayableEntryDTO.Reference = expensePayableEntryDTO.Reference;
                savingsProductDTOs.Add(expensePayableEntryDTO);
            };

            TempData["savingsProductDTOs"] = savingsProductDTOs;


            TempData["customerAccountDTO"] = customerAccountDTO;
            Session["savingsProductDTOs"] = savingsProductDTOs;
            ViewBag.savingsProductDTOs = savingsProductDTOs;
            ViewBag.JournalVoucherTypeSelectList = GetJournalVoucherTypeSelectList(customerAccountDTO.Type.ToString());
            ViewBag.JournalVoucherEntryTypeSelectList = GetJournalVoucherEntryTypeSelectList(customerAccountDTO.Type.ToString());
            ViewBag.JournalVoucherTypeSelectList = GetJournalVoucherTypeSelectList(customerAccountDTO.Type.ToString());
            if (Session["investmentProductDTOs"] != null)
            {
                investmentProductDTOs = Session["investmentProductDTOs"] as ObservableCollection<InvestmentProductDTO>;
                ViewBag.investmentProductDTOs = investmentProductDTOs;
            }
            if (Session["customerAccountDTO"] != null)
            {
                customerAccountDTO = Session["customerAccountDTO"] as CustomerAccountDTO;
            }
            return View("Create", customerAccountDTO);
        }

        [HttpGet]
        public async Task<ActionResult> GetCustomerDetails(Guid customerId)
        {
            try
            {
                var customer = await _channelService.FindCustomerAsync(customerId, GetServiceHeader());
                Session["customerDTO"] = customer;

                if (customer == null)
                {
                    return Json(new { success = false, message = "Customer not found." }, JsonRequestBehavior.AllowGet);
                }
                var savingsProductDTOs = await _channelService.FindSavingsProductsAsync(GetServiceHeader());
                var investment = await _channelService.FindInvestmentProductsAsync(GetServiceHeader());
                var debitypes = await _channelService.FindDebitTypesAsync(GetServiceHeader());
                ViewBag.investment = investment;
                ViewBag.savings = savingsProductDTOs;
                ViewBag.debit = debitypes;
                // Assuming you want to store the customer Id in Session
                Session["id"] = customer.Id;

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        IndividualFirstName = customer.IndividualFirstName,
                        customerId = customer.Id,
                        IndividualLastName = customer.IndividualLastName,
                        FullName = customer.FullName,
                        StationZoneDivisionEmployerId = customer.StationZoneDivisionEmployerId,
                        StationZoneDivisionEmployerDescription = customer.StationZoneDivisionEmployerDescription,
                        IndividualIdentityCardNumber = customer.IndividualIdentityCardNumber,
                        IndividualPayrollNumbers = customer.IndividualPayrollNumbers,
                        Reference1 = customer.Reference1,
                        Reference2 = customer.Reference2,
                        Reference3 = customer.Reference3,
                        StationId = customer.StationId,
                        StationDescription = customer.StationDescription,
                        SerialNumber = customer.SerialNumber,
                        Remarks = customer.Remarks,
                    }
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                // Log the exception (optional)
                return Json(new { success = false, message = "An error occurred while fetching the customer details." }, JsonRequestBehavior.AllowGet);
            }
        }


        public async Task<ActionResult> Create(Guid? id, CustomerAccountDTO customerAccount)
        {
            Session["branchid"] = customerAccount.BranchId;
            await ServeNavigationMenus();
            ViewBag.CustomerAccountManagementActionSelectList = GetCustomerAccountManagementActionSelectList(string.Empty);
            ViewBag.ProductCode = GetProductCodeSelectList(string.Empty);
            ViewBag.RecordStatus = GetRecordStatusSelectList(string.Empty);
            ViewBag.customerFilter = GetCustomerFilterSelectList(string.Empty);
            ViewBag.CustomerTypeSelectList = GetCustomerTypeSelectList(string.Empty);
            Guid parseId;

            var savingsProductDTOs = await _channelService.FindSavingsProductsAsync(GetServiceHeader());
            var investment = await _channelService.FindInvestmentProductsAsync(GetServiceHeader());
            var debitypes = await _channelService.FindLoanProductsAsync(GetServiceHeader());
            ViewBag.investment = investment;
            ViewBag.savings = savingsProductDTOs;
            ViewBag.debit = debitypes;
            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            var customer = await _channelService.FindCustomerAsync(parseId, GetServiceHeader());
            var documents = await GetDocumentsAsync(customer.Id);
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
            CustomerAccountDTO customerAccountDTO = new CustomerAccountDTO();

            if (customer != null)
            {

                customerAccountDTO.CustomerId = customer.Id;
                customerAccountDTO.Customers = customer;
                customerAccountDTO.CustomerIndividualFirstName = customer.FullName;
                customerAccountDTO.CustomerIndividualPayrollNumbers = customer.IndividualPayrollNumbers;
                customerAccountDTO.CustomerSerialNumber = customer.SerialNumber;
                customerAccountDTO.CustomerIndividualIdentityCardNumber = customer.IndividualIdentityCardNumber;
                customerAccountDTO.CustomerStationZoneDivisionEmployerDescription = customer.StationZoneDivisionEmployerDescription;
            }
            Session["customerDTO"] = customer;


            return View(customerAccountDTO);
        }

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

        [HttpPost]
        public async Task<ActionResult> Create(CustomerAccountDTO customerAccountDTO, string[] debittypes, string[] savingsproducts, string[] investmentproducts)
        {

            var customerDTO = Session["customerDTO"] as CustomerDTO;

            var mandatoryInvestmentProducts = new List<InvestmentProductDTO>();
            var mandatorySavingsProducts = new List<SavingsProductDTO>();
            var mandatoryProducts = new ProductCollectionInfo();
            if (savingsproducts != null && investmentproducts != null)
            {
                if (savingsProductDTOs == null)
                    savingsProductDTOs = new ObservableCollection<SavingsProductDTO>();
                foreach (var savingproductid in savingsproducts)
                {
                    var savingsDTO = await _channelService.FindSavingsProductAsync(Guid.Parse(savingproductid), GetServiceHeader());

                    savingsProductDTOs.Add(savingsDTO);
                }
                if (investmentProductDTOs == null)
                    investmentProductDTOs = new ObservableCollection<InvestmentProductDTO>();
                foreach (var investmentid in investmentproducts)
                {
                    var investmentDTO = await _channelService.FindInvestmentProductAsync(Guid.Parse(investmentid), GetServiceHeader());

                    investmentProductDTOs.Add(investmentDTO);
                }


                if (loanProductDTOs == null)
                    loanProductDTOs = new ObservableCollection<LoanProductDTO>();
                foreach (var laon in debittypes)
                {
                    var loanProductDTO = await _channelService.FindLoanProductAsync(Guid.Parse(laon), GetServiceHeader());
                    loanProductDTOs.Add(loanProductDTO);
                }

            }
            customerAccountDTO.TotalValue = 1;
            customerAccountDTO.CustomerId = customerDTO.Id;
            customerDTO.BranchId = customerAccountDTO.BranchId;

            customerAccountDTO.ValidateAll();

            if (!customerAccountDTO.HasErrors)
            {
                await _channelService.AddCustomerAccountsAsync(customerDTO, savingsProductDTOs, investmentProductDTOs, loanProductDTOs, GetServiceHeader());
                TempData["SuccessMessage"] = "Successfully Created Customer Account for  " + customerDTO.FullName;
                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = customerAccountDTO.ErrorMessages;
                TempData["SuccessMessage"] = "Successfully Created Customer Account for  " + errorMessages;

                return View(customerAccountDTO);
            }
        }


        //public async Task<ActionResult> SavingsProduct(Guid? id, CustomerAccountDTO customerAccountDTO)
        //{
        //    await ServeNavigationMenus();
        //    ViewBag.ProductCode = GetProductCodeSelectList(string.Empty);
        //    ViewBag.RecordStatus = GetRecordStatusSelectList(string.Empty);
        //    ViewBag.customerFilter = GetCustomerFilterSelectList(string.Empty);
        //    ViewBag.CustomerTypeSelectList = GetCustomerTypeSelectList(string.Empty);
        //    Guid parseId;

        //    if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
        //    {
        //        return View("Create");
        //    }



        //    if (Session["customerAccountDTO"] != null)
        //    {
        //        customerAccountDTO = Session["customerAccountDTO"] as CustomerAccountDTO;
        //    }
        //    if (Session["BranchId"] != null)
        //    {
        //        customerAccountDTO.BranchId = (Guid)Session["BranchId"];
        //    }
        //    if (Session["BranchDescription"] != null)
        //    {
        //        customerAccountDTO.BranchDescription = Session["BranchDescription"].ToString();
        //    }
        //    if (Session["benefactorAccounts"] != null)
        //    {
        //        customerAccountDTO.Investments = Session["benefactorAccounts"] as InvestmentProductDTO;
        //    }

        //    var savingsProduct = await _channelService.FindSavingsProductAsync(parseId, GetServiceHeader());
        //    if (savingsProduct != null)
        //    {
        //        ViewBag.ProductCode = GetProductCodeSelectList(string.Empty);
        //        ViewBag.ChargeType = GetChargeTypeSelectList(string.Empty);
        //        customerAccountDTO.Savings = savingsProduct;
        //        Session["beneficiaryAccounts"] = customerAccountDTO.Savings;
        //        customerAccountDTO.CustomerAccountTypeTargetProductId = savingsProduct.Id;
        //        customerAccountDTO.CustomerAccountTypeTargetProductDescription = savingsProduct.Description;
        //        customerAccountDTO.CustomerAccountTypeTargetProductChartOfAccountName = savingsProduct.ChartOfAccountName;
        //        customerAccountDTO.CustomerAccountTypeProductCode = savingsProduct.Code;

        //        Session["savingsProductId"] = customerAccountDTO.CustomerAccountTypeTargetProductId;
        //        Session["savingsProductDescription"] = customerAccountDTO.CustomerAccountTypeTargetProductDescription;
        //    }
        //    // Ensure savingsProductDTOs is properly initialized
        //    if (savingsProductDTOs == null)
        //    {
        //        savingsProductDTOs = new ObservableCollection<SavingsProductDTO>();
        //        savingsProductDTOs.Add(savingsProduct);
        //    }


        //    Session["BranchId"] = customerAccountDTO.BranchId;
        //    Session["BranchDescription"] = customerAccountDTO.BranchDescription;


        //    return View("Create", customerAccountDTO);
        //}

        //[HttpPost]
        //public async Task<ActionResult> AddInvestments(Guid? id, CustomerAccountDTO customerAccountDTO)
        //{
        //    await ServeNavigationMenus();
        //    ViewBag.ProductCode = GetProductCodeSelectList(string.Empty);
        //    ViewBag.RecordStatus = GetRecordStatusSelectList(string.Empty);
        //    ViewBag.customerFilter = GetCustomerFilterSelectList(string.Empty);
        //    ViewBag.CustomerTypeSelectList = GetCustomerTypeSelectList(string.Empty);
        //    investmentProductDTOs = TempData["investmentProductDTOs"] as ObservableCollection<InvestmentProductDTO>;

        //    if (investmentProductDTOs == null)
        //        investmentProductDTOs = new ObservableCollection<InvestmentProductDTO>();

        //    foreach (var expensePayableEntryDTO in customerAccountDTO.investmentProducts)
        //    {
        //        expensePayableEntryDTO.Id = customerAccountDTO.investmentProducts[0].Id;
        //        var k = await _channelService.FindInvestmentProductAsync(expensePayableEntryDTO.Id, GetServiceHeader());
        //        expensePayableEntryDTO.ChartOfAccountId = customerAccountDTO.CustomerAccountTypeTargetProductId;
        //        expensePayableEntryDTO.ChartOfAccountAccountName = expensePayableEntryDTO.ChartOfAccountAccountName;
        //        expensePayableEntryDTO.Description = expensePayableEntryDTO.Description;
        //        //expensePayableEntryDTO.ChartOfAccountAccountName = expensePayableEntryDTO.ChartOfAccountName;
        //        //expensePayableEntryDTO.TotalValue = expensePayableEntryDTO.TotalValue;
        //        //expensePayableEntryDTO.PrimaryDescription = expensePayableEntryDTO.PrimaryDescription;
        //        //expensePayableEntryDTO.SecondaryDescription = expensePayableEntryDTO.SecondaryDescription;
        //        //expensePayableEntryDTO.Reference = expensePayableEntryDTO.Reference;
        //        investmentProductDTOs.Add(expensePayableEntryDTO);
        //    };
        //    Session["BranchId"] = customerAccountDTO.BranchId;
        //    Session["BranchDescription"] = customerAccountDTO.BranchDescription;

        //    TempData["investmentProductDTOs"] = investmentProductDTOs;

        //    TempData["customerAccountDTO"] = customerAccountDTO;

        //    ViewBag.investmentProductDTOs = investmentProductDTOs;
        //    Session["investmentProductDTOs"] = investmentProductDTOs;
        //    ViewBag.JournalVoucherTypeSelectList = GetJournalVoucherTypeSelectList(customerAccountDTO.Type.ToString());
        //    ViewBag.JournalVoucherEntryTypeSelectList = GetJournalVoucherEntryTypeSelectList(customerAccountDTO.Type.ToString());
        //    ViewBag.JournalVoucherTypeSelectList = GetJournalVoucherTypeSelectList(customerAccountDTO.Type.ToString());
        //    if (Session["savingsProductDTOs"] != null)
        //    {
        //        savingsProductDTOs = Session["savingsProductDTOs"] as ObservableCollection<SavingsProductDTO>;
        //        ViewBag.savingsProductDTOs = savingsProductDTOs;
        //    }
        //    if (Session["customerAccountDTO"] != null)
        //    {
        //        customerAccountDTO = Session["customerAccountDTO"] as CustomerAccountDTO;
        //    }
        //    return View("Create", customerAccountDTO);
        //}


        //public async Task<ActionResult> LoansProduct(Guid? id, CustomerAccountDTO directDebitDTO)
        //{
        //    await ServeNavigationMenus();

        //    Guid parseId;

        //    if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
        //    {
        //        return View("Create");
        //    }

        //    var loanProductsdetails = await _channelService.FindLoanProductAsync(parseId, GetServiceHeader());
        //    if (loanProductsdetails != null)
        //    {
        //        ViewBag.ProductCode = GetProductCodeSelectList(string.Empty);
        //        ViewBag.ChargeType = GetChargeTypeSelectList(string.Empty);

        //        directDebitDTO.CustomerAccountTypeTargetProductId = loanProductsdetails.Id;
        //        directDebitDTO.CustomerAccountTypeTargetProductDescription = loanProductsdetails.Description;
        //        directDebitDTO.CustomerAccountTypeTargetProductChartOfAccountName = loanProductsdetails.ChartOfAccountAccountName;


        //        directDebitDTO.CustomerAccountTypeProductCode = loanProductsdetails.Code;

        //        Session["loanProductId"] = directDebitDTO.CustomerAccountTypeTargetProductId;
        //        Session["loanProductDescription"] = directDebitDTO.CustomerAccountTypeTargetProductDescription;
        //        Session["loanProductDescription"] = loanProductsdetails;
        //    }


        //    return View("Create", directDebitDTO);
        //}



        //public async Task<ActionResult> InvestmentProduct(Guid? id, CustomerAccountDTO directDebitDTO)
        //{
        //    await ServeNavigationMenus();

        //    Guid parseId;

        //    if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
        //    {
        //        return View("Create");
        //    }
        //    if (Session["BranchId"] != null)
        //    {
        //        directDebitDTO.BranchId = (Guid)Session["BranchId"];
        //    }
        //    if (Session["BranchDescription"] != null)
        //    {
        //        directDebitDTO.BranchDescription = Session["BranchDescription"].ToString();
        //    }
        //    if (Session["beneficiaryAccounts"] != null)
        //    {
        //        directDebitDTO.Savings = Session["beneficiaryAccounts"] as SavingsProductDTO;
        //    }
        //    if (Session["customerAccountDTO"] != null)
        //    {
        //        directDebitDTO.Customers = Session["customerAccountDTO"] as CustomerDTO;
        //    }

        //    var investmentProductDetails = await _channelService.FindInvestmentProductAsync(parseId, GetServiceHeader());
        //    if (investmentProductDetails != null)
        //    {
        //        ViewBag.ProductCode = GetProductCodeSelectList(string.Empty);
        //        ViewBag.ChargeType = GetChargeTypeSelectList(string.Empty);

        //        directDebitDTO.Investments = investmentProductDetails;
        //        directDebitDTO.investmentProducts[0].Id = investmentProductDetails.Id;
        //        directDebitDTO.investmentProducts[1].Description = investmentProductDetails.Description;
        //        Session["benefactorAccounts"] = directDebitDTO.Investments;
        //        directDebitDTO.CustomerAccountTypeTargetProductId = investmentProductDetails.Id;
        //        directDebitDTO.CustomerAccountTypeTargetProductDescription = investmentProductDetails.Description;
        //        directDebitDTO.CustomerAccountTypeTargetProductChartOfAccountName = investmentProductDetails.ChartOfAccountAccountName;
        //        directDebitDTO.CustomerAccountTypeTargetProductParentId = investmentProductDetails.ChartOfAccountId;
        //        directDebitDTO.CustomerAccountTypeProductCode = 3;

        //        Session["investmentProductId"] = directDebitDTO.CustomerAccountTypeTargetProductId;
        //        Session["investmentProductDescription"] = directDebitDTO.CustomerAccountTypeTargetProductDescription;
        //    }
        //    Session["BranchId"] = directDebitDTO.BranchId;
        //    Session["BranchDescription"] = directDebitDTO.BranchDescription;

        //    ViewBag.k = directDebitDTO.Investments;

        //    return View("Create", directDebitDTO);
        //}
        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            bool includeInterestBalanceForLoanAccounts = false;
            bool includeBalances = false;
            bool includeProductDescription = false;
            bool considerMaturityPeriodForInvestmentAccounts = false;

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }
            var CustomerAccount = await _channelService.FindCustomerAccountAsync(id, includeInterestBalanceForLoanAccounts, includeBalances, includeProductDescription, considerMaturityPeriodForInvestmentAccounts, GetServiceHeader());
            CustomerAccountDTO customerAccountDTO = new CustomerAccountDTO();

            if (CustomerAccount != null)
            {

                customerAccountDTO.CustomerId = CustomerAccount.Id;
                customerAccountDTO.CustomerIndividualFirstName = CustomerAccount.CustomerIndividualFirstName;
                customerAccountDTO.CustomerIndividualPayrollNumbers = CustomerAccount.CustomerIndividualPayrollNumbers;
                customerAccountDTO.CustomerSerialNumber = CustomerAccount.CustomerSerialNumber;
                customerAccountDTO.CustomerIndividualIdentityCardNumber = CustomerAccount.CustomerIndividualIdentityCardNumber;
                customerAccountDTO.CustomerStationZoneDivisionEmployerDescription = CustomerAccount.CustomerStationZoneDivisionEmployerDescription;
            }
            return View(customerAccountDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, CustomerAccountDTO customerAccountDTO)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateCustomerAccountAsync(customerAccountDTO, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                return View(customerAccountDTO);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetCustomersCountAsync(CustomerAccountDTO customerAccountDTO)
        {
            await ServeNavigationMenus();
            var customerAccountId = customerAccountDTO.Id;
            bool includeInterestBalanceForLoanAccounts = false;
            bool includeBalances = false;
            bool includeProductDescription = false;
            bool considerMaturityPeriodForInvestmentAccounts = false;
            var CustomerAccount = await _channelService.FindCustomerAccountAsync(customerAccountId, includeInterestBalanceForLoanAccounts, includeBalances, includeProductDescription, considerMaturityPeriodForInvestmentAccounts, GetServiceHeader());
            return Json(customerAccountDTO, JsonRequestBehavior.AllowGet);
        }

        public async Task<ActionResult> CustomerManagement(Guid id)
        {
            await ServeNavigationMenus();
            bool includeInterestBalanceForLoanAccounts = false;
            bool includeBalances = false;
            bool includeProductDescription = false;
            bool considerMaturityPeriodForInvestmentAccounts = false;


            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }


            CustomerAccountDTO customerAccountDTO = new CustomerAccountDTO();

            var CustomerAccount = await _channelService.FindCustomerAccountAsync(parseId, includeBalances, includeProductDescription, includeInterestBalanceForLoanAccounts, considerMaturityPeriodForInvestmentAccounts, GetServiceHeader());
            if (CustomerAccount != null)
            {
                customerAccountDTO.CustomerId = CustomerAccount.Id;
                customerAccountDTO.CustomerIndividualFirstName = CustomerAccount.CustomerIndividualFirstName;
                customerAccountDTO.CustomerIndividualPayrollNumbers = CustomerAccount.CustomerIndividualPayrollNumbers;
                customerAccountDTO.CustomerSerialNumber = CustomerAccount.CustomerSerialNumber;
                customerAccountDTO.CustomerIndividualIdentityCardNumber = CustomerAccount.CustomerIndividualIdentityCardNumber;
                customerAccountDTO.CustomerStationZoneDivisionEmployerDescription = CustomerAccount.CustomerStationZoneDivisionEmployerDescription;
            }
            return View(customerAccountDTO);
        }

        [HttpPost]
        public async Task<ActionResult> CustomerManagement(Guid id, CustomerAccountDTO customerAccountHistoryDTO)
        {
            if (ModelState.IsValid)
            {
                int managementAction = 0;

                string remarks = "";

                int remarkType = 0;

                await _channelService.ManageCustomerAccountAsync(id, managementAction, remarks, remarkType, GetServiceHeader());

                ViewBag.CustomerAccountManagementActionSelectList = GetCustomerAccountManagementActionSelectList(customerAccountHistoryDTO.CustomerIndividualSalutationDescription.ToString());
                return RedirectToAction("Index");
            }
            else
            {
                return View(customerAccountHistoryDTO);
            }
        }

    }
}