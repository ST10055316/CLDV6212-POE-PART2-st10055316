using Microsoft.AspNetCore.Mvc;
using ABCRetailers.Models;
using ABCRetailers.Models.ViewModels;
using ABCRetailers.Services;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Net.Http; // Added for HttpRequestException

namespace ABCRetailers.Controllers
{
    public class OrderController : Controller
    {
        // Replaced IAzureStorageService with IFunctionsApi
        private readonly IFunctionsApi _functionsApi;

        public OrderController(IFunctionsApi functionsApi)
        {
            _functionsApi = functionsApi;
        }

        public async Task<IActionResult> Index()
        {
            // Use the API to get all orders
            var orders = await _functionsApi.GetOrdersAsync();
            return View(orders);
        }

        public async Task<IActionResult> Create()
        {
            var viewModel = await PopulateDropdownsAsync(new OrderCreateViewModel());
            viewModel.OrderDate = DateTime.Now;
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OrderCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Call the Azure Function API endpoint to handle the order creation, 
                    // stock check, stock update, and queue messages in a single transaction.
                    var newOrder = await _functionsApi.CreateOrderAsync(
                        model.CustomerId,
                        model.ProductId,
                        model.Quantity
                    );

                    // If the API call succeeds, the order and all side effects (stock/queues) are processed.
                    TempData["Success"] = $"Order {newOrder.Id} created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (HttpRequestException ex)
                {
                    // FIX: Removed the invalid access to 'ex.Response'. 
                    // HttpRequestException in modern .NET does not expose the Response object directly.
                    // We rely on the exception's message, which typically includes the status code and an error phrase.

                    // Note: If the backend function returns a specific user-friendly message, the 
                    // FunctionsApiClient would need to be updated to parse and throw a custom exception
                    // containing that message. For now, we use the general HTTP error message.
                    string errorMessage = $"API Error when creating order: {ex.Message}";

                    ModelState.AddModelError("", errorMessage);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"A general error occurred: {ex.Message}");
                }
            }

            // If ModelState is invalid or an error occurred, re-populate dropdowns and show the view.
            var viewModel = await PopulateDropdownsAsync(model);
            return View(viewModel);
        }

        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var order = await _functionsApi.GetOrderAsync(id);

            if (order == null) return NotFound();

            return View(order);
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            // Note: Since the IFunctionsApi currently only supports full order updates via 
            // the status endpoint, the "Edit" view will likely be limited to status changes,
            // or we would need a dedicated UpdateOrderAsync method in the API for full edits.
            var order = await _functionsApi.GetOrderAsync(id);

            if (order == null) return NotFound();

            return View(order);
        }

        // Removed the generic [HttpPost] Edit action as the API design promotes 
        // specialized status updates over generic full PUT/PATCH.
        // public async Task<IActionResult> Edit(Order order) { ... }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                // Use the API for deletion
                await _functionsApi.DeleteOrderAsync(id);
                TempData["Success"] = "Order deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting order: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<JsonResult> GetProductPrice(string productId)
        {
            try
            {
                // Use the API to get product details
                var product = await _functionsApi.GetProductAsync(productId);

                if (product != null)
                {
                    return Json(new
                    {
                        success = true,
                        price = product.Price,
                        stock = product.StockAvailable,
                        productName = product.ProductName,
                    });
                }
                return Json(new { success = false, message = "Product not found" });
            }
            catch (Exception ex)
            {
                // Log the exception here
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus(string id, string newStatus)
        {
            try
            {
                // Use the API for status update. The function handles the update and queue message.
                await _functionsApi.UpdateOrderStatusAsync(id, newStatus);
                return Json(new { success = true, message = $"Order status updated to {newStatus}" });
            }
            catch (Exception ex)
            {
                // Log the exception here
                return Json(new { success = false, message = ex.Message });
            }
        }

        private async Task<OrderCreateViewModel> PopulateDropdownsAsync(OrderCreateViewModel model)
        {
            // Use the API to get customers and products
            model.Customers = await _functionsApi.GetCustomersAsync();
            model.Products = await _functionsApi.GetProductsAsync();

            // Ensure lists are initialized, even if empty
            model.Customers ??= new List<Customer>();
            model.Products ??= new List<Product>();

            return model;
        }
    }
}
