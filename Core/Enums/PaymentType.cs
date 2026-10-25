namespace pizza_core_api.Core.Enums
{
    public class PaymentType
    {
        public static readonly PaymentType Card = new PaymentType("CARD", "Карта");
        public static readonly PaymentType Cash = new PaymentType("CASH", "Наличные");

        public string Code { get; }
        public string Name { get; }

        private PaymentType(string code, string name)
        {
            Code = code;
            Name = name;
        }

        public static string GetName(string? code)
        {
            if (code == Card.Code)
                return Card.Name;

            if (code == Cash.Code)
                return Cash.Name;

            return code ?? "";
        }

        public override string ToString() => Name;
    }
}
