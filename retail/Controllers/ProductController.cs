using ABCRetailers.Models;
using ABCRetailers.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http; // Required for IFormFile
using System;
using System.Collections.Generic; // Required for List<Product>

namespace ABCRetailers.Controllers
{
    public class ProductController : Controller
    {
        // Only IFunctionsApi dependency is needed, IAzureStorageService is removed.
        private readonly IFunctionsApi _functionsApi;

        private readonly ILogger<ProductController> _logger;

        // Constructor updated to remove IAzureStorageService dependency
        // We rely entirely on the Functions API for all data persistence, including images.
        public ProductController(IFunctionsApi functionsApi, ILogger<ProductController> logger)
        {
            _functionsApi = functionsApi;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // MIGRATED: Use the API to get all products
                var products = await _functionsApi.GetProductsAsync();
                return View(products);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to load products from API: {ex.Message}";
                // Return an empty list if API fails
                return View(new List<Product>());
            }
        }

        public IActionResult Create()
        {
            return View(new Product());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, IFormFile? imageFile)
        {
            // Keep manual price parsing logic if needed due to potential binding issues
            if (Request.Form.TryGetValue("Price", out var priceFormValue))
            {
                if (int.TryParse(priceFormValue, out var parsedPrice))
                {
                    product.Price = parsedPrice;
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // MIGRATED: Use the API service to create the product, passing the file directly.
                    // The FunctionsApiClient handles multipart form creation and API submission, 
                    // and the Azure Function handles the blob storage upload (with a blob trigger/output).
                    await _functionsApi.CreateProductAsync(product, imageFile);

                    TempData["Success"] = $"Product {product.ProductName} created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating product via API");
                    ModelState.AddModelError("", $"Error creating product: {ex.Message}");
                }
            }
            return View(product);
        }

        // Standard GET action to display product details
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                var product = await _functionsApi.GetProductAsync(id);
                if (product == null)
                {
                    return NotFound();
                }
                return View(product);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error retrieving product details: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                var product = await _functionsApi.GetProductAsync(id);
                if (product == null)
                {
                    return NotFound();
                }
                return View(product);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading product for edit: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Product product, IFormFile? imageFile)
        {
            // Keep manual price parsing logic if needed
            if (Request.Form.TryGetValue("Price", out var priceFormValue))
            {
                if (int.TryParse(priceFormValue, out var parsedPrice))
                {
                    product.Price = parsedPrice;
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // MIGRATED: Use the API service to update the product, passing the file directly.
                    await _functionsApi.UpdateProductAsync(product.ProductId, product, imageFile);

                    TempData["Success"] = "Product updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating product via API: {Message}", ex.Message);
                    ModelState.AddModelError("", $"Error updating product: {ex.Message}");
                }
            }
            return View(product);
        }

        // Standard GET action to display the Delete confirmation view
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                var product = await _functionsApi.GetProductAsync(id);
                if (product == null)
                {
                    return NotFound();
                }
                return View(product);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading product for delete confirmation: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                // MIGRATED: Use the API to delete the product
                await _functionsApi.DeleteProductAsync(id);
                TempData["Success"] = "Product deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting product: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
