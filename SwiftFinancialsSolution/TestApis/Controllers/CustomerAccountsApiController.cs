//using System;
//using System.Collections.Generic;
//using System.Collections.ObjectModel;
//using System.Configuration;
//using System.Linq;
//using System.Threading.Tasks;
//using System.Web.Http;
//using Application.MainBoundedContext.DTO;
//using Application.MainBoundedContext.DTO.AccountsModule;
//using Application.MainBoundedContext.DTO.RegistryModule;
//using DistributedServices.MainBoundedContext.Identity;
//using Infrastructure.Crosscutting.Framework.Utils;
//using Microsoft.AspNet.Identity;
//using SwiftFinancials.Presentation.Infrastructure.Services;
//using TestApis.Controllers;
//using TestApis.Models;
//using Application.MainBoundedContext.DTO.AdministrationModule;
//using DistributedServices.MainBoundedContext.Identity;
//using Infrastructure.Crosscutting.Framework.Utils;
//using Microsoft.AspNet.Identity;
//using Microsoft.AspNet.Identity.EntityFramework;
//using Microsoft.AspNet.Identity.Owin;
//using Microsoft.Owin.Security.DataProtection;
//using System;
//using System.Configuration;
//using System.Threading.Tasks;
//using System.Web.Http;
//using TestApis.Identity;
//using TestApis.Models;
//using SwiftFinancials.Web.Identity;

//[RoutePrefix("api/customeraccounts")]
//public class CustomerAccountsApiController : ApiController
//{
//    private readonly IChannelService _channelService;
//    private readonly MasterController _master;
//    public CustomerAccountsApiController() { _master = new MasterController(); }
//    private static readonly int PasswordExpiryPeriod =
//       Convert.ToInt32(ConfigurationManager.AppSettings["PasswordExpiryPeriod"]);

//    private ApplicationUserManager _userManager;
//    private ApplicationSignInManager _signInManager;
//    public CustomerAccountsApiController(
//        IChannelService channelService,
//        ApplicationUserManager userManager,
//        MasterController master)
//    {
//        _channelService = channelService ?? throw new ArgumentNullException(nameof(channelService));
//        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
//        _master = master ?? throw new ArgumentNullException(nameof(master));
//    }

   


//    private ApplicationUserManager UserManager
//    {
//        get
//        {
//            if (_userManager == null)
//                _userManager = CreateUserManager();
//            return _userManager;
//        }
//    }

//    private ApplicationSignInManager SignInManager
//    {
//        get
//        {
//            if (_signInManager == null)
//                _signInManager = new ApplicationSignInManager(UserManager);
//            return _signInManager;
//        }
//    }

//    private ApplicationUserManager CreateUserManager()
//    {
//        var context = new ApplicationDbContext("AuthStore");
//        var userStore = new UserStore<ApplicationUser>(context);
//        var manager = new ApplicationUserManager(userStore);

//        var provider = new DpapiDataProtectionProvider("SwiftFinancials");
//        manager.UserTokenProvider = new DataProtectorTokenProvider<ApplicationUser>(provider.Create());

//        return manager;
//    }
//    private ServiceHeader GetServiceHeader() => _master.GetServiceHeader();

//    // ============================================================
//    // 1. GET ALL CUSTOMER ACCOUNTS — NO FILTERS
//    // ============================================================
//    [HttpGet]
//    [Route("all")]
//    public async Task<IHttpActionResult> GetAllCustomerAccounts()
//    {
//        var serviceHeader = _master.GetServiceHeader();
//        const int pageSize = 200;     // Bigger pages → fewer round-trips.
//        int pageIndex = 0;

//        var result = await _master._channelService.FindCustomerAccountsInPageAsync(
//                    pageIndex,
//                    pageSize,
//                    includeBalances: true,
//                    includeProductDescription: true,
//                    includeInterestBalanceForLoanAccounts: true,
//                    considerMaturityPeriodForInvestmentAccounts: true,
//                    serviceHeader: serviceHeader
//                );

//        if (result == null || !result.PageCollection.Any())
//            return Ok(new { total = 0, data = new List<CustomerAccountDTO2>() });

//        var sorted = result.PageCollection
//            .OrderByDescending(x => x.Id)
//            .ToList();

//        return Ok(new
//        {
//            data = sorted
//        });
//    }

//    // ============================================================
//    // 2. FILTERED ACCOUNT SEARCH
//    // ============================================================
//    //[HttpGet]
//    //[Route("search")]
//    //public async Task<IHttpActionResult> SearchAccounts(int? productCode = null, int? recordStatus = null, string search = null)
//    //{
//    //    PageCollectionInfo<CustomerAccountDTO> pageCollection;

