using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RouteMasterFrontend.EFModels;
using ECPay.Payment.Integration;
using Microsoft.AspNetCore.Session;
using RouteMasterFrontend.Models.Dto;
using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.SignalR;
using RouteMasterFrontend.Models.Infra;

namespace RouteMasterFrontend.Controllers
{
    
    public class CartsController : Controller
    {
        private readonly HttpClient _httpClient;
        //private readonly IHubContext<BookingHub> _hubContext;
        private readonly RouteMasterContext _context;
        private const string MerchantID = "3002607";
        private const string PaymentApiUrl = "https://payment-stage.ecpay.com.tw/Cashier/AioCheckOut/V5";
        private const string HashKey = "pwFHCqoQZGmho4w6"; 
        private const string HashIV = "EkRm7iFT261dpevs";
        public CartsController(RouteMasterContext context/*, IHubContext<BookingHub> hubContext*/)
        {
            _context = context;
            //_hubContext = hubContext;
        }
      
        // GET: Carts
        public async Task<IActionResult> Index2()
        {
            int cartIdFromCookie = Convert.ToInt32(Request.Cookies["CartId"] ?? "0");

            // 將讀取的值存入 ViewData
            ViewData["CartId"] = cartIdFromCookie;
            var routeMasterContext = _context.Carts.Include(c => c.Member);
            return View(await routeMasterContext.ToListAsync());
        }


        public IActionResult IndexVue()
        {  
            return View();
        }
    
        public IActionResult Index()
        {
            int cartIdFromCookie = Convert.ToInt32(Request.Cookies["CartId"] ?? "0");

            using (var context = new RouteMasterContext())
            {
                var cartDetailsDto = new CartDetailDto
                {
                    ExtraServices = context.Cart_ExtraServicesDetails
                   .Where(c => c.CartId == cartIdFromCookie)
                   .Include(c => c.ExtraServiceProduct)
                   .Include(c => c.ExtraServiceProduct.ExtraService)
                   .Select(cartDetail => new Cart_ExtraServicesDetailDto
                       {
                           Id = cartDetail.Id,
                           Name = cartDetail.ExtraServiceProduct.ExtraService.Name,
                           Description = cartDetail.ExtraServiceProduct.ExtraService.Description,
                           Price = cartDetail.ExtraServiceProduct.Price,
                           Date = cartDetail.ExtraServiceProduct.Date,
                           Quantity = cartDetail.Quantity,
                           ImageUrl = "/ExtraServiceImages/"+cartDetail.ExtraServiceProduct.ExtraService.Image,
                    })
                    .ToList(),


                    Accommodations = context.Cart_AccommodationDetails
                    .Where(c => c.CartId == cartIdFromCookie)
                    .Include(c => c.RoomProduct)
                    .Include(c => c.RoomProduct.Room)
                    .Include(c => c.RoomProduct.Room.Accommodation)
                    .Include(c => c.RoomProduct.Room.RoomType)
                    .Include(c => c.RoomProduct.Room.RoomImages)
                    .Select(cartDetail => new Cart_AccommodationDetailDto
                           {
                           Id = cartDetail.Id,
                           RoomId = cartDetail.RoomProduct.Id,
                           RoomName = cartDetail.RoomProduct.Room.Name,
                           AccommodationName = cartDetail.RoomProduct.Room.Accommodation.Name,
                           RoomTypeName = cartDetail.RoomProduct.Room.RoomType.Name,
                           Price = cartDetail.RoomProduct.NewPrice,
                           Date = cartDetail.RoomProduct.Date,
                           Quantity = cartDetail.Quantity,
                           ImageUrl = cartDetail.RoomProduct.Room.RoomImages.First().Image
                       })
                        .ToList(),

                    Activities = context.Cart_ActivitiesDetails
                       .Where(c => c.CartId == cartIdFromCookie)
                       .Include(c => c.ActivityProduct)
                       .Include(c => c.ActivityProduct.Activity)
                       .Select(cartDetail => new Cart_ActivitiesDetailDto
                       {
                           Id = cartDetail.Id,
                           ActivityName = cartDetail.ActivityProduct.Activity.Name,
                           Description = cartDetail.ActivityProduct.Activity.Description,
                           Price = cartDetail.ActivityProduct.Price,
                           StartTime = cartDetail.ActivityProduct.StartTime,
                           EndTime = cartDetail.ActivityProduct.EndTime,
                           Quantity = cartDetail.Quantity,
                           ImageUrl = "/ActivityImages/"+cartDetail.ActivityProduct.Activity.Image,
                       })
                       .ToList()
                        };

                ViewData["CartId"] = cartIdFromCookie;
                ViewData["CartDetailsDto"] = cartDetailsDto;
                return View(cartDetailsDto);
            }

           
        }

