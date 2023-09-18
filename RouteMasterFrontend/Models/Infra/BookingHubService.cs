using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace RouteMasterFrontend.Models.Infra
{
    public class BookingHubService
    {
        public BookingHubService(IHubConnectionContext<dynamic> clients)
        {
            Clients = clients;
        }
        private IHubConnectionContext<dynamic> Clients { get; set; }

        public static BookingHubService Instance { get; } = new BookingHubService(GlobalHost.ConnectionManager.GetHubContext<BookingHub>().Clients);

        public void Hello()
        {
            Clients.All.Hello();
        }
        public void SendClient(string deviceSN)
        {
            Clients.Group(deviceSN).SendClient("設備數據");
        }
    }
}
