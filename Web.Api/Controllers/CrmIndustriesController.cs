using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KDMApi.DataContexts;
using KDMApi.Models;

namespace KDMApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CrmIndustriesController : ControllerBase
    {
        private readonly DefaultContext _context;

        public CrmIndustriesController(DefaultContext context)
        {
            _context = context;
        }

        // GET: api/CrmIndustries
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CrmIndustry>>> GetCrmIndustries()
        {
            return await _context.CrmIndustries.ToListAsync();
        }

        // GET: api/CrmIndustries/5
        [HttpGet("{id}")]
        public async Task<ActionResult<CrmIndustry>> GetCrmIndustry(int id)
        {
            var crmIndustry = await _context.CrmIndustries.FindAsync(id);

            if (crmIndustry == null)
            {
                return NotFound();
            }

            return crmIndustry;
        }

        // PUT: api/CrmIndustries/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCrmIndustry(int id, CrmIndustry crmIndustry)
        {
            if (id != crmIndustry.Id)
            {
                return BadRequest();
            }

            _context.Entry(crmIndustry).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CrmIndustryExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/CrmIndustries
        [HttpPost]
        public async Task<ActionResult<CrmIndustry>> PostCrmIndustry(CrmIndustry crmIndustry)
        {
            _context.CrmIndustries.Add(crmIndustry);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetCrmIndustry", new { id = crmIndustry.Id }, crmIndustry);
        }

        // DELETE: api/CrmIndustries/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<CrmIndustry>> DeleteCrmIndustry(int id)
        {
            var crmIndustry = await _context.CrmIndustries.FindAsync(id);
            if (crmIndustry == null)
            {
                return NotFound();
            }

            _context.CrmIndustries.Remove(crmIndustry);
            await _context.SaveChangesAsync();

            return crmIndustry;
        }

        private bool CrmIndustryExists(int id)
        {
            return _context.CrmIndustries.Any(e => e.Id == id);
        }
    }
}
