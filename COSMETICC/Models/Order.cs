using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace COSMETICC.Models;

public partial class Order
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? OrderDate { get; set; }

    [Column(TypeName = "decimal(10, 2)")]
    public decimal? TotalAmount { get; set; }

    [StringLength(50)]
    public string? Status { get; set; }

    [StringLength(30)]
    public string? OrderCode { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? CancelledAt { get; set; }

    [InverseProperty("Order")]
    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    [InverseProperty("Order")]
    public virtual ICollection<OrderDiscount> OrderDiscounts { get; set; } = new List<OrderDiscount>();

    [InverseProperty("Order")]
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    [InverseProperty("Order")]
    public virtual ICollection<Shipping> Shippings { get; set; } = new List<Shipping>();

    [ForeignKey("UserId")]
    [InverseProperty("Orders")]
    public virtual User User { get; set; } = null!;
}
