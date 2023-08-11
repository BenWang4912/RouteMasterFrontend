﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using RouteMasterFrontend.Models.Infra.EFRepositories;
using RouteMasterFrontend.Models.Infra.ExtenSions;
using RouteMasterFrontend.Models.Interfaces;
using RouteMasterFrontend.Models.Services;
using RouteMasterFrontend.Models.ViewModels.AttractionVMs;
using RouteMasterFrontend.Models.ViewModels.Members;
using System.Drawing.Printing;

namespace RouteMasterFrontend.Controllers
{
    public class AttractionsController : Controller
    {
        public IActionResult Index(AttractionCriteria criteria, int page = 1)
        {
            IEnumerable<AttractionIndexVM> attractions = GetAttractions();

            ViewBag.Categories = attractions.Select(a => a.AttractionCategory).Distinct().ToList();
            ViewBag.Tags = attractions.SelectMany(a => a.Tags).Distinct().ToList();
            ViewBag.Regions = attractions.Select(a => a.Region).Distinct().ToList();

            ViewBag.Criteria = criteria;

            #region where
            if (!string.IsNullOrEmpty(criteria.Keyword))
            {
                attractions = attractions.Where(a => a.Name.Contains(criteria.Keyword));
            }
            if (criteria.category != null)
            {
                attractions = attractions.Where(a => criteria.category.Contains(a.AttractionCategory));
            }
            if (criteria.tag != null)
            {
                attractions = attractions.Where(a => a.Tags.Intersect(criteria.tag).Any());
            }
            if (criteria.region != null)
            {
                attractions = attractions.Where(a => criteria.region.Contains(a.Region));
            }
            if (criteria.order == "click")
            {
                attractions = attractions.OrderByDescending(a => a.Clicks);
            }
            if (criteria.order == "clickInThirty")
            {
                attractions = attractions
                    .Where(a => a.ClicksInThirty > 0)
                    .OrderByDescending(a => a.ClicksInThirty);
            }
            if (criteria.order == "score")
            {
                attractions = attractions.OrderByDescending(a => a.Score);
            }
            if (criteria.order == "hours")
            {
                attractions = attractions.OrderBy(a => a.Hours);
            }
            if (criteria.order == "hoursDesc")
            {
                attractions = attractions.OrderByDescending(a => a.Hours);
            }
            if (criteria.order == "price")
            {
                attractions = attractions.OrderBy(a => a.Price);
            }
            if (criteria.order == "priceDesc")
            {
                attractions = attractions.OrderByDescending(a => a.Price);
            }
            if (criteria.order == "")
            {
                attractions = attractions.OrderByDescending(a => a.Id);
            }

            

            #endregion

            ViewBag.Count = attractions.ToList().Count();
            int pageSize = 15;

            int totalItems = attractions.Count();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            attractions = attractions.Skip((page - 1) * pageSize).Take(pageSize);

            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = totalPages;

            return View(attractions);
        }

        public IActionResult Details(int id)
        {
            AddClick(id);
            AttractionDetailVM vm = Get(id);

            
            // 在這裡檢查景點是否已加入最愛，並將結果傳遞到視圖
            bool isFavorite = CheckIfFavorite(id); // 需要實現這個方法來檢查是否已加入最愛
            
                

            ViewBag.IsFavorite = isFavorite; // 將結果傳遞到視圖中

            return View(vm);
        }

        public IActionResult Details2(int id)
        {
            AddClick(id);
            AttractionDetailVM vm = Get(id);


            // 在這裡檢查景點是否已加入最愛，並將結果傳遞到視圖
            bool isFavorite = CheckIfFavorite(id); // 需要實現這個方法來檢查是否已加入最愛



            ViewBag.IsFavorite = isFavorite; // 將結果傳遞到視圖中

            return View(vm);
        }

        private bool CheckIfFavorite(int id)
        {
            if (User.Identity.IsAuthenticated)
            {
                var customerAccount = User.Identity.Name;
                return GetFavoriteAtt(customerAccount).Select(a=>a.Id).Contains(id);
            }
            else
            {
                return false;
            }
            
        }

        public IActionResult AddToFavorite (int id)
        {
            if (User.Identity.IsAuthenticated)
            {
                var customerAccount = User.Identity.Name;
                Add2Favorite(customerAccount, id);

                return Json(new { success = true });
            }
            else
            {
                return Json(new { success = false });
            }
        }

        public IActionResult RemoveFromFavorite(int id)
        {
            if (User.Identity.IsAuthenticated)
            {
                var customerAccount = User.Identity.Name;
                RemoveAttFromFavorite(customerAccount, id);

                return Json(new { success = true });
            }
            else
            {
                return Json(new { success = false });
            }
        }

        private void RemoveAttFromFavorite(string? customerAccount, int id)
        {
            IAttractionRepository repo = new AttractionEFRepository();
            AttractionService service = new AttractionService(repo);

            service.RemoveAttFromFavorite(customerAccount, id);
        }

        [Authorize]
        public IActionResult FavoriteAtt(int page = 1)
        {
            var customerAccount = User.Identity.Name;
            IEnumerable<AttractionIndexVM> attractions = GetFavoriteAtt(customerAccount);

            int pageSize = 15;

            int totalItems = attractions.Count();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            attractions = attractions.Skip((page - 1) * pageSize).Take(pageSize);

            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = totalPages;

            return View(attractions);
        }

        private IEnumerable<AttractionIndexVM> GetFavoriteAtt(string? customerAccount)
        {
            IAttractionRepository repo = new AttractionEFRepository();
            AttractionService service = new AttractionService(repo);

            return service.GetFavoriteAtt(customerAccount).Select(dto => dto.ToIndexVM());
        }

        private void Add2Favorite(string? customerAccount, int id)
        {
            IAttractionRepository repo = new AttractionEFRepository();
            AttractionService service = new AttractionService(repo);

            service.AddToFarvorite(customerAccount, id);
        }

        private void AddClick(int id)
        {
            IAttractionRepository repo = new AttractionEFRepository();
            AttractionService service = new AttractionService(repo);

            service.AddClick(id);
        }

        private AttractionDetailVM Get(int id)
        {
            IAttractionRepository repo = new AttractionEFRepository();
            AttractionService service = new AttractionService(repo);

            return service.Get(id).ToDetailVM();
        }

        private IEnumerable<AttractionIndexVM> GetAttractions()
        {
            IAttractionRepository repo = new AttractionEFRepository();
            AttractionService service = new AttractionService(repo);

            return service.Search().Select(dto => dto.ToIndexVM());
        }
    }
}
