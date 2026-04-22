
namespace Common.Models
{
    public class RootModels
    {
    }
    public class ProductModel
    {
        public int? Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ProductCode { get; set; }
        public int CategoryId { get; set; }
        public ProductCategoryModel Category { get; set; }
        public DateTime? CreatedOn { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public int? ModifiedBy { get; set; }
        public List<VendorStockModel> Stocks { get; set; }
    }
    public class ProductCategoryModel
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public string Description { get; set; }
        public DateTime? CreatedOn { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public int? ModifiedBy { get; set; }
    }
    public class VendorStockModel
    {
        public int Id { get; set; }

        public int? VendorId { get; set; }
        public int? ProductId { get; set; }
        public int? Quantity { get; set; } = 0;
        public decimal? TotalPurchasePrice { get; set; }
        public long? StockNumber { get; set; }
        public bool IsActive { get; set; } = true;

    }
    public class PricingModel
    {
        public int Id { get; set; }
        public decimal? UnitPrice { get; set; }// Sell Price
        public string? ProductCode { get; set; }
        public long? StockNumber { get; set; }
        public DateTime? CreatedOn { get; set; } = DateTime.Now;
        public int? CreatedBy { get; set; }

    }
}
