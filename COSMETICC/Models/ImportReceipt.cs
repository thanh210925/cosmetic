using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace COSMETICC.Models
{
    [Table("ImportReceipts")]
    public partial class ImportReceipt
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string ReceiptCode { get; set; } = null!;

        public int SupplierId { get; set; }

        public int? AdminId { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime ImportDate { get; set; } = DateTime.Now;

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Draft"; // Draft, Confirmed, Cancelled

        [Column(TypeName = "decimal(18, 2)")]
        public decimal SubTotal { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal Discount { get; set; } = 0;

        [Column(TypeName = "decimal(18, 2)")]
        public decimal ShippingFee { get; set; } = 0;

        [Column(TypeName = "decimal(18, 2)")]
        public decimal Tax { get; set; } = 0;

        [Column(TypeName = "decimal(18, 2)")]
        public decimal TotalAmount { get; set; }

        [StringLength(500)]
        public string? Note { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("SupplierId")]
        public virtual Supplier Supplier { get; set; }

        [ForeignKey("AdminId")]
        public virtual Admin? Admin { get; set; }

        public virtual ICollection<ImportReceiptDetail> ImportReceiptDetails { get; set; } = new List<ImportReceiptDetail>();
    }
}
