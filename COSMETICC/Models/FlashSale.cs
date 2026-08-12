using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace COSMETICC.Models
{
    public class FlashSale
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string Title { get; set; } = null!;

        [Column(TypeName = "datetime")]
        public DateTime StartTime { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime EndTime { get; set; }

        public bool IsActive { get; set; } = true;

        [Column(TypeName = "datetime")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual ICollection<FlashSaleItem> FlashSaleItems { get; set; } = new List<FlashSaleItem>();
    }
}
