using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Routing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ODataAspNetCorePoc.Models;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace ODataAspNetCorePoc.Controllers
{
    [Produces("application/json")]
    public class ProductController : ODataController
    {
        private readonly NorthwindSlimContext _context;

        public ProductController(NorthwindSlimContext context)
        {
            _context = context;
        }

        [HttpGet]
        [EnableQuery]
        public IQueryable<Product> Get()
        {
            return _context.Product;
        }

        // Note: Throws NotImplementedException. See https://github.com/OData/WebApi/issues/1142
        //[HttpGet("{id}")]
        //public async Task<Product> Get([FromODataUri]int id)
        //{
        //    var result = await _context.Product.FirstOrDefaultAsync(e => e.ProductId == id);
        //    return result;
        //}

        [HttpPost]
        public async Task<IActionResult> Post([FromBody]Product item)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.Entry(item).State = EntityState.Added;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (ItemExists(item.ProductId))
                {
                    return StatusCode((int)HttpStatusCode.Conflict);
                }
                throw;
            }

            return Created(item);
        }

        // Note: Put and Patch actions return 404 Not Found
        [HttpPut]
        public async Task<IActionResult> Put([FromBody]Product item)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.Entry(item).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (!ItemExists(item.ProductId))
                {
                    return NotFound();
                }
                throw;
            }

            return Updated(item);
        }

        private bool ItemExists(int id)
        {
            return _context.Product.Any(e => e.ProductId == id);
        }
    }
}