namespace pizza_core_api.Core.Enums
{
    public class Size
    {
        public static readonly Size Small = new Size("SMALL", "25 см", 1);
        public static readonly Size Medium = new Size("MEDIUM", "35 см", 1.4);
        public static readonly Size Big = new Size("BIG", "25 см", 1.8);

        public string Code { get; }
        public string Name { get; }
        public double Multiplier { get; }

        private Size(string code, string name, double multiplier)
        {
            Code = code;
            Name = name;
            Multiplier = multiplier;
        }

        public static string GetName(string? code)
        {
            if (code == Small.Code)
                return Small.Name;

            if (code == Medium.Code)
                return Medium.Name;

            if (code == Big.Code)
                return Big.Name;

            return code ?? "";
        }

        public static double GetMultiplier(string? code)
        {
            if (code == Small.Code)
                return Small.Multiplier;

            if (code == Medium.Code)
                return Medium.Multiplier;

            if (code == Big.Code)
                return Big.Multiplier;

            return -1;
        }

        public override string ToString() => Name;
    }
}
