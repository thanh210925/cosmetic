using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace COSMETICC.Models;

[Table("OrderDiscount")]
public partial class OrderDiscount
{
    [Key]
    public int Id { get; set; }

    public int? OrderId { get; set; }

    public int? DiscountId { get; set; }

    [ForeignKey("DiscountId")]
    [InverseProperty("OrderDiscounts")]
    public virtual Discount? Discount { get; set; }

    [ForeignKey("OrderId")]
    [InverseProperty("OrderDiscounts")]
    public virtual Order? Order { get; set; }
}
