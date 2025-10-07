using Microsoft.AspNetCore.Http; // Required for IFormFile

namespace ABCRetailers.Models
{
    public class FileUploadModel
    {
        // Properties required by UploadController, as per CS1061 errors
        public IFormFile? ProofOfPayment { get; set; } // Can be null if no file is selected
        public string OrderId { get; set; } = string.Empty; // Initialized to prevent CS8618
        public string CustomerName { get; set; } = string.Empty; // Initialized to prevent CS8618

        // Other properties causing CS8618 errors, now initialized
        public string Name { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
    }
}