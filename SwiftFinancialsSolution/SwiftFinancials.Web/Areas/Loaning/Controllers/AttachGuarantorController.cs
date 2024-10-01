using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.BackOfficeModule;
using Application.MainBoundedContext.DTO.RegistryModule;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

using System.Data.SqlClient;
using System.Data;
using System.Configuration;
using System.Windows.Forms;

namespace SwiftFinancials.Web.Areas.Loaning.Controllers
{
    public class AttachGuarantorController : MasterController
    {
        string connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;
        private bool IsBusy { get; set; }

        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public async Task<JsonResult> Index(JQueryDataTablesModel jQueryDataTablesModel, string text, int pageIndex, int pageSize, LoanCaseDTO loancaseDTO)
        {
            int totalRecordCount = 0;

            int searchRecordCount = 0;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindCustomerAccountsByCustomerAccountTypeTargetProductIdInPageAsync(loancaseDTO.Id, pageIndex, pageSize, true, true, true, false, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<CustomerAccountDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }


        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var loanguarantorsDTO = await _channelService.FindLoanGuarantorAsync(id, GetServiceHeader());

            return View(loanguarantorsDTO);
        }


        public async Task<ActionResult> Create(Guid? id, LoanGuarantorDTO loanGuarantorDTO)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            var myloanCases = await _channelService.FindLoanCaseAsync(parseId, GetServiceHeader());

            var loanproductId = myloanCases.LoanProductId;
            Session["LoanProductIdID"] = loanproductId;

            var sourceCustomerId = myloanCases.CustomerId;
            var findCustomerAccount = await _channelService.FindCustomerAccountsByCustomerIdAsync(sourceCustomerId, true, true, true, true, GetServiceHeader());

            List<Guid> customerAccountsIDs = new List<Guid>();

            foreach (var accounts in findCustomerAccount)
            {
                customerAccountsIDs.Add(accounts.Id);
            }

            var sourceCustomerAccountId = customerAccountsIDs[0];

            Session["sourceCustomerAccountId"] = sourceCustomerAccountId;

            if (myloanCases != null)
            {
                loanGuarantorDTO.LoanCase = myloanCases;

                Session["loanCases"] = loanGuarantorDTO.LoanCase;

                Session["status"] = loanGuarantorDTO.LoanCase.Status;
            }

            var findLoanGuarantors = await _channelService.FindLoanGuarantorsByLoanCaseIdAsync(myloanCases.Id, GetServiceHeader());
            ViewBag.LoanGuarantors = findLoanGuarantors;

            Session["gGuarantors"] = findLoanGuarantors;

            Session["loanCaseId"] = myloanCases.Id;

            return View(loanGuarantorDTO);
        }

