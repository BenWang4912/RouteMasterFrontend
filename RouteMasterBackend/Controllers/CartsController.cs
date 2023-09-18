
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RouteMasterBackend.DTOs;
using RouteMasterBackend.Models;
using RouteMasterFrontend.EFModels;
using RouteMasterFrontend.Models.Dto;

namespace RouteMasterBackend.Controllers
{
    [EnableCors("AllowAny")]
    [Route("api/[controller]")]
    [ApiController]
    public class CartsController : ControllerBase
    {
        private readonly Models.RouteMasterContext _context;

        public CartsController(Models.RouteMasterContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<CartItemsDto> LoadCartItems(int cartId, int quantity)
        {


            var data = new CartItemsDto();
            data.cartId= cartId;
            data.ActivityCartItems = await _context.CartActivitiesDetails
                    .Where(x => x.CartId == cartId)
                    .Select(x => new ActivityCartItem
                    {
                        imgUrl = x.ActivityProduct.Activity.Image,
                        Name = x.ActivityProduct.Activity.Name,
                        Description = x.ActivityProduct.Activity.Description,
                        startTime = x.ActivityProduct.StartTime,
                        endTime = x.ActivityProduct.EndTime,
                        Price = x.ActivityProduct.Price,
                        Quantity = quantity
                    })
                    .ToListAsync();
            data.AccommodationCartItems = await _context.CartAccommodationDetails
                 .Where(x => x.CartId == cartId)
                 .Select(x => new AccommodationCartItem
                 {
                     imgUrl = "123",
                     AccommodationName = x.RoomProduct.Room.Accommodation.Name,
                     RoomTypeName = x.RoomProduct.Room.RoomType.Name,
                     Date = x.RoomProduct.Date,
                     Quantity = quantity,
                     Price = x.RoomProduct.NewPrice,

                 })
                 .ToListAsync();

            data.ExtraServiceCartItems = await _context.CartExtraServicesDetails
                .Where(x => x.CartId == cartId)
                .Select(x => new ExtraServiceCartItem
                {
                    Name = x.ExtraServiceProduct.ExtraService.Name,
                    ImgUrl = x.ExtraServiceProduct.ExtraService.Image,
                    Description = x.ExtraServiceProduct.ExtraService.Description,
                    Date = x.ExtraServiceProduct.Date,
                    Price = x.ExtraServiceProduct.Price,
                    Quantity = quantity,
                })
                .ToListAsync();
        
            return data;
           
        }


        [HttpGet("getcartview")]
       
        [HttpPost("addextraservice")]
        public IActionResult AddExtraService2Cart([FromBody]AddExtraServiceDto dto)
        {
            try
            {
                var extraServiceProduct = _context.ExtraServiceProducts
                .FirstOrDefault(p => p.Id == dto.extraserviceId);

                if (extraServiceProduct.Quantity <= 0 || extraServiceProduct.Quantity < dto.quantity)
                {
                    return BadRequest(new { success = false, message = "此商品已售完" });
                }
                var existingCartItem = _context.CartExtraServicesDetails
                .FirstOrDefault(c => c.CartId == dto.cartId && c.ExtraServiceProductId == dto.extraserviceId);

                if (existingCartItem != null)
                {

                    existingCartItem.Quantity += dto.quantity;
                }
                else
                {

                    var cartItem = new CartExtraServicesDetail
                    {
                        CartId = dto.cartId,
                        ExtraServiceProductId = dto.extraserviceId,
                        Quantity = dto.quantity,
                    };



                    _context.CartExtraServicesDetails.Add(cartItem);
                }
                    _context.SaveChanges();
                    
                return Ok(new { success = true, message = "已成功加入購物車!" });
            }

            catch(Exception ex)
            {
               
                return BadRequest(new { success = false, message = "Failed to add to cart.", error = ex.Message });
            }
        }
        [HttpPost("removeextraservice")]
        public IActionResult RemoveExtraServiceFromCart([FromBody]AddExtraServiceDto dto)
        {
            try
            {
                    Console.WriteLine($"Received request with extraserviceId: {dto.extraserviceId}");
                var existingCartItem = _context.CartExtraServicesDetails.FirstOrDefault(c => c.CartId == dto.cartId && c.ExtraServiceProductId == dto.extraserviceId);
                if(existingCartItem != null)
                {
                    //existingCartItem.Quantity -= dto.quantity;
                    _context.CartExtraServicesDetails.Remove(existingCartItem);
                    _context.SaveChanges();
                    return Ok(new { success = true, message = "已成功刪除!" });
                }
                else
                {
                    return BadRequest(new { success = false, message = "Item not found in cart." });
                }
            }catch{
            
            return BadRequest(new { success = false, message = "Failed to remove from cart." });}
        }
        [HttpPost("addactivity")]
        public IActionResult AddActivitiesDetail2Cart([FromBody]AddActivityDto dto)
        {
            try
            {
                var activitiesProduct = _context.ActivityProducts.FirstOrDefault(p => p.Id == dto.activityId);

                if (activitiesProduct.Quantity <= 0 || activitiesProduct.Quantity < dto.quantity)
                {
                    return BadRequest(new { success = false, message = "此商品已售完" });
                }
                var activitiesCartItems = _context.CartActivitiesDetails.FirstOrDefault(c => c.CartId == dto.cartId && c.ActivityProductId == dto.activityId);
                if(activitiesCartItems != null)
                {
                    activitiesCartItems.Quantity += dto.quantity;
                }
                else
                {
                    var cartItem = new CartActivitiesDetail
                    {
                        CartId = dto.cartId,
                        ActivityProductId = dto.activityId,
                        Quantity = dto.quantity,
                    };
                    _context.CartActivitiesDetails.Add(cartItem);
                 }           
                _context.SaveChanges();
             
                return Ok(new { success = true, message = "已成功加入購物車!" });
            }
            catch(Exception ex)
            {
                return BadRequest(new { success = false, message = "Failed to add to cart.", error = ex.Message });
            }
        }

        [HttpPost("addAccommodation")]
        public IActionResult AddAccommodation2Cart([FromBody]AddAccommodationDto dto)
        {
            try
            {
                var roomProduct = _context.RoomProducts.FirstOrDefault(p => p.Id == dto.roomproductId);
                if (roomProduct.Quantity <= 0 || roomProduct.Quantity < dto.quantity)
                {
                    return BadRequest(new { success = false, message = "此商品已售完" });
                }
                var roomProductCartItems = _context.CartAccommodationDetails.FirstOrDefault(c => c.CartId == dto.cartId && c.RoomProductId == dto.roomproductId);
                if(roomProductCartItems != null)
                {
                    roomProductCartItems.Quantity += dto.quantity;
                }
                else
                {
                    var cartItem = new CartAccommodationDetail
                    {
                        CartId = dto.cartId,
                        RoomProductId = dto.roomproductId,
                        Quantity = dto.quantity,
                    };
                    _context.CartAccommodationDetails.Add(cartItem);
                }          
                _context.SaveChanges();
                return Ok(new { success = true, message = "已成功加入購物車!" });

            }
            catch(Exception ex) 
            {
                return BadRequest(new { success = false, message = "Failed to add to cart.", error = ex.Message });
            }
        }

        [HttpPut("updateExtraServicequantity")]
        public IActionResult UpdateExtraServiceQuantity([FromBody] UpdateExtQuantityDto dto)
        {
            try
            {
              
                var cartItem = _context.CartExtraServicesDetails
                    .FirstOrDefault(item => item.CartId == dto.CartId && item.ExtraServiceProductId == dto.ExtraServiceProductId);

                if (cartItem == null)
                {
                    return NotFound(new { success = false, message = "Cart item not found." });
                }

                cartItem.Quantity = dto.Quantity;
                _context.SaveChanges();

                return Ok(new { success = true, message = "Successfully updated quantity." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = "Failed to update quantity.", error = ex.Message });
            }
        }
        [HttpPut("updateActivityquantity")]
        public IActionResult UpdateActivityQuantity([FromBody] UpdateActQuantityDto dto)
        {
            try
            {

                var cartItem = _context.CartActivitiesDetails
                    .FirstOrDefault(item => item.CartId == dto.CartId && item.ActivityProductId == dto.ActivityProductId);

                if (cartItem == null)
                {
                    return NotFound(new { success = false, message = "Cart item not found." });
                }

                cartItem.Quantity = dto.Quantity;
                _context.SaveChanges();

                return Ok(new { success = true, message = "Successfully updated quantity." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = "Failed to update quantity.", error = ex.Message });
            }
        }
        [HttpPut("updateAccommodationquantity")]
        public IActionResult UpdateAccommodationQuantity([FromBody] UpdateAccoQuantityDto dto)
        {
            try
            {

                var cartItem = _context.CartAccommodationDetails
                    .FirstOrDefault(item => item.CartId == dto.CartId && item.RoomProductId == dto.RoomProductId);

                if (cartItem == null)
                {
                    return NotFound(new { success = false, message = "Cart item not found." });
                }

                cartItem.Quantity = dto.Quantity;
                _context.SaveChanges();

                return Ok(new { success = true, message = "Successfully updated quantity." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = "Failed to update quantity.", error = ex.Message });
            }
        }




        [HttpPost("Post/Travel")]
        public void AddTravelItemToCart(TravelProductDto dto)
        {
              
            var cart = _context.Carts.Where(x => x.Id == dto.cartId).First();


            if(dto.activityProductIds!=null)
            {
				for (int i = 0; i < dto.activityProductIds.Length; i++)
				{
					var cartActivityDetails = new CartActivitiesDetail()
					{
						CartId = dto.cartId,
						ActivityProductId = dto.activityProductIds[i].actProductId,
						Quantity = dto.activityProductIds[i].quantity,
					};
					cart.CartActivitiesDetails.Add(cartActivityDetails);
					_context.SaveChanges();
				}

			}

            if(dto.extraServiceProductIds!=null)
            {
				for (int i = 0; i < dto.extraServiceProductIds.Length; i++)
				{
					var cartExtraServiceDetails = new CartExtraServicesDetail()
					{
						CartId = dto.cartId,
						ExtraServiceProductId = dto.extraServiceProductIds[i].extProductId,
						Quantity = dto.extraServiceProductIds[i].quantity,
                    };
					cart.CartExtraServicesDetails.Add(cartExtraServiceDetails);
					_context.SaveChanges();
				}
			}			

            if(dto.roomProducts!=null)
            {
				for (int i = 0; i < dto.roomProducts.Length; i++)
				{
                    for (int j = 0; j < dto.roomProducts[i].RoomProductId.Length; j++)
                    {
                        var cartAccommodationDetails = new CartAccommodationDetail()
                        {
                            CartId = dto.cartId,
                            RoomProductId = dto.roomProducts[i].RoomProductId[j],
                            Quantity = dto.roomProducts[i].Quantity
                        };
					        cart.CartAccommodationDetails.Add(cartAccommodationDetails);
					        _context.SaveChanges();
                    }
				}
			}			


        }


        [HttpPost("Post/PackageTour")]
        public void AddPackageItemToCart(PackageTourCartItemsDto dto)
        {
            var cart = _context.Carts.Where(x => x.Id == dto.cartId).First();
            if(dto.selectedActProductIdsWithQuantity!=null)
            {
               foreach(var item in dto.selectedActProductIdsWithQuantity)
                {
                    var cartActivityDetails = new CartActivitiesDetail()
                    {
                        CartId = dto.cartId,
                        ActivityProductId = item.id,
                        Quantity = item.quantity,
                    };
                    cart.CartActivitiesDetails.Add(cartActivityDetails);
                }
                _context.SaveChanges();
            }
         


            if(dto.selectedExtProductIdsWithQuantity!= null)
            {
               foreach(var item in dto.selectedExtProductIdsWithQuantity)
                {
                    var cartExtraServiceDetails = new CartExtraServicesDetail()
                    {
                        CartId=dto.cartId,
                        ExtraServiceProductId=item.id,
                        Quantity=item.quantity, 
                    };
                    cart.CartExtraServicesDetails.Add(cartExtraServiceDetails); 
                }
                _context.SaveChanges();
            }          
        }
    }
}
