using System; // Required for Guid and DateTime
using System.ComponentModel.DataAnnotations; // Required for [Required] attribute
using Azure.Data.Tables; // Required for ITableEntity
using Azure; // Required for ETag

namespace ABCRetailers.Models
{
    public class Customer : ITableEntity // Inherit from ITableEntity
    {
        // Azure Table Storage properties required for IAzureStorageService
        // PartitionKey helps organize entities and is part of the primary key.
        // For a customer entity, "Customer" can serve as a logical partition.
        public string PartitionKey { get; set; } = "Customer";

        // RowKey uniquely identifies an entity within a PartitionKey.
        // The controller uses CustomerId to generate and assign to RowKey.
        [Required(ErrorMessage = "RowKey is required.")] // Required for storage operations
        public string RowKey { get; set; } = string.Empty;

        // CustomerId is used by the controller to generate a unique GUID and assign it to RowKey.
        // It also serves as a direct identifier for the customer.
        [Required(ErrorMessage = "Customer ID is required.")]
        public string CustomerId { get; set; } = string.Empty;

        // Data properties for the customer
        [Required(ErrorMessage = "First Name is required.")]
        [StringLength(100, ErrorMessage = "First Name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty; // Initialize to prevent nullability warnings

        [Required(ErrorMessage = "Last Name is required.")]
        [StringLength(100, ErrorMessage = "Last Name cannot exceed 100 characters.")]
        public string Surname { get; set; } = string.Empty; // Initialize to prevent nullability warnings

        [Required(ErrorMessage = "Username is required.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters.")]
        public string Username { get; set; } = string.Empty; // Initialize to prevent nullability warnings

        [Required(ErrorMessage = "Email Address is required.")]
        [EmailAddress(ErrorMessage = "Invalid Email Address format.")]
        [StringLength(200, ErrorMessage = "Email Address cannot exceed 200 characters.")]
        public string Email { get; set; } = string.Empty; // Initialize to prevent nullability warnings

        [Required(ErrorMessage = "Shipping Address is required.")]
        [StringLength(500, ErrorMessage = "Shipping Address cannot exceed 500 characters.")]
        public string ShippingAddress { get; set; } = string.Empty; // Initialize to prevent nullability warnings

        // Automatically set the creation date when a new customer object is instantiated.
        // Changed DateTime.Now to DateTime.UtcNow to ensure UTC kind for Azure Table Storage.
        public DateTime CreationDate { get; set; } = DateTime.UtcNow;

        // ITableEntity properties
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        // Constructor to ensure CustomerId and RowKey are initialized when a new Customer is created
        public Customer()
        {
            // Generate a new GUID for CustomerId and use it for RowKey
            string newGuid = Guid.NewGuid().ToString();
            CustomerId = newGuid;
            RowKey = newGuid;
        }
    }
}
