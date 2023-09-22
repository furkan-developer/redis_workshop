using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;

namespace Caching.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<ProductsController> _logger;
        private const string productListCacheKey = "productlist";

        public ProductsController(IDistributedCache cache, ILogger<ProductsController> logger)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        public IActionResult GetAllProducts()
        {
            _logger.LogInformation("Trying to fetch products list");

            string? data = _cache.GetString(productListCacheKey);
            if (data != null)
            {
                _logger.LogInformation("Products list took from redis cache storage");
                return Ok(JsonSerializer.Deserialize<List<string>>(data));
            }

            var products = GetAllProductsFromDB();
            _cache.SetString(
                productListCacheKey,
                JsonSerializer.Serialize(products),
                new DistributedCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromSeconds(60))
                    .SetSlidingExpiration(TimeSpan.FromSeconds(30)));

            _logger.LogInformation($"Products list took from database, then data caching at {DateTime.Now.Minute}:{DateTime.Now.Second}");

            return Ok(products);
        }

        [NonAction]
        public List<string>? GetAllProductsFromDB()
        {
            List<string>? products = null;
            string dataFilePath = Path.Combine(Environment.CurrentDirectory, "datas", "products.json");

            if (!System.IO.File.Exists(dataFilePath)) throw new FileNotFoundException();

            using (StreamReader reader = new StreamReader(dataFilePath))
            {
                string json = reader.ReadToEnd();
                products = JsonSerializer.Deserialize<List<string>>(json);
            }
            return products;
        }
    }
}