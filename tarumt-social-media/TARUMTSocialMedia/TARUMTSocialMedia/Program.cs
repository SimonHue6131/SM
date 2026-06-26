global using TARUMTSocialMedia.Models;
global using TARUMTSocialMedia;
using System.IO.Pipelines;
//global using TARUMTSocialMedia;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddAuthentication().AddCookie();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<Helper>();
builder.Services.AddWebOptimizer(pipeline =>
{
    pipeline.AddCssBundle("/bundle.css",
            "css/style.css"
        );

    pipeline.AddJavaScriptBundle("/bundle.js",
            "/js/jquery.min.js",
            "/js/jquery.unobtrusive-ajax.min.js",
            "/js/jquery.validate.min.js",
            "/js/jquery.validate.unobtrusive.min.js",
            "/js/app.js"
        );
});
builder.Services.AddSqlServer<DB>($@"
    Data Source=(LocalDB)\MSSQLLocalDB;
    AttachDbFilename={builder.Environment.ContentRootPath}\DB.mdf;
");


var app = builder.Build();
app.UseHttpsRedirection();
app.UseWebOptimizer();
app.UseStaticFiles();
app.MapDefaultControllerRoute();
app.Run();
