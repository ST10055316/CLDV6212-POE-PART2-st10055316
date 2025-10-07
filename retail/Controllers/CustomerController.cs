using ABCRetailers.Models; // Assuming Models namespace
using ABCRetailers.Services; // Assuming Services namespace
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace ABCRetailers.Controllers
{
    public class CustomerController : Controller
    {
        // Changed dependency from IAzureStorageService to IFunctionsApi
        private readonly IFunctionsApi _functionsApi;

        public CustomerController(IFunctionsApi functionsApi)
        {
            _functionsApi = functionsApi;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // Use the API to get all customers
                var customers = await _functionsApi.GetCustomersAsync();
                return View(customers);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to load customers: {ex.Message}";
                return View(new System.Collections.Generic.List<Customer>());
            }
        }

        public IActionResult Create()
        {
            // Pass a new Customer object for form initialization
            return View(new Customer());
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // Added for security best practice
        public async Task<IActionResult> Create(Customer customer)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Use the API to create the customer (API handles ID generation/validation)
                    await _functionsApi.CreateCustomerAsync(customer);
                    TempData["Success"] = "Customer created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Failed to create customer via API: {ex.Message}");
                }
            }
            return View(customer);
        }

        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                // Use the API to get the customer by ID
                var customer = await _functionsApi.GetCustomerAsync(id);

                if (customer == null)
                {
                    return NotFound();
                }
                return View(customer);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error retrieving customer details: {ex.Message}";
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
                // Use the API to get the customer by ID
                var customer = await _functionsApi.GetCustomerAsync(id);

                if (customer == null)
                {
                    return NotFound();
                }
                return View(customer);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading customer for edit: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Customer customer)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Use the API to update the entity. 
                    // Assuming CustomerId/RowKey is correctly set in the incoming model.
                    await _functionsApi.UpdateCustomerAsync(customer.CustomerId, customer);

                    TempData["Success"] = "Customer updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error updating customer via API: {ex.Message}");
                }
            }
            return View(customer);
        }

        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                // Use the API to get the customer by ID for display
                var customer = await _functionsApi.GetCustomerAsync(id);

                if (customer == null)
                {
                    return NotFound();
                }
                return View(customer);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading customer for delete confirmation: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            try
            {
                // Use the API to delete the customer by ID
                await _functionsApi.DeleteCustomerAsync(id);
                TempData["Success"] = "Customer deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting customer: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
