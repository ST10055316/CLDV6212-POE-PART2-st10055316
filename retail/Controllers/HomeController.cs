using ABCRetail.Models; // Note: Original code had this mixed namespace
using ABCRetailers.Models;
using ABCRetailers.Models.ViewModels;
using ABCRetailers.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Threading.Tasks;
using System;
using System.Linq; // Needed for .Take()
using System.Collections.Generic; // Needed for List<T>

namespace ABCRetailers.Controllers
{
    public class HomeController : Controller
    {
        // Changed dependency from IAzureStorageService to IFunctionsApi
        private readonly IFunctionsApi _functionsApi;

        public HomeController(IFunctionsApi functionsApi)
        {
            _functionsApi = functionsApi;
        }

        public async Task<IActionResult> Index()
        {
            List<Product> products = new List<Product>();
            List<Customer> customers = new List<Customer>();
            List<Order> orders = new List<Order>();

            try
            {
                // Use the API layer to fetch data
                products = await _functionsApi.GetProductsAsync();
                customers = await _functionsApi.GetCustomersAsync();
                orders = await _functionsApi.GetOrdersAsync();
            }
            catch (Exception ex)
            {
                // Log the error and display a message, but allow the dashboard to load with zero counts
                TempData["Error"] = $"Could not load all data from API: {ex.Message}";
            }

            var viewModel = new HomeViewModel
            {
                // Ensure products is not null before using LINQ
                FeaturedProducts = products?.Take(5).ToList() ?? new List<Product>(),
                ProductCount = products?.Count ?? 0,
                CustomerCount = customers?.Count ?? 0,
                OrderCount = orders?.Count ?? 0
            };

            return View(viewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [HttpPost]
        public IActionResult InitializeStorage()
        {
            // IMPORTANT: Since we moved all storage logic to the Azure Functions API, 
            // the web app can no longer directly force storage initialization.
            // If this functionality is required, a dedicated administrative endpoint 
            // (e.g., /admin/initialize) must be created in the Azure Functions API 
            // and called via the IFunctionsApi service.

            TempData["Info"] = "Storage initialization is now handled by the Azure Functions API. This endpoint needs an API update.";

            return RedirectToAction(nameof(Index));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
