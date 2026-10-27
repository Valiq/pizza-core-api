namespace pizza_core_api.Core.Enums
{
    public class EdgeType
    {
        public static readonly EdgeType Standard = new EdgeType("STANDARD", "Стандартное");
        public static readonly EdgeType Cheese = new EdgeType("CHEESE", "Сырный");
        public static readonly EdgeType Sausage = new EdgeType("SAUSAGE", "С сосисками");

        public string Code { get; }
        public string Name { get; }

        private EdgeType(string code, string name)
        {
            Code = code;
            Name = name;
        }

        public static string GetName(string? code)
        {
            if (code == Standard.Code)
                return Standard.Name;

            if (code == Cheese.Code)
                return Cheese.Name;

            if (code == Sausage.Code)
                return Sausage.Name;

            return code ?? "";
        }

        public static bool IsValid(string? code)
        {
            if (string.IsNullOrEmpty(code))
                return false;

            return code == Standard.Code
                || code == Cheese.Code
                || code == Sausage.Code;
        }

        public override string ToString() => Name;
    }
}
