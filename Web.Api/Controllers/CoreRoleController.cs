using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using KDMApi.DataContexts;
using KDMApi.Models;
using Microsoft.AspNetCore.Authorization;

namespace KDMApi.Controllers
{
    [Produces("application/json")]
    [Route("v1/[controller]")]
    [ApiController]
    [EnableCors("QuBisaPolicy")]
    public class CoreRolesController : ControllerBase
    {
        private readonly DefaultContext _context;
        public CoreRolesController(DefaultContext context)
        {
            _context = context;
        }

        // GET: v1/CoreRoles/5
        [Authorize(Policy = "ApiUser")]
        [HttpGet("{id}")]
        public async Task<ActionResult<CoreRole>> GetCoreRole(int id)
        {
            var role = _context.CoreRoles.FirstOrDefault<CoreRole>(a => a.Id == id);

            if (role == null)
            {
                return NotFound();
            }

            return role;
        }

    }

}
