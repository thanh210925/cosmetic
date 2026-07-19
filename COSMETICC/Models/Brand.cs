using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace COSMETICC.Models;

public partial class Brand
{
    [Key]
    public int Id { get; set; }

    [StringLength(255)]
    public string Name { get; set; } = null!;

    [StringLength(500)]
    public string? LogoUrl { get; set; }

    [StringLength(100)]
    public string? Country { get; set; }

    [StringLength(255)]
    public string? Website { get; set; }

    [StringLength(500)]
    public string? BannerUrl { get; set; }

    [InverseProperty("Brand")]
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
