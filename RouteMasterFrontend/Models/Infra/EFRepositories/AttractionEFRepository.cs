using Microsoft.EntityFrameworkCore;
using RouteMasterFrontend.EFModels;
using RouteMasterFrontend.Models.Dto;
using RouteMasterFrontend.Models.Interfaces;
using RouteMasterFrontend.Models.ViewModels.AttractionVMs;
using System;
using System.Linq;
using static System.Formats.Asn1.AsnWriter;


namespace RouteMasterFrontend.Models.Infra.EFRepositories
{
    public class AttractionEFRepository : IAttractionRepository
    {
        private RouteMasterContext _db;

        public AttractionEFRepository()
        {
            _db = new RouteMasterContext();

        }

        public async Task<List<AttractionMapVM>> GetAtts()
        {
            

            var query = await _db.Attractions
                .AsNoTracking()
                .Include(a => a.AttractionCategory)
                .Include(a => a.Region)
                .Include(a => a.Town)
                .Include(a => a.AttractionImages)
                .Include(a => a.Comments_Attractions)
                .Include(a => a.Tags)
                .Include(a => a.AttractionClicks)
                .ToListAsync();



            var result = query//.OrderByDescending(q => q.Id)
                .Select(q => new AttractionMapVM
                {
                    Id = q.Id,
                    AttractionCategory = q.AttractionCategory.Name,
                    Name = q.Name,
                    Region = q.Region.Name,
                    Town = q.Town.Name,
                    Image = q.AttractionImages
                        .Where(i => i.AttractionId == q.Id)
                        .Select(i => i.Image)
                        .FirstOrDefault() ?? string.Empty,
                    
                    Tags = q.Tags
                        .Select(t => t.Name)
                        .ToList(),
                    Score = Math.Round(_db.Comments_Attractions
                        .Where(c => c.AttractionId == q.Id)
                        .Select(c => c.Score)
                        .DefaultIfEmpty()
                        .Average(), 1),
                    ScoreCount = _db.Comments_Attractions
                        .Where(c => c.AttractionId == q.Id)
                        .Count(),
                    Hours = Math.Round(_db.Comments_Attractions
                        .Where(c => c.AttractionId == q.Id && c.StayHours != null)
                        .Select(c => c.StayHours.Value)
                        .DefaultIfEmpty()
                        .Average(), 1),
                    HoursCount = _db.Comments_Attractions
                        .Where(c => c.AttractionId == q.Id && c.StayHours != null)
                        .Count(),
                    Price = (int)Math.Round(_db.Comments_Attractions
                        .Where(c => c.AttractionId == q.Id && c.Price != null)
                        .Select(c => c.Price.Value)
                        .DefaultIfEmpty()
                        .Average(), 0),
                    PriceCount = _db.Comments_Attractions
                        .Where(c => c.AttractionId == q.Id && c.Price != null)
                        .Count(),
                    Clicks = q.AttractionClicks
                        .Where(a => a.AttractionId == q.Id)
                        .Count(),
                    PositionX = q.PositionX,
                    PositionY = q.PositionY,
                }).ToList();

            return result;
        }

        public IEnumerable<AttractionForDistsnceVM> GetAllWithXY()
        {
            var query = _db.Attractions
               .AsNoTracking()
               .ToList();

            var result = query
                .Select(q => new AttractionForDistsnceVM
                {
                    Id = q.Id,
                    Name = q.Name,
                    PosX = (decimal?)q.PositionX,
                    PosY = (decimal?)q.PositionY
                });

            return result;
        }

        public void AddClick(int id)
        {
            AttractionClick attractionClick = new AttractionClick
            {
                AttractionId = id,
            };

            _db.Add(attractionClick);
            _db.SaveChanges();
        }

