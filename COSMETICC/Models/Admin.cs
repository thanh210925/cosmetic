using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace COSMETICC.Models;

[Index("Username", Name = "UQ__Admins__536C85E4BCA247FA", IsUnique = true)]
public partial class Admin
{
    [Key]
    public int Id { get; set; }

    [StringLength(100)]
    public string Username { get; set; } = null!;

    [StringLength(255)]
    public string Password { get; set; } = null!;

    [StringLength(255)]
    public string? FullName { get; set; }

    [StringLength(255)]
    public string? Email { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? CreatedAt { get; set; }

    [InverseProperty("Admin")]
    public virtual ICollection<AdminLog> AdminLogs { get; set; } = new List<AdminLog>();

    [InverseProperty("CreatedByAdmin")]
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
