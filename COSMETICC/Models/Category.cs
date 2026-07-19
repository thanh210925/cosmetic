using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace COSMETICC.Models;

public partial class Category
{
    [Key]
    public int Id { get; set; }

    [StringLength(255)]
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public int? ParentCategoryId { get; set; }

    [ForeignKey("ParentCategoryId")]
    public virtual Category? ParentCategory { get; set; }

    public virtual ICollection<Category> ChildCategories { get; set; } = new List<Category>();

    [InverseProperty("Category")]
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
