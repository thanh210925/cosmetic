using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace COSMETICC.Models
{
    public class ReturnRequest
    {
        [Key]
        public int Id { get; set; }

        public int OrderId { get; set; }
        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }

        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [Required]
        [StringLength(1000)]
        public string Reason { get; set; } = null!;

        [StringLength(500)]
        public string? ImageUrl { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal RefundAmount { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected, Refunded

        [StringLength(500)]
        public string? AdminNote { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column(TypeName = "datetime")]
        public DateTime? ProcessedAt { get; set; }
    }
}
