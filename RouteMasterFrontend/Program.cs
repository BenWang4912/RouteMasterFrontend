using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RouteMasterFrontend.EFModels;
using RouteMasterFrontend.Models.Infra;
using RouteMasterFrontend.Models.Infra.DapperRepositories;
using RouteMasterFrontend.Models.Interfaces;
using RouteMasterFrontend.Models.Services;
using System.Configuration;
using static System.Net.WebRequestMethods;

var builder = WebApplication.CreateBuilder(args);

// Add services to the DI container.
builder.Services.AddDbContext<RouteMasterContext>(options =>
{
	options.UseSqlServer(builder.Configuration.GetConnectionString("RouteMaster"));
});


builder.Services.Configure<IdentityOptions>(options =>
{
	options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(1);
	options.Lockout.MaxFailedAccessAttempts = 2;
	options.Lockout.AllowedForNewUsers = false;
});

//builder.Services.AddIdentity<IdentityUser, IdentityRole>()
//	.AddSignInManager<SignInManager<IdentityUser>>();
    

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddGoogle(options =>
{
	options.ClientId = "161026089487 - tl7dobkpg2r05cnahqrho3af1vdjn33q.apps.googleusercontent.com";
	options.ClientSecret = "GOCSPX - USIP0 - CadhVekEq_roiE3MsuzmAS";
}).AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
	//未登入時導入的網址
	options.LoginPath = new PathString("/Members/MemberLogin");
	
	//options.AccessDeniedPath = new PathString("/Members/MemberLogin"); /*存取失敗的路徑*/
});

builder.Services.AddDistributedMemoryCache(); 
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(120); 
});


builder.Services.Configure<CookiePolicyOptions>(options =>
{
	options.MinimumSameSitePolicy = SameSiteMode.Lax;
	options.HttpOnly = HttpOnlyPolicy.Always;
	options.Secure = (CookieSecurePolicy)SameSiteMode.None; // 設置 Secure 屬性為 None
});

builder.Services.AddTransient<ICoupon, CouponService>();



builder.Services.AddControllersWithViews();

builder.Services.AddSignalR();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Home/Error");
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSession();
app.UseRouting();

app.UseCookiePolicy();
app.UseAuthentication();
app.UseAuthorization();

//app.MapHub<BookingHub>("/bookingHub");

app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
