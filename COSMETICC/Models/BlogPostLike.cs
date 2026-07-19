using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace COSMETICC.Models
{
    [Table("BlogPostLikes")]
    public partial class BlogPostLike
    {
        [Key]
        public int Id { get; set; }

        public int BlogPostId { get; set; }

        public int UserId { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("BlogPostId")]
        [InverseProperty("BlogPostLikes")]
        public virtual BlogPost? BlogPost { get; set; }

        [ForeignKey("UserId")]
        [InverseProperty("BlogPostLikes")]
        public virtual User? User { get; set; }
    }
}
