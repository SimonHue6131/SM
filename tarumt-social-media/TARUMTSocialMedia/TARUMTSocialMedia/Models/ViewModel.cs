using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace TARUMTSocialMedia.Models;

#nullable disable warnings

public class RegisterVM
{
    [StringLength(100)]
    [EmailAddress]
    //[Remote("CheckEmail", "Account", ErrorMessage = "Duplicated {0}.")]
    [Display(Name = "Email Address")]
    public string Email { get; set; }

    [StringLength(100)]
    [Display(Name = "Username")]
    //[Remote("CheckUsername", "Account", ErrorMessage = "Duplicated {0}.")]
    public string Username { get; set; }

    [StringLength(100, MinimumLength = 5)]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; }

    [StringLength(100, MinimumLength = 5)]
    [Compare("Password")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    public string Confirm { get; set; }

    [Display(Name = "Gender")]
    public string Gender { get; set; }

    [Display(Name = "Profile Photo")]
    public IFormFile? Photo { get; set; }
}

public class LoginVM
{
    [StringLength(100)]
    [EmailAddress]
    public string Email {get; set; }

    [StringLength (100, MinimumLength = 5)]
    [DataType(DataType.Password)] 
    public string Password { get; set; }

    public bool RememberMe { get; set; }
}

public class EditVM
{
    [StringLength(100)]
    [EmailAddress]
    [Remote("CheckEmail", "Account", ErrorMessage = "Duplicated {0}.")]
    [Display(Name = "Email Address")]
    public string? Email { get; set; }
    
    [StringLength(100)]
    [Display(Name = "Username")]
    [Remote("CheckUsername", "Account", ErrorMessage = "Duplicated {0}.")]
    public string? Username { get; set; }

    [StringLength(100)]
    [Display(Name = "Nickname")]
    public string? Nickname { get; set; }

    [Display(Name = " ")]
    public bool DisplayableNickname { get; set; }

    [StringLength(255)]
    [Display(Name = "Bio")]
    public string? Bio { get; set; }

    [Display(Name = "Gender")]
    public string? Gender { get; set; }

    [StringLength(100, MinimumLength = 5)]
    [DataType(DataType.Password)]
    [Display(Name = "New Password")]
    public string? Password { get; set; }

    [StringLength(100, MinimumLength = 5)]
    [Compare("Password")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    public string? Confirm { get; set; }

    public IFormFile? Photo { get; set; }
    public IFormFile? Banner { get; set; }

    public string? BannerURL { get; set; }
    public string? PhotoURL { get; set; }
}

public class CreatePostVM
{
    public string? Info { get; set; }
    public IFormFile[] Imgs { get; set; }

    public bool Anomymous { get; set; }
}

public class NotifyForAdmin
{
    [Display(Name = "User's ID")]
    public int UserID { get; set; }

    [Display(Name = "Title")]
    public string Title { get; set; }

    [StringLength(100)]
    [Display(Name = "Profile's Image")]
    public string? ProfileImage { get; set; }

    [StringLength(100)]
    [Display(Name = "Content Image")]
    public string ContentImage { get; set; }

    [Display(Name = "Link")]
    public string? Link { get; set; }
 }

public class ResetPasswordVM
{
    [StringLength(100)]
    [EmailAddress]
    public string Email { get; set; }
}

public class EmailVM
{
    [StringLength(100)]
    [EmailAddress]
    public string Email { get; set; }

    public string Subject { get; set; }

    public string Body { get; set; }

    public bool IsBodyHtml { get; set; }
}

public class PasswordReseterVM
{
    [StringLength(100)]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; }

    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 5)]
    [Display(Name = "Password")]
    public string Password { get; set; }
 
}

public class ReportVM
{
    public string ContentType { get; set; }
    public int ContentID { get; set; }

    [Display(Name = "Reason to report")]
    public string Reason { get; set; }

    public string ContentString { get; set; }

    [StringLength(100)]
    [Display(Name = "Additional Information")]
    public string? Info { get; set; }

    public int UserID { get; set; }
    public int TargetUserID { get; set; }
}