        public IActionResult Info()
		{
			var customerAccount = User.Identity.Name;
			int memberId = GetMemberIdByAccount(customerAccount);
			Cart cart = GetCartInfo(memberId);
			return View(cart);
		}

        //public IActionResult Add2Cart(int extraserviceId, string customerAccount)
        //{
        //    var memberId = GetMemberIdByAccount(customerAccount);
        //    var cart = _context.Carts.SingleOrDefault(c=>c.MemberId == memberId);
        //    if (cart == null)
        //    {
        //        cart = new Cart { MemberId = memberId };
        //        _context.Carts.Add(cart);
        //        _context.SaveChanges();
        //    }
        //    ViewBag.cartid = cart.Id;


        //    return Json(new { success = true, message = "已加入購物車", cartId = cart.Id });

        //}
        [HttpGet]
        public IActionResult ExtraServicesDetailsPartialView(int cartId)
        {
            var extraServicesDetails = _context.Cart_ExtraServicesDetails
                   .Where(c => c.CartId == cartId)
                   .Include(c => c.ExtraServiceProduct) // Load the ExtraServiceProduct
                   .Include(c => c.ExtraServiceProduct.ExtraService) // Load the ExtraService within ExtraServiceProduct
                   .ToList();

            //List<Cart_ExtraServiceDetailsVM> viewModelList = extraServicesDetails.Select(cartExtraServiceDetail => new Cart_ExtraServiceDetailsVM
            //{
            //    Id = cartExtraServiceDetail.Id,
            //    CartId = cartExtraServiceDetail.CartId,
            //    ExtraServiceProductId = cartExtraServiceDetail.ExtraServiceProductId,
            //    Quantity = cartExtraServiceDetail.Quantity,
            //    ExtraServicePrice= cartExtraServiceDetail.ExtraServiceProduct.Price,
            //    ExtraServiceName = cartExtraServiceDetail.ExtraServiceProduct.ExtraService.Name // Assuming you have ExtraServiceName property in Cart_ExtraServicesDetail entity   
            //}).ToList();

            return ViewComponent("CartPartial");
        }
        public List<Cart_ExtraServicesDetail> GetCartExtraServicesDetails(int memberId)
		{


			// 使用 EF Core 的資料庫上下文查詢 Cart_ExtraServicesDetails 資料
			var extraServicesDetails = _context.Cart_ExtraServicesDetails
				.Where(c => c.Cart.MemberId == memberId)
				.ToList();

			return extraServicesDetails;
		}
        [HttpPost]
        public IActionResult AddExtraService2Cart([FromBody] ExtraserviceProductsDto dto)
        {
            try
            {
                var extraServiceProduct = _context.ExtraServiceProducts
                 .Where(e => dto.extraserviceId.Contains(e.Id));

                if (extraServiceProduct != null)
                {
                    var cartIdFromCookie = Convert.ToInt32(HttpContext.Request.Cookies["CartId"] ?? "0");
                    foreach(var ep in extraServiceProduct){

                        var cartItem = new Cart_ExtraServicesDetail
                        {
                            CartId = cartIdFromCookie,
                            ExtraServiceProductId = ep.ExtraServiceId,
                            Quantity = dto.quantity
                        };
                        _context.Cart_ExtraServicesDetails.Add(cartItem);
                    }
                   

                   
                    _context.SaveChanges();
                    Response.Cookies.Append("CartId", cartIdFromCookie.ToString());
                    return ViewComponent("CartPartial");
                }
                else
                {
                    return Json(new { success = false, message = "Product not found." });
                }
            }
            catch
            {
                // 加入購物車失敗時回傳 JSON 物件
                return Json(new { success = false, message = "Failed to add to cart." });
            }
        }
        public IActionResult RemoveExtraServiceFromCart(int extraserviceId)
        {
            try
            {
                var cartIdFromCookie = Convert.ToInt32(HttpContext.Request.Cookies["CartId"] ?? "0");
                var cartItem = _context.Cart_ExtraServicesDetails.FirstOrDefault(p => p.CartId == cartIdFromCookie && p.Id == extraserviceId);
                if (cartItem != null)
                {
                    _context.Cart_ExtraServicesDetails.Remove(cartItem);
                    _context.SaveChanges();

                    return Json(new { success = true, message = "已成功刪除!", extraserviceId = extraserviceId });
                }
                else
                {
                    return Json(new { success = false, message = "Item not found in cart." });
                }
            }
            catch
            {
                return Json(new { success = false, message = "Failed to remove from the cart." });
            }

        }