//    //    // Variant 1: product + record status
//    //    if (productCode != null && recordStatus != null)
//    //    {
//    //        pageCollection = await _channelService.FindCustomerAccountsByProductCodeAndRecordStatusAndFilterInPageAsync(
//    //            productCode.Value,
//    //            recordStatus.Value,
//    //            search ?? "",
//    //            (int)CustomerFilter.FirstName,
//    //            0,
//    //            int.MaxValue,
//    //            false, false, false, false,
//    //            GetServiceHeader()
//    //        );
//    //    }
//    //    // Variant 2: only product
//    //    else if (productCode != null)
//    //    {
//    //        pageCollection = await _channelService.FindCustomerAccountsByProductCodeAndFilterInPageAsync(
//    //            productCode.Value,
//    //            search ?? "",
//    //            (int)CustomerFilter.FirstName,
//    //            0,
//    //            int.MaxValue,
//    //            false, false, false, false,
//    //            GetServiceHeader()
//    //        );
//    //    }
//    //    // Variant 3: no filters
//    //    else
//    //    {
//    //        return await GetAllCustomerAccounts();
//    //    }

//    //    if (pageCollection == null || !pageCollection.PageCollection.Any())
//    //        return Ok(new { total = 0, data = new List<CustomerAccountDTO>() });

//    //    var sorted = pageCollection.PageCollection
//    //        .OrderByDescending(x => x.CreatedDate)
//    //        .ToList();

//    //    return Ok(new
//    //    {
//    //        total = sorted.Count,
//    //        data = sorted
//    //    });
//    //}

//    // ============================================================
//    // 3. GET ACCOUNT BY ID
//    // ============================================================
//    //[HttpGet]
//    //[Route("{id:guid}")]
//    //public async Task<IHttpActionResult> GetAccount(Guid id)
//    //{
//    //    var account = await _channelService.FindCustomerAccountAsync(
//    //        id,
//    //        false,
//    //         false,
//    //         false,
//    //        considerMaturityPeriodForInvestmentAccounts: false,
//    //        GetServiceHeader()
//    //    );

//    //    if (account == null)
//    //        return NotFound();

//    //    return Ok(account);
//    //}

//    // ============================================================
//    // 4. CREATE CUSTOMER ACCOUNT
//    // ============================================================
//    [HttpPost]
//    [Route("create")]
//    public async Task<IHttpActionResult> CreateAccount(
//    CustomerAccountDTO2 account,
//    [FromUri] string[] debitTypes = null,
//    [FromUri] string[] savingsProducts = null,
//    [FromUri] string[] investmentProducts = null)
//    {
//        if (account == null)
//            return BadRequest("Missing account payload.");

//        var serviceHeader = GetServiceHeader();

//        // Retrieve the customer
//        var customer = await _master._channelService.FindCustomerAsync(account.CustomerId, serviceHeader);
//        if (customer == null)
//            return BadRequest("Customer not found.");
         
//        //// Get user info for branch/company context
//        //var userDTO = await UserManager.FindByEmailAsync("info@stamlinetechnlogies.com");
//        //if (userDTO?.BranchId == null)
//        //    return BadRequest("Branch not found for current user.");

//        var branch = await _master._channelService.FindBranchAsync(account.BranchId, serviceHeader);
//        if (branch == null)
//            return BadRequest("Branch information not found.");

//        var company = await _master._channelService.FindCompanyAsync(branch.CompanyId, serviceHeader);
//        if (company == null)
//            return BadRequest("Company information not found.");

//        // Get mandatory company products
//        var attachedProducts = await _master._channelService.FindAttachedProductsByCompanyIdAsync(company.Id, serviceHeader);
//        var mandatoryDebitTypesList = await _master._channelService.FindDebitTypesByCompanyIdAsync(company.Id, serviceHeader);

//        if (attachedProducts?.InvestmentProductCollection == null ||
//            attachedProducts.SavingsProductCollection == null ||
//            mandatoryDebitTypesList == null)
//        {
//            return BadRequest("Company does not contain mandatory products. Setup is required to proceed.");
//        }

//        // Convert Lists to ObservableCollections
//        var mandatorySavingsProducts = new ObservableCollection<SavingsProductDTO>(attachedProducts.SavingsProductCollection);
//        var mandatoryInvestmentProducts = new ObservableCollection<InvestmentProductDTO>(attachedProducts.InvestmentProductCollection);
//        var mandatoryLoanProducts = new ObservableCollection<LoanProductDTO>(); // Empty if not applicable

//        // Validate the account payload
//        account.ValidateAll();
//        if (account.HasErrors)
//            return BadRequest(string.Join("; ", account.ErrorMessages));

//        customer.BranchId = account.BranchId;
//        // Create customer accounts using company setup
//        var created = await _master._channelService.AddCustomerAccountsAsync(
//            customer,
//            mandatorySavingsProducts,
//            mandatoryInvestmentProducts,
//            null,
//            serviceHeader
//        );

//        if (!created)
//            return InternalServerError(new Exception("Failed to create customer accounts."));

//        return Ok(new
//        {
//            created = true,
//            customerId = customer.Id,
//            message = "Customer account created successfully"
//        });
//    }


//}
