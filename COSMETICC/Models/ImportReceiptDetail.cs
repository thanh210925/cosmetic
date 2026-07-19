using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace COSMETICC.Models
{
    [Table("ImportReceiptDetails")]
    public partial class ImportReceiptDetail
    {
        [Key]
        public int Id { get; set; }

        public int ReceiptId { get; set; }

        public int ProductId { get; set; }

        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal UnitPrice { get; set; }

        [Required]
        [StringLength(100)]
        public string BatchNumber { get; set; } = null!;

        [Column(TypeName = "datetime")]
        public DateTime ExpiryDate { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime? ManufactureDate { get; set; }

        [StringLength(500)]
        public string? Note { get; set; }

        [ForeignKey("ReceiptId")]
        public virtual ImportReceipt ImportReceipt { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }
    }
}
