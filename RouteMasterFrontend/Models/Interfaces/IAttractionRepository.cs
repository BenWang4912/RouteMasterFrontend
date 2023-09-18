using RouteMasterFrontend.Models.Dto;

namespace RouteMasterFrontend.Models.Interfaces
{
    public interface IAttractionRepository
    {
        IEnumerable<AttractionIndexDto> Search();

        //IEnumerable<AttractionIndexDto> GetTopTen();

        AttractionDetailDto Get(int id);

        void AddClick(int id);
    }
}
