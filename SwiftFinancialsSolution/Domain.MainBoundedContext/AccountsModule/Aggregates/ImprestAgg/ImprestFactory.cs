using Domain.MainBoundedContext.AccountsModule.Aggregates.PurchaseInvoiceAgg;
using Infrastructure.Crosscutting.Framework.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.MainBoundedContext.AccountsModule.Aggregates.ImprestAgg
{
    public class ImprestFactory 
    {

                 public static Imprest CreateImprest(string no, string employeeNo, string employeeName, string purpose, decimal amountRequested, DateTime requestDate, Boolean posted, ServiceHeader serviceHeader)
            {
                var imprest = new Imprest();

            imprest.Posted = posted;    
            
            imprest.No = no;

            imprest.EmployeeName = employeeName;

            imprest.EmployeeNo = employeeNo;

            imprest.Purpose = purpose;

            imprest.AmountRequested = amountRequested;

            imprest.RequestDate = requestDate;


            imprest.CreatedDate = DateTime.Now;

            imprest.GenerateNewIdentity();

                return imprest;

            }
        }
    }
