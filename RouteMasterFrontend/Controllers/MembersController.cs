using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RouteMasterFrontend.EFModels;
using RouteMasterFrontend.Models.Infra;
using RouteMasterFrontend.Models.ViewModels.Members;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Http;
using System.Security.Principal;
using System.Runtime.Intrinsics.X86;
using static System.Net.Mime.MediaTypeNames;
using Google.Apis.Auth;
using System.IO;
using RouteMasterFrontend.Models.Dto;
using Microsoft.Data.SqlClient;
using Dapper;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using static Google.Apis.Requests.BatchRequest;
using RouteMasterFrontend.Models.ViewModels.Carts;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Identity;

namespace RouteMasterFrontend.Controllers
{
    public class MembersController : Controller
    {
        private readonly RouteMasterContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly HashUtility _hashUtility;
        private readonly IConfiguration _configuration;
        //private readonly SignInManager<IdentityUser> _signInManager;

        public MembersController(RouteMasterContext context, IWebHostEnvironment environment, IConfiguration configuration)
        {
            _context = context;
            _environment = environment;
            _hashUtility = new HashUtility(configuration);
            _configuration = configuration;
            //_signInManager = signInManager;
        }

        // GET: Members/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Members == null)
            {
                return NotFound();
            }

            var member = await _context.Members.FindAsync(id);
            if (member == null)
            {
                return NotFound();
            }
            return View(member);
        }

        // POST: Members/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FirstName,LastName,Account,EncryptedPassword,Email,CellPhoneNumber,Address,Gender,Birthday,CreateDate,Image,IsConfirmed,ConfirmCode,GoogleAccessCode,FaceBookAccessCode,LineAccessCode,IsSuspended,LoginTime,IsSuscribe")] Member member)
        {
            if (id != member.Id)
            {
                return NotFound();
            }
            _context.Update(member);
            await _context.SaveChangesAsync();
            //if (ModelState.IsValid)
            //{
            //    try
            //    {
            //        _context.Update(member);
            //        await _context.SaveChangesAsync();
            //    }
            //    catch (DbUpdateConcurrencyException)
            //    {
            //        if (!MemberExists(member.Id))
            //        {
            //            return NotFound();
            //        }
            //        else
            //        {
            //            throw;
            //        }
            //    }
            //    return ViewComponent("MemberArea");
            //}
            return ViewComponent("MemberArea");
        }

        //會員普通登入
        [HttpGet]
        public async Task<IActionResult> MemberLogin()
        {
            return View();
        }


        [HttpPost]
        //會員普通登入
        public async Task<object> MemberLogin([FromBody] MemberLoginVM vm)
        {
            if (ModelState.IsValid == false) return BadRequest(new { success = false, message = "Invalid input" });


            ////鎖定帳號
            //if (ModelState.IsValid)
            //{
            //    var attemptResult = await _signInManager.PasswordSignInAsync(vm.Account, vm.Password, vm.RememberMe, lockoutOnFailure: true);

            //    if (attemptResult.Succeeded)
            //    {
            //        // 登入成功的處理
            //        return RedirectToAction("Index", "Home");
            //    }
            //    if (attemptResult.IsLockedOut)
            //    {
            //        // 處理使用者被鎖定的情況
            //        return View("AccountLocked");
            //    }
            //    else
            //    {
            //        // 登入失敗的處理
            //        ModelState.AddModelError(string.Empty, "無效的登入嘗試。");
            //    }
            //}



            //帳密IsExist
            Result result = ValidLogin(vm);
            if (result.IsSuccess != true) return BadRequest(new { success = false, message = "Invalid input" });

            //設定使用者登入資訊
            const bool rememberMe = false;
            var member = _context.Members.First(m => m.Account == vm.Account);


            //圖片可在優化位置
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, vm.Account),
                new Claim("memberImage", member.Image),
                new Claim("id",member.Id.ToString()),
                new Claim("Email",member.Email)
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            identity.AddClaim(new Claim("memberImage", member.Image));
            
