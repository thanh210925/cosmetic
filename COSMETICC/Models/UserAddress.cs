using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace COSMETICC.Models;

[Table("UserAddresses")]
public partial class UserAddress
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }

    [Required]
    [StringLength(250)]
    public string ReceiverName { get; set; } = null!;

    [Required]
    [StringLength(50)]
    public string ReceiverPhone { get; set; } = null!;

    [Required]
    [StringLength(500)]
    public string SpecificAddress { get; set; } = null!;

    [Required]
    [StringLength(250)]
    public string City { get; set; } = null!;

    public bool IsDefault { get; set; } = false;

    [ForeignKey("UserId")]
    [InverseProperty("UserAddresses")]
    public virtual User User { get; set; } = null!;
}
