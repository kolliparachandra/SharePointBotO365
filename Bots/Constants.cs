using Bot.Builder.Community.Dialogs.FormFlow;

namespace EchoBot1.Bots
{
    public static class Constants
    {
        //public const string LUIS_EMPLOYEE_HELPER_APP_ID = "a87d3c19-af7b-4348-a89c-83c52a473c5a";
        public const string LUIS_EMPLOYEE_HELPER_APP_ID = "72eaa867-e3b5-4026-bef4-f3d154428b3c";
        //public const string LUIS_SUBSCRIPTION_KEY = "6587e88502964c1e9dca1af530ba4ffa";
        public const string LUIS_SUBSCRIPTION_KEY = "aa6119c9fd654711bedd54013cb5fbd7";
        //public const string LUIS_SUBSCRIPTION_KEY = "c7516642105b4b1093c5fb9212b34653";

        public const string CRM_HELPER_APP = "9df33e90-99c0-425f-9e33-2be7937a1e4d";
        //public const string CRM_HELPER_APP = "a8609b9c-e2b8-4f5e-8d5f-64ecca349138";
        public const string CRM_HELPER_KEY = "c7516642105b4b1093c5fb9212b34653";
        //public const string CRM_HELPER_KEY = "41912dd21242441db948cac60a46018f";

        public enum SandwichOptions
        {
            BLT, BlackForestHam, BuffaloChicken, ChickenAndBaconRanchMelt, ColdCutCombo, MeatballMarinara,
            OvenRoastedChicken, RoastBeef, RotisserieStyleChicken, SpicyItalian, SteakAndCheese, SweetOnionTeriyaki, Tuna,
            TurkeyBreast, Veggie
        };

        public enum ProductOptions { Product1, Test1, Product2, Test2 }

        public enum AccountOptions { Havas, TradeWorx, SalesForce, Acme }

        public enum GenderOpts { Male, Female, Other };

        public enum CuisinesOpts
        {
            [Terms("except", "but", "not", "no", "all", "everything")]
            Everything,
            Continental, Italian, Thai, Chinese, PanAsian, Labanese
        };

        public enum ComplementoryDrinkOpts { Beer, Scotch, Mojito };

    }
}
