using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Windows.Forms;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.BackOfficeModule;
using Application.MainBoundedContext.DTO.RegistryModule;
using Infrastructure.Crosscutting.Framework.Utils;
using Newtonsoft.Json;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.Loaning.Controllers
{
    public class GuarantorAttachmentController : MasterController
    {

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

            var pageCollectionInfo = await _channelService.FindLoanGuarantorAttachmentHistoryByStatusAndFilterInPageAsync((int)LoanGuarantorAttachmentHistoryStatus.Attached, DateTime.Now.AddDays(-30), DateTime.Now.AddDays(-30), string.Empty, 0, 300);

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(LoanCase => LoanCase.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<LoanGuarantorDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }


        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var dataCapture = await _channelService.FindLoanGuarantorAttachmentHistoryEntriesByLoanGuarantorAttachmentHistoryIdAsync(id, GetServiceHeader());

            return View(dataCapture);
        }

        public async Task<ActionResult> CustomerAccountLookUp(Guid? id, LoanGuarantorDTO loanGuarantorDTO)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View("create");
            }

            var accounts = await _channelService.FindCustomerAccountAsync(parseId, true, true, true, true, GetServiceHeader());

            if (accounts != null)
            {
                loanGuarantorDTO.CustomerAccountFullAccountNumber = accounts.FullAccountNumber;
                loanGuarantorDTO.CustomerAccountAccountId = accounts.Id;
                loanGuarantorDTO.CustomerAccountAccountStatusDescription = accounts.StatusDescription;
                loanGuarantorDTO.CustomerAccountAccountRemarks = accounts.Remarks;
                loanGuarantorDTO.BookBalance = accounts.BookBalance;
                loanGuarantorDTO.CustomerId = accounts.CustomerId;
                loanGuarantorDTO.CustomerIndividualFirstName = accounts.CustomerFullName;
                loanGuarantorDTO.CustomerAccounntCustomerTypeDescription = accounts.CustomerTypeDescription;
                loanGuarantorDTO.CustomerIndividualPayrollNumbers = accounts.CustomerIndividualPayrollNumbers;
                loanGuarantorDTO.CustomerPersonalIdentificationNumber = accounts.CustomerPersonalIdentificationNumber;
                loanGuarantorDTO.CustomerReference1 = accounts.CustomerReference1;
                loanGuarantorDTO.CustomerReference2 = accounts.CustomerReference2;
                loanGuarantorDTO.CustomerReference3 = accounts.CustomerReference3;

                var findLoanCaseId = await _channelService.FindLoanCasesByCustomerIdInProcessAsync(loanGuarantorDTO.CustomerId, GetServiceHeader());

                var loanCaseId = Guid.Empty;
                var loaneeCustomerId = Guid.Empty;
                var loanCaseLoanProductId = Guid.Empty;

                foreach (var takeId in findLoanCaseId)
                {
                    loanCaseId = takeId.Id;
                    loaneeCustomerId = takeId.CustomerId;
                    loanCaseLoanProductId = takeId.LoanProductId;
                }

                Session["LoanProductId"] = loanCaseLoanProductId;

                var guarantors = await _channelService.FindLoanGuarantorsByLoaneeCustomerIdAndLoanProductIdAsync(loaneeCustomerId, loanCaseLoanProductId, GetServiceHeader());

                var sumAmountGuaranteed = guarantors.Sum(x => x.AmountGuaranteed);

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        CustomerAccountFullAccountNumber = loanGuarantorDTO.CustomerAccountFullAccountNumber,
                        CustomerAccountAccountId = loanGuarantorDTO.CustomerAccountAccountId,
                        CustomerAccountAccountStatusDescription = loanGuarantorDTO.CustomerAccountAccountStatusDescription,
                        CustomerAccountAccountRemarks = loanGuarantorDTO.CustomerAccountAccountRemarks,
                        BookBalance = loanGuarantorDTO.BookBalance,
                        CustomerId = loanGuarantorDTO.CustomerId,
                        CustomerIndividualFirstName = loanGuarantorDTO.CustomerIndividualFirstName,
                        CustomerAccounntCustomerTypeDescription = loanGuarantorDTO.CustomerAccounntCustomerTypeDescription,
                        CustomerIndividualPayrollNumbers = loanGuarantorDTO.CustomerIndividualPayrollNumbers,
                        CustomerPersonalIdentificationNumber = loanGuarantorDTO.CustomerPersonalIdentificationNumber,
                        CustomerReference1 = loanGuarantorDTO.CustomerReference1,
                        CustomerReference2 = loanGuarantorDTO.CustomerReference2,
                        CustomerReference3 = loanGuarantorDTO.CustomerReference3,

                        Guarantors = guarantors,
                        AmountGuaranteed = sumAmountGuaranteed
                    }
                });
            }

            return Json(new { success = false, message = "Customer account not found" });
        }



        public async Task<ActionResult> Create(Guid? id, LoanGuarantorDTO loanGuarantorDTO)
        {
            await ServeNavigationMenus();

            ViewBag.MonthSelectList = GetMonthsAsync(string.Empty);

            ViewBag.ProductCode = GetProductCodeSelectList(string.Empty);
            ViewBag.RecordStatus = GetRecordStatusSelectList(string.Empty);

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(LoanGuarantorDTO loanGuarantorDTO, string GuarantorsJson)
        {

            ObservableCollection<LoanGuarantorDTO> loanGuarantors = JsonConvert.DeserializeObject<ObservableCollection<LoanGuarantorDTO>>(GuarantorsJson);

            if (Session["LoanProductId"] != null)
            {
                loanGuarantorDTO.LoanProductId = (Guid)Session["LoanProductId"];
            }

            loanGuarantorDTO.ValidateAll();

            if (!loanGuarantorDTO.HasErrors)
            {
                string message = string.Format(
                                      "Do you want to proceed and attach the selected Loan guarantors?"
                                  );

                DialogResult result = MessageBox.Show(
                   message,
                   "Attach Loan Guarantor",
                   MessageBoxButtons.YesNo,
                   MessageBoxIcon.Question,
                   MessageBoxDefaultButton.Button1,
                   MessageBoxOptions.ServiceNotification
               );

                if (result == DialogResult.Yes)
                {
                    await _channelService.AttachLoanGuarantorsAsync(loanGuarantorDTO.CustomerAccountAccountId, loanGuarantorDTO.LoanProductId, loanGuarantors, 1234, GetServiceHeader());
                    MessageBox.Show("Operation completed successfully.", "Guarantor Attachment", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                    //TempData["message"] = "Successfully created Data Period";

                    return RedirectToAction("Index");
                }
                else
                {
                    MessageBox.Show("Operation cancelled.", "Attach Guarantor", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                    return View("Create", loanGuarantorDTO);
                }
            }
            else
            {
                //var errorMessages = dataPeriodDTO.ErrorMessages.ToString();

                //TempData["BugdetBalance"] = errorMessages;

                TempData["messageError"] = "Could not create Data Period";

                //ViewBag.MonthSelectList = GetMonthsAsync(dataPeriodDTO.MonthDescription);

                await ServeNavigationMenus();

                return View();
            }
        }
    }
}
