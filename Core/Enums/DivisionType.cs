namespace pizza_core_api.Core.Enums
{
    public class DivisionType
    {
        public static readonly DivisionType Whole = new DivisionType("WHOLE", "Целая");
        public static readonly DivisionType Half = new DivisionType("HALF", "Половина");
        public static readonly DivisionType Quarter = new DivisionType("QUARTER", "Четверть");

        public string Code { get; }
        public string Name { get; }

        private DivisionType(string code, string name)
        {
            Code = code;
            Name = name;
        }

        public static string GetName(string? code)
        {
            if (code == Whole.Code)
                return Whole.Name;

            if (code == Half.Code)
                return Half.Name;

            if (code == Quarter.Code)
                return Quarter.Name;

            return code ?? "";
        }

        public override string ToString() => Name;
    }
}
