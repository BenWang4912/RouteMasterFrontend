using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RouteMasterFrontend.EFModels;
using RouteMasterFrontend.Models.Infra.EFRepositories;
using RouteMasterFrontend.Models.Infra.ExtenSions;
using RouteMasterFrontend.Models.Interfaces;
using RouteMasterFrontend.Models.Services;
using RouteMasterFrontend.Models.ViewModels.AttractionVMs;
using RouteMasterFrontend.Models.ViewModels.Members;
using System.Security.Claims;

namespace RouteMasterFrontend.Views.Shared.Components.MemberPartial
{
    public class MemberAreaViewComponent:ViewComponent
    {
        private readonly RouteMasterContext _context;
        public MemberAreaViewComponent(RouteMasterContext context)
        {
            _context = context;
        }

        public IViewComponentResult Invoke(int pagecase=0)
        {
            ClaimsPrincipal user = HttpContext.User;
            var id = user.FindFirst("id").Value;
            var memberEmail = user.FindFirst("Email").Value;
            int memberid = int.Parse(id);
            Member myMember = _context.Members.First(m => m.Email == memberEmail);

            var modelPasword = new MemberEditPasswordVM();
            modelPasword.id =memberid;

            var customerAccount = User.Identity.Name;
            IEnumerable<AttractionIndexVM> attractions = GetFavoriteAtt(customerAccount);

           

            switch (pagecase)
            {
                case 0:
                    return View("MemEdit", myMember);
                case 1:
					return View("EditPassword", modelPasword);
				case 2:
					return View("MemOrder", memberid);					
                case 3:
                    return View("_MessageNonVue");
                case 4:
                    return View("_FavoriteAtt", attractions);
                case 5:
                    return View("_SchduleTable");

            }

            var model = new MemberEditPasswordVM();
            return View("EditPassword", model);
        }

        private IEnumerable<AttractionIndexVM> GetFavoriteAtt(string? customerAccount)
        {
            IAttractionRepository repo = new AttractionEFRepository();
            AttractionService service = new AttractionService(repo);

            return service.GetFavoriteAtt(customerAccount).Select(dto => dto.ToIndexVM());
        }
    }
}
