using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using System.Net.Mail;

namespace TARUMTSocialMedia.Controllers;

public class AccountController : Controller
{
    private readonly DB db;
    private readonly Helper hp;
    private readonly IWebHostEnvironment en;

    public AccountController(DB db, Helper hp, IWebHostEnvironment en)
    {
        this.db = db;
        this.hp = hp;
        this.en = en;
    }

    public IActionResult Logout(string? returnURL)
    {
        TempData["Info"] = "Logout successfully.";
        hp.SignOut();
        return RedirectToAction("Index", "Home");
    }

    public IActionResult Login()
    {
        ViewBag.HideNavBar = true;
        return View();
    }

    [HttpPost]
    public IActionResult Login(LoginVM vm, string? returnURL)
    {
        ViewBag.HideNavBar = true;
        var u = db.Users.FirstOrDefault(u => u.Email == vm.Email);

        if (u == null || !(hp.VerifyPassword(u.Password, vm.Password)))
        {
            ModelState.AddModelError("Password", "Invalid E-mail address or login credientials inmatched");
            return View(vm);
        }

        if (ModelState.IsValid)
        {
            hp.SignIn(u!.Email, u.Role, vm.RememberMe);

            if (u != null && db.BanRecord.Any(b => b.UserId == u.Id && (DateTime.Today <= b.EndDate || b.EndDate == null)))
            {
                return RedirectToAction("Unauthorize", "Home", new { userid = u.Id });
            }

            if (string.IsNullOrEmpty(returnURL))
            {
                return RedirectToAction("Index", "Home");
            }
        }

        return View(vm);
    }

    public IActionResult Register()
    {
        ViewBag.HideNavBar = true;
        return View(); 
    }

    [HttpPost]
    public IActionResult Register(RegisterVM vm)
    {
        ViewBag.HideNavBar = true;
        
        if (ModelState.IsValid("Email") && db.Users.Any(u => u.Email == vm.Email))
        {
            ModelState.AddModelError("Email", "Duplicated Email.");
        }

        if (ModelState.IsValid("Username") && db.Users.Any(u => u.Username == vm.Username))
        {
            ModelState.AddModelError("Username", "The username has been taken.");
        }

        if (vm.Photo != null)
        {
            var err = hp.ValidatePhoto(vm.Photo);
            if (err != "") ModelState.AddModelError("Photo", err);
        }


        if (ModelState.IsValid)
        {
            var PhotoURL = "_null.png";

            if (vm.Photo != null)
            {
                PhotoURL = hp.SavePhoto(vm.Photo, "images/profile");
            }

            db.Users.Add(new()
            {
                Email = vm.Email,
                Username = vm.Username,
                Password = hp.HashPassword(vm.Password),
                Gender = vm.Gender,
                DateJoin = DateTime.Now.Date,
                Role = 0.ToString(),

                PhotoURL = PhotoURL, 

                Detail = new UserDetail
                {
                    FollowerCount = 0,
                    FollowingCount = 0,
                    Bio = "Hello there!",
                    BannerURL = "_nullbanner.png",
                    Nickname = "Unnamed",
                    PostCount = 0,
                }
            });
            db.SaveChanges();

            TempData["Info"] = "Register successfully. Please login.";
            return RedirectToAction("Login");
        }

        return View(vm);
    }

    [Route("Account/Profile/@{username}/{id}")]
    public IActionResult Profile(string username, int id)
    {
        var user = db.Users.Include(u => u.Detail)
                           .FirstOrDefault(u => u.Id == id && u.Username == username);
        if (user == null)
        {
            return NotFound();
        }

        ViewBag.user = user;

        return View(user);
    }

    [HttpPost]
    public IActionResult FollowUser(int targetID)
    {
        var currentUser = db.Users.Include(u => u.Detail).FirstOrDefault(u => u.Email == User.Identity!.Name);
        var targetUser = db.Users.Include(u => u.Detail).FirstOrDefault(u => u.Id == targetID);

        bool validToFollow = true;
        if ((currentUser!.Id == targetID) || (db.Follows.Any(f => f.UserId == currentUser.Id && f.FollowedUserId == targetID)))
        {
            validToFollow = false;
        }

        if (currentUser == null)
            throw new Exception("Current user not found. User.Identity.Name = " + User.Identity?.Name);

        if (targetUser == null)
            throw new Exception("Target user not found with id " + targetID);

        if (validToFollow)
        {
            db.Follows.Add(new Follow
            {
                UserId = currentUser!.Id,
                FollowedUserId = targetUser!.Id,
            });
            targetUser.Detail.FollowerCount += 1;
            currentUser.Detail.FollowingCount += 1;
            db.SaveChanges();

            hp.Notify(targetUser.Id,                                    //UserID
                "@" + currentUser.Username + " started following you!", //Message
                currentUser.PhotoURL,                                   //Show Profile User
                "profile/" + targetUser.PhotoURL,                       //Show current user
                "/Account/Profile/@" + currentUser.Username + "/" + currentUser.Id); //link
        }
        if (Request.IsAjax())
        {
            return PartialView("_FollowUserBox", targetID);
        }
        return Redirect(Request.Headers.Referer.ToString());
    }

