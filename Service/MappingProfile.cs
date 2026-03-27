using AutoMapper;
using Common.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Service
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Domain entity → DTO
            CreateMap<Product, ProductModel>();
            CreateMap<ProductCategory, ProductCategoryModel>();
        }
    }
}
