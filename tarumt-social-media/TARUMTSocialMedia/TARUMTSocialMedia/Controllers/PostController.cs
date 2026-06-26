using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Text.Json;
using TARUMTSocialMedia.Models;

namespace TARUMTSocialMedia.Controllers;

public class PostController : Controller
{
    private readonly DB db;
    private readonly Helper hp;
    public PostController(DB db, Helper hp)
    {
        this.db = db;
        this.hp = hp;
    }

    public IActionResult Content(int PostID)
    {
        var op = db.Post.Include(p => p.User).FirstOrDefault(u => u.Id == PostID);

        if (op == null)
        {
            return RedirectToAction("Unexist");
        }


        return View(op);
    }

    [Route("Post/Template")]
    public IActionResult Template()
    {
        return View();
    }

    public IActionResult Unexist()
    {
        return View();
    }

    [Authorize]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [Authorize]
    public IActionResult Create(CreatePostVM vm)
    {
        int TextOnly = 1;
        int MediaOnly = 1;

        if (string.IsNullOrEmpty(vm.Info))
        {
            TextOnly = 0;
        }
        
        String[] imgNameArray = new string[4];

        if(vm.Imgs != null)
        {
            TextOnly = 0;
            List<String> imgName = new List<string>();

            if (vm.Imgs.Length <= 4)
            {
                foreach (var file in vm.Imgs)
                {
                    using (var stream = file.OpenReadStream())
                    using (var image = Image.Load<Rgba32>(stream))
                    {

                        if (hp.ValidatePhoto(file, 10, true) == "")
                        {
                            string filename = hp.SavePhoto(file, "images/post", image.Width, image.Height, true);
                            imgName.Add(filename);
                        }
                        else
                        {
                            ModelState.AddModelError("Imgs", "Ensure all of the images are under the size of 10MB, and we only supports JPEG/PNG/GIF");
                            return View(vm);
                        }

                    }
                }

                imgNameArray = imgName.ToArray();

            }
            else
            {
                ModelState.AddModelError("Imgs", "Images upload exceeds over 4! (You must upload images under limit of 4.)");
                return View(vm);
            }
        }
        else
        {
            MediaOnly = 0;
        }

        if (string.IsNullOrEmpty(vm.Info) && vm.Imgs == null)
        {
            ModelState.AddModelError("Imgs", "At least a word or an image in order to upload your post");
            return View(vm);
        }

        var post = new Post
        {
            info = vm.Info,
            TextOnly = TextOnly,
            MediaOnly = MediaOnly,
            Anomymous = hp.GetBoolNumber(vm.Anomymous),
            img1 = vm.Imgs?.ElementAtOrDefault(0) != null ? imgNameArray[0] : null,
            img2 = vm.Imgs?.ElementAtOrDefault(1) != null ? imgNameArray[1] : null,
            img3 = vm.Imgs?.ElementAtOrDefault(2) != null ? imgNameArray[2] : null,
            img4 = vm.Imgs?.ElementAtOrDefault(3) != null ? imgNameArray[3] : null,
            User = db.Users.FirstOrDefault(x => x.Email == User.Identity.Name),
        };

        db.Post.Add(post);

        db.SaveChanges();

        return RedirectToAction("Content", "Post", new { PostID = post.Id });
    }

    [HttpPost]
    public IActionResult Like(int PostId)
    {
        var CurrentUser = db.Users.Include(u => u.Detail).FirstOrDefault(u => u.Email == User.Identity!.Name);
        var TargetPost = db.Post.Include(p => p.User).FirstOrDefault(p  => p.Id == PostId);

        bool ValidToLike = true;

        if(db.Like.Any(p => p.UserId == CurrentUser.Id && p.PostId == PostId))
        {
            ValidToLike = false;
        }

        if (ValidToLike)
        {
            db.Like.Add(new Like
            {
                UserId = CurrentUser.Id,
                PostId = PostId,
            });
            TargetPost.LikeCount += 1;
            db.SaveChanges();
        }

        return PartialView("_LoadPost", TargetPost);
    }

    [HttpPost]
    public IActionResult Unlike(int PostId)
    {
        var CurrentUser = db.Users.Include(u => u.Detail).FirstOrDefault(u => u.Email == User.Identity!.Name);
        var TargetPost = db.Post.Include(p => p.User).FirstOrDefault(p => p.Id == PostId);

        var liked = db.Like.FirstOrDefault(p => p.PostId == PostId && p.UserId == CurrentUser.Id);

        if (liked != null) {
            db.Like.Remove(liked);
            if(TargetPost.LikeCount > 0)
            {
                TargetPost.LikeCount -= 1;
            }
            db.SaveChanges();
        }

        return PartialView("_LoadPost", TargetPost);
    }

