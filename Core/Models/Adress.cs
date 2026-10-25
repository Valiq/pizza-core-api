namespace pizza_core_api.Core.Models
{
    public class Adress
    {
        public long id { get; set; }
        public UserShort user { get; set; }
        public string city { get; set; }
        public string street { get; set; }
        public string house { get; set; }
        public string? apartment { get; set; }
        public string createdAt { get; set; }
        public string updatedAt { get; set; }
    }
}
