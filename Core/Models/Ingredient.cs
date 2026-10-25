namespace pizza_core_api.Core.Models
{
    public class Ingredient
    {
        public long id { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public decimal basePrice { get; set; }
        public bool isDeleted { get; set; }
        public string createdAt { get; set; }
        public string updatedAt { get; set; }
        public UserShort? updatedBy { get; set; }
    }
}
