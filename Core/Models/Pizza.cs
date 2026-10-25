namespace pizza_core_api.Core.Models
{
    public class Pizza
    {
        public long id { get; set; }
        public string type { get; set; }
        public string size { get; set; }
        public string name { get; set; }
        public decimal basePrice { get; set; }
        public string imagePath { get; set; }
        public bool isDeleted { get; set; }
        public string createdAt { get; set; }
        public string updatedAt { get; set; }
        public List<IngredientShort> ingredients { get; set; }
        public UserShort? updatedBy { get; set; }
    }

    public class IngredientShort
    {
        public long id { get; set; }
        public string name { get; set; }
        public string type { get; set; }
    }
}
