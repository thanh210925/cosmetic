using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace COSMETICC.Models
{
    public class FlashSaleItem
    {
        [Key]
        public int Id { get; set; }

        public int FlashSaleId { get; set; }

        public int ProductId { get; set; }

        [Column(TypeName = "decimal(10, 2)")]
        public decimal DiscountPrice { get; set; }

        public int QuantityForSale { get; set; }

        public int SoldQuantity { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        [ForeignKey("FlashSaleId")]
        [InverseProperty("FlashSaleItems")]
        public virtual FlashSale? FlashSale { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }
    }
}
