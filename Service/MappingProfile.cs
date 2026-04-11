using AutoMapper;
using Common.Models;
using Common.Models.RequestModel;
using Domain.Entities;
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
            CreateMap<Product, ProductRequesModel>()
            .ForMember(dest => dest.UserId,
               opt => opt.MapFrom(src => src.CreatedBy))
            .ReverseMap(); 
            CreateMap<ProductCategory, ProductCategoryRequesModel>()
            .ForMember(dest => dest.UserId,
               opt => opt.MapFrom(src => src.CreatedBy))
            .ReverseMap();
            CreateMap<ProductCategory, ProductCategoryModel>();
            CreateMap<VendorClientDetail, ClientModel>();
            CreateMap<VendorStock, VendorStockModel>();
            CreateMap<Pricing, PricingModel>();
            CreateMap<VendorTransaction, VendorTransactionModel>();
        }
    }
}
