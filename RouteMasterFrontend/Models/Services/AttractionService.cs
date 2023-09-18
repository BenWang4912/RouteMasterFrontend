using Microsoft.EntityFrameworkCore;
using RouteMasterFrontend.EFModels;
using RouteMasterFrontend.Models.Dto;
using RouteMasterFrontend.Models.Interfaces;

namespace RouteMasterFrontend.Models.Services
{
    public class AttractionService
    {
        private IAttractionRepository _repo;

        public AttractionService(IAttractionRepository repo)
        {
            _repo = repo;
        }

        public IEnumerable<AttractionIndexDto> Search()
        {
            return _repo.Search();
        }

        //public IEnumerable<AttractionIndexDto> GetTopTen()
        //{
        //    return _repo.GetTopTen();
        //}

        public AttractionDetailDto Get(int id)
        {
            return _repo.Get(id);
        }

        public void AddClick (int id)
        {
            _repo.AddClick(id);
        }

        public void AddToFarvorite(string? customerAccount, int id)
        {
            var db = new RouteMasterContext();

            var memberId = db.Members
                .Where(m => m.Account.Contains(customerAccount))
                .Select(m => m.Id)
                .FirstOrDefault();

            var item = db.FavoriteAttractions
                .Where(f => f.MemberId == memberId && f.AttractionId == id)
                .FirstOrDefault();

            
            if (item == null && memberId != 0)
            {
                var favorite = new FavoriteAttraction
                {
                    MemberId = memberId,
                    AttractionId = id
                };

                db.Add(favorite);
                db.SaveChanges();
            }
        }

        public void RemoveAttFromFavorite(string? customerAccount, int id)
        {
            var db = new RouteMasterContext();

            var memberId = db.Members
                .Where(m => m.Account.Contains(customerAccount))
                .Select(m => m.Id)
                .FirstOrDefault();

            var item = db.FavoriteAttractions
                .Where(f => f.MemberId == memberId && f.AttractionId == id)
                .FirstOrDefault();


            if (memberId != 0)
            {
                db.Remove(item);
                db.SaveChanges();
            }
        }


        public IEnumerable<AttractionIndexDto> GetFavoriteAtt(string? customerAccount)
        {
            var db = new RouteMasterContext();

            var memberId = db.Members
                .Where(m => m.Account.Contains(customerAccount))
                .Select(m => m.Id)
                .FirstOrDefault();


            if (memberId != null)
            {
                var query = db.FavoriteAttractions
                .AsNoTracking()
                .Include(f => f.Attraction)
                .ThenInclude(f => f.AttractionCategory)
                .Include(f => f.Attraction)
                .ThenInclude(f => f.Region)
                .Include(f => f.Attraction)
                .ThenInclude(f => f.Town)
                .Include(f => f.Attraction)
                .ThenInclude(f => f.AttractionImages)
                .Include(f => f.Attraction)
                .ThenInclude(f => f.Comments_Attractions)
                .Include(f => f.Attraction)
                .ThenInclude(f => f.Tags)
                .Include(f => f.Attraction)
                .ThenInclude(f => f.Tags)
                .Where(f => f.MemberId == memberId)  // 只選取對應使用者的最愛景點
                .ToList();

                var result = query
                .OrderByDescending(f => f.Id)
                .Select(q => new AttractionIndexDto
                {
                    Id = q.Attraction.Id,
                    AttractionCategory = q.Attraction.AttractionCategory.Name,
                    Name = q.Attraction.Name,
                    Region = q.Attraction.Region.Name,
                    Town = q.Attraction.Town.Name,
                    Image = q.Attraction.AttractionImages
                        .Where(i => i.AttractionId == q.Attraction.Id)
                        .Select(i => i.Image)
                        .FirstOrDefault() ?? string.Empty,
                    Description = q.Attraction.Description,
                    Tags = q.Attraction.Tags
                        .Select(t => t.Name)
                        .ToList(),
                    Score = Math.Round(db.Comments_Attractions
                        .Where(c => c.AttractionId == q.Attraction.Id)
                        .Select(c => c.Score)
                        .DefaultIfEmpty()
                        .Average(), 1),
                    ScoreCount = db.Comments_Attractions
                        .Where(c => c.AttractionId == q.Attraction.Id)
                        .Count(),
                    Hours = Math.Round(db.Comments_Attractions
                        .Where(c => c.AttractionId == q.Attraction.Id && c.StayHours != null)
                        .Select(c => c.StayHours.Value)
                        .DefaultIfEmpty()
                        .Average(), 1),
                    HoursCount = db.Comments_Attractions
                        .Where(c => c.AttractionId == q.Attraction.Id && c.StayHours != null)
                        .Count(),
                    Price = (int)Math.Round(db.Comments_Attractions
                        .Where(c => c.AttractionId == q.Attraction.Id && c.Price != null)
                        .Select(c => c.Price.Value)
                        .DefaultIfEmpty()
                        .Average(), 0),
                    PriceCount = db.Comments_Attractions
                        .Where(c => c.AttractionId == q.Attraction.Id && c.Price != null)
                        .Count(),
                    Clicks = 0,
                    ClicksInThirty = 0
                }).ToList();

                return result;
            }
            else
            {
                return Enumerable.Empty<AttractionIndexDto>();
            }

        }
    }
}
