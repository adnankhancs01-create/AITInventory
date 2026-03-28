using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Models.RequestModel
{
    public class ProductRequesModel
    {
        public int? Id { get; set; }
        public int? UserId { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public int CategoryId { get; set; }
    }
    public class ProductCategoryRequesModel
    {
        public int? Id { get; set; }
        public int? UserId { get; set; }
        public string?Name { get; set; }
        public string? Description { get; set; }
    }
}
