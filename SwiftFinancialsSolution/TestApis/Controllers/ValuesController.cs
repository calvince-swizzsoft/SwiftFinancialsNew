using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Http;
using Application.MainBoundedContext.DTO.AdministrationModule;
using DistributedServices.MainBoundedContext.Identity;

namespace TestApis.Controllers
{
    [RoutePrefix("api/values")] // Base path: /api/values
    public class ValuesController : MasterController
    {
        /// <summary>
        /// Test Apis 
        /// </summary>
        /// <returns></returns>
        // GET: api/values
        [HttpGet]
        [Route("{id:guid}")]
        public IHttpActionResult Get()
        {
            var values = new string[] { "value1", "value2" };
            return Ok(values);
        }
        
        // GET: api/values/{id}
        [HttpGet]
        [Route("")]
        public async Task<IHttpActionResult> GetAsync(Guid id)
        {
            try
            {
                await ServeNavigationMenus();

                var generalLedgerDTO = await _channelService.FindCustomersAsync( GetServiceHeader());
                if (generalLedgerDTO == null)
                    return NotFound();

                var batchEntries = await _channelService.FindGeneralLedgerEntriesByGeneralLedgerIdAsync(id, GetServiceHeader());

                var result = new
                {
                    GeneralLedger = generalLedgerDTO,
                    Entries = batchEntries
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // POST: api/values
        [HttpPost]
        [Route("")]
        public IHttpActionResult Post([FromBody] string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return BadRequest("Value cannot be empty.");

            return Ok(new { Message = "Value received", Value = value });
        }

        // PUT: api/values/{id}
        [HttpPut]
        [Route("{id:int}")]
        public IHttpActionResult Put(int id, [FromBody] string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return BadRequest("Value cannot be empty.");

            return Ok(new { Message = $"Value with ID {id} updated.", Value = value });
        }

        // DELETE: api/values/{id}
        [HttpDelete]
        [Route("{id:int}")]
        public IHttpActionResult Delete(int id)
        {
            return Ok(new { Message = $"Value with ID {id} deleted." });
        }
    }
}
