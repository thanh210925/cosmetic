using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace COSMETICC.Models;

public partial class ShippingFee
{
    [Key]
    public int Id { get; set; }

    [StringLength(255)]
    public string Region { get; set; } = null!;

    [Column(TypeName = "decimal(10, 2)")]
    public decimal Fee { get; set; }

    [Column(TypeName = "decimal(10, 2)")]
    public decimal? MinAmountForFreeShipping { get; set; }
}
