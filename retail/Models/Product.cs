using System;
using System.ComponentModel.DataAnnotations;
using Azure.Data.Tables;
using Azure;

namespace ABCRetailers.Models
{
    public class Product : ITableEntity
    {
        public string PartitionKey { get; set; } = "Product";

        [Required(ErrorMessage = "RowKey is required.")]
        public string RowKey { get; set; } = string.Empty;

        [Required(ErrorMessage = "Product ID is required.")]
        public string ProductId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Product Name is required.")]
        [StringLength(200, ErrorMessage = "Product Name cannot exceed 200 characters.")]
        public string ProductName { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Price is required.")]
        [Range(0.01, 1000000.00, ErrorMessage = "Price must be between 0.01 and 1,000,000.")]
        [DataType(DataType.Currency)]
        public int Price { get; set; }

        [Required(ErrorMessage = "Stock available is required.")]
        [Range(0, int.MaxValue, ErrorMessage = "Stock available cannot be negative.")]
        public int StockAvailable { get; set; }

        public string ImageUrl { get; set; } = string.Empty; // URL for product image

        public DateTime CreationDate { get; set; } = DateTime.UtcNow;

        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public Product()
        {
            string newGuid = Guid.NewGuid().ToString();
            ProductId = newGuid;
            RowKey = newGuid;
        }
    }
}