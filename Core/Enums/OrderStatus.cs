namespace pizza_core_api.Core.Enums
{
    public class OrderStatus
    {
        public static readonly OrderStatus Created = new OrderStatus("CREATED", "Создан");
        public static readonly OrderStatus Preparation = new OrderStatus("PREPARATION", "Готовка");
        public static readonly OrderStatus Delivery = new OrderStatus("DELIVERY", "Доставка");
        public static readonly OrderStatus Paid = new OrderStatus("PAID", "Оплачено");
        public static readonly OrderStatus Canceled = new OrderStatus("CANCELED", "Отменён");

        public string Code { get; }
        public string Name { get; }

        private OrderStatus(string code, string name)
        {
            Code = code;
            Name = name;
        }

        public static string GetName(string? code)
        {
            if (code == Created.Code)
                return Created.Name;

            if (code == Preparation.Code)
                return Preparation.Name;

            if (code == Delivery.Code)
                return Delivery.Name;

            if (code == Paid.Code)
                return Paid.Name;

            return code ?? "";
        }

        public override string ToString() => Name;
    }
}
