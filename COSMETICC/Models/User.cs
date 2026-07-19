using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace COSMETICC.Models;

[Index("Username", Name = "UQ__Users__536C85E47BC6478C", IsUnique = true)]
public partial class User
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

    [StringLength(20)]
    public string? Phone { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? CreatedAt { get; set; }

    public bool IsLocked { get; set; } = false;

    public int Points { get; set; } = 0;

    [Column(TypeName = "date")]
    public DateTime? BirthDate { get; set; }

    [NotMapped]
    public string MembershipTier
    {
        get
        {
            if (Points >= 10000) return "Platinum";
            if (Points >= 5000) return "Gold";
            if (Points >= 1000) return "Silver";
            return "Bronze";
        }
    }

    [StringLength(255)]
    public string? GoogleId { get; set; }

    [StringLength(255)]
    public string? FacebookId { get; set; }

    [StringLength(500)]
    public string? Avatar { get; set; }

    [StringLength(20)]
    public string? Gender { get; set; }

    [StringLength(100)]
    public string? SkinType { get; set; }

    [InverseProperty("User")]
    public virtual ICollection<UserAddress> UserAddresses { get; set; } = new List<UserAddress>();

    [InverseProperty("User")]
    public virtual ICollection<Cart> Carts { get; set; } = new List<Cart>();

    [InverseProperty("User")]
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    [InverseProperty("User")]
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    [InverseProperty("User")]
    public virtual ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();

    [InverseProperty("User")]
    public virtual ICollection<BlogPost> BlogPosts { get; set; } = new List<BlogPost>();

    [InverseProperty("User")]
    public virtual ICollection<BlogPostLike> BlogPostLikes { get; set; } = new List<BlogPostLike>();

    [InverseProperty("User")]
    public virtual ICollection<BlogPostComment> BlogPostComments { get; set; } = new List<BlogPostComment>();
}
