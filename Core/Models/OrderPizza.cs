namespace pizza_core_api.Core.Models
{
    public class OrderPizza
    {
        public long id { get; set; }
        public Pizza pizza { get; set; }
        public List<AdditionalIngredient> additionalIngredients { get; set; }
        public string divisionType { get; set; }
        public string doughType { get; set; }
        public string edgeType { get; set; }
        public decimal totalPrice { get; set; }
        public string size { get; set; }
        public string createdAt { get; set; }
    }
}
