
using System;
using System.Web.Http;
using TestApis.Models;
using TestApis.Services;

namespace TestApis.Controllers
{
    [RoutePrefix("api/paye-setup")]
    public class PAYESetupController : ApiController
    {
        private readonly PAYESetupService _service = new PAYESetupService();

        // GET: api/paye-setup
        [HttpGet, Route("")]
        public IHttpActionResult GetAll()
        {
            try
            {
                var items = _service.GetAll();
                return Ok(new { success = true, message = "PAYE setups retrieved", data = items });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // GET: api/paye-setup/5
        [HttpGet, Route("{id:int}")]
        public IHttpActionResult Get(int id)
        {
            try
            {
                var item = _service.GetById(id);
                if (item == null)
                    return NotFound();

                return Ok(new { success = true, message = "PAYE setup retrieved", data = item });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // POST: api/paye-setup
        [HttpPost, Route("")]
        public IHttpActionResult Create([FromBody] PAYESetup setup)
        {
            try
            {
                if (setup == null)
                    return BadRequest("Invalid data");

                _service.Add(setup);
                return Created(Request.RequestUri, new { success = true, message = "PAYE setup created", data = setup });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // PUT: api/paye-setup/5
        [HttpPut, Route("{id:int}")]
        public IHttpActionResult Update(int id, [FromBody] PAYESetup setup)
        {
            try
            {
                if (setup == null || setup.Id != id)
                    return BadRequest("Invalid data");

                _service.Update(setup);
                return Ok(new { success = true, message = "PAYE setup updated", data = setup });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // DELETE: api/paye-setup/5
        [HttpDelete, Route("{id:int}")]
        public IHttpActionResult Delete(int id)
        {
            try
            {
                var existing = _service.GetById(id);
                if (existing == null)
                    return NotFound();

                _service.Delete(id);
                return Ok(new { success = true, message = "PAYE setup deleted" });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
    }
}
