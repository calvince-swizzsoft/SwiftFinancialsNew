using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.FrontOfficeModule;
using System.Threading.Tasks;

namespace SwiftFinancials.Web.Areas.FrontOffice.Controllers
{
    public class SundryPaymentsController : MasterController
    {
        // GET: FrontOffice/SundryPayments
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

            DateTime startDate = DateTime.Now;

            DateTime endDate = DateTime.Now;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindCashDepositRequestsByFilterInPageAsync(startDate, endDate, jQueryDataTablesModel.iColumns, jQueryDataTablesModel.sSearch, jQueryDataTablesModel.sEcho, 1, 1, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<CashDepositRequestDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var tellerDTO = await _channelService.FindCashDepositRequestAsync(id);

            return View(tellerDTO);
        }

        public async Task<ActionResult> Create(Guid? id)
        {


            await ServeNavigationMenus();
            ViewBag.TransactionTypeSelectList = GetGeneralTransactionTypeSelectList(string.Empty);

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }


            ViewBag.TrasnactionTypeSelectList = GetGeneralTransactionTypeSelectList(string.Empty);

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(CustomerAccountDTO customerAccountDTO)


        {


            customerAccountDTO.ValidateAll();


            if (!customerAccountDTO.HasErrors)
            {
                decimal totalValue = customerAccountDTO.TotalValue;
                int frontOfficeTransactionType = customerAccountDTO.Type;

                await _channelService.ComputeTellerCashTariffsAsync(customerAccountDTO, totalValue, frontOfficeTransactionType, GetServiceHeader());

                if (customerAccountDTO.TypeDescription.Equals("Cash Withdrawal"))
                {

                    CashWithdrawalRequestDTO cashWithdrawalRequestDTO = new CashWithdrawalRequestDTO(customerAccountDTO);

                    cashWithdrawalRequestDTO.ValidateAll();

                    await _channelService.AddCashWithdrawalRequestAsync(cashWithdrawalRequestDTO, GetServiceHeader());

                }

                else if (customerAccountDTO.TypeDescription.Equals("Cash Deposit"))
                {


                    CashDepositRequestDTO cashDepositRequestDTO = new CashDepositRequestDTO(customerAccountDTO);

                    cashDepositRequestDTO.ValidateAll();

                    await _channelService.AddCashDepositRequestAsync(cashDepositRequestDTO, GetServiceHeader());

                }

                else if (customerAccountDTO.TypeDescription.Equals("Cheque Deposit"))
                {


                    ExternalChequeDTO chequeDepositDTO = new ExternalChequeDTO(customerAccountDTO);
                    chequeDepositDTO.ValidateAll();

                    await _channelService.AddExternalChequeAsync(chequeDepositDTO, GetServiceHeader());

                }

                ViewBag.TransactionTypeSelectList = GetFrontOfficeTransactionTypeSelectList(customerAccountDTO.CustomerAccountManagementActionDescription.ToString());

                return RedirectToAction("Create");
            }
            else
            {
                var errorMessages = customerAccountDTO.ErrorMessages;
                // ViewBag.TellerTypeSelectList = GetTellerTypeSelectList(cashDepositRequestDTO.CustomerAccountCustomerTypeDescription.ToString());
                ViewBag.TransactionTypeSelectList = GetFrontOfficeTransactionTypeSelectList(customerAccountDTO.Type.ToString());

                return View(customerAccountDTO);
            }
        }
    }
}