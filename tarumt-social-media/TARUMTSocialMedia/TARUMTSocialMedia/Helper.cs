using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Net;
using System.Net.Mail;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text.RegularExpressions;
using TARUMTSocialMedia.Models;

namespace TARUMTSocialMedia;

public class Helper
{
    private readonly IWebHostEnvironment en;
    private readonly IHttpContextAccessor ct;
    private readonly IConfiguration cf;
    private readonly DB db;

    public Helper(IWebHostEnvironment en,
                  IHttpContextAccessor ct,
                  IConfiguration cf,
                  DB db)
    {
        this.en = en;
        this.ct = ct;
        this.cf = cf;
        this.db = db;
    }

    public void SignIn(string email, string role, bool RememberMe)
    {
        List<Claim> claims =
        [
            new(ClaimTypes.Name, email),
            new(ClaimTypes.Role, role),
        ];

        ClaimsIdentity identity = new ClaimsIdentity(claims, "Cookies");

        ClaimsPrincipal principal = new(identity);

        AuthenticationProperties properties = new()
        {
            IsPersistent = RememberMe,
        };

        ct.HttpContext!.SignInAsync(principal, properties);
    }

    public void SignOut()
    {
        ct.HttpContext!.SignOutAsync();
    }

    private PasswordHasher<object> ph = new();

    public string HashPassword(string password)
    {
        return ph.HashPassword(0, password);
    }

    public bool VerifyPassword(string hash, string password)
    {
        return ph.VerifyHashedPassword(0, hash, password) == PasswordVerificationResult.Success;
    }

    public string ValidatePhoto(IFormFile f, int sizelimit = 4, bool GifSupport = false)
    {
        var reType = new Regex(@"^image\/(jpeg|png|gif)$", RegexOptions.IgnoreCase);
        var reName = new Regex(@"^.+\.(jpeg|jpg|png)$", RegexOptions.IgnoreCase);

        if (GifSupport)
        {
            reType = new Regex(@"^image\/(jpeg|png|gif)$", RegexOptions.IgnoreCase);
            reName = new Regex(@"^.+\.(jpeg|jpg|png|gif)$", RegexOptions.IgnoreCase);
        }

        if (!reType.IsMatch(f.ContentType) || !reName.IsMatch(f.FileName))
        {
            return "Only JPG and PNG photo is allowed. (GIF support upon posting a content)";
        }
        else if (f.Length > sizelimit * 1024 * 1024)
        {
            return "Photo size cannot more than 4MB.";
        }

        return "";
    }

    public string SavePhoto(IFormFile f, string folder, int width = 200, int height = 200, bool GIFsupport = false)
    {
        using var stream = f.OpenReadStream();
        var format = SixLabors.ImageSharp.Image.DetectFormat(stream);

        stream.Position = 0;

        string ext = GIFsupport ? ".gif" : ".jpg";

        if (GIFsupport && format is SixLabors.ImageSharp.Formats.Gif.GifFormat)
        {
            ext = ".gif";
        }
        else
        {
            ext = ".jpg";
        }

        var file = Guid.NewGuid().ToString("n") + ext;
        var path = Path.Combine(en.WebRootPath, folder, file);

        var options = new ResizeOptions
        {
            Size = new(width, height),
            Mode = ResizeMode.Crop,
        };

        using var img = Image.Load(stream);
        img.Mutate(x => x.Resize(options));
        if (ext == ".gif")
        {
            img.Save(path, new SixLabors.ImageSharp.Formats.Gif.GifEncoder());
        }
        else
        {
            img.Save(path, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder());
        }

        return file;
    }

    public void DeletePhoto(string file, string folder)
    {
        file = Path.GetFileName(file);
        var path = Path.Combine(en.WebRootPath, "images", folder, file);
        File.Delete(path);
    }

    public string DisplayNickname(string username)
    {
        var u = db.Users.Include(u => u.Detail).FirstOrDefault(u => u.Username == username);
        if (u.Detail.DisplayableNickname == 0)
        {
            return u.Username;
        }
        return u.Detail.Nickname;
    }

    public void Notify(int userID, string message, string ProfileImg, string img, string link)
    {
        db.Notification.Add(new Notification
        {
            UserId = userID,        //Which userID received the notification                           谁会收到通知通过ID
            Message = message,      //Message of the notification                                      通知信息
            UserPhoto = ProfileImg, //Display image of OTHER user, Apply "NULL" for display nothing    显示用户照片，输入"NULL"显示无照片
            Photo = img,            //Display image of content with folder path                        显示内容照片，通过文件路径
            Link = link,            //Navigatable, Apply '#' for unnavigatable.                        按得到链接，输入"#"进不到链接
            Read = 1,               //Start with 1. if user checked the notification, it will set to 0 从一开始，如果用户查看通知，变0
            DateNotificated = DateTime.Now
        });
        db.SaveChanges();
    }

    public bool CheckBan(int userId)
    {
        var CheckBan = db.BanRecord.Any(b => b.UserId == userId && (b.EndDate == null || b.EndDate >= DateTime.Today));

        if (CheckBan)
        {
            return true;
        }
        return false;
    }

    public bool CheckBanUsingEmail(string email)
    {
        var CheckBan = db.BanRecord.Any(b => b.UserId == GetUserID(email) && (b.EndDate == null || b.EndDate >= DateTime.Today));

        if (CheckBan)
        {
            return true;
        }
        return false;
    }

    public int GetUserID(string email)
    {
        var userID = db.Users.FirstOrDefault(u => u.Email == email);
        return userID!.Id;
    }

    public string GetUserRole(string email)
    {
        var userID = db.Users.FirstOrDefault(u => u.Email == email);
        return userID!.Role;
    }

    public string GetUserName(int userId)
    {
        var userID = db.Users.FirstOrDefault(u => u.Id == userId);
        return userID!.Username;
    }

    public int GetBoolNumber(bool option)
    {
        return (option ? 1 : 0);
    }

    public void SendEmail(MailMessage mail)
    {
        string user = cf["Smtp:User"] ?? "";
        string pass = cf["Smtp:Pass"] ?? "";
        string name = cf["Smtp:Name"] ?? "";
        string host = cf["Smtp:Host"] ?? "";
        int port = cf.GetValue<int>("Smtp:Port");

        mail.From = new MailAddress(user, name);

        using var smtp = new SmtpClient
        {
            Host = host,
            Port = port,
            EnableSsl = true,
            Credentials = new NetworkCredential(user, pass),
        };

        smtp.Send(mail);
    }

    public string RandomPassword()
    {
        string s = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        string password = "";

        Random r = new();

        for (int i = 1; i <= 10; i++)
        {
            password += s[r.Next(s.Length)];
        }

        return password;
    }
}
