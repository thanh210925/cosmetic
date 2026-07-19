using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace COSMETICC.Models;

public partial class CollectedVoucher
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }

    public int DiscountId { get; set; }

    public DateTime CollectedAt { get; set; } = DateTime.Now;

    public bool IsUsed { get; set; } = false;

    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    [ForeignKey("DiscountId")]
    public virtual Discount Discount { get; set; } = null!;
}
