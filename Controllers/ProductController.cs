using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalesApp.Data;
using SalesApp.Models;

namespace SalesApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _context.Products.ToListAsync());
        }

        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request)
        {
            decimal total = 0;

            foreach (var item in request.Items)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product == null || product.Quantity < item.Quantity)
                    return BadRequest("Not enough stock or product not found.");

                total += product.Price * item.Quantity;
                product.Quantity -= item.Quantity;
            }

            if (request.CashPaid < total)
                return BadRequest("Not enough cash.");

            await _context.SaveChangesAsync();

            var change = CalculateChange(request.CashPaid - total);
            return Ok(new { Total = total, Change = change });
        }

        private Dictionary<decimal, int> CalculateChange(decimal change)
        {
            var denominations = new List<decimal> { 50, 20, 10, 5, 2, 1, 0.5m, 0.2m, 0.1m, 0.05m, 0.02m, 0.01m };
            var result = new Dictionary<decimal, int>();

            foreach (var denom in denominations)
            {
                int count = (int)(change / denom);
                if (count > 0)
                {
                    result[denom] = count;
                    change -= count * denom;
                }
            }

            return result;
        }

        [HttpPut("update-stock/{id}")]
        public async Task<IActionResult> UpdateStock(int id, [FromBody] int quantity)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            product.Quantity = quantity;
            await _context.SaveChangesAsync();

            return Ok(product);
        }

    }
}
