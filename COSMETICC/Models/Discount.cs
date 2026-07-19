using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace COSMETICC.Models;

[Index("Code", Name = "UQ__Discount__A25C5AA709384166", IsUnique = true)]
public partial class Discount
{
    [Key]
    public int Id { get; set; }

    [StringLength(50)]
    public string? Code { get; set; }

    public int? Percentage { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? ExpiryDate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? StartDate { get; set; }

    public int? UsageLimit { get; set; }

    public int UsedCount { get; set; } = 0;

    [InverseProperty("Discount")]
    public virtual ICollection<OrderDiscount> OrderDiscounts { get; set; } = new List<OrderDiscount>();
}
