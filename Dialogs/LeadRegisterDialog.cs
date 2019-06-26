using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using BotApplication.Forms;
using EchoBot1.Bots;
using EchoBot1.CRM;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using static EchoBot1.Bots.Constants;

namespace EchoBot1.Dialogs
{
    public class LeadRegisterDialog : LogoutDialog
    {
        protected readonly ILogger _logger;
        private readonly IStatePropertyAccessor<UserProfileState> _userProfileStateAccessor;
        private readonly IStatePropertyAccessor<DialogState> _dialogStateAccessor;
        private readonly UserState _userState;
        private readonly ConversationState _conversationState;
        private readonly BotServices _services;

        public LeadRegisterDialog(IConfiguration configuration, ILogger<LeadRegisterDialog> logger, BotServices botServices, UserState userState, ConversationState conversationState)
            : base(nameof(LeadRegisterDialog), configuration["ConnectionName"], userState)
        {
            _logger = logger;

            _userState = userState ?? throw new ArgumentNullException(nameof(userState));
            _conversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));

            _userProfileStateAccessor = _userState.CreateProperty<UserProfileState>(nameof(UserProfileState));
            _dialogStateAccessor = _conversationState.CreateProperty<DialogState>(nameof(DialogState));

            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ChoicePrompt("GenderChoice"));
            AddDialog(new ChoicePrompt("ProductChoice"));
            AddDialog(new ChoicePrompt("AccountChoice"));
            AddDialog(new ChoicePrompt("ComplementaryDrinkChoices"));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new NumberPrompt<int>("Attendees"));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                NameStepAsync,
                GenderStepAsync,
                ProductStepAsync,
                AccountStepAsync,
                AttendeesStepAsync,
                ComplementaryDrinkStepAsync,
                ProcessStepAsync
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> NameStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var leadForm = (LeadRegisterForm)stepContext.Options;
            if(leadForm != null && string.IsNullOrWhiteSpace(leadForm.Name))
            {
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("May I know your good name?") }, cancellationToken);
            }
            else
                return await stepContext.NextAsync(leadForm.Name, cancellationToken);
        }

        private async Task<DialogTurnResult> GenderStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var leadForm = (LeadRegisterForm)stepContext.Options;
            leadForm.Name = (string)stepContext.Result;
            if (leadForm != null && leadForm.Gender == null)
            {
                var choices = new List<Choice> { new Choice("Male"), new Choice("Female"), new Choice("Other") };
                return await stepContext.PromptAsync("GenderChoice", new PromptOptions { Prompt = MessageFactory.Text("Please select Gender?"), Choices = choices }, cancellationToken);
            }
            else
                return await stepContext.NextAsync(leadForm.Gender, cancellationToken);
        }

        private async Task<DialogTurnResult> ProductStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var leadForm = (LeadRegisterForm)stepContext.Options;
            if (stepContext.Result is FoundChoice choice && Enum.TryParse(choice.Value, out GenderOpts gender))
                leadForm.Gender = gender;
            if (leadForm != null && leadForm.Product == null)
            {
                Activity textPrompt =  MessageFactory.Text("Which Product are you looking for?");
                PromptOptions promptOptions = new PromptOptions
                {
                    Prompt = textPrompt,
                    Choices = new List<Choice> { new Choice("Product1"), new Choice("Test1"), new Choice("Product2"), new Choice("Test2") }
                };
                return await stepContext.PromptAsync("ProductChoice", promptOptions, cancellationToken);

            }
            else
                return await stepContext.NextAsync(leadForm.Product, cancellationToken);
        }

        private async Task<DialogTurnResult> AccountStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var leadForm = (LeadRegisterForm)stepContext.Options;
            if (stepContext.Result is FoundChoice choice && Enum.TryParse(choice.Value, out ProductOptions product))
                leadForm.Product = product;
            if (leadForm != null && leadForm.Accounts == null)
            {
                Activity textPrompt =  MessageFactory.Text("Which account are you interested in?");
                PromptOptions promptOptions = new PromptOptions
                {
                    Prompt = textPrompt,
                    Choices = new List<Choice> { new Choice("TradeWorx"), new Choice("Havas"), new Choice("SalesForce"), new Choice("Acme") }
                };
                return await stepContext.PromptAsync("AccountChoice", promptOptions, cancellationToken);

            }
            else
                return await stepContext.NextAsync(leadForm.Accounts, cancellationToken);
        }

        private async Task<DialogTurnResult> AttendeesStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var leadForm = (LeadRegisterForm)stepContext.Options;
            if (stepContext.Result is FoundChoice choice && Enum.TryParse(choice.Value, out AccountOptions accounts))
                leadForm.Accounts = accounts;
            if (leadForm != null && leadForm.TotalAttendees == 0)
            {
                Activity textPrompt =  MessageFactory.Text("How many users are registering?<br>If more than 3, you will get complementory drink ! :)");
                PromptOptions promptOptions = new PromptOptions
                {
                    Prompt = textPrompt,
                };
                return await stepContext.PromptAsync("Attendees", promptOptions, cancellationToken);

            }
            else
                return await stepContext.NextAsync(leadForm.TotalAttendees, cancellationToken);
        }

        private async Task<DialogTurnResult> ComplementaryDrinkStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var leadForm = (LeadRegisterForm)stepContext.Options;
            if (stepContext.Result is int intVal)
                leadForm.TotalAttendees = (short)intVal;
            if (leadForm != null && leadForm.ComplementoryDrink == null)
            {
                Activity textPrompt =  MessageFactory.Text("Which complementory drink you would like to have?");

                var choices = new List<Choice>
                {
                    new Choice() { Value = Convert.ToString(ComplementoryDrinkOpts.Beer), Action = new CardAction() { Title = Convert.ToString(ComplementoryDrinkOpts.Beer), Type = ActionTypes.PostBack, Value = Convert.ToString(ComplementoryDrinkOpts.Beer), Image = "https://dydza6t6xitx6.cloudfront.net/ci_4868.jpg" } },
                    new Choice() { Value = Convert.ToString(ComplementoryDrinkOpts.Scotch), Action = new CardAction() { Title = Convert.ToString(ComplementoryDrinkOpts.Scotch), Type = ActionTypes.PostBack, Value = Convert.ToString(ComplementoryDrinkOpts.Scotch), Image = "http://cdn6.bigcommerce.com/s-7a906/images/stencil/750x750/products/1453/1359/ardbeg-10-750__13552.1336419033.jpg?c=2" } },
                    new Choice() { Value = Convert.ToString(ComplementoryDrinkOpts.Mojito), Action = new CardAction() { Title = Convert.ToString(ComplementoryDrinkOpts.Mojito), Type = ActionTypes.PostBack, Value = Convert.ToString(ComplementoryDrinkOpts.Mojito), Image = "http://www.panningtheglobe.com/wp-content/uploads/2013/04/froz-mojito-two.jpg" } }
                };

                PromptOptions promptOptions = new PromptOptions
                {
                    Prompt = textPrompt, Choices = choices
                };
                return await stepContext.PromptAsync("ComplementaryDrinkChoices", promptOptions, cancellationToken);

            }
            else
                return await stepContext.NextAsync(leadForm.ComplementoryDrink, cancellationToken);
        }

        private async Task<DialogTurnResult> ProcessStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var leadForm = (LeadRegisterForm)stepContext.Options;
            if (stepContext.Result is FoundChoice choice && Enum.TryParse(choice.Value, out ComplementoryDrinkOpts drinks))
                leadForm.ComplementoryDrink = drinks;
            
            if (stepContext.Result != null)
            {
                return await stepContext.EndDialogAsync(leadForm, cancellationToken: cancellationToken);
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("We couldn't log you in. Please try again later."), cancellationToken);
            }

            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }

    }
}
