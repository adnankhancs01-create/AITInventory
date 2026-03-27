using Common;
using Common.Models;
using Microsoft.AspNetCore.Mvc;
using Service.IService;

namespace AIT_Inventory.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;
        public ProductController(IProductService productService)
        {
            _productService = productService;
        }
        [HttpGet]
        [Route("Get")]
        public async Task<IActionResult> Get(int? productId)
        {
            var getProduct = await _productService.GetProductsAsync(productId);

            return Ok(getProduct);
        }
    }
}
