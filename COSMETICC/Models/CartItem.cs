using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace COSMETICC.Models;

public partial class CartItem
{
    [Key]
    public int Id { get; set; }

    public int CartId { get; set; }

    public int ProductId { get; set; }

    public int? Quantity { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [ForeignKey("CartId")]
    [InverseProperty("CartItems")]
    public virtual Cart Cart { get; set; } = null!;

    [ForeignKey("ProductId")]
    [InverseProperty("CartItems")]
    public virtual Product Product { get; set; } = null!;
}
