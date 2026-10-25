namespace pizza_core_api.Core.Enums
{
    public class DoughType
    {
        public static readonly DoughType Classic = new DoughType("CLASSIC", "Классическое");
        public static readonly DoughType Subtle = new DoughType("SUBTLE", "Тонкое");
        public static readonly DoughType Lush = new DoughType("LUSH", "Пышное");

        public string Code { get; }
        public string Name { get; }

        private DoughType(string code, string name)
        {
            Code = code;
            Name = name;
        }

        public static string GetName(string? code)
        {
            if (code == Classic.Code)
                return Classic.Name;

            if (code == Subtle.Code)
                return Subtle.Name;

            if (code == Lush.Code)
                return Lush.Name;

            return code ?? "";
        }

        public override string ToString() => Name;
    }
}