    [HttpPost]
    public IActionResult DeletePost(int PostID)
    {

        var CurrentUser = db.Users.Include(u => u.Detail).FirstOrDefault(u => u.Email == User.Identity!.Name);
        var TargetPost = db.Post.Include(u => u.User).FirstOrDefault(p => p.Id == PostID);

        if (TargetPost.User.Id == CurrentUser.Id || User.IsInRole("1"))
        {
            //hp.Notify(hp.GetUserID(User.Identity.Name), "Debug sucess", "", "", "");
            var likes = db.Like.Where(l => l.Post.Id == PostID);
            db.Like.RemoveRange(likes);

            var comments = db.Comments.Where(c => c.PostId == PostID);
            db.Comments.RemoveRange(comments); 

            if (TargetPost.img1 != null){hp.DeletePhoto(TargetPost.img1, "post");};
            if (TargetPost.img2 != null){hp.DeletePhoto(TargetPost.img2, "post");};
            if (TargetPost.img3 != null){hp.DeletePhoto(TargetPost.img3, "post");};
            if (TargetPost.img4 != null){ hp.DeletePhoto(TargetPost.img4, "post"); };

            db.Post.Remove(TargetPost);
            db.SaveChanges();

            if (User.IsInRole("1"))
            {
                hp.Notify(TargetPost.User.Id, "Your post has been removed by the admin.", 
                                "", "icon/danger.png", "");
            }
        }

        return Content("Deleted " + PostID);
    }

    public IActionResult CreateComment(int postID, string CommentContent, string CommenterEmail)
    {
        var u = db.Users.FirstOrDefault(x => x.Email ==  CommenterEmail);
        var p = db.Post.Include(x => x.User).FirstOrDefault(x => x.Id == postID);

        db.Comments.Add(new Comment
        {
            UserId = u.Id,
            PostId = p.Id,
            Content = CommentContent,
        });
        p.CommentCount += 1;

        if (p.User.Id != u.Id)
        {
            hp.Notify(p.User.Id, hp.DisplayNickname(u.Username) + " has commented your post: \"" + CommentContent + "\"", u.PhotoURL,
                "post/" + p.img1, "/Post/Content?PostID=" + p.Id);
        }


        db.SaveChanges();

        return Redirect(Request.Headers.Referer.ToString());
    }

    [HttpPost]
    public IActionResult DeleteComment(int CommentID)
    {
        var currentUser = db.Users.Include(u => u.Detail).FirstOrDefault(u => u.Email == User.Identity!.Name);

        if (currentUser == null) return Unauthorized();

        var targetComment = db.Comments.Include(c => c.User).FirstOrDefault(c => c.Id == CommentID);

        if (targetComment == null) return NotFound();

        var post = db.Post.Include(p => p.User).FirstOrDefault(p => p.Id == targetComment.PostId);

        if (post == null) return NotFound();

        // Rule checks:
        bool isCommentAuthor = targetComment.User.Id == currentUser.Id;
        bool isPostOwner = post.User.Id == currentUser.Id;
        bool isAdmin = User.IsInRole("1");

        if (isCommentAuthor || isPostOwner || isAdmin)
        {
            db.Comments.Remove(targetComment);

            if (post.CommentCount > 0)
                post.CommentCount--;

            db.SaveChanges();

            if (isAdmin)
            {
                hp.Notify(
                    targetComment.User.Id,
                    "Your comment has been removed by the admin.",
                    "",
                    "icon/danger.png",
                    ""
                );
            }

            return Json(new { success = true, deletedId = CommentID });
        }

        return Forbid();
    }

    public IActionResult Profile_LoadAllPost(int profileId)
    {
        var profile = db.Post.Include(p => p.User).Where(p => p.User.Id == profileId).OrderByDescending(p => p.Id).ToList();
        return PartialView("_LoadProfile_Post", profile);
    }

    public IActionResult Profile_LoadTextPost(int profileId)
    {
        var profile = db.Post.Include(p => p.User).Where(p => p.User.Id == profileId && p.TextOnly == 1).OrderByDescending(p => p.Id).ToList();
        return PartialView("_LoadProfile_Post", profile);
    }

    public IActionResult Profile_LoadMediaPost(int profileId)
    {
        var profile = db.Post.Include(p => p.User).Where(p => p.User.Id == profileId && p.MediaOnly == 1).OrderByDescending(p => p.Id).ToList();
        return PartialView("_LoadProfile_Post", profile);
    }

    public IActionResult Profile_LoadSearchedPost(int profileId, string searchbox)
    {
        var profile = db.Post.Include(p => p.User).Where(p => p.User.Id == profileId && p.info.Contains(searchbox)).ToList();

        return PartialView("_LoadProfile_Post", profile);
        //return Content("You loaded searched post for " + profileId + "and show " + searchbox);
    }

    public IActionResult LoadReportWindow(string ContentType, int ContentID, int UserId, int TargetUserId)
    {
        string ContentString = "";

        if (ContentType == "post")
        {
            var post = db.Post.FirstOrDefault(x => x.Id == ContentID);
            ContentString = post?.info ?? "";
        }

        if (ContentType == "comment")
        {
            var comment = db.Comments.FirstOrDefault(x => x.Id == ContentID);
            ContentString = comment?.Content ?? "";
        }

        var vm = new ReportVM
        {
            ContentType = ContentType,
            ContentID = ContentID,
            ContentString = ContentString,
            UserID = UserId,
            TargetUserID = TargetUserId
        };
        return PartialView("_ReportWindow", vm);
    }

    [HttpPost]
    public IActionResult SubmitReport(ReportVM vm)
    {
        db.Report.Add(new()
        {
            Reason = vm.Reason,
            Info = vm.Info,
            ReportedBy = vm.UserID,
            ReportedOn = vm.TargetUserID,
            ContentType = vm.ContentType,
            ContentString = vm.ContentString, //What the content saying
            ContentID = vm.ContentID
        });
        db.SaveChanges();

        return Content("You submitted");
    }
}
