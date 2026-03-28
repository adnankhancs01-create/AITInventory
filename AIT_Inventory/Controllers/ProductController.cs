using Common;
using Common.Models;
using Common.Models.RequestModel;
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
        [HttpPost]
        [Route("AddEditProduct")]
        public async Task<IActionResult> AddEditProduct(ProductRequesModel request)
        {
            var result = await _productService.AddEditProductAsync(request);

            return Ok(result);
        }
        [HttpPost]
        [Route("AddEditProductCategory")]
        public async Task<IActionResult> AddEditProductCategory(ProductCategoryRequesModel request)
        {
            var result = await _productService.AddEditProductCategoryAsync(request);

            return Ok(result);
        }
    }
}
