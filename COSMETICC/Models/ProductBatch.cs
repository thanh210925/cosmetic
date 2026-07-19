using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace COSMETICC.Models;

public partial class ProductBatch
{
    [Key]
    public int Id { get; set; }

    public int ProductId { get; set; }

    public int? ImportReceiptDetailId { get; set; }

    [Required]
    [StringLength(100)]
    public string BatchNumber { get; set; } = null!;

    public int ImportQuantity { get; set; }

    public int Quantity { get; set; }

    public int RemainingQuantity { get; set; }

    public DateTime ImportDate { get; set; } = DateTime.Now;

    public DateTime? ManufactureDate { get; set; }

    public DateTime ExpiryDate { get; set; }

    [StringLength(100)]
    public string? Location { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    [ForeignKey("ProductId")]
    public virtual Product Product { get; set; } = null!;

    [ForeignKey("ImportReceiptDetailId")]
    public virtual ImportReceiptDetail? ImportReceiptDetail { get; set; }
}