        public IActionResult ActivitiesDetailsPartialView(int memberId)
        {
            var activitiesServicesDetails = _context.Cart_ActivitiesDetails
                .Include(c => c.ActivityProduct)
                .Include(c => c.ActivityProduct.Activity)
                .ToList();
            return PartialView("ActivitiesDetailsPartialView");
        }
        public List<Cart_ActivitiesDetail>GetCartActivities(int memberId)
        {
            var activitiesDetails= _context.Cart_ActivitiesDetails
                .Where(c=>c.Cart.MemberId==memberId)
                .ToList() ;
            return activitiesDetails;
        }

        public IActionResult AccommodationDetailsPartialView(int memberId)
        {
            var accommodationDetails = _context.Cart_AccommodationDetails
                .Include(c => c.RoomProduct)
                .Include(c => c.RoomProduct.Room.Accommodation)
                .ToList();
            return PartialView("AccommodationDetailsPartialView");
        }
        public List<Cart_AccommodationDetail>GetCartAccommodationDetails(int memberId)
        {
            var accomodationDetails = _context.Cart_AccommodationDetails
                .Where(c => c.CartId == memberId)
                .ToList();
            return accomodationDetails;
        }

        [HttpPost]
        public IActionResult AddAccommodation2Cart([FromBody] RoomProductsDto dto)
        {
            try
            {
                var RoomProduct = _context.RoomProducts.Where(r=> dto.RoomProductId.Contains(r.Id));
                if(RoomProduct != null)
                {
                    var cartIdFromCookie = Convert.ToInt32(HttpContext.Request.Cookies["cartId"] ?? "0");
                     
                        foreach(var rp in RoomProduct)
                        {
                            var cartItem = new Cart_AccommodationDetail
                            {
                                CartId = cartIdFromCookie,
                                RoomProductId = rp.Id,
                                Quantity = dto.Quantity
                            };
                            _context.Cart_AccommodationDetails.Add(cartItem);
                        }
                        _context.SaveChanges();
                        Response.Cookies.Append("cartId", cartIdFromCookie.ToString());

                    return ViewComponent("CartPartial");
                }
                else
                {
                    return Json(new { success = false, message = "Product not found." });
                }
            }
            catch(Exception ex)
            {
                return Json(new { success = false, message = "Failed to add to cart."+ex });
            }
        }

        [HttpPost]
        public IActionResult RemoveAccommodationFromCart(int roomProductId)
        {
            try
            {
                var cartIdFromCookie = Convert.ToInt32(HttpContext.Request.Cookies["cartId"] ?? "0");
                var cartItem=_context.Cart_AccommodationDetails.FirstOrDefault(x=>x.CartId==cartIdFromCookie&& x.RoomProductId == roomProductId);
                if (cartItem != null)
                {
                    _context.Cart_AccommodationDetails.Remove(cartItem);
                    _context.SaveChanges();
                    return Json(new {success=true, message="已成功刪除!", roomProductId = roomProductId });
                }
                else
                {
                    return Json(new { success = false, message = "Item not found in Cart." });
                }
            }
            catch
            {
                return Json(new { success = false, message = "Failed to remove from the cart." });
            }
        }
        public IActionResult AddActivitiesDetail2Cart(int activitiesId, int quantity)
        {
            try
            {
                var activitiesProduct = _context.ActivityProducts.FirstOrDefault(p => p.Id == activitiesId);
                if (activitiesProduct != null)
                {
                    var cartIdFromCookie = Convert.ToInt32(HttpContext.Request.Cookies["cartId"] ?? "0");
                    var cartItem = new Cart_ActivitiesDetail
                    {
                        CartId = cartIdFromCookie,
                        ActivityProductId = activitiesProduct.Id,
                        Quantity = quantity
                    };
                    _context.Cart_ActivitiesDetails.Add(cartItem);
                    _context.SaveChanges();

                    Response.Cookies.Append("cartId", cartIdFromCookie.ToString());
                    return Json(new { success = true, message = "Successfully added to cart." });
                }
                else
                {
                    return Json(new { succes = false, message = "Product not found." });
                }
            }
            catch
            {
                // 加入購物車失敗時回傳 JSON 物件
                return Json(new { success = false, message = "Failed to add to cart." });
            }

		
        }
      