        public AttractionDetailDto Get(int id)
        {
            var query = _db.Attractions
                .AsNoTracking()
                .Include(a => a.AttractionCategory)
                .Include(a => a.Region)
                .Include(a => a.Town)
                .Include(a => a.AttractionImages)
                .Include(a => a.Comments_Attractions)
                .Include(a => a.Tags)
                .Include(a => a.AttractionClicks)
                .ToList();

            var result = query
                .Where(q => q.Id == id)
                .Select(q => new AttractionDetailDto
                {
                    Id = q.Id,
                    AttractionCategory = q.AttractionCategory.Name,
                    Name = q.Name,
                    Region = q.Region.Name,
                    Town = q.Town.Name,
                    Images = q.AttractionImages
                        .Where(i => i.AttractionId == q.Id)
                        .Select(i => i.Image)
                        .DefaultIfEmpty()
                        .ToList()
                        .ConvertAll(i => i ?? ""),
                    Description = q.Description,
                    Tags = q.Tags
                        .Select(t => t.Name)
                        .ToList(),
                    Score = Math.Round(_db.Comments_Attractions
                        .Where(c => c.AttractionId == q.Id)
                        .Select(c => c.Score)
                        .DefaultIfEmpty()
                        .Average(), 1),
                    ScoreCount = _db.Comments_Attractions
                        .Where(c => c.AttractionId == q.Id)
                        .Count(),
                    Hours = Math.Round(_db.Comments_Attractions
                        .Where(c => c.AttractionId == q.Id && c.StayHours != null)
                        .Select(c => c.StayHours.Value)
                        .DefaultIfEmpty()
                        .Average(), 1),
                    HoursCount = _db.Comments_Attractions
                        .Where(c => c.AttractionId == q.Id && c.StayHours != null)
                        .Count(),
                    Price = (int)Math.Round(_db.Comments_Attractions
                        .Where(c => c.AttractionId == q.Id && c.Price != null)
                        .Select(c => c.Price.Value)
                        .DefaultIfEmpty()
                        .Average(), 0),
                    PriceCount = _db.Comments_Attractions
                        .Where(c => c.AttractionId == q.Id && c.Price != null)
                        .Count(),
                    Clicks = q.AttractionClicks
                        .Where(a => a.AttractionId == q.Id)
                        .Count(),
                    Website = q.Website,
                }).FirstOrDefault();

            return result;
        }

        //public IEnumerable<AttractionIndexDto> GetTopTen()
        //{
        //    DateTime thirtyDaysAgo = DateTime.Now.AddDays(-30);

        //    var query = _db.Attractions
        //        .AsNoTracking()
        //        .Include(a => a.AttractionCategory)
        //        .Include(a => a.Region)
        //        .Include(a => a.Town)
        //        .Include(a => a.AttractionImages)
        //        .Include(a => a.Comments_Attractions)
        //        .Include(a => a.Tags)
        //        .Include(a => a.AttractionClicks)
        //        .ToList();

        //    // 過濾只保留在過去30天內有點擊紀錄的景點
        //    query = query.Where(a => a.AttractionClicks
        //                        .Any(ac => ac.ClickDate >= thirtyDaysAgo))
        //         .ToList();

        //    // 依照過去30天內的點擊數量，將景點由高到低排序
        //    query = query.OrderByDescending(a => a.AttractionClicks
        //                                        .Count(ac => ac.ClickDate >= thirtyDaysAgo))
        //                 .ToList();

        //    // 只保留前十個點擊數量最高的景點
        //    query = query.Take(10).ToList();


