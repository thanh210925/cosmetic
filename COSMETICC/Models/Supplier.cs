using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace COSMETICC.Models
{
    [Table("Suppliers")]
    public partial class Supplier
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên nhà cung cấp không được để trống")]
        [StringLength(255)]
        public string Name { get; set; } = null!;

        [StringLength(255)]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        public string? Email { get; set; }

        [StringLength(50)]
        [Phone(ErrorMessage = "Số điện thoại không đúng định dạng")]
        public string? Phone { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(50)]
        public string? TaxCode { get; set; }

        public string? ContractDetails { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
