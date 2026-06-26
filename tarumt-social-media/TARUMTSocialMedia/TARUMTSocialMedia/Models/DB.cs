using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace TARUMTSocialMedia.Models;

public class DB : DbContext
{
    public DB(DbContextOptions options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<UserDetail> UserDetail { get; set; }
    public DbSet<Follow> Follows { get; set; }
    public DbSet<Like> Like { get; set; }
    public DbSet<Post> Post { get; set; }
    public DbSet<Notification> Notification{ get; set; }
    public DbSet<BanRecord> BanRecord{ get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<Report> Report { get; set; }
}

#nullable disable warnings

public class User
{
    [Key]
    public int Id { get; set; }

    [MaxLength(100)]
    [EmailAddress]
    public string Email { get; set; }

    [MaxLength(100)]
    public string Username { get; set; }

    [MaxLength(100)]
    public string Password { get; set; }

    [MaxLength(1)]
    public string Gender { get; set; }

    [Column(TypeName = "Date")]
    public DateTime DateJoin { get; set; }

    public string Role { get; set; }
    //0: Member role
    //1: Admin role

    public string PhotoURL { get; set; }

    //Navigation Property
    public virtual UserDetail Detail { get; set; }
    public virtual ICollection<Post> Post { get; set; }
    public virtual ICollection<BanRecord> BanRecords { get; set; }
}

public class UserDetail
{
    [Key]
    public int UserId { get; set; }

    public int FollowerCount { get; set; } = 0;
    public int FollowingCount { get; set; } = 0;
    public string Bio { get; set; }
    public string BannerURL { get; set; }
    public string Nickname { get; set;  } //Display Name
    public int DisplayableNickname { get; set; }
    public int PostCount { get; set; }

    //Back reference
    public virtual User User { get; set; }
}

public class BanRecord
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set;}

    public string Reason { get; set; }
    public DateTime StartDate { get; set; } = DateTime.Now; 
    public DateTime? EndDate { get; set; } 

    public virtual User User { get; set; }
}

public class Follow
{
    [Key]
    public int Id { get; set; }
    //Follow里的ID不重要，不要管

    public int UserId { get; set; }
    public int? FollowedUserId { get; set; }

    [ForeignKey("UserId")]
    public User User { get; set; }

    [ForeignKey("FollowedUserId")]
    public User FollowedUser { get; set; }
}

public class Like
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }
    public int? PostId { get; set; }

    [ForeignKey("UserId")]
    public User User { get; set; }

    [ForeignKey("PostId")]
    public Post Post { get; set; }
}

public class Notification
{
    [Key]
    public int Id { get; set; }

    public int? UserId { get; set; }
    public string Message { get; set; }
    public string UserPhoto {  get; set; }
    public string Photo { get; set; }
    public string Link { get; set; }
    public int Read { get; set; } = 1;

    [Column(TypeName = "DATETIME")]
    public DateTime DateNotificated { get; set; }

    [ForeignKey("UserId")]
    public User User { get; set; }
}

public class Post
{
    [Key]
    public int Id { get; set; }
    public string? info { get; set; }
    //public string?[] Img { get; set; } = new string?[4];    
    public string? img1 { get; set; }
    public string? img2 { get; set; }
    public string? img3 { get; set; }
    public string? img4 { get; set; }
    public int TextOnly { get; set; }
    public int MediaOnly { get; set; }
    public int Anomymous { get; set; }

    [Column(TypeName = "Date")]
    public DateTime DatePost { get; set; } = DateTime.Now;

    public int LikeCount { get; set; } = 0;
    public int CommentCount { get; set; } = 0;

    public virtual User User { get; set; }

}

public class Comment
{
    [Key]
    public int Id { get; set; }
    public string Content { get; set; }
    public DateTime DateComment { get; set; } = DateTime.Now;

    public int UserId { get; set; }
    public int? PostId { get; set; }

    [ForeignKey("UserId")]
    public User User { get; set; }

    [ForeignKey("PostId")]
    public Post Post { get; set; }
}

public class Report
{
    [Key]
    public int Id { get; set; }

    public string Reason { get; set; } //Report category
    public string? Info { get; set; } //Additional content
    public DateTime DateReport { get; set; } = DateTime.Now;

    public int ReportedBy { get; set; } //Who reported the content
    
    public int ReportedOn { get; set; } //The content originally made by who

    public string ContentType { get; set; } //Type of content (Post or Comment)

    public string ContentString { get; set; } //Content

    public int ContentID { get; set; } //ContentID (CommentID or PostID)
}