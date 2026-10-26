namespace pizza_core_api.Core.Enums
{
    public class PizzaType
    {
        public static readonly PizzaType Castom = new PizzaType("CASTOM", "Пользовательская");
        public static readonly PizzaType Standart = new PizzaType("STANDART", "Из меню");

        public string Code { get; }
        public string Name { get; }

        private PizzaType(string code, string name)
        {
            Code = code;
            Name = name;
        }

        public static string GetName(string? code)
        {
            if (code == Castom.Code)
                return Castom.Name;

            if (code == Standart.Code)
                return Standart.Name;

            return code ?? "";
        }

        public static bool IsValid(string? code)
        {
            if (string.IsNullOrEmpty(code))
                return false;

            return code == Castom.Code || code == Standart.Code;
        }

        public override string ToString() => Name;
    }
}
