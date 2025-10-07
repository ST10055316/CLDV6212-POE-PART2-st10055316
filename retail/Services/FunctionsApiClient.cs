using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ABCRetailers.Models;
using Microsoft.AspNetCore.Http; // Needed for IFormFile

namespace ABCRetailers.Services;

public class FunctionsApiClient : IFunctionsApi
{
    private readonly HttpClient _http;
    // Use JsonSerializerOptions for web defaults (camelCase)
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    // Centralize your Function routes here
    private const string CustomersRoute = "customers";
    private const string ProductsRoute = "products";
    private const string OrdersRoute = "orders";
    private const string UploadsRoute = "uploads/proof-of-payment"; // multipart

    public FunctionsApiClient(IHttpClientFactory factory)
    {
        _http = factory.CreateClient("Functions"); // BaseAddress set in Program.cs
    }

    // ---------- Helpers ----------
    private static HttpContent JsonBody(object obj)
        => new StringContent(JsonSerializer.Serialize(obj, _json), Encoding.UTF8, "application/json");

    private static async Task<T> ReadJsonAsync<T>(HttpResponseMessage resp)
    {
        resp.EnsureSuccessStatusCode();
        var stream = await resp.Content.ReadAsStreamAsync();
        // Null-forgiving operator '!' used as EnsureSuccessStatusCode makes successful deserialization highly likely
        var data = await JsonSerializer.DeserializeAsync<T>(stream, _json);
        return data!;
    }

    // ---------- Customers ----------
    public async Task<List<Customer>> GetCustomersAsync()
        => await ReadJsonAsync<List<Customer>>(await _http.GetAsync(CustomersRoute));

    public async Task<Customer?> GetCustomerAsync(string id)
    {
        var resp = await _http.GetAsync($"{CustomersRoute}/{id}");
        if (resp.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        return await ReadJsonAsync<Customer>(resp);
    }

    public async Task<Customer> CreateCustomerAsync(Customer c)
        // Using an anonymous object to explicitly map to the expected function JSON payload
        => await ReadJsonAsync<Customer>(await _http.PostAsync(CustomersRoute, JsonBody(new
        {
            name = c.Name,
            surname = c.Surname,
            username = c.Username,
            email = c.Email,
            shippingAddress = c.ShippingAddress
        })));

    public async Task<Customer> UpdateCustomerAsync(string id, Customer c)
        // Using an anonymous object to explicitly map to the expected function JSON payload
        => await ReadJsonAsync<Customer>(await _http.PutAsync($"{CustomersRoute}/{id}", JsonBody(new
        {
            name = c.Name,
            surname = c.Surname,
            username = c.Username,
            email = c.Email,
            shippingAddress = c.ShippingAddress
        })));

    public async Task DeleteCustomerAsync(string id)
        => (await _http.DeleteAsync($"{CustomersRoute}/{id}")).EnsureSuccessStatusCode();

    // ---------- Products ----------
    public async Task<List<Product>> GetProductsAsync()
        => await ReadJsonAsync<List<Product>>(await _http.GetAsync(ProductsRoute));

    public async Task<Product?> GetProductAsync(string id)
    {
        var resp = await _http.GetAsync($"{ProductsRoute}/{id}");
        if (resp.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        return await ReadJsonAsync<Product>(resp);
    }

    public async Task<Product> CreateProductAsync(Product p, IFormFile? imageFile)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(p.ProductName), "ProductName");
        form.Add(new StringContent(p.Description ?? string.Empty), "Description");
        // Ensure price and stock are sent using invariant culture to avoid decimal format errors
        form.Add(new StringContent(p.Price.ToString(System.Globalization.CultureInfo.InvariantCulture)), "Price");
        form.Add(new StringContent(p.StockAvailable.ToString(System.Globalization.CultureInfo.InvariantCulture)), "StockAvailable");
        if (!string.IsNullOrWhiteSpace(p.ImageUrl)) form.Add(new StringContent(p.ImageUrl), "ImageUrl");
        if (imageFile is not null && imageFile.Length > 0)
        {
            var file = new StreamContent(imageFile.OpenReadStream());
            file.Headers.ContentType = new MediaTypeHeaderValue(imageFile.ContentType ?? "application/octet-stream");
            form.Add(file, "ImageFile", imageFile.FileName);
        }
        return await ReadJsonAsync<Product>(await _http.PostAsync(ProductsRoute, form));
    }

