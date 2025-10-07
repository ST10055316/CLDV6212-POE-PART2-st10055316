using ABCRetailers.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http; // Needed for IFormFile (assuming it's available in the MVC project)

namespace ABCRetailers.Services
{
    public interface IFunctionsApi
    {
        // Customers
        Task<List<Customer>> GetCustomersAsync();
        Task<Customer?> GetCustomerAsync(string id);
        Task<Customer> CreateCustomerAsync(Customer c);
        Task<Customer> UpdateCustomerAsync(string id, Customer c);
        Task DeleteCustomerAsync(string id);

        // Products
        Task<List<Product>> GetProductsAsync();
        Task<Product?> GetProductAsync(string id);
        // These are the correct signatures requiring IFormFile for potential upload
        Task<Product> CreateProductAsync(Product p, IFormFile? imageFile);
        Task<Product> UpdateProductAsync(string id, Product p, IFormFile? imageFile);
        Task DeleteProductAsync(string id);

        // Orders
        Task<List<Order>> GetOrdersAsync();
        Task<Order?> GetOrderAsync(string id);
        // This is the combined operation: Create order, check stock, update stock, send queue messages.
        Task<Order> CreateOrderAsync(string customerId, string productId, int quantity);
        Task UpdateOrderStatusAsync(string id, string newStatus);
        Task DeleteOrderAsync(string id);

        // Uploads
        Task<string> UploadProofOfPaymentAsync(IFormFile file, string? orderId, string? customerName);

        // Removed the problematic/redundant methods (Task UpdateProductAsync(string productId, Product product) 
        // and Task CreateProductAsync(Product product)) to resolve CS0535.
    }
}
