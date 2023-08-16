﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using RouteMasterFrontend.EFModels;

namespace RouteMasterFrontend.Controllers
{
    public class CouponsController : Controller
    {
        private RouteMasterContext _db;

        public CouponsController()
        {
            _db = new RouteMasterContext();

        }

        public async Task<ContentResult> GetAllCoupons()
        {
            var now = DateTime.Now;

            List<Coupon> coupons = await _db.Coupons.Select(c => new Coupon
            {
                Id = c.Id,
                Code = c.Code,
                Discount = c.Discount,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                IsActive = c.IsActive,
            }).ToListAsync();

            foreach(var coupon in coupons)
            {
                if (coupon.StartDate < now && now < coupon.EndDate)
                {
                    coupon.valuable = true;
                }
                else
                {
                    coupon.valuable = false;
                }
            }

            return Content(JsonConvert.SerializeObject(coupons), "application/json");
        }
    }

    public class Coupon
    {
        public int Id { get; set; }
        public int Code { get; set; }
        public int Discount { get; set; }
        public DateTime StartDate { get; set; }

        public string StartDateText
        {
            get
            {
                return StartDate.ToString("yyyy/MM/dd");
            }
        }
        public DateTime EndDate { get; set; }

        public string EndDateText
        {
            get
            {
                return EndDate.ToString("yyyy/MM/dd");
            }
        }
        public bool IsActive { get; set; }
        public bool? valuable { get; set; }

        public bool used { get; set; } = false;
    }
}
