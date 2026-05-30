using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace COSMETICC.Models;

public partial class AdminLog
{
    [Key]
    public int Id { get; set; }

    public int? AdminId { get; set; }

    [StringLength(255)]
    public string? Action { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? CreatedAt { get; set; }

    [ForeignKey("AdminId")]
    [InverseProperty("AdminLogs")]
    public virtual Admin? Admin { get; set; }
}
