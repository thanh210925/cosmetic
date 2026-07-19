using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace COSMETICC.Models;

public partial class Banner
{
    [Key]
    public int Id { get; set; }

    [StringLength(255)]
    public string? Title { get; set; }

    [StringLength(500)]
    public string ImageUrl { get; set; } = null!;

    [StringLength(500)]
    public string? LinkUrl { get; set; }

    [StringLength(50)]
    public string Type { get; set; } = null!; // 'Home', 'FlashSale', 'Brand'

    public bool IsActive { get; set; } = true;
}
