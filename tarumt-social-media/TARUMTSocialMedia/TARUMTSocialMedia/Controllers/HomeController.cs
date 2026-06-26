using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System.Net.Mail;
using TARUMTSocialMedia.Models;

namespace TARUMTSocialMedia.Controllers;

public class HomeController : Controller
{
    private readonly DB db;
    private readonly Helper hp;
    private readonly IWebHostEnvironment en;

    public HomeController(DB db, Helper hp, IWebHostEnvironment en)
    {
        this.db = db;
        this.hp = hp;
        this.en = en;
    }

    [Authorize]
    public IActionResult Index()
    {
        var u = db.Users.FirstOrDefault(x => x.Email == User.Identity!.Name);

        if(u == null)
        {
            return RedirectToAction("Login", "Account");
        }

        if (hp.CheckBan(u.Id))
        {
            return RedirectToAction("Unauthorize", "Home", new { userid = u.Id });
        }

        var posts = db.Post.Include(p => p.User).OrderByDescending(x => x.Id).Take(50).ToList();

        return View(posts);
    }

    public IActionResult Demo1()
    {
        return View();
    }

    [Authorize]
    [HttpGet]
    public IActionResult Searching(string Searchbox)
    {
        ViewBag.searchDraft = Searchbox;
        ViewBag.up = db.Post.Include(p => p.User).Where(p => p.info.Contains(Searchbox)).ToList();
        //ViewBag.uc = db.Users.Include(u => u.Detail).Where(u => u.Username.Contains(Searchbox)).ToList(); 

        return View();
    }

    [Authorize]
    [HttpGet]
    public IActionResult Unauthorize(int userid)
    {
        var ban = db.BanRecord.Where(b => b.UserId == userid && (b.EndDate == null || b.EndDate >= DateTime.Today))
                            .OrderByDescending(b => b.StartDate)
                            .FirstOrDefault();

        var currentUser = db.Users.FirstOrDefault(u => u.Email == User.Identity!.Name);

        if (ban.UserId != currentUser!.Id)
        {
            return RedirectToAction("Index");
        }

        ViewBag.HideNavBar = true;
        return View(ban);
    }

    [Authorize]
    public IActionResult Notification()
    {
        var u = db.Users.FirstOrDefault(x => x.Email == User.Identity!.Name);
        var noti = db.Notification.Where(n => n.UserId == u.Id).ToList();

        if (Request.IsAjax())
        {
            db.Notification.RemoveRange(noti);
            db.SaveChanges();

            return PartialView("_NoNotification");
        }

        foreach (var n in noti)
        {
            n.Read = 0;
        }
        db.SaveChanges();

        return View();
    }

    //ONLY WORK FOR PARTIAL
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
        //db.Follows.Add(new Follow { UserId = currentUser.Id, FollowedUserId = targetID });
        db.SaveChanges();

        return PartialView("_FollowUserBox", targetUser);
    }

    //ONLY WORK FOR PARTIAL
    [HttpPost]
    public IActionResult UnfollowUser(int targetID)
    {
        var currentUser = db.Users.Include(u => u.Detail).FirstOrDefault(u => u.Email == User.Identity!.Name);
        var targetUser = db.Users.Include(u => u.Detail).FirstOrDefault(u => u.Id == targetID);

        var follow = db.Follows.FirstOrDefault(f => f.UserId == currentUser.Id && f.FollowedUserId == targetID);
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

        return PartialView("_FollowUserBox", targetUser);
    }

    [HttpPost]
    public IActionResult Search_Post(string p)
    {
        var posts = db.Post.Include(u => u.User).Where(u => u.info.Contains(p)).OrderByDescending(p => p.Id).ToList();

        return PartialView("_Search_Post", posts);
    }

    [HttpPost]
    public IActionResult Search_User(string p)
    {
        var uc = db.Users.Include(u => u.Detail).Where(u => u.Username.Contains(p)).ToList();
        return PartialView("_Search_User", uc);
    }

    public IActionResult Blackjack()
    {
        return View();
    }

}
