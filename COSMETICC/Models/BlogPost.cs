using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace COSMETICC.Models;

public partial class BlogPost
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(250)]
    public string Title { get; set; } = null!;

    [Required]
    [StringLength(250)]
    public string Slug { get; set; } = null!;

    [StringLength(1000)]
    public string? Summary { get; set; }

    [Required]
    public string Content { get; set; } = null!;

    [StringLength(500)]
    public string? ImageUrl { get; set; }

    [StringLength(250)]
    public string? Tags { get; set; }

    [StringLength(250)]
    public string? MetaTitle { get; set; }

    [StringLength(500)]
    public string? MetaDescription { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public bool IsPublished { get; set; } = true;
}
