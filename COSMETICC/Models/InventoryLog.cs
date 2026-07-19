using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace COSMETICC.Models;

public partial class InventoryLog
{
    [Key]
    public int Id { get; set; }

    public int ProductId { get; set; }

    [StringLength(50)]
    public string Type { get; set; } = null!; // 'NHAP', 'XUAT'

    public int Quantity { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [ForeignKey("ProductId")]
    public virtual Product Product { get; set; } = null!;
}
