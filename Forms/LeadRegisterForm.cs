using Bot.Builder.Community.Dialogs.FormFlow;
using Bot.Builder.Community.Dialogs.FormFlow.Advanced;
using System;
using static EchoBot1.Bots.Constants;

namespace BotApplication.Forms
{
    [Serializable]
    public class LeadRegisterForm
    {
        [Prompt("May I know your good name?")]
        public string Name; // type: String

        [Optional]
        [Template(TemplateUsage.EnumSelectOne, "Please select gender. {||}", ChoiceStyle = ChoiceStyleOptions.Buttons)]
        public GenderOpts? Gender; // type: Enumeration

        [Template(TemplateUsage.EnumSelectOne, "Which Product are you looking for?. {||}", ChoiceStyle = ChoiceStyleOptions.Buttons)]
        public ProductOptions? Product; // type: Enumeration

        [Template(TemplateUsage.EnumSelectOne, "Which account are you interested in? {||}", ChoiceStyle = ChoiceStyleOptions.Buttons)]
        public AccountOptions? Accounts; // type: Enumeration

        [Numeric(1, 6)]
        [Prompt("How many users are registering?<br>If more than 3, you will get complementory drink ! :)")]
        public Int16 TotalAttendees; // type: Integral

        [Template(TemplateUsage.EnumSelectOne, "Which complementory drink you would like to have? {||}", ChoiceStyle = ChoiceStyleOptions.Carousel)]
        public ComplementoryDrinkOpts? ComplementoryDrink; // type: Enumeration

        [Template(TemplateUsage.EnumSelectOne, "Which complementory drink you would like to have? {||}", ChoiceStyle = ChoiceStyleOptions.Buttons)]
        public ComplementoryDrinkOpts? ComplementoryDrinkSkype; // type: Enumeration
        public static IForm<LeadRegisterForm> BuildForm()
        {
            return new FormBuilder<LeadRegisterForm>()
                    .Field(nameof(Name))
                    .Field(nameof(Gender))
                    .Field(nameof(Product))
                    .Field(nameof(Accounts))
                    .Field(nameof(TotalAttendees))
                    .Field(new FieldReflector<LeadRegisterForm>(nameof(ComplementoryDrink))
                        .SetType(null)
                        .SetActive(state => state.TotalAttendees > 3)
                        .SetDefine(async (state, field) =>
                        {
                            field
                            .AddDescription(ComplementoryDrinkOpts.Beer, Convert.ToString(ComplementoryDrinkOpts.Beer),
                            "https://dydza6t6xitx6.cloudfront.net/ci_4868.jpg")
                            .AddTerms(ComplementoryDrinkOpts.Beer, Convert.ToString(ComplementoryDrinkOpts.Beer))

                            .AddDescription(ComplementoryDrinkOpts.Scotch, Convert.ToString(ComplementoryDrinkOpts.Scotch),
                            "http://cdn6.bigcommerce.com/s-7a906/images/stencil/750x750/products/1453/1359/ardbeg-10-750__13552.1336419033.jpg?c=2")
                            .AddTerms(ComplementoryDrinkOpts.Scotch, Convert.ToString(ComplementoryDrinkOpts.Scotch))

                            .AddDescription(ComplementoryDrinkOpts.Mojito, Convert.ToString(ComplementoryDrinkOpts.Mojito),
                            "https://www.bbcgoodfood.com/sites/default/files/styles/recipe/public/recipe/recipe-image/2013/11/mojito-cocktails.jpg")
                            .AddTerms(ComplementoryDrinkOpts.Mojito, Convert.ToString(ComplementoryDrinkOpts.Mojito));

                            return true;
                        }))
                    .Confirm(async (state) =>
                    {
                        return new PromptAttribute("Hi {Name}, Please review your selection. No. of registrations: {TotalAttendees}   , Product: {Product}  Account: {Accounts}. Do you want to continue? {||}");
                    })
                    .Build();
        }

        public static IForm<LeadRegisterForm> BuildFormSkype()
        {
            return new FormBuilder<LeadRegisterForm>()
                    .Field(nameof(Name))
                    .Field(nameof(Gender))
                    .Field(nameof(Product))
                    .Field(nameof(Accounts))
                    .Field(nameof(TotalAttendees))
                    .Field(new FieldReflector<LeadRegisterForm>(nameof(ComplementoryDrinkSkype))
                        .SetType(null)
                        .SetActive(state => state.TotalAttendees > 3))
                    .Confirm(async (state) =>
                    {
                        return new PromptAttribute("Hi {Name}, Please review your selection. No. of registrations: {TotalAttendees}   , Product: {Product}  Account: {Accounts}. Do you want to continue? {||}");
                    })
                    .Build();
        }

    }
}