            //設定驗證資訊
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2)
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity),
            authProperties);

            Response.Cookies.Append("Id", member.Id.ToString(), new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddHours(2)
            });

            //購物車
            var memberid = _context.Members.Where(m => m.Account == vm.Account).FirstOrDefault()?.Id;
            if (memberid != null)
            {
                var cart = _context.Carts.FirstOrDefault(x => x.MemberId == memberid);
                if (cart != null)
                {
                    int cartId = cart.Id;


                    Response.Cookies.Append("CartId", cartId.ToString(), new CookieOptions
                    {
                        Expires = DateTimeOffset.UtcNow.AddHours(2)
                    });
                }
                else
                {
                    var newCart = new Cart
                    {
                        MemberId = memberid.Value
                    };

                    _context.Carts.Add(newCart);
                    _context.SaveChanges();

                    int cartId = newCart.Id;


                    Response.Cookies.Append("CartId", cartId.ToString(), new CookieOptions
                    {
                        Expires = DateTimeOffset.UtcNow.AddHours(2)
                    });
                }

            }

            //轉成ajax呼叫
            return Ok(new { success = true, message = "Login successful" });

        }

        //加入id到claim
        //Google登入
        public async Task<IActionResult> GoogleLogin()
        {
            string? formCredential = Request.Form["credential"]; //回傳憑證
            string? formToken = Request.Form["g_csrf_token"]; //回傳令牌
            string? cookiesToken = Request.Cookies["g_csrf_token"]; //Cookie 令牌

            //驗證Google Token
            GoogleJsonWebSignature.Payload? payload = VerifyGoogleToken(formCredential, formToken, cookiesToken).Result;
            if (payload == null)
            {
                //驗證失敗
                ViewData["info"] = "驗證Google授權失敗";
            }
            else
            {
                ViewData["info"] = "驗證 Google 授權成功" + "<br>";
                ViewData["info"] += "Email:" + payload.Email + "<br>";
                ViewData["info"] += "Name:" + payload.Name + "<br>";
                ViewData["info"] += "Picture:" + payload.Picture;
            }


            //step1.判斷帳號是否存在，存在=>創身分登入:註冊後創身分後登入
            if (_context.Members.Any(m => m.Email == payload.Email))
            {
                var member = _context.Members.First((m => m.Email == payload.Email));
                const bool rememberMe = false;
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, payload.Name),
                    //new Claim(ClaimTypes.Email, payload.Email) // 使用 payload 中的郵件地址作為身份標識 
                    new Claim("id",member.Id.ToString()),
                    new Claim("memberImage", member.Image),
                    new Claim("Email",member.Email)

                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                identity.AddClaim(new Claim("memberImage", member.Image));
                //identity.AddClaim(new Claim(ClaimTypes.Name, payload.Name)); 如果需要可以加其他資料

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = rememberMe,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2)
                };

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity),
                    authProperties);

                Response.Cookies.Append("Id", member.Id.ToString(), new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddHours(2)
                });

                //購物車功能
                var memberid = _context.Members.Where(m => m.Email == payload.Email).FirstOrDefault()?.Id;

                if (memberid != null)
                {
                    var cart = _context.Carts.FirstOrDefault(x => x.MemberId == memberid);
                    if (cart != null)
                    {
                        int cartId = cart.Id;


                        Response.Cookies.Append("CartId", cartId.ToString(), new CookieOptions
                        {
                            Expires = DateTimeOffset.UtcNow.AddHours(2)
                        });
                    }
                    else
                    {
                        var newCart = new Cart
                        {
                            MemberId = memberid.Value
                        };

                        _context.Carts.Add(newCart);
                        _context.SaveChanges();

                        int cartId = newCart.Id;


                        Response.Cookies.Append("CartId", cartId.ToString(), new CookieOptions
                        {
                            Expires = DateTimeOffset.UtcNow.AddHours(2)
                        });
                    }

                }

                return RedirectToAction("Index", "Home");
            }
            else
            {
                MemberImage img = new MemberImage();
                
                Result result = RegisterGoogleMember(payload, img);
                Member member = _context.Members.FirstOrDefault(m => m.Email == payload.Email);


                if (result.IsSuccess)
                {
                    const bool rememberMe = false;
                    var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, payload.Name),
                    new Claim("id",member.Id.ToString()),
                    new Claim("memberImage", member.Image),
                    new Claim("Email",member.Email)

                };

                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    identity.AddClaim(new Claim("memberImage", member.Image));
                    //identity.AddClaim(new Claim(ClaimTypes.Name, payload.Name)); 如果需要有Name再加

                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = rememberMe,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2)
                    };

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity),
                        authProperties);



                    //購物車功能
                    var memberid = _context.Members.Where(m => m.Email == payload.Email).FirstOrDefault()?.Id;
                    if (memberid != null)
                    {
                        var cart = _context.Carts.FirstOrDefault(x => x.MemberId == memberid);
                        if (cart != null)
                        {
                            int cartId = cart.Id;


                            Response.Cookies.Append("CartId", cartId.ToString(), new CookieOptions
                            {
                                Expires = DateTimeOffset.UtcNow.AddHours(2)
                            });
                        }
                        else
                        {
                            var newCart = new Cart
                            {
                                MemberId = memberid.Value
                            };

                            _context.Carts.Add(newCart);
                            _context.SaveChanges();

                            int cartId = newCart.Id;


                            Response.Cookies.Append("CartId", cartId.ToString(), new CookieOptions
                            {
                                Expires = DateTimeOffset.UtcNow.AddHours(2)
                            });
                        }

                    }

                    return RedirectToAction("Index", "Home");

                }
                else
                {
                    ModelState.AddModelError(string.Empty, result.ErrorMessage);
                    return View(payload);
                }
            }

        }


        //會員資料頁
        [HttpGet]
        public IActionResult MyMemberIndex()
        {
            //抓會員登入資訊
            ClaimsPrincipal user = HttpContext.User;

            //列出與登入符合資料
            string userEmail = user.FindFirst("Email").Value;
            string userID = user.FindFirst("id").Value;

            Member myMember = _context.Members.First(m => m.Email == userEmail);

            List<Region> regions = _context.Regions.ToList();
            List<Town> towns = _context.Towns.ToList();
            ViewBag.Regions = regions;
            ViewBag.Towns = towns;

            if (user.Identity.IsAuthenticated)
            {
                return View(myMember);
            }
            return RedirectToAction("MemberLogin", "Members");
        }

        [HttpGet]
        public async Task<IActionResult> MemberRegister()
        {
            List<Region> regions = await _context.Regions.ToListAsync();
            List<Town> towns = await _context.Towns.ToListAsync();
            ViewBag.Regions = regions;
            ViewBag.Towns = towns;
            return View();
        }

        //註冊會員
        [HttpPost]
        public IActionResult MemberRegister(MemberRegisterVM vm, IFormFile? facePhoto)

        {
            MemberImage img = new MemberImage();
            List<Region> regions = _context.Regions.ToList();
            List<Town> towns = _context.Towns.ToList();


            if (ModelState.IsValid)
            {
                if (facePhoto != null && facePhoto.Length > 0)
                {
                    string path = Path.Combine(_environment.WebRootPath, "MemberUploads");
                    string fileName = SaveUploadFile(path, facePhoto);
                    img.Image = fileName;
                    img.Name = "未命名";
                    vm.Image = fileName;
                }
                else
                {
                    string newFileName = vm.Image;
                    img.Image = newFileName;
                    img.Name = "未命名";
                    vm.Image = newFileName;
                }
            }
            else
            {

                ViewBag.Regions = regions;
                ViewBag.Towns = towns;
                return View(vm);
            }



            Result result = RegisterMember(vm, img);


            if (result.IsSuccess)
            {
                return View("MemberLogin");
            }
            else
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage);
                ViewBag.Regions = regions;
                ViewBag.Towns = towns;
                return View(vm);

            }

        }

        [AcceptVerbs("GET")]
        public  IActionResult CheckRepeatAccount(string Account)
        {
            var isAccountReapeat = _context.Members.FirstOrDefault(m=>m.Account == Account);

            if (isAccountReapeat != null)
            {
                return Json($"{Account} 已經被註冊過囉，請換一個.");
            }

            return Json(true);
        }

        [AcceptVerbs("GET", "POST")]
        public IActionResult CheckRepeatEmail (string Email)
        {
            var isEmailReapeat = _context.Members.FirstOrDefault(m => m.Email == Email);

            if (isEmailReapeat != null)
            {
                return Json($"{Email} 已經被註冊過囉，請換一個.");
            }
            return Json(true);
        }

        //會員登出
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Response.Cookies.Delete(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("LogoutSuccess", "Members");
        }

        [HttpPost]
        public async Task<IActionResult> EditPasswordLogout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Response.Cookies.Delete(CookieAuthenticationDefaults.AuthenticationScheme);

            // 返回 JSON 响应
            return Json(new { message = "登出成功" });
        }

        //登出成功畫面
        public async Task<IActionResult> LogoutSuccess()
        {
            return View();
        }

        //忘記密碼並寄信
        public IActionResult MemberForgetPassword()
        {
            return View();
        }

        //忘記密碼並寄信
        [HttpPost]
        public IActionResult MemberForgetPassword(MemberForgetPasswordVM vm)
        {
            if (ModelState.IsValid == false) return View(vm);


            //// 生成email裡的連結
            //var urlTemplate = $"{Request.Scheme}://{Request.Host.Value}/Members/MemberResetPassword?Account={myAccount}&confirmCode={0}";


            Result result = ProcessResetPassword(vm.Account, vm.Email);

            if (result.IsFalse)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage);
                return View(vm);
            }


            //先跳一個成功更改密碼的畫面
            return View("GetNewPasswordSuccess");
            //return View("MemberLogin");
        }

        [HttpGet]
        public IActionResult GetNewPasswordSuccess()
        {
            return View();
        }





        //忘記密碼/更改密碼       
        public IActionResult MemberResetPassword()
        {
            return View();
        }

        //忘記密碼/更改密碼 
        [HttpPost]
        public IActionResult MemberResetPassword(MemberResetPasswordVM vm, string account, string confirmCode)
        {
            if (ModelState.IsValid == false) return View(vm);

            Result result = ProcessChangePassword(account, confirmCode, vm.Password);

            //if (result.IsSuccess == false) { }
            //if (!result.IsSuccess) { }
            if (result.IsFalse)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage);
                return View(vm);
            }

            return View("MemberLogin");
        }

        public IActionResult EditPassword()
        {
            return View();
        }

        [HttpPost]
        public IActionResult EditPassword([FromBody] MemberEditPasswordVM vm)
        {
            

            //return Json(new { success = false, message = "帳密錯誤，請重新輸入" });

            var currentUserAccount = User.Identity.Name;
            Result result = ChangePassword(currentUserAccount, vm);

            if (result.IsSuccess == false)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage);
                return View(result);
            }

            return ViewComponent("MemberArea");

        }

        [HttpPut]
        public IActionResult MemEdit(MemberEditDTO dto)
        {
            var currentUserAccount = User.Identity.Name;
            Member member = _context.Members.FirstOrDefault(m => m.Account == currentUserAccount);

            member.FirstName=dto.firstName;
            member.LastName=dto.lastName;           
            member.CellPhoneNumber=dto.cellPhoneNumber;
            member.Address=dto.address;
            member.Gender=dto.gender;
            member.Birthday=dto.birthday;
            member.IsSuscribe = dto.isSuscribe;

            List<Region> regions = _context.Regions.ToList();
            List<Town> towns = _context.Towns.ToList();
            ViewBag.Regions = regions;
            ViewBag.Towns = towns;

            //_context.Entry(member).State =EntityState.Modified;
            _context.SaveChanges();
            return ViewComponent("MemberArea");
        }

        [HttpGet("Member/HistoryOrder/{memberid?}")]
        public async Task<IActionResult> HistoryOrder(int? memberid)
        {
          
			if (!memberid.HasValue)
			{
				return BadRequest("Member ID is required");
			}
			var memberId = await _context.Members.Where(m => m.Id == memberid.Value).Select(m => m.Id).FirstOrDefaultAsync();

			if (memberId == 0)
			{
				return NotFound("Member not found");
			}
          
            var orders = await _context.Orders
		    .Where(o => o.MemberId == memberId)
		    .Include(x => x.OrderExtraServicesDetails)
            .ThenInclude(x => x.ExtraService)
		    .Include(x => x.OrderActivitiesDetails)
            .ThenInclude(x => x.Activity)
		    .Include(x => x.OrderAccommodationDetails)
            .ThenInclude(x => x.RoomProduct)
            .ThenInclude(x => x.Room)
            .ThenInclude(x => x.RoomImages)
		    .Include(x => x.Coupons)
		    .Include(x => x.OrderHandleStatus)
		    .Include(x => x.PaymentStatus)
		    .Include(x => x.PaymentMethod)
		    .ToListAsync();

            if (!orders.Any())
            {
                return NotFound("Member not found or no orders for this member");
            }
            var orderDTOs = orders.Select(o => new OrderDto
			{
				Id = o.Id,
				MemberId = o.MemberId,
                PaymentMethodName = o.PaymentMethod.Name,   
                PaymentStatusName = o.PaymentStatus.Name,
				OrderHandleStatusId = o.OrderHandleStatusId,
				CouponsId = o.CouponsId,
				CreateDate = o.CreateDate,
				ModifiedDate = o.ModifiedDate,  
				Total = o.Total,

				ExtraServiceDetails = o.OrderExtraServicesDetails.Select(es => new OrderExtraServiceDetailDTO
				{
					Id = es.Id,
					OrderId = es.OrderId,
					ExtraServiceId = es.ExtraServiceId,
					ExtraServiceName = es.ExtraServiceName,
					ExtraServiceProductId = es.ExtraServiceProductId,
                    Date = es.Date,
					Price = es.Price,
                    ImageUrl = "/ExtraServiceImages/" + es.ExtraService.Image,
                    Quantity = es.Quantity
                    
				}).ToList(),

				ActivityDetails = o.OrderActivitiesDetails.Select(ad => new OrderActivityDetailDTO
				{
					Id = ad.Id,
					OrderId = ad.OrderId,
					ActivityId = ad.ActivityId,
					ActivityName = ad.ActivityName,
					ActivityProductId = ad.ActivityProductId,
                   ImageUrl = "/ActivityImages/"+ad.Activity.Image,
					Date = ad.Date,
					StartTime = ad.StartTime, 
					EndTime = ad.EndTime,
					Price = ad.Price,
					Quantity = ad.Quantity
				}).ToList(),

				AccommodationDetails = o.OrderAccommodationDetails.Select(ac => new OrderAccommodationDetailDTO
				{
					Id = ac.Id,
					OrderId = ac.OrderId,
					AccommodationId = ac.AccommodationId,
					AccommodationName = ac.AccommodationName,
					RoomProductId = ac.RoomProductId,
					RoomType = ac.RoomType,
					RoomName = ac.RoomName,
					CheckIn = (DateTime)ac.CheckIn,
					CheckOut = (DateTime)ac.CheckOut,
					RoomPrice = ac.RoomPrice,
                    ImageUrl = "/AccommodationImages/" + ac.RoomProduct.Room.RoomImages.First().Image,
					Quantity = ac.Quantity,
					Note = ac.Note
				}).ToList()
			}).ToList();
           
			return View(orderDTOs);
		}

        ////使用dapper做資料庫存取歷史訂單    
        //[HttpGet]
        //public async Task<IActionResult> HistoryOrder()
        //{
        //    ClaimsPrincipal user = HttpContext.User;
            
        //    //列出與登入符合資料
        //    string userAccount = user.Identity.Name;

        //    Member myMember = _context.Members.FirstOrDefault(m => m.Account == userAccount);


        //    string connStr = _configuration.GetConnectionString("RouteMaster");

        //     string sql = $@"select ord.memberid, ord.PaymentStatusId, ord.CreateDate, ord.ModifiedDate, ord.Total, m.Account, m.Email
        //                    from Orders as ORD
        //                    inner join Members as M on ORD.MemberId = m.Id
        //                    where m.Account=@Account";
        //    IEnumerable<HistoryOrderDTO> orderDTOs = new SqlConnection(connStr).Query<HistoryOrderDTO>(sql, new { Account = myMember.Account });
        //    return Json(orderDTOs);
        //}

        public IActionResult Test123()
        {
            return View();
        }


        //註冊成功頁面
        public ActionResult SuccessRegister()
        {
            return View();
        }


        [HttpGet]
        //上傳系統圖片 -- 有空時應該改去後台管理系統
        public IActionResult UploadSystemImages()
        {
            return View();
        }

        //上傳系統內建大頭貼
        [HttpPost]
        public IActionResult UploadSystemImages(IFormFile[] files)
        {
            if (files != null && files.Length > 0)
            {
                foreach (var file in files)
                {
                    string path = Path.Combine(_environment.WebRootPath, "SystemImages");
                    string fileName = SaveUploadFile(path, file);
                    SystemImage img = new SystemImage();
                    img.Image = fileName;
                    _context.SystemImages.Add(img);
                    _context.SaveChanges();
                }
            }
            return RedirectToAction("Index", "Home");
        }

        //歷史訂單
        //這是前後端分離，做成web api 的服務
        public  IActionResult MemOrder()
        {
            return View();
        }

        [HttpPost]
        public IActionResult MembersNavbar([FromBody]Abc dto)
        {
            var page = dto.pagecase;
            return ViewComponent("MemberArea", page);
        }


        public IActionResult DropdownTown(int regionId)
        {
            List<Town> towns = _context.Towns.Where(t=>t.RegionId == regionId).ToList();

            return Json(towns);
        }

        //更改大頭貼
        public IActionResult MemberChangePhoto(string img)
        {
            return View();
        }

        [HttpPost]
        public IActionResult MemberChangePhoto(IFormFile? facePhoto)
        {
            MemberImage img = new MemberImage();
            //抓會員登入資訊
            ClaimsPrincipal user = HttpContext.User;


            //列出與登入符合資料
            string userAccount = user.Identity.Name;

            Member myMember = _context.Members.First(m => m.Account == userAccount);
            if (facePhoto != null && facePhoto.Length > 0)
            {
                string path = Path.Combine(_environment.WebRootPath, "MemberUploads");
                string fileName = SaveUploadFile(path, facePhoto);
                img.Image = fileName;
                img.Name = "未命名";
                myMember.Image = fileName;
            }
            else
            {
                string newFileName = myMember.Image;
                img.Image = newFileName;
                img.Name = "未命名";
                myMember.Image = newFileName;

            }

            _context.SaveChanges();
            img.MemberId = myMember.Id;
            _context.MemberImages.Add(img);
            _context.SaveChanges();

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public IActionResult ShowTownSelect([FromBody]int regionId)
        {
            IEnumerable<Town> townList = _context.Towns.Where(t=>t.RegionId == regionId);

            var townData = townList.Select(t=>new
            {
                townId = t.Id,
                name= t.Name
            }).ToList();

            return Json(townData);
        }

        private Result ChangePassword(string account, MemberEditPasswordVM vm)
        {
            var salt = _hashUtility.GetSalt();
            var hashOrigPassword = HashUtility.ToSHA256(vm.OldPassword, salt);

            var memberInDb = _context.Members.FirstOrDefault(m => m.Account == account && m.EncryptedPassword == hashOrigPassword);
            if (memberInDb == null) return Result.Failure("找不到要修改的會員記錄");

            var hashPassword = HashUtility.ToSHA256(vm.NewPassword, salt);


            // 更新密碼
            memberInDb.EncryptedPassword = hashPassword;
            _context.SaveChanges();

            return Result.Success();
        }

        private Result ProcessChangePassword(string account, string confirmCode, string password)
        {

            // 驗證 memberId, confirmCode是否正確
            var memberInDb = _context.Members.FirstOrDefault(m => m.Account == account && m.ConfirmCode == confirmCode);
            if (memberInDb == null) return Result.Failure("找不到對應的會員記錄");

            // 更新密碼,並將 confirmCode清空
            var salt = _hashUtility.GetSalt();
            var encryptedPassword = HashUtility.ToSHA256(password, salt);

            memberInDb.EncryptedPassword = encryptedPassword;
            memberInDb.ConfirmCode = null;

            _context.SaveChanges();

            return Result.Success();
        }

        //會員是否存在,密碼雜湊,存進資料庫
        private Result IsMemberExist(MemberRegisterVM vm)
        {
            if (_context.Members.Any(m => m.Account == vm.Account))
            {
                // 丟出異常,或者傳回 Result
                return Result.Failure($"帳號 {vm.Email} 已存在, 請更換後再試一次");
            }

            // 將密碼進行雜湊
            var salt = _hashUtility.GetSalt();
            var hashPassword = HashUtility.ToSHA256(vm.Password, salt);
            string EncryptedPassword = hashPassword;

            // 填入 isConfirmed, ConfirmCode
            //vm.IsConfirmed = false;
            //vm.ConfirmCode = Guid.NewGuid().ToString("N");

            Member member = new Member
            {
                FirstName = vm.FirstName,
                LastName = vm.LastName,
                Account = vm.Account,
                EncryptedPassword = EncryptedPassword,
                Email = vm.Email,
                CellPhoneNumber = vm.CellPhoneNumber,
                Address = vm.Address,
                Birthday = DateTime.Now,
                Gender = vm.Gender,
                ConfirmCode = Guid.NewGuid().ToString("N"),
                IsConfirmed = false,
                IsSuspended = false
                //IsSuscribe = vm.IsSuscribe
            };

            // 將它存到db
            _context.Members.Add(member);
            _context.SaveChanges();
            return Result.Success();
        }

        //有效登入
        private Result ValidLogin(MemberLoginVM vm)
        {
            var db = new RouteMasterContext();
            var member = db.Members.FirstOrDefault(m => m.Account == vm.Account);
            var salt = _hashUtility.GetSalt();
            var hashPassword = HashUtility.ToSHA256(vm.Password, salt);


            //帳號先，後密碼
            if (member != null && string.Compare(hashPassword, member.EncryptedPassword) == 0)
            {
                return Result.Success();               
            }
            else
            {
                return Result.Failure("帳密有錯");
            }
              
        }

        //上傳圖片
        private string SaveUploadFile(string filePath, IFormFile file)
        {
            if (file == null || file.Length == 0) return string.Empty;

            string ext = Path.GetExtension(file.FileName);

            string[] allowExts = new string[] { ".jpg", ".jpeg", ".png", ".tif" };

            if (allowExts.Contains(ext.ToLower()) == false)
            {
                return string.Empty;
            }
            string newFileName = Guid.NewGuid().ToString("N") + ext;
            string fullName = Path.Combine(filePath, newFileName);

            using (var fileStream = new FileStream(fullName, FileMode.Create))
            {
                file.CopyTo(fileStream);
            }
            return newFileName;
        }

        private async Task<GoogleJsonWebSignature.Payload?> VerifyGoogleToken(string? formCredential, string? formToken, string? cookiesToken)
        {
            // 檢查預防空值
            if (formCredential == null || formToken == null && cookiesToken == null) return null;

            GoogleJsonWebSignature.Payload? payload;
            try
            {
                // 驗證 token，預防令牌值被竄改，兩者必須是來自同一次請求的令牌
                if (formToken != cookiesToken) return null;
 
                // 取得GoolgeApiClientId
                IConfiguration Config = new ConfigurationBuilder().AddJsonFile("appSettings.json").Build();
                string GoogleApiClientId = Config.GetSection("GoogleApiClientId").Value;
                var settings = new GoogleJsonWebSignature.ValidationSettings()
                {
                    Audience = new List<string>() { GoogleApiClientId }
                };
                payload = await GoogleJsonWebSignature.ValidateAsync(formCredential, settings);
                if (!payload.Issuer.Equals("accounts.google.com") && !payload.Issuer.Equals("https://accounts.google.com"))
                {
                    return null;
                }
                if (payload.ExpirationTimeSeconds == null)
                {
                    return null;
                }
                else
                {
                    DateTime now = DateTime.Now.ToUniversalTime();
                    DateTime expiration = DateTimeOffset.FromUnixTimeSeconds((long)payload.ExpirationTimeSeconds).DateTime;
                    if (now > expiration)
                    {
                        return null;
                    }
                }
            }
            catch
            {
                return null;
            }
            return payload;
        }


        //會員是否存在
        private bool MemberExists(int id)
        {
            return (_context.Members?.Any(e => e.Id == id)).GetValueOrDefault();
        }

        //會員存進DB
        private Result RegisterMember(MemberRegisterVM vm, MemberImage img)
        {
            if (_context.Members.Any(m => m.Account == vm.Account))
            {
                // 丟出異常,或者傳回 Result
                return Result.Failure($"帳號 {vm.Account} 已存在, 請更換後再試一次");
            }
            if (_context.Members.Any(m => m.Email == vm.Email))
            {
                // 丟出異常,或者傳回 Result
                return Result.Failure($"帳號 {vm.Email} 已存在, 請更換後再試一次");
            }

            bool securityPassword = Regex.IsMatch(vm.Password, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)");
            if (!securityPassword)
            {
                return Result.Failure($"密碼安全性有疑慮，請加強密碼");
            }
            // 將密碼進行雜湊

            var salt = _hashUtility.GetSalt();
            var hashPassword = HashUtility.ToSHA256(vm.Password, salt);
            string EncryptedPassword = hashPassword;

            // 填入 isConfirmed, ConfirmCode
            //vm.IsConfirmed = false;
            //vm.ConfirmCode = Guid.NewGuid().ToString("N");

            Member member = new Member
            {
                FirstName = vm.FirstName,
                LastName = vm.LastName,
                Account = vm.Account,
                EncryptedPassword = EncryptedPassword,
                Email = vm.Email,
                CellPhoneNumber = vm.CellPhoneNumber,
                Address = vm.Address,
                Birthday = DateTime.Now,
                Gender = vm.Gender,
                Image = vm.Image,
                ConfirmCode = Guid.NewGuid().ToString("N"),
                IsConfirmed = true,
                IsSuspended = false
                //IsSuscribe = vm.IsSuscribe
            };

            // 將它存到db
            _context.Members.Add(member);
            _context.SaveChanges();
            img.MemberId = member.Id;
            _context.MemberImages.Add(img);
            _context.SaveChanges();

            return Result.Success();
        }

        private Result RegisterGoogleMember(GoogleJsonWebSignature.Payload payload, MemberImage img)
        {
            if (_context.Members.Any(m => m.Email == payload.Email))
            {
                return Result.Failure($"帳號 {payload.Email} 已被註冊過, 請更換後再試一次");
            }



            Regex PasswordRegex = new Regex("^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[^a-zA-Z\\d]).{8,}$");
            string[] CommonPasswords = new string[]
            {
        "password",
        "123456",
        "qwerty",
                // 添加其他常見密碼
            };

            string imageFile = "56b080c68be248628a3f4fa4f0a1d9c7.png";
         
            Member member = new Member
            {
                FirstName = payload.GivenName,
                LastName = payload.FamilyName,
                Account = payload.Name,
                Email = payload.Email,
                CellPhoneNumber="尚未新增電話",
                Address="尚未新增地址",
                Birthday= DateTime.Today,
                Gender= false,
                Image= imageFile,
                IsConfirmed = true,
                IsSuspended = false,
                IsSuscribe = false,
            };

            //將它存到db
            _context.Members.Add(member);
            _context.SaveChanges();
            img.MemberId = member.Id;
           
            return Result.Success();
        }

        //確認帳號發送更改密碼信件
        private Result ProcessResetPassword(string account, string email)
        {

            // 檢查account,email正確性
            var memberInDb = _context.Members.FirstOrDefault(m => m.Account == account);

            if (memberInDb == null) return Result.Failure("帳號或註冊信箱錯誤"); // 故意不告知確切錯誤原因

            var myAccount = memberInDb.Account;        

            if (string.Compare(email, memberInDb.Email, StringComparison.CurrentCultureIgnoreCase) != 0) return Result.Failure("帳號或註冊信箱錯誤");

            // 檢查 IsConfirmed必需是true, 因為只有已啟用的帳號才能重設密碼
            if (memberInDb.IsConfirmed == false) return Result.Failure("您還沒有啟用本帳號, 請先完成才能重設密碼");

            // 更新記錄, 填入 confirmCode
            var confirmCode = Guid.NewGuid().ToString("N");
            memberInDb.ConfirmCode = confirmCode;
            _context.SaveChanges();

            var urlTemplate = $"{Request.Scheme}://{Request.Host.Value}/Members/MemberResetPassword?Account={myAccount}&confirmCode={confirmCode}";

            // 發email
            var url = string.Format(urlTemplate, memberInDb.Account, confirmCode);
            new EmailHelper().SendForgetPasswordEmail(url, memberInDb.FirstName, email);

            return Result.Success();
        }
       
    }
}