    public async Task<Product> UpdateProductAsync(string id, Product p, IFormFile? imageFile)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(p.ProductName), "ProductName");
        form.Add(new StringContent(p.Description ?? string.Empty), "Description");
        form.Add(new StringContent(p.Price.ToString(System.Globalization.CultureInfo.InvariantCulture)), "Price");
        form.Add(new StringContent(p.StockAvailable.ToString(System.Globalization.CultureInfo.InvariantCulture)), "StockAvailable");
        if (!string.IsNullOrWhiteSpace(p.ImageUrl)) form.Add(new StringContent(p.ImageUrl), "ImageUrl");
        if (imageFile is not null && imageFile.Length > 0)
        {
            var file = new StreamContent(imageFile.OpenReadStream());
            file.Headers.ContentType = new MediaTypeHeaderValue(imageFile.ContentType ?? "application/octet-stream");
            form.Add(file, "ImageFile", imageFile.FileName);
        }
        return await ReadJsonAsync<Product>(await _http.PutAsync($"{ProductsRoute}/{id}", form));
    }

    public async Task DeleteProductAsync(string id)
        => (await _http.DeleteAsync($"{ProductsRoute}/{id}")).EnsureSuccessStatusCode();

    // ---------- Orders ----------
    public async Task<List<Order>> GetOrdersAsync()
    {
        // Fetch DTOs and map them to the MVC 'Order' model
        var dtos = await ReadJsonAsync<List<OrderDto>>(await _http.GetAsync(OrdersRoute));
        return dtos.Select(ToOrder).ToList();
    }

    public async Task<Order?> GetOrderAsync(string id)
    {
        var resp = await _http.GetAsync($"{OrdersRoute}/{id}");
        if (resp.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        var dto = await ReadJsonAsync<OrderDto>(resp);
        return ToOrder(dto);
    }

    public async Task<Order> CreateOrderAsync(string customerId, string productId, int quantity)
    {
        // Send required payload data to the Function (Function handles business logic)
        var payload = new { customerId, productId, quantity };
        var dto = await ReadJsonAsync<OrderDto>(await _http.PostAsync(OrdersRoute, JsonBody(payload)));
        return ToOrder(dto);
    }

    public async Task UpdateOrderStatusAsync(string id, string newStatus)
    {
        // PATCH request to update only the status field
        var payload = new { status = newStatus };
        (await _http.PatchAsync($"{OrdersRoute}/{id}/status", JsonBody(payload))).EnsureSuccessStatusCode();
    }

    public async Task DeleteOrderAsync(string id)
        => (await _http.DeleteAsync($"{OrdersRoute}/{id}")).EnsureSuccessStatusCode();

    // ---------- Uploads ----------
    public async Task<string> UploadProofOfPaymentAsync(IFormFile file, string? orderId, string? customerName)
    {
        using var form = new MultipartFormDataContent();
        var sc = new StreamContent(file.OpenReadStream());
        sc.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
        form.Add(sc, "ProofOfPayment", file.FileName);
        if (!string.IsNullOrWhiteSpace(orderId)) form.Add(new StringContent(orderId), "OrderId");
        if (!string.IsNullOrWhiteSpace(customerName)) form.Add(new StringContent(customerName), "CustomerName");

        var resp = await _http.PostAsync(UploadsRoute, form);
        resp.EnsureSuccessStatusCode();

        // The function returns a JSON object like { "fileName": "..." }
        var doc = await ReadJsonAsync<Dictionary<string, string>>(resp);
        return doc.TryGetValue("fileName", out var name) ? name : file.FileName;
    }

    // ---------- Mapping ----------
    // Maps the OrderDto (received from the Function API) to the MVC's internal Order model
    private static Order ToOrder(OrderDto d)
    {
        // Simplified status parsing for the MVC model
        var status = d.Status ?? "Submitted";

        return new Order
        {
            OrderId = d.Id, // Assuming OrderId in the MVC model maps to Id in the DTO
            CustomerId = d.CustomerId,
            ProductId = d.ProductId,
            ProductName = d.ProductName,
            Quantity = d.Quantity,
            UnitPrice = (double)d.UnitPrice,
            // Convert back from DateTimeOffset to DateTime for the MVC model
            OrderDate = d.OrderDateUtc.UtcDateTime,
            TotalPrice = (double)(d.Quantity * d.UnitPrice), // Recalculate or ensure it's in the DTO
            Status = status
        };
    }

    // DTOs that match Functions JSON (camelCase structure)
    private sealed record OrderDto(
        string Id,
        string CustomerId,
        string ProductId,
        string ProductName,
        int Quantity,
        decimal UnitPrice,
        DateTimeOffset OrderDateUtc,
        string Status);
}

// Minimal PATCH extension for HttpClient (Kept for completeness)
internal static class HttpClientPatchExtensions
{
    public static Task<HttpResponseMessage> PatchAsync(this HttpClient client, string requestUri, HttpContent content)
        => client.SendAsync(new HttpRequestMessage(HttpMethod.Patch, requestUri) { Content = content });
}
