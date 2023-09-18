
using Microsoft.AspNet.SignalR;

namespace RouteMasterFrontend.Models.Infra
{
    public class BookingHub:Hub
    {
        private readonly BookingHubService _bookingHubService;
        public static BookingHub Instance { get; } = new BookingHub(BookingHubService.Instance);

        public BookingHub() : this(BookingHubService.Instance) { }

        public BookingHub(BookingHubService bookingHubService)
        {
            _bookingHubService = bookingHubService;
        }

        public void Hello(string projectID)
        {
            Groups.Add(Context.ConnectionId, projectID);
            _bookingHubService.Hello();
        }

        public void SendClient(string deviceSN)
        {
            _bookingHubService.SendClient(deviceSN);
        }


        //public async Task SendBookingMessage(string ID)
        //{
        //    _bookingHubService.SendMessage();
        //    //await Clients.All.SendAsync("ReceiveBookingMessage");
        //}
    }
}