        public IActionResult RemoveActivitiesFromCart(int activitiesId)
        {
            try
            {
                var cartIdFromCookie = Convert.ToInt32(HttpContext.Request.Cookies["cartId"] ?? "0");
                var cartItem = _context.Cart_ActivitiesDetails.FirstOrDefault(x=>x.CartId== cartIdFromCookie&& x.Id == activitiesId);
                if(cartItem != null)
                {
                    _context.Cart_ActivitiesDetails.Remove(cartItem);
                    _context.SaveChanges();

                    return Json(new {success=true, message="已成功刪除!", activitiesId=activitiesId});
                }
                else
                {
                    return Json(new { sucess = false, message = "Item not found in cart." });
                }
            }
            catch
            {
                return Json(new { success = false, message = "Failed to remove from the cart." });
            }
        }

        [HttpGet]
        public IActionResult RefreshCart()
        {
           

            //ViewData["CartId"] = _context.Carts.Where(s => s.MemberId == memberId).First().Id;
            return ViewComponent("CartPartial");
        }

        //public IActionResult Checkout()
        //{
        //    var memberId = _context.Members.FirstOrDefault(m => m.Account == User.Identity.Name).Id;
        //    var cart = GetCartInfo(memberId);

        //    if (cart.AllowCheckout == false) ViewBag.ErrorMessage = "購物車是空的,無法進行結帳";

        //    return View();
        //}
        [HttpGet]
        public ActionResult Checkout(int? id)
        {
            var memberId = _context.Members.FirstOrDefault(m => m.Account == User.Identity.Name)?.Id;
            if (memberId == null)
            {
                
                return RedirectToAction("Index", "Home"); 
            }

            var cart = GetCartInfo(memberId.Value);

            bool allowCheckout = cart.Cart_ExtraServicesDetails.Any() || cart.Cart_ActivitiesDetails.Any() || cart.Cart_AccommodationDetails.Any();

            if (!allowCheckout)
            {
                ViewBag.ErrorMessage = "購物車是空的，無法進行結帳";
            }
            var member = _context.Members.FirstOrDefault(m => m.Id == memberId.Value);

            if (member != null)
            {
                ViewBag.MemberFirstName = member.FirstName;
                ViewBag.MemberLastName = member.LastName;
                ViewBag.CellPhoneNumber = member.CellPhoneNumber;
                ViewBag.Address = member.Address;
            }

            ViewBag.PaymentMethods = _context.PaymentMethods.ToList();
            ViewBag.CouponsId = id;
            List<Cart> cartList = new List<Cart> { cart };

            return View(cartList);
        }

     
        [HttpPost]
        public ActionResult CheckoutPost(string? paymentmethod, string? name, string? telephone, string? address, string?note,int? CouponsId)
        {
            var member = _context.Members.FirstOrDefault(m => m.Account == User.Identity.Name);
            if (member == null)
            {
                return RedirectToAction("Index", "Home");
            }
            int memberId = member.Id;
            var cart = GetCartInfo(memberId);
            bool allowCheckout = cart.Cart_ExtraServicesDetails.Any() || cart.Cart_ActivitiesDetails.Any() || cart.Cart_AccommodationDetails.Any();

            if (!allowCheckout)
            {
                ModelState.AddModelError(string.Empty, "購物車是空的，無法進行結帳");
                return View("Checkout");
            }
            //ProcessCheckout(memberId, CouponsId, note);
           
            return RedirectToAction("Index", "Ecpay", new { memberId = memberId, selectedCouponId = CouponsId });
          
        }

        public ActionResult PayInfo(int memberId, int selectedCouponId)
        {
            ProcessCheckout(memberId, selectedCouponId, null);
            return RedirectToAction("ConfirmPayment", "Carts");

        }

