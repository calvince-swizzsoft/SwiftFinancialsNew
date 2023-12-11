using Application.MainBoundedContext.DTO.AccountsModule;
using SwiftFinancials.Web.Controllers;
using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Application.MainBoundedContext.DTO;
using SwiftFinancials.Presentation.Infrastructure.Util;
using SwiftFinancials.Web.Helpers;


namespace SwiftFinancials.Web.Areas.Accounts.Controllers
{
    public class ManagementController : MasterController
    {

        public async Task<ActionResult> CustomerManagement(Guid? id)
         {
            await ServeNavigationMenus();


            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            var customer = await _channelService.FindCustomerAsync(parseId, GetServiceHeader());

            CustomerAccountDTO customerAccountDTO = new CustomerAccountDTO();

            if (customer != null)
            {

                customerAccountDTO.CustomerId = customer.Id;
                customerAccountDTO.CustomerIndividualFirstName = customer.IndividualFirstName;
                customerAccountDTO.CustomerIndividualPayrollNumbers = customer.IndividualPayrollNumbers;
                customerAccountDTO.CustomerSerialNumber = customer.SerialNumber;
                customerAccountDTO.CustomerIndividualIdentityCardNumber = customer.IndividualIdentityCardNumber;
                customerAccountDTO.CustomerStationZoneDivisionEmployerDescription = customer.StationZoneDivisionEmployerDescription;
            }



            ViewBag.CCustomerAccountManagementActionSelectList = GetCCustomerAccountManagementActionSelectList(string.Empty);

            ViewBag.CustomerAccountManagementActionSelectList = GetCustomerAccountManagementActionSelectList(string.Empty);


            return View(customerAccountDTO);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CustomerManagement(Guid id, CustomerAccountDTO customerAccountDTO)
        {
            customerAccountDTO.ValidateAll();

            if (!customerAccountDTO.HasErrors)
            {
                int managementAction = 0;

                string remarks = "";

                int remarkType = 0;

                await _channelService.ManageCustomerAccountAsync(id, managementAction, remarks, remarkType, GetServiceHeader());

                ViewBag.CCustomerAccountManagementActionSelectList = GetCCustomerAccountManagementActionSelectList(customerAccountDTO.CustomerIndividualSalutation.ToString());


                ViewBag.CustomerAccountManagementActionSelectList = GetCustomerAccountManagementActionSelectList(customerAccountDTO.CustomerIndividualSalutationDescription.ToString());


                return RedirectToAction("CustomerManagement");
            }
            else
            {
                var errorMessages = customerAccountDTO.ErrorMessages;

                ViewBag.CCustomerAccountManagementActionSelectList = GetCCustomerAccountManagementActionSelectList(customerAccountDTO.CustomerIndividualSalutation.ToString());

                ViewBag.CustomerAccountManagementActionSelectList = GetCustomerAccountManagementActionSelectList(customerAccountDTO.CustomerIndividualSalutationDescription.ToString());


                return View(customerAccountDTO);
            }
        }



    }
}


