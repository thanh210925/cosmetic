using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace COSMETICC.Models
{
    [Table("ProductVariants")]
    public partial class ProductVariant
    {
        [Key]
        public int Id { get; set; }

        public int ProductId { get; set; }

        [Required(ErrorMessage = "Tên thuộc tính không được để trống (VD: Màu sắc, Dung tích)")]
        [StringLength(100)]
        public string Name { get; set; } = null!;

        [Required(ErrorMessage = "Giá trị thuộc tính không được để trống (VD: Đỏ, 50ml)")]
        [StringLength(100)]
        public string Value { get; set; } = null!;

        [Column(TypeName = "decimal(10, 2)")]
        public decimal PriceAdjustment { get; set; } = 0;

        public int Stock { get; set; } = 0;

        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }
    }
}
