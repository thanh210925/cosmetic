using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace COSMETICC.Models;

[Table("Shipping")]
public partial class Shipping
{
    [Key]
    public int Id { get; set; }

    public int OrderId { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    [StringLength(20)]
    public string? Phone { get; set; }

    [StringLength(50)]
    public string? ShippingStatus { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? ShippingDate { get; set; }

    [ForeignKey("OrderId")]
    [InverseProperty("Shippings")]
    public virtual Order Order { get; set; } = null!;
}
