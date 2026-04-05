using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;
using System.Text;

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
        public VendorStockModel Stock { get; set; }
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
        public int ProductId { get; set; }

        public int? Quantity { get; set; } = 0;

        public decimal? UnitPrice { get; set; }
        public decimal? SellPrice { get; set; }
    }
}
