namespace pizza_core_api.Core.Enums
{
    public class ActionType
    {
        public static readonly ActionType Add = new ActionType("ADD", "Добавить");
        public static readonly ActionType Remove = new ActionType("REMOVE", "Убрать");

        public string Code { get; }
        public string Name { get; }

        private ActionType(string code, string name)
        {
            Code = code;
            Name = name;
        }

        public static string GetName(string? code)
        {
            if (code == Add.Code)
                return Add.Name;

            if (code == Remove.Code)
                return Remove.Name;

            return code ?? "";
        }

        public override string ToString() => Name;
    }
}
