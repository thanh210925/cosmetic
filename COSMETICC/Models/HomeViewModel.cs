using System;
using System.Collections.Generic;

namespace COSMETICC.Models
{
    public class HomeViewModel
    {
        public List<Category> Categories { get; set; } = new();
        public List<Product> Bestsellers { get; set; } = new();
        public List<Product> NewProducts { get; set; } = new();
        public Product? FlashSaleProduct { get; set; }
        public List<Brand> Brands { get; set; } = new();
        public List<BlogPost> BlogPosts { get; set; } = new();
        public List<Review> FeaturedReviews { get; set; } = new();
    }
}