    [HttpPost]
    public IActionResult unfollowUser(int targetID)
    {
        var currentUser = db.Users.Include(u => u.Detail).FirstOrDefault(u => u.Email == User.Identity!.Name);
        var targetUser = db.Users.Include(u => u.Detail).FirstOrDefault(u => u.Id == targetID);

        var follow = db.Follows.FirstOrDefault(f => f.UserId == currentUser.Id && f.FollowedUserId == targetUser.Id);

        if (follow != null)
        {
            db.Follows.Remove(follow);

            if (currentUser.Detail != null && currentUser.Detail.FollowingCount > 0)
            {
                currentUser.Detail.FollowingCount -= 1;
            }

            if (targetUser.Detail != null && targetUser.Detail.FollowerCount > 0)
            {
                targetUser.Detail.FollowerCount -= 1;
            }

            db.SaveChanges();
        }
        if (Request.IsAjax())
        {
            return PartialView("_FollowUserBox", targetID);
        }

        return Redirect(Request.Headers.Referer.ToString());

    }

    public IActionResult GetFollowing(int userID)
    {
        var followingIds = db.Follows.Where(f => f.UserId == userID).Select(f => f.FollowedUserId).ToList();
        var followings = db.Users.Where(u => followingIds.Contains(u.Id)).ToList();

        return PartialView("_LoadUser", followings);
    }

    public IActionResult GetFollower(int userID)
    {
        var followerIds = db.Follows.Where(f => f.FollowedUserId == userID).Select(f => f.UserId).ToList();
        var followers = db.Users.Where(u => followerIds.Contains(u.Id)).ToList();

        return PartialView("_LoadUser", followers);
    }

    public IActionResult GetLike(int postID)
    {
        var like = db.Like.Where(l => l.PostId == postID).Select(l => l.UserId).ToList();
        var liker = db.Users.Where(u => like.Contains(u.Id)).ToList();

        //hp.Notify(hp.GetUserID(User.Identity.Name), postID.ToString(), "", "", ""); //DEBUG
         
        return PartialView("_LoadUser", liker);
    }

    [Authorize]
    public IActionResult Edit()
    {
        bool checkDisplayableusername = false;

        var u = db.Users.Include(x => x.Detail).FirstOrDefault(x => x.Email == User.Identity!.Name);

        if (u.Detail.DisplayableNickname == 1) checkDisplayableusername = true;

        var vm = new EditVM
        {
            Username = u.Username,
            Nickname = u.Detail.Nickname,
            DisplayableNickname = checkDisplayableusername,
            BannerURL = u.Detail.BannerURL,
            PhotoURL = u.PhotoURL,
            Bio = u.Detail.Bio,
            Gender = u.Gender
        };
        return View(vm);
    }

    [HttpPost]
    public IActionResult Edit(EditVM vm)
    {
        var u = db.Users
                  .Include(x => x.Detail)
                  .FirstOrDefault(x => x.Email == User.Identity!.Name);

        vm.BannerURL = u.Detail.BannerURL;
        vm.PhotoURL = u.PhotoURL;

        if (u == null)
        {
            return NotFound();
        }

        if (db.Users.Any(x => x.Email == vm.Email && x.Id != u.Id))
        {
            ModelState.AddModelError("Email", "Duplicated Email.");
        }
        
        if (db.Users.Any(x => x.Username == vm.Username && x.Id != u.Id))
        {
            ModelState.AddModelError("Username", "The username has been taken.");
        }

        if (!ModelState.IsValid)
        {
            return View(vm);
        }


        if (!string.IsNullOrEmpty(vm.Username))
        {
            u.Username = vm.Username;
        }

        if (!string.IsNullOrEmpty(vm.Nickname))
        {
            u.Detail.Nickname = vm.Nickname;
        }

        if (vm.DisplayableNickname)
        {
            u.Detail.DisplayableNickname = 1;
        }
        else
        {
            u.Detail.DisplayableNickname = 0;
        }

        if (!string.IsNullOrEmpty(vm.Bio))
        {
            u.Detail.Bio = vm.Bio;
        }

        u.Gender = vm.Gender;


        if (vm.Banner != null && hp.ValidatePhoto(vm.Banner) == "")
        {
            if(vm.BannerURL != "_nullbanner.png")
            {
                hp.DeletePhoto(vm.BannerURL, "banner");
            }
            u.Detail.BannerURL = hp.SavePhoto(vm.Banner, "images/banner", 1000, 300);
        }

        if (vm.Photo != null && hp.ValidatePhoto(vm.Photo) == "")
        {
            if (vm.PhotoURL != "_null.png")
            {
                hp.DeletePhoto(vm.PhotoURL, "profile");
            }
            u.PhotoURL = hp.SavePhoto(vm.Photo, "images/profile", 200, 200);
        }


        if (!string.IsNullOrWhiteSpace(vm.Password))
        {
            if (vm.Password == vm.Confirm)
            {
                u.Password = hp.HashPassword(vm.Password);
            }
            else
            {
                ModelState.AddModelError("Confirm", "Password and Confirm Password don't match");
                return View(vm);
            }
        }

        db.SaveChanges();

        return Redirect(Request.Headers.Referer.ToString());

    }

