using Application.MainBoundedContext.DTO.AccountsModule;
using Infrastructure.Crosscutting.Framework.Utils;
using System;
using System.Collections.Generic;
using System.Web.Http;
using TestApis.Services;

namespace TestApis.Controllers
{
    [RoutePrefix("api/chartofaccounts")]
    public class ChartOfAccountsController : ApiController
    {
        private readonly ChartOfAccountService _service = new ChartOfAccountService();

        private IHttpActionResult ApiResponse(bool success, string message, object data = null)
        {
            return Ok(new { success, message, data });
        }

        [HttpGet, Route("")]
        public IHttpActionResult GetAll()
        {
            try
            {
                var accounts = _service.GetAll();
                return ApiResponse(true, "Chart of accounts retrieved successfully", accounts);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpGet, Route("hierarchy")]
        public IHttpActionResult GetHierarchy()
        {
            try
            {
                var hierarchy = _service.GetHierarchy();
                return ApiResponse(true, "Chart of accounts hierarchy retrieved successfully", hierarchy);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpGet, Route("{id:guid}")]
        public IHttpActionResult Get(Guid id)
        {
            try
            {
                var account = _service.GetById(id);
                if (account == null)
                    return Content(System.Net.HttpStatusCode.NotFound,
                                   new { success = false, message = "Account not found" });

                return ApiResponse(true, "Account retrieved successfully", account);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpGet, Route("by-code/{accountCode:int}")]
        public IHttpActionResult GetByAccountCode(int accountCode)
        {
            try
            {
                var account = _service.GetByAccountCode(accountCode);
                if (account == null)
                    return Content(System.Net.HttpStatusCode.NotFound,
                                   new { success = false, message = "Account not found" });

                return ApiResponse(true, "Account retrieved successfully", account);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpGet, Route("by-type/{accountType:int}")]
        public IHttpActionResult GetByAccountType(int accountType)
        {
            try
            {
                var accounts = _service.GetByAccountType(accountType);
                return ApiResponse(true, "Accounts retrieved successfully", accounts);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpGet, Route("by-parent/{parentId:guid}")]
        public IHttpActionResult GetByParent(Guid parentId)
        {
            try
            {
                var accounts = _service.GetByParentId(parentId);
                return ApiResponse(true, "Child accounts retrieved successfully", accounts);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpGet, Route("root")]
        public IHttpActionResult GetRootAccounts()
        {
            try
            {
                var accounts = _service.GetByParentId(null);
                return ApiResponse(true, "Root accounts retrieved successfully", accounts);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpGet, Route("control-accounts")]
        public IHttpActionResult GetControlAccounts()
        {
            try
            {
                var accounts = _service.GetControlAccounts();
                return ApiResponse(true, "Control accounts retrieved successfully", accounts);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpGet, Route("reconciliation-accounts")]
        public IHttpActionResult GetReconciliationAccounts()
        {
            try
            {
                var accounts = _service.GetReconciliationAccounts();
                return ApiResponse(true, "Reconciliation accounts retrieved successfully", accounts);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpGet, Route("search")]
        public IHttpActionResult Search([FromUri] string query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Search query is required" });

                var accounts = _service.Search(query);
                return ApiResponse(true, "Accounts retrieved successfully", accounts);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpPost, Route("")]
        public IHttpActionResult Create([FromBody] ChartOfAccountDTO account)
        {
            try
            {
                if (account == null)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Invalid account data" });

                // Validate required fields
                if (string.IsNullOrWhiteSpace(account.AccountName))
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Account name is required" });

                // Validate account type
                if (!Enum.IsDefined(typeof(ChartOfAccountType), account.AccountType))
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Invalid account type" });

                // Validate account category
                if (!Enum.IsDefined(typeof(ChartOfAccountCategory), account.AccountCategory))
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Invalid account category" });

                var createdAccount = _service.Create(account);

                return Content(System.Net.HttpStatusCode.Created,
                               new
                               {
                                   success = true,
                                   message = "Account created successfully",
                                   data = createdAccount
                               });
            }
            catch (InvalidOperationException ex)
            {
                return Content(System.Net.HttpStatusCode.BadRequest,
                               new { success = false, message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return Content(System.Net.HttpStatusCode.NotFound,
                               new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpPut, Route("{id:guid}")]
        public IHttpActionResult Update(Guid id, [FromBody] ChartOfAccountDTO account)
        {
            try
            {
                if (account == null || account.Id != id)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Invalid account data" });

                // Check if account exists
                var existing = _service.GetById(id);
                if (existing == null)
                    return Content(System.Net.HttpStatusCode.NotFound,
                                   new { success = false, message = "Account not found" });

                // Check if account is locked
                if (existing.IsLocked)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Cannot update a locked account" });

                // Validate required fields
                if (string.IsNullOrWhiteSpace(account.AccountName))
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Account name is required" });

                // Validate account type
                if (!Enum.IsDefined(typeof(ChartOfAccountType), account.AccountType))
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Invalid account type" });

                // Validate account category
                if (!Enum.IsDefined(typeof(ChartOfAccountCategory), account.AccountCategory))
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Invalid account category" });

                _service.Update(account);
                return ApiResponse(true, "Account updated successfully", account);
            }
            catch (InvalidOperationException ex)
            {
                return Content(System.Net.HttpStatusCode.BadRequest,
                               new { success = false, message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return Content(System.Net.HttpStatusCode.NotFound,
                               new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpDelete, Route("{id:guid}")]
        public IHttpActionResult Delete(Guid id)
        {
            try
            {
                // Check if account exists
                var account = _service.GetById(id);
                if (account == null)
                    return Content(System.Net.HttpStatusCode.NotFound,
                                   new { success = false, message = "Account not found" });

                _service.Delete(id);
                return ApiResponse(true, "Account deleted successfully");
            }
            catch (InvalidOperationException ex)
            {
                return Content(System.Net.HttpStatusCode.BadRequest,
                               new { success = false, message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return Content(System.Net.HttpStatusCode.NotFound,
                               new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpPost, Route("{id:guid}/lock")]
        public IHttpActionResult Lock(Guid id)
        {
            try
            {
                var account = _service.GetById(id);
                if (account == null)
                    return Content(System.Net.HttpStatusCode.NotFound,
                                   new { success = false, message = "Account not found" });

                account.IsLocked = true;
                _service.Update(account);

                return ApiResponse(true, "Account locked successfully", account);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpPost, Route("{id:guid}/unlock")]
        public IHttpActionResult Unlock(Guid id)
        {
            try
            {
                var account = _service.GetById(id);
                if (account == null)
                    return Content(System.Net.HttpStatusCode.NotFound,
                                   new { success = false, message = "Account not found" });

                account.IsLocked = false;
                _service.Update(account);

                return ApiResponse(true, "Account unlocked successfully", account);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpGet, Route("{id:guid}/generate-code")]
        public IHttpActionResult GenerateAccountCode(Guid? id)
        {
            try
            {
                int accountCode = _service.GenerateAccountCode(id);
                return ApiResponse(true, "Account code generated successfully", new { AccountCode = accountCode });
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpGet, Route("validate-code/{accountCode:int}")]
        public IHttpActionResult ValidateAccountCode(int accountCode)
        {
            try
            {
                var existing = _service.GetByAccountCode(accountCode);
                if (existing != null)
                {
                    return ApiResponse(false, $"Account code {accountCode} already exists", new
                    {
                        IsAvailable = false,
                        ExistingAccount = existing.AccountName
                    });
                }

                return ApiResponse(true, $"Account code {accountCode} is available", new { IsAvailable = true });
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }
    }
}