        //    var result = query.Select(q => new AttractionIndexDto
        //        {
        //            Id = q.Id,
        //            AttractionCategory = q.AttractionCategory.Name,
        //            Name = q.Name,
        //            Region = q.Region.Name,
        //            Town = q.Town.Name,
        //            Image = q.AttractionImages
        //                .Where(i => i.AttractionId == q.Id)
        //                .Select(i => i.Image)
        //                .FirstOrDefault() ?? string.Empty,
        //            Description = q.Description,
        //            Tags = q.Tags
        //                .Select(t => t.Name)
        //                .ToList(),
        //            Score = Math.Round(_db.Comments_Attractions
        //                .Where(c => c.AttractionId == q.Id)
        //                .Select(c => c.Score)
        //                .DefaultIfEmpty()
        //                .Average(), 1),
        //            ScoreCount = _db.Comments_Attractions
        //                .Where(c => c.AttractionId == q.Id)
        //                .Count(),
        //            Hours = Math.Round(_db.Comments_Attractions
        //                .Where(c => c.AttractionId == q.Id && c.StayHours != null)
        //                .Select(c => c.StayHours.Value)
        //                .DefaultIfEmpty()
        //                .Average(), 1),
        //            HoursCount = _db.Comments_Attractions
        //                .Where(c => c.AttractionId == q.Id && c.StayHours != null)
        //                .Count(),
        //            Price = (int)Math.Round(_db.Comments_Attractions
        //                .Where(c => c.AttractionId == q.Id && c.Price != null)
        //                .Select(c => c.Price.Value)
        //                .DefaultIfEmpty()
        //                .Average(), 0),
        //            PriceCount = _db.Comments_Attractions
        //                .Where(c => c.AttractionId == q.Id && c.Price != null)
        //                .Count(),
        //            Clicks = q.AttractionClicks
        //                .Where(a => a.AttractionId == q.Id)
        //                .Count()
        //        }).ToList();

        //    return result;
        //}

        public IEnumerable<AttractionIndexDto> Search()
        {
            DateTime thirtyDaysAgo = DateTime.Now.AddDays(-30);

            var query = _db.Attractions
                .AsNoTracking()
                .Include(a => a.AttractionCategory)
                .Include(a => a.Region)
                .Include(a => a.Town)
                .Include(a => a.AttractionImages)
                .Include(a => a.Comments_Attractions)
                .Include(a => a.Tags)
                .Include(a => a.AttractionClicks)
                .ToList();



            var result = query//.OrderByDescending(q => q.Id)
                .Select(q => new AttractionIndexDto
                {
                    Id = q.Id,
                    AttractionCategory = q.AttractionCategory.Name,
                    Name = q.Name,
                    Region = q.Region.Name,
                    Town = q.Town.Name,
                    Image = q.AttractionImages
                        .Where(i => i.AttractionId == q.Id)
                        .Select(i => i.Image)
                        .FirstOrDefault() ?? string.Empty,
                    Description = q.Description,
                    Tags = q.Tags
                        .Select(t => t.Name)
                        .ToList(),
                    Score = Math.Round(_db.Comments_Attractions
                        .Where(c => c.AttractionId == q.Id)
                        .Select(c => c.Score)
                        .DefaultIfEmpty()
                        .Average(), 1),
                    ScoreCount = _db.Comments_Attractions
                        .Where(c => c.AttractionId == q.Id)
                        .Count(),
                    Hours = Math.Round(_db.Comments_Attractions
                        .Where(c => c.AttractionId == q.Id && c.StayHours != null)
                        .Select(c => c.StayHours.Value)
                        .DefaultIfEmpty()
                        .Average(), 1),
                    HoursCount = _db.Comments_Attractions
                        .Where(c => c.AttractionId == q.Id && c.StayHours != null)
                        .Count(),
                    Price = (int)Math.Round(_db.Comments_Attractions
                        .Where(c => c.AttractionId == q.Id && c.Price != null)
                        .Select(c => c.Price.Value)
                        .DefaultIfEmpty()
                        .Average(), 0),
                    PriceCount = _db.Comments_Attractions
                        .Where(c => c.AttractionId == q.Id && c.Price != null)
                        .Count(),
                    Clicks = q.AttractionClicks
                        .Where(a => a.AttractionId == q.Id)
                        .Count(),
                    ClicksInThirty = q.AttractionClicks
                        .Where(a => a.AttractionId == q.Id)
                        .Where(a => a.ClickDate >= thirtyDaysAgo)
                        .Count()
                }).ToList();

            return result;
        }

    }
}