    public bool CheckEmail(string email)
    {
        int id = hp.GetUserID(User.Identity!.Name!);
        return !db.Users.Any(u => u.Email == email && u.Id != id);
    }

    public bool CheckUsername(string username)
    {
        int id = hp.GetUserID(User.Identity!.Name!);
        return !db.Users.Any(u => u.Username == username && u.Id != id);
    }

    

    //[REMOVE THIS BEFORE HOSTING]
    public IActionResult SwitchToAdmin()
    {
        var u = db.Users.FirstOrDefault(x => x.Email == User.Identity!.Name);
        u!.Role = "1";
        db.SaveChanges();

        return Redirect(Request.Headers.Referer.ToString());
    }

    public IActionResult ResetPassword()
    {
        ViewBag.HideNavBar = true;
        return View();
    }


    //POST: Account/ResetPassword
    [HttpPost]
    public IActionResult ResetPassword(ResetPasswordVM vm)
    {
        ViewBag.HideNavBar = true;

        var u = db.Users?.FirstOrDefault(x => x.Email == vm.Email);

        if (u == null)
        {
            ModelState.AddModelError("Email", "Email not found.");
        }

        if (ModelState.IsValid)
        {
            string password = hp.RandomPassword();

            u!.Password = hp.HashPassword(password);
            db.SaveChanges();

            // Send reset password email
            SendResetPasswordEmail(u, password);

            TempData["Info"] = $"Password reset. Check your email.";
            return RedirectToAction();
        }

        return View();
    }

    private void SendResetPasswordEmail(User u, string password)
    {
        ViewBag.HideNavBar = true;

        var mail = new MailMessage();
        mail.To.Add(new MailAddress(u.Email, u.Username));
        mail.Subject = "Reset Password";
        mail.IsBodyHtml = true;

        // TODO
        var url = Url.Action("Login", "Account", null, "https");

        // TODO
        var path = Path.Combine(en.WebRootPath, "images", "profile", u.PhotoURL);


        var att = new Attachment(path);
        mail.Attachments.Add(att);
        att.ContentId = "photo";
        // TODO

        mail.Body = $@"
            <img src='cid:photo' style='width: 200px; height: 200px;
                                        border: 1px solid #333'>
            <p>Dear {u.Username},<p>
            <p>Your password has been reset to:</p>
            <h1 style='color: red'>{password}</h1>
            <p>
                Please <a href='{url}'>login</a>
                with your new password.
            </p>
            <p>From, 🐱 Super Admin</p>
        ";

        hp.SendEmail(mail);
    }


    public IActionResult Email()
    {
        ViewBag.HideNavBar = true;
        return View();
    }

    [HttpPost]
    public IActionResult Email(EmailVM vm)
    {
        ViewBag.HideNavBar = true;

        if (ModelState.IsValid)
        {
            // Construct email
            var mail = new MailMessage();
            mail.To.Add(new MailAddress(vm.Email, "My Lovely"));
            mail.Subject = vm.Subject;
            mail.Body = vm.Body;
            mail.IsBodyHtml = vm.IsBodyHtml;

            // File attachment (optional)
            var path = Path.Combine(en.ContentRootPath, "Secret.pdf");
            var att = new Attachment(path);
            mail.Attachments.Add(att);

            // Send email
            hp.SendEmail(mail);

            TempData["Info"] = "Email sent.";
            return RedirectToAction();
        }

        return View(vm);
    }
}