        public async Task<ActionResult> Search(Guid? id, LoanGuarantorDTO loanGuarantorDTO)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }


            if (Session["loanCaseId"] != null)
            {
                Guid loanCaseId = (Guid)Session["loanCaseId"];

                var findLoanGuarantors = await _channelService.FindLoanGuarantorsByLoanCaseIdAsync(loanCaseId, GetServiceHeader());
                ViewBag.LoanGuarantors = findLoanGuarantors;
            }

            if (Session["loanCases"] != null)
            {
                loanGuarantorDTO.LoanCase = Session["loanCases"] as LoanCaseDTO;
            }


            var customer = await _channelService.FindCustomerAsync(parseId, GetServiceHeader());

            if (customer != null)
            {
                loanGuarantorDTO.Customer = customer;

                Session["Customer"] = loanGuarantorDTO.Customer;
            }

            return View("Create", loanGuarantorDTO);
        }


        [HttpPost]
        public async Task<ActionResult> Create(LoanGuarantorDTO loanGuarantorDTO)
        {
            var customerDTO = await _channelService.FindCustomerAsync(loanGuarantorDTO.Customer.Id, GetServiceHeader());

            var loanCaseDTO = await _channelService.FindLoanCaseAsync(loanGuarantorDTO.LoanCase.Id, GetServiceHeader());

            loanGuarantorDTO.LoanCase = Session["loanCases"] as LoanCaseDTO;

            var guarantor = loanGuarantorDTO.CustomerIndividualSalutationDescription + " " + loanGuarantorDTO.CustomerIndividualFirstName + " " + loanGuarantorDTO.CustomerIndividualLastName;

            // cheat
            loanGuarantorDTO.CustomerId = customerDTO.Id;
            loanGuarantorDTO.LoaneeCustomerId = loanCaseDTO.CustomerId;
            loanGuarantorDTO.LoanProductId = loanCaseDTO.LoanProductId;
            loanGuarantorDTO.TotalShares = 10000;
            loanGuarantorDTO.CommittedShares = 10000;
            loanGuarantorDTO.AmountPledged = 10000;
            loanGuarantorDTO.AppraisalFactor = 1;
            loanGuarantorDTO.LoanCaseId = loanCaseDTO.Id;
            loanGuarantorDTO.LoanCaseBranchId = loanCaseDTO.BranchId;


            loanGuarantorDTO.ValidateAll();

            await ServeNavigationMenus();

            if (!loanGuarantorDTO.HasErrors)
            {
                var status = Convert.ToInt32(Session["status"].ToString());
                if (status != 48826)
                {
                    //TempData["status"] = "You can only attach guarantor for loans that are registered !";

                    MessageBox.Show(Form.ActiveForm, "You can only attach guarantor for loans that are registered !", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                    return View();
                }

                string message = string.Format(
                                       "{0}.\nDo you want to proceed and add the selected customer as guarantor?",
                                       guarantor
                                   );

                // Show the message box with Yes/No options
                DialogResult result = MessageBox.Show(
                    message,
                    "Add Loan Guarantor",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.ServiceNotification
                );


                if (result == DialogResult.Yes)
                {
                    var AddLoanGuarantor = await _channelService.AddLoanGuarantorAsync(loanGuarantorDTO, GetServiceHeader());

                    loanGuarantorDTO.Customer = null;

                    if (AddLoanGuarantor.ErrorMsgResult != null)
                    {
                        await ServeNavigationMenus();

                        MessageBox.Show(Form.ActiveForm, AddLoanGuarantor.ErrorMsgResult, "Failed", MessageBoxButtons.OK, MessageBoxIcon.Hand, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);

                        //TempData["failedErrorMsg"] = AddLoanGuarantor.ErrorMsgResult;
                        Guid loanCaseId = (Guid)Session["loanCaseId"];

                        var findLoanGuarantors = await _channelService.FindLoanGuarantorsByLoanCaseIdAsync(loanCaseId, GetServiceHeader());
                        ViewBag.LoanGuarantors = findLoanGuarantors;
                        return View();
                    }

                    if (Session["loanCaseId"] != null)
                    {
                        Guid loanCaseId = (Guid)Session["loanCaseId"];

                        var findLoanGuarantors = await _channelService.FindLoanGuarantorsByLoanCaseIdAsync(loanCaseId, GetServiceHeader());
                        ViewBag.LoanGuarantors = findLoanGuarantors;
                    }

                    loanGuarantorDTO.Customer = null;

                    MessageBox.Show(Form.ActiveForm, "Operation completed successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);

                    return View();
                }
                else
                {
                    IsBusy = false;

                    return View(loanGuarantorDTO);
                }
            }
            else
            {
                MessageBox.Show($"An error occurred: {loanGuarantorDTO.ErrorMessages}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);


                IsBusy = false;
                var errorMessages = loanGuarantorDTO.ErrorMessages;

                TempData["ErrorMsg"] = "Failed to attach Loan Guarantor.";

                return View(loanGuarantorDTO);
            }
        }


        [HttpPost]
        public async Task<ActionResult> Finish()
        {
            await ServeNavigationMenus();
            //if (Session["loanCaseId"] != null || Session["LoanProductIdID"] != null || Session["sourceCustomerAccountId"] != null)
            //{
            //    Guid loanCaseID = (Guid)Session["loanCaseId"];
            //    Guid loanProductId = (Guid)Session["LoanProductIdID"];
            //    Guid sourceCustomerAccountId = (Guid)Session["sourceCustomerAccountId"];


            //    var LoanGuarantors = await _channelService.FindLoanGuarantorsByLoanCaseIdAsync(loanCaseID, GetServiceHeader());

            //    ObservableCollection<LoanGuarantorDTO> loanGuarantors = new ObservableCollection<LoanGuarantorDTO>();

            //    foreach (var guarantors in LoanGuarantors)
            //    {
            //        loanGuarantors.Add(guarantors);
            //    }

            //    await _channelService.AttachLoanGuarantorsAsync(sourceCustomerAccountId, loanProductId, loanGuarantors, 1234, GetServiceHeader());

            //    MessageBox.Show(Form.ActiveForm, "Operation completed successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);

            //    return RedirectToAction("Index", "LoanRegistration");
            //}

            //TempData["emptyIDs"] = "Sorry, something went wrong !";
            //return View("Create");

            MessageBox.Show(Form.ActiveForm, "Operation completed successfully", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
            return RedirectToAction("Index", "LoanRegistration");
        }


        [HttpPost]
        public async Task<ActionResult> CallDeleteGuarantor(Guid GuarantorId, Guid LoanCaseId)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    var query = "DELETE  FROM swiftFin_LoanGuarantors WHERE LoanCaseId = @LoanCaseId AND CustomerId = @CustomerId";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@LoanCaseId", LoanCaseId);
                        cmd.Parameters.AddWithValue("@CustomerId", GuarantorId);
                        int rowsAffected = await cmd.ExecuteNonQueryAsync();

                        if (rowsAffected > 0)
                        {
                            return new HttpStatusCodeResult(200);
                        }
                        else
                        {
                            return new HttpStatusCodeResult(404, "Guarantor and Loancase not found");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(500, "Internal server error: " + ex.Message);
            }
        }


        [HttpPost]
        public async Task<ActionResult> removeGuarantors(Guid? id, LoanGuarantorDTO loanGuarantorDTO)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            var customer = await _channelService.FindCustomerAsync(parseId, GetServiceHeader());

            Guid loanCaseId = (Guid)Session["loanCaseId"];


            string message = string.Format(
                                      "{0}.\nAre you sure you want to remove the selected guarantor?", customer.FullName);

            // Show the message box with Yes/No options
            DialogResult result = MessageBox.Show(
                message,
                "Remove Loan Guarantor",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button1,
                MessageBoxOptions.ServiceNotification
            );

            if (result == DialogResult.Yes)
            {
                await CallDeleteGuarantor(parseId, loanCaseId);

                var findLoanGuarantors = await _channelService.FindLoanGuarantorsByLoanCaseIdAsync(loanCaseId, GetServiceHeader());
                ViewBag.LoanGuarantors = findLoanGuarantors;
            }

            MessageBox.Show(Form.ActiveForm, "Operation completed successfully", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
            return View("create");
        }
    }
}