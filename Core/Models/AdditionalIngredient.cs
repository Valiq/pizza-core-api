namespace pizza_core_api.Core.Models
{
    public class AdditionalIngredient
    {
        public long id { get; set; }
        public Ingredient ingredient { get; set; }
        public decimal totalPrice { get; set; }
        public decimal quantity { get; set; }
        public string actionType { get; set; }
        public string createdAt { get; set; }
    }
}
