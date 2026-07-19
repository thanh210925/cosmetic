using System;
using System.ComponentModel.DataAnnotations;

namespace COSMETICC.Models;

public partial class ChatMessage
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string Sender { get; set; } = null!; // "Customer" or "Staff"

    [Required]
    public string Message { get; set; } = null!;

    public DateTime Timestamp { get; set; } = DateTime.Now;

    public int? UserId { get; set; }

    [StringLength(100)]
    public string ConnectionId { get; set; } = null!;
}