        private void ProcessCheckout(int memberId, int?CouponsId, string?note)
        {
            var cart = GetCartInfo(memberId);

            bool allowCheckout = cart.Cart_ExtraServicesDetails.Any() ||
                                 cart.Cart_ActivitiesDetails.Any() ||
                                 cart.Cart_AccommodationDetails.Any();

            if (!allowCheckout)
            {
                return;
            }
            CreateOrder(memberId, CouponsId, note);
            EmptyCart(memberId);

            TempData["MemberIdForEmptyCart"] = memberId;
        }


        private void CreateOrder(int memberId, int? CouponsId, string note)
        {
            var cart = GetCartInfo(memberId);

            bool allowCheckout = cart.Cart_ExtraServicesDetails.Any() ||
                                 cart.Cart_ActivitiesDetails.Any() ||
                                 cart.Cart_AccommodationDetails.Any();

            if (!allowCheckout)
            {
                return;
            }

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var cartTotal = CalculateCartTotal(cart);

                    var order = new Order
                    {
                        MemberId = memberId,
                        PaymentMethodId = 1,
                        PaymentStatusId = 2,
                        OrderHandleStatusId = 1,
                        CouponsId = (int)(CouponsId == 0 ? 1 : CouponsId),
                        CreateDate = DateTime.Now,
                        ModifiedDate = DateTime.Now,
                        Total = cartTotal
                    };

                    _context.Orders.Add(order);
                    _context.SaveChanges();

                    foreach (var accommodationItem in cart.Cart_AccommodationDetails)
                    {
                        var roomId = accommodationItem.RoomProductId;
                        var roomProduct = _context.RoomProducts.Include(x => x.Room).FirstOrDefault(x => x.Id == roomId);
                        if (roomProduct != null)
                        {
                            var accommodationId = roomProduct.Room.AccommodationId;
                            var accommodation = _context.Accommodations.Find(accommodationId);
                            var roomType = _context.RoomTypes.Where(x => x.Id == roomProduct.Room.RoomTypeId).Select(x => x.Name).FirstOrDefault();
                            var roomName = roomProduct.Room.Name;
                            var roomPrice = roomProduct.NewPrice;
                            var quantity = accommodationItem.Quantity;

                            var accommodationDetail = new OrderAccommodationDetail
                            {
                                OrderId = order.Id,
                                AccommodationId = accommodationId,
                                AccommodationName = accommodation.Name,
                                RoomProductId = roomProduct.Id,
                                RoomType = roomType,
                                RoomName = roomName,
                                CheckIn = roomProduct.Date.Date + accommodation.CheckIn,
                                CheckOut = roomProduct.Date.AddDays(1).Date + accommodation.CheckOut,
                                Quantity = quantity,
                                RoomPrice = (int)(roomPrice * quantity),
                                Note = note
                            };

                            _context.OrderAccommodationDetails.Add(accommodationDetail);
                            roomProduct.Quantity -= quantity;
                        }
                    }

                    foreach (var extraserviceItem in cart.Cart_ExtraServicesDetails)
                    {
                        var extraserviceProductId = extraserviceItem.ExtraServiceProductId;
                        var extraserviceProduct = _context.ExtraServiceProducts.Include(x => x.ExtraService).FirstOrDefault(x => x.Id == extraserviceProductId);

                        if (extraserviceProduct.ExtraService != null)
                        {
                            var extraServicesId = extraserviceProduct.ExtraServiceId;
                            var extraServiceName = extraserviceProduct.ExtraService.Name;
                            var date = extraserviceProduct.Date;
                            var price = extraserviceProduct.Price;
                            var quantity = extraserviceItem.Quantity;

                            var extraserviceDetails = new OrderExtraServicesDetail
                            {
                                OrderId = order.Id,
                                ExtraServiceId = extraServicesId,
                                ExtraServiceName = extraServiceName,
                                ExtraServiceProductId = extraserviceItem.ExtraServiceProductId,
                                Date = date,
                                Price = price * quantity,
                                Quantity = quantity
                            };

                            _context.OrderExtraServicesDetails.Add(extraserviceDetails);
                            extraserviceProduct.Quantity -= quantity;
                        }
                    }

                    foreach (var activitiesDetail in cart.Cart_ActivitiesDetails)
                    {
                        var activitiesproductsId = activitiesDetail.ActivityProductId;
                        var activitiesproducts = _context.ActivityProducts.Include(x => x.Activity).FirstOrDefault(x => x.Id == activitiesproductsId);

                        if (activitiesproducts != null)
                        {
                            var activityId = activitiesproducts.ActivityId;
                            var activitiesName = activitiesproducts.Activity.Name;
                            var date = activitiesproducts.Date;
                            var startTime = activitiesproducts.StartTime;
                            var endTime = activitiesproducts.EndTime;
                            var price = activitiesproducts.Price;
                            var quantity = activitiesDetail.Quantity;

                            var activitiesDetails = new OrderActivitiesDetail
                            {
                                OrderId = order.Id,
                                ActivityId = activityId,
                                ActivityName = activitiesName,
                                ActivityProductId = activitiesDetail.ActivityProductId,
                                Date = date,
                                StartTime = startTime,
                                EndTime = endTime,
                                Price = price * quantity,
                                Quantity = quantity
                            };

                            _context.OrderActivitiesDetails.Add(activitiesDetails);
                            activitiesproducts.Quantity -= quantity;
                        }
                    }

                    
                    _context.SaveChanges();
                    transaction.Commit();

                }
                catch (DbUpdateException ex)
                {
                    var innerException = ex.InnerException;
                    transaction.Rollback();
                }
            }
        }

      
        private int CalculateCartTotal(Cart cart)
        {
            int total = 0;
            foreach (var accommodationItem in cart.Cart_AccommodationDetails)
            {
                var roomId = accommodationItem.RoomProductId;
                var rp = _context.RoomProducts.FirstOrDefault(x => x.Id == roomId);

                if (rp != null)
                {
                    int roomTotal = (int)rp.NewPrice * accommodationItem.Quantity;
                    total += roomTotal;
                }
            }
            foreach (var extraserviceItem in cart.Cart_ExtraServicesDetails)
            {

                var extraserviceProductId = extraserviceItem.ExtraServiceProductId;
                var extraserviceProduct = _context.ExtraServiceProducts.FirstOrDefault(x => x.Id == extraserviceProductId);

                if (extraserviceProduct != null)
                {
                    int extraserviceTotal = extraserviceProduct.Price * extraserviceItem.Quantity;
                    total += extraserviceTotal;
                }
            }
            foreach (var activityItem in cart.Cart_ActivitiesDetails)
            {
                var activityProductId = activityItem.ActivityProductId;
                var activityProduct = _context.ActivityProducts.FirstOrDefault(x => x.Id == activityProductId);

                if (activityProduct != null)
                {
                    int activityTotal = activityProduct.Price * activityItem.Quantity;
                    total += activityTotal;
                }
            }

            return total;
        }

   

        


        private void EmptyCart(int memberId)
        {
            var cart = _context.Carts.FirstOrDefault(c => c.MemberId == memberId);
            if (cart == null) return;


            var toBeDeletedAcco = _context.Cart_AccommodationDetails.Where(x => x.CartId == cart.Id);
            _context.Cart_AccommodationDetails.RemoveRange(toBeDeletedAcco);
            _context.SaveChanges();

            var toBeDeleteEXT = _context.Cart_ExtraServicesDetails.Where(x=>x.CartId== cart.Id);
            _context.Cart_ExtraServicesDetails.RemoveRange(toBeDeleteEXT);
            _context.SaveChanges();

            var toBeDeleteAct = _context.Cart_ActivitiesDetails.Where(x=>x.CartId==cart.Id);
            _context.Cart_ActivitiesDetails.RemoveRange(toBeDeleteAct);
            _context.SaveChanges();




            _context.Carts.Remove(cart);
            _context.SaveChanges();
        }
   
        
        private int GetMemberIdByAccount(string Account)
		{

            if (!User.Identity.IsAuthenticated)
            {
              
                return -1;
            }
            var member = _context.Members
				.Where(m => m.Account == Account)
				.FirstOrDefault();
			if (member != null)
			{
				return member.Id;
			}
			else
			{
				return -1;
			}
		}
        private int GetCartId()
        {
            var userClaims = HttpContext.User.Claims;
            var userAccountClaim = userClaims.FirstOrDefault(c => c.Type == ClaimTypes.Name);

            if (userAccountClaim != null)
            {
                string userAccount = userAccountClaim.Value;

                var memberId = GetMemberIdByAccount(userAccount);
                var cart = GetCartInfo(memberId);

                return cart.Id;
            }

          
            return -1;
        }

        public Cart GetCartInfo(int memberId)
        {
            var cart = _context.Carts
                .Include(c => c.Cart_ExtraServicesDetails)
                .Include(c => c.Cart_ActivitiesDetails)
                .Include(c => c.Cart_AccommodationDetails)
                .Where(c => c.MemberId == memberId)
                .FirstOrDefault();
            if (cart == null)
            {
                cart = new Cart { MemberId = memberId };
                _context.Carts.Add(cart);
                _context.SaveChanges();
            }
            return cart;

        }
        // GET: Carts/Details/5

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Carts == null)
            {
                return NotFound();
            }

            var cart = await _context.Carts
                .Include(c => c.Member)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (cart == null)
            {
                return NotFound();
            }

            return View(cart);
        }

        // GET: Carts/Create
        public IActionResult Create()
        {
            ViewData["MemberId"] = new SelectList(_context.Members, "Id", "Account");
            return View();
        }

        // POST: Carts/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,MemberId")] Cart cart)
        {
            if (ModelState.IsValid)
            {
                _context.Add(cart);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["MemberId"] = new SelectList(_context.Members, "Id", "Account", cart.MemberId);
            return View(cart);
        }

        // GET: Carts/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Carts == null)
            {
                return NotFound();
            }

            var cart = await _context.Carts.FindAsync(id);
            if (cart == null)
            {
                return NotFound();
            }
            ViewData["MemberId"] = new SelectList(_context.Members, "Id", "Account", cart.MemberId);
            return View(cart);
        }

        // POST: Carts/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,MemberId")] Cart cart)
        {
            if (id != cart.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(cart);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CartExists(cart.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["MemberId"] = new SelectList(_context.Members, "Id", "Account", cart.MemberId);
            return View(cart);
        }

      

        private bool CartExists(int id)
        {
          return (_context.Carts?.Any(e => e.Id == id)).GetValueOrDefault();
        }

        [HttpPost]
        public IActionResult UpdateQuantity(int extraServiceId, int accommodationId, int activityId, int quantity)
        {
            try
            {
                var extraServiceProduct = _context.ExtraServiceProducts.FirstOrDefault(e => e.Id == extraServiceId);
                var accomodationProduct =_context.RoomProducts.FirstOrDefault(r => r.Id == accommodationId);
                var activityProduct = _context.ActivityProducts.FirstOrDefault(a => a.Id == activityId);

                if (extraServiceProduct != null)
                {
                    extraServiceProduct.Quantity = quantity;
                }
                if (accomodationProduct != null)
                {
                    accomodationProduct.Quantity = quantity;
                }
                if (activityProduct != null)
                {
                    activityProduct.Quantity = quantity;
                }
                _context.SaveChanges();

                return Json(new { success = true, message = "數量已更新。" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "更新數量時出現錯誤：" + ex.Message });
            }
        }
        public async Task<ActionResult> ShowPaymentResult(int memberId,int selectedCouponId)
        {
            var postData = Request.Form;

            if (postData.Count > 0)
            {
                var resultStatus = postData["RtnCode"];
                var resultMessage = postData["RtnMsg"];
                if (resultStatus == "1")
                {
                    ProcessCheckout(memberId, selectedCouponId, null);
                    //await BookingHub.Instance.SendBookingMessage();

                    BookingHub.Instance.SendClient("10000");
                    return Redirect("https://localhost:7145/Carts/ConfirmPayment");
                }
                else
                {

                    TempData["ErrorMessage"] = resultMessage;
                    return Redirect("https://localhost:7145/Carts/Index");
                }
            }


            return Redirect("https://localhost:7145/Carts/Index");
        }
        [HttpPost]
        public ActionResult HandleReturnURL()
        {

            var postData = Request.Form;


            if (postData.Count > 0)
            {

                var resultStatus = postData["RtnCode"];
                var resultMessage = postData["RtnMsg"];

                if (resultStatus == "1")
                {

                    return Content("1|OK");
                }
                else
                {

                    return Content(resultStatus + "|" + resultMessage);
                }
            }


            return Content("0|Error");
        }

        public IActionResult ReloadCartItemsWithoutCondition()
        {
            return ViewComponent("CartPartial");
        }

        public IActionResult ConfirmPayment()
        {
            return View();
        }
    }
   
}
