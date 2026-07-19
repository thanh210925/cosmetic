using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace COSMETICC.Models;

public class Notification
{
    [Key]
    public int Id { get; set; }

    public int? UserId { get; set; }

    [Required, StringLength(255)]
    public string Title { get; set; } = null!;

    [Required, StringLength(1000)]
    public string Message { get; set; } = null!;

    [StringLength(50)]
    public string? Type { get; set; } // order, payment, system, promo

    public bool IsRead { get; set; } = false;

    [Column(TypeName = "datetime")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [StringLength(500)]
    public string? Link { get; set; }

    [ForeignKey("UserId")]
    public virtual User? User { get; set; }
}
