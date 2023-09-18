namespace RouteMasterFrontend.Models.Dto
{
    public class Comments_AttractionIndexDTO
    {
        public int Id { get; set; }
        public string Account { get; set; } 
        public string AttractionName  { get; set; }
        public int Score { get; set; }
        public string? Content { get; set; }
        public int? StayHours { get; set; }
        public int? Price { get; set; }
        public string CreateDate { get; set; }
        public bool IsHidden { get; set; }
        public IEnumerable<string>? ImageList { get; set; }
        public string Profile { get; set; }
        public bool? Gender { get; set; }
        public string? Address { get; set; }
    }
}
