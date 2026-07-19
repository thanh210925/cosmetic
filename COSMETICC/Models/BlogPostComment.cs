using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace COSMETICC.Models
{
    [Table("BlogPostComments")]
    public partial class BlogPostComment
    {
        [Key]
        public int Id { get; set; }

        public int BlogPostId { get; set; }

        public int UserId { get; set; }

        [Required]
        [StringLength(1000)]
        public string Content { get; set; } = null!;

        [Column(TypeName = "datetime")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("BlogPostId")]
        [InverseProperty("BlogPostComments")]
        public virtual BlogPost? BlogPost { get; set; }

        [ForeignKey("UserId")]
        [InverseProperty("BlogPostComments")]
        public virtual User? User { get; set; }
    }
}
