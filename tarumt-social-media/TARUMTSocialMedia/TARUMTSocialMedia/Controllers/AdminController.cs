using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace TARUMTSocialMedia.Controllers;

public class AdminController : Controller
{
    private readonly DB db;
    private readonly Helper hp;

    public AdminController(DB db, Helper hp)
    {
        this.db = db;
        this.hp = hp;
    }

    [Authorize(Roles = "1")]
    public IActionResult Hub()
    {
        return View();
    }

    [Authorize(Roles = "1")]
    public IActionResult Notification()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Notification(NotifyForAdmin vm) {

        //var userExist = db.Users.Any(u => u.Id == vm.UserID);

        

        if (vm.ProfileImage == null || string.IsNullOrEmpty(vm.ProfileImage))
        {
            vm.ProfileImage = "NULL";
        }

        if (vm.Link == null || string.IsNullOrEmpty(vm.Link))
        {
            vm.Link = "#";
        }

        if (ModelState.IsValid("UserID") && !(db.Users.Any(u => u.Id == vm.UserID)))
        {
            ModelState.AddModelError("UserID", "Unexisting user, search user first before using here.");
        }
        else
        {
            hp.Notify(vm.UserID, vm.Title, vm.ProfileImage, vm.ContentImage, vm.Link);
        }
        return View(vm);
    }

    [Authorize(Roles = "1")]
    public IActionResult Memberlist()
    {
        var u = db.Users.ToList();
        return View(u);
    }

    [Authorize(Roles = "1")]
    public IActionResult postlist()
    {
        var u = db.Post.Include(p => p.User).ToList();
        return View(u);
    }

    [Authorize(Roles = "1")]
    public IActionResult commentlist()
    {
        var u = db.Comments.Include(p => p.User).ToList();
        return View(u);
    }

    [Authorize(Roles = "1")]
    public IActionResult reported_post()
    {
        var u = db.Report.Where(x => x.ContentType == "post").ToList();
        return View(u);
    }

    [Authorize(Roles = "1")]
    public IActionResult reported_comment()
    {
        var u = db.Report.Where(x => x.ContentType == "comment").ToList();
        return View(u);
    }

    [Authorize(Roles = "1")]
    public IActionResult ban_record_list()
    {
        var u = db.BanRecord.ToList();
        return View(u);
    }

    [Authorize(Roles = "1")]
    public IActionResult adminlist()
    {
        var u = db.Users.Where(x => x.Role == "1").ToList();
        return View(u);
    }

    [HttpGet]
    public IActionResult Memberlist_Search(int? id, string username, string email, DateTime? date)
    {
        var query = db.Users.AsQueryable();

        if (id.HasValue){
            query = query.Where(u => u.Id == id.Value);
        }

        if (!string.IsNullOrEmpty(username)){
            query = query.Where(u => u.Username.Contains(username));
        }

        if (!string.IsNullOrEmpty(email)){
            query = query.Where(u => u.Email.Contains(email));
        }

        if (date.HasValue){
            query = query.Where(u => u.DateJoin.Date == date.Value.Date);
        }

        var result = query.ToList();

        if (Request.IsAjax())
        {
            return PartialView("_LoadUser", result);
        }

        return Content("Unable to load");

    }

    [HttpGet]
    public IActionResult Postlist_Search(int? id, string username, string? content, DateTime? date)
    {
        var query = db.Post.Include(p => p.User).AsQueryable();

        if (id.HasValue){
            query = query.Where(u => u.Id == id.Value);
        }

        if (!string.IsNullOrEmpty(username)){
            query = query.Where(u => u.User.Username.Contains(username));
        }

        if (!string.IsNullOrEmpty(content)){
            query = query.Where(u => u.info!.Contains(content));
        }

        if (date.HasValue){
            query = query.Where(u => u.DatePost.Date == date.Value.Date);
        }

        var result = query.ToList();

        if (Request.IsAjax())
        {
            return PartialView("_LoadPost", result);
        }

        return Content("Unable to load");

    }

