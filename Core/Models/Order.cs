namespace pizza_core_api.Core.Models
{
    public class Order
    {
        public long id { get; set; }
        public UserShort user { get; set; }
        public Adress adress { get; set; }
        public List<OrderPizza> orderPizzas { get; set; }
        public decimal totalPrice { get; set; }
        public string status { get; set; }
        public string paymentType { get; set; }
        public string? comment { get; set; }
        public string createdAt { get; set; }
        public string updatedAt { get; set; }
    }
}
