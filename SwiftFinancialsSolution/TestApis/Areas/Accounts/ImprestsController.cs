using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using DistributedServices.MainBoundedContext.Identity;
using Microsoft.AspNet.Identity;
using Org.BouncyCastle.Utilities;
using SwiftFinancials.Presentation.Shared.Application.MainBoundedContext.DTO.AccountsModule;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using TestApis.Controllers;
using static TestApis.Controllers.BudgetManagementController;

namespace TestApis.Controllers
{

    [RoutePrefix("api/imprests")]
    public class ImprestsController : MasterController
    {

       [HttpGet]
        [Route("GetImprests")]
        public async Task<IHttpActionResult> GetImprests(bool? posted = null)
        {
            var serviceHeader = GetServiceHeader();
            var imprests = await _channelService.FindImprestsAsync(serviceHeader);

            // Apply filtering if 'posted' param is provided
            if (posted.HasValue)
            {
                imprests = imprests.Where(i => i.Posted == posted.Value).ToList();
            }

            return Json(new ApiResponse<object>
            {
                Success = true,
                Message = imprests?.Count > 0 ? $"{imprests.Count} imprests found." : "No imprests found.",
                Data = imprests
            });
        }



        [HttpPost]
        [Route("AddImprest")]
        public async Task<IHttpActionResult> AddImprest([FromBody] ImprestDTO imprestDTO)
        {

            var serviceHeader = GetServiceHeader();

            if (imprestDTO != null)
            {
                  //imprestDTO.AmountRequested = 0;
     
                var linesTotal = 0.00m;
            
                foreach (var gl in imprestDTO.ImprestLines)
                {

                    linesTotal = linesTotal + gl.Amount;

                    if (gl.ExpenseChartOfAccountId != Guid.Empty)
                    {

                        var debitGl = await _channelService.FindChartOfAccountAsync(gl.ExpenseChartOfAccountId);
                        gl.LineNo = debitGl.AccountCode;

                    }
                    else
                    {
                        return Json(new
                        {
                            success = false,
                            message = "YOU HAVE A LINE WITHOUT PROPERT EXPENSECHARTOFAACCOUNTID"
                        });
                    }

                }

                if (imprestDTO.AmountRequested != linesTotal)
                {

                    return Json(new
                    {
                        success = false,
                        message = "Amounts in Lines dont add up to value of Total Amount"
                    });
                }

                imprestDTO.ValidateAll();

                if (imprestDTO.ErrorMessages.Count > 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = imprestDTO.ErrorMessages
                    });
                }

                var result = await _channelService.AddNewImprestAsync(imprestDTO, serviceHeader);


                if (result != null)
                {


                    return Json(new
                    {
                        success = true,
                        message = "Successfully added imprest with lines."
                    });
                }


                else
                {
                    return Json(new
                    {
                        success = false,
                        message = "Failed to add imprest with lines."
                    });
                }

            }


            else
            {

                return Json(new
                {
                    success = false,
                    message = "Request Body is incomplete"
                });

            }

        }


        [HttpPut]
        [Route("UpdateImprest")]
        public async Task<IHttpActionResult> UpdateImprest([FromBody] ImprestDTO imprestDTO)
        {

            var serviceHeader = GetServiceHeader();

            if (imprestDTO != null)
            {

                imprestDTO.ValidateAll();


                if (imprestDTO.ErrorMessages.Count > 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = imprestDTO.ErrorMessages
                    });
                }

                var result = await _channelService.UpdateImprestAsync(imprestDTO, serviceHeader);


                if (result != null)
                {


                    return Json(new
                    {
                        success = true,
                        message = "Successfully updated Imprest Header with lines."
                    });
                }


                else
                {
                    return Json(new
                    {
                        success = false,
                        message = "Failed to update Imprest header with lines."
                    });
                }

            }


            else
            {

                return Json(new
                {
                    success = false,
                    message = "Request Body is incomplete"
                });

            }

        }


        [HttpPost]
        [Route("PostImprest/{id}")]
        public async Task<IHttpActionResult> PostImprest(Guid id)
        {

            var serviceHeader = GetServiceHeader();

            ImprestDTO imprestDTO = null;


            var imprestDTOs = await _channelService.FindImprestsAsync(serviceHeader);

            if (imprestDTOs != null)
            {

                imprestDTO = imprestDTOs.FirstOrDefault(p => p.Id == id);
            }


            if (imprestDTO != null)
            {

                var banks = await _channelService.FindBankLinkagesAsync(serviceHeader);

                var bank = banks[0];



                imprestDTO.BranchId = bank.BranchId;
                imprestDTO.BankId = bank.Id;
                imprestDTO.BankBranchName = bank.BankBranchName;

                imprestDTO.ValidateAll();
                if (imprestDTO.ErrorMessages.Count > 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = imprestDTO.ErrorMessages
                    });
                }

                //var transactionModel = new TransactionModel();

                int moduleNavigationItemCode = 0;

                var tariffs = new ObservableCollection<TariffWrapper>();

                //var result = await master._channelService.AddJournalAsync(transactionModel, tariffs, serviceHeader);

                var result = await _channelService.PostImprestAsync(imprestDTO, moduleNavigationItemCode, serviceHeader);

                if (result != null)
                {

                    return Json(new
                    {
                        success = true,
                        message = "Succesfully posted Journal",
                        data = result
                    });
                }

                else
                {


                    return Json(new
                    {
                        success = false,
                        message = "Failed to post journal"
                    });
                }


            }

            else
            {

                return Json(new
                {
                    success = false,
                    message = "Request Object is empty"
                });
            }

        }


        //[HttpPost]
        //[Route("PayImprest")]
        //public async Task<IHttpActionResult> PayImprest(PaymentDTO paymentDTO)
        //{

        //    var serviceHeader = GetServiceHeader();

        //    if (paymentDTO != null && paymentDTO.PaymentLines.Any())
        //    {

        //        decimal totalOfLines = paymentDTO.PaymentLines.Sum(x => x.Amount);

        //        if (paymentDTO.TotalAmount != totalOfLines)
        //        {
        //            return Json(new
        //            {
        //                success = false,
        //                message = $"Total mismatch: Header TotalAmount ({paymentDTO.TotalAmount:N2}) " +
        //                          $"does not equal sum of PaymentLines ({totalOfLines:N2})."
        //            });
        //        }

        //        int moduleNavigationItemCode = 0;

        //        var tariffs = new ObservableCollection<TariffWrapper>();

        //        var result = await _channelService.PayImprestAsync(paymentDTO, moduleNavigationItemCode, serviceHeader);

        //      //  var result = await _c

        //        if (result != null)
        //        {

        //            return Json(new
        //            {
        //                success = true,
        //                message = "Succesfully posted Journal",
        //                data = result
        //            });
        //        }

        //        else
        //        {


        //            return Json(new
        //            {
        //                success = false,
        //                message = "Failed to post journal"
        //            });
        //        }


        //    }

        //    else
        //    {

        //        return Json(new
        //        {
        //            success = false,
        //            message = "Request Object is empty"
        //        });
        //    }

        //}

      
    }
}
