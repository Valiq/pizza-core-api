namespace pizza_core_api.Core.Enums
{
    public class ProductType
    {
        public static readonly ProductType Sauces = new ProductType("SAUCES", "Соусы");
        public static readonly ProductType Cheeses = new ProductType("CHEESES", "Сыры");
        public static readonly ProductType MeatProducts = new ProductType("MEAT_PRODUCTS", "Мясные продукты и колбасы");
        public static readonly ProductType Seafood = new ProductType("SEAFOOD", "Морепродукты");
        public static readonly ProductType Greens = new ProductType("GREENS", "Овощи и зелень");
        public static readonly ProductType Mushrooms = new ProductType("MUSHROOMS", "Грибы");
        public static readonly ProductType OtherToppings = new ProductType("OHER_TOPPINGS", "Прочие топпинги");

        public string Code { get; }
        public string Name { get; }

        private ProductType(string code, string name)
        {
            Code = code;
            Name = name;
        }

        public static string GetName(string? code)
        {
            if (code == Sauces.Code)
                return Sauces.Name;

            if (code == Cheeses.Code)
                return Cheeses.Name;

            if (code == MeatProducts.Code)
                return MeatProducts.Name;

            if (code == Seafood.Code)
                return Seafood.Name;

            if (code == Greens.Code)
                return Greens.Name;

            if (code == Mushrooms.Code)
                return Mushrooms.Name;

            if (code == OtherToppings.Code)
                return OtherToppings.Name;

            return code ?? "";
        }

        public override string ToString() => Name;
    }
}
