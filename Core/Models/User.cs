namespace pizza_core_api.Core.Models
{
    public class User
    {
        public long id { get; set; }
        public string name { get; set; }
        public string? email { get; set; }
        public string phone { get; set; }
        public string role { get; set; }
        public string? passwordHash { get; set; }
        public string? tokenHash { get; set; }
        public bool isDeleted { get; set; }
        public string createdAt { get; set; }
        public string updatedAt { get; set; }
        public UserShort? updatedBy { get; set; }
    }
}