    [HttpGet]
    public IActionResult Commentlist_Search(int? id, string username, int? post, string content, DateTime? date)
    {
        var query = db.Comments
                      .Include(c => c.User) 
                      .AsQueryable();

        if (id.HasValue){
            query = query.Where(c => c.Id == id.Value);
        }

        if (!string.IsNullOrEmpty(username)){
            query = query.Where(c => c.User.Username.Contains(username));
        }

        if (post.HasValue){
            query = query.Where(c => c.PostId == post.Value);
        }

        if (!string.IsNullOrEmpty(content)){
            query = query.Where(c => c.Content.Contains(content));
        }

        if (date.HasValue){
            query = query.Where(c => c.DateComment.Date == date.Value.Date);
        }

        var result = query.ToList();

        if (Request.IsAjax())
        {
            return PartialView("_LoadComment", result);
        }

        return Content("Unable to load");
    }

    [HttpGet]
    public IActionResult reportlist_Search(int? id, int? reporter, string contenttype, string content, DateTime? date)
    {
        var query = db.Report.Where(x => x.ContentType == contenttype).AsQueryable();

        if (id.HasValue)
            { query = query.Where(r => r.Id == id.Value); }

        if (reporter.HasValue)
            { query = query.Where(r => r.ReportedBy == reporter.Value); }

        if (!string.IsNullOrEmpty(content))
            { query = query.Where(r => r.ContentString.Contains(content)); }

        if (date.HasValue)
           { query = query.Where(r => r.DateReport.Date == date.Value.Date); }

        var result = query.ToList();
        return PartialView("_LoadReport", result);
    }

    [HttpGet]
    public IActionResult banneduser_Search(int? userid, DateTime? startdate)
    {
        var query = db.BanRecord.AsQueryable();

        if (userid.HasValue)
        {
            query = query.Where(r => r.UserId == userid);
        }

        if (startdate.HasValue)
           { query = query.Where(r => r.StartDate.Date == startdate.Value.Date); 
        }

        var result = query.ToList();
        return PartialView("_LoadBanRecord", result);
    }

    [Authorize(Roles = "1")]
    public IActionResult DetailUser(int userid) {
        var u = db.Users.Include(x => x.Detail).FirstOrDefault(x => x.Id == userid);
        return View(u);
    }

    [Authorize(Roles = "1")]
    public IActionResult DetailPost(int postid)
    {
        var p = db.Post.Include(x => x.User).FirstOrDefault(x => x.Id == postid);
        return View(p);
    }

    [Authorize(Roles = "1")]
    public IActionResult DetailComment(int commentid)
    {
        var p = db.Comments.Include(x => x.User).Include(x => x.Post).FirstOrDefault(x => x.Id == commentid);
        return View(p);
    }

    [Authorize(Roles = "1")]
    public IActionResult DetailReport(int reportid)
    {
        var p = db.Report.FirstOrDefault(x => x.Id == reportid);
        return View(p);
    }

    [HttpPost]
    public IActionResult BanUser(int UserId, int? DayRange, string Reason)
    {
        var user = db.Users.FirstOrDefault(u => u.Id == UserId);

        if (string.IsNullOrEmpty(Reason))
        {
            Reason = "No reason LOL.";
        }

        var ban = new BanRecord
        {
            UserId = UserId,
            Reason = Reason,
            StartDate = DateTime.Now,
            EndDate = DayRange.HasValue ? DateTime.Now.AddDays(DayRange.Value) : (DateTime?)null
        };

        db.BanRecord.Add(ban);
        db.SaveChanges();

        return Redirect(Request.Headers.Referer.ToString());
    }

    [HttpPost]
    public IActionResult unbanUser (int BanRecordID)
    {
        var b = db.BanRecord.FirstOrDefault(x => x.Id == BanRecordID); 
        db.BanRecord.Remove(b);
        db.SaveChanges();
        return Redirect(Request.Headers.Referer.ToString());
    }

    [HttpPost]
    public IActionResult PromoteAdmin(int UserID) {
        var u = db.Users.FirstOrDefault(x => x.Id == UserID);
        u.Role = "1";
        db.SaveChanges();
        return Redirect(Request.Headers.Referer.ToString());
    }

    [HttpPost]
    public IActionResult DemoteAdmin(int UserID)
    {
        var u = db.Users.FirstOrDefault(x => x.Id == UserID);
        u.Role = "0";
        db.SaveChanges();
        return Redirect(Request.Headers.Referer.ToString());
    }
}
