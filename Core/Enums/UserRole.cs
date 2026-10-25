namespace pizza_core_api.Core.Enums
{
    public class UserRole
    {
        public static readonly UserRole Customer = new UserRole("CUSTOMER", "Покупатель");
        public static readonly UserRole Admin = new UserRole("ADMIN", "Администратор");

        public string Code { get; }
        public string Name { get; }

        private UserRole(string code, string name)
        {
            Code = code;
            Name = name;
        }

        public static string GetName(string? code)
        {
            if (code == Customer.Code)
                return Customer.Name;

            if (code == Admin.Code)
                return Admin.Name;

            return code ?? "";
        }

        public override string ToString() => Name;
    }
}
