using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Threading.Tasks;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.FrontOfficeModule;
using Infrastructure.Crosscutting.Framework.Utils;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using Microsoft.AspNet.Identity;
using System.Windows.Forms;

namespace SwiftFinancials.Web.Areas.FrontOffice.Controllers
{
    public class EndOfDayController : MasterController
    {

        TellerDTO _selectedTeller;
        public TellerDTO SelectedTeller
        {
            get { return _selectedTeller; }
            set
            {
                if (_selectedTeller != value)
                {
                    _selectedTeller = value;

                }
            }
        }


        // GET: FrontOffice/EndOfDay
        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();
            return View();
        }

        public async Task<ActionResult> Create(Guid? id)
        {

            await ServeNavigationMenus();
           
            var model = new CashTransferRequestDTO();

            var currentUser = await _applicationUserManager.FindByIdAsync(User.Identity.GetUserId());

            _selectedTeller = await _channelService.FindTellerByEmployeeIdAsync((Guid)currentUser.EmployeeId, true, GetServiceHeader());


            var untransferredCheques = await _channelService.FindUnTransferredExternalChequesByTellerId(SelectedTeller.Id, "", GetServiceHeader());

            var untransferredChequesValue = untransferredCheques.Sum(cheque => cheque.Amount);

            model.EmployeeId = SelectedTeller.EmployeeId;
            model.TotalCredits = SelectedTeller.TotalCredits;
            model.TotalDebits = SelectedTeller.TotalDebits;
            model.BookBalance = SelectedTeller.BookBalance;

            model.OpeningBalance = SelectedTeller.OpeningBalance;
            model.ClosingBalance = SelectedTeller.ClosingBalance;

            model.UntransferredChequesValue = untransferredChequesValue;
            return View(model);
        }


        [HttpPost]
        public async Task<ActionResult> Create(CashTransferRequestDTO cashTransferRequestDTO)
        {
            /*ashTransferRequestDTO.ValidateAll();*/

            var currentUser = await _applicationUserManager.FindByIdAsync(User.Identity.GetUserId());

            _selectedTeller = await _channelService.FindTellerByEmployeeIdAsync((Guid)currentUser.EmployeeId, true, GetServiceHeader());

            cashTransferRequestDTO.EmployeeId = SelectedTeller.EmployeeId;

            var employee = await _channelService.FindEmployeeAsync((Guid)SelectedTeller.EmployeeId, GetServiceHeader());

            if (!cashTransferRequestDTO.HasErrors)
            { 
                
                var executed = await _channelService.EndOfDayExecutedAsync(employee, GetServiceHeader());
        

                if (executed)
                {
                    
                    MessageBox.Show("End of Day Successfully Executed", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                    return View(cashTransferRequestDTO);
                }
                else
                {
                   
                   
                    MessageBox.Show("End of Day has not been executed yet for this employee.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);

                    return View(cashTransferRequestDTO);
                }
            }
            else
            {
                var errorMessages = cashTransferRequestDTO.ErrorMessages;

                return View(cashTransferRequestDTO);
            }
        }



    }

}