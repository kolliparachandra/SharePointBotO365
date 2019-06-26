using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using EchoBot1.Bots;
using EchoBot1.CRM;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EchoBot1.Dialogs
{
    public class MainDialog : LogoutDialog
    {
        protected readonly ILogger _logger;
        private readonly IStatePropertyAccessor<UserProfileState> _userProfileStateAccessor;
        private readonly IStatePropertyAccessor<DialogState> _dialogStateAccessor;
        private readonly UserState _userState;
        private readonly ConversationState _conversationState;
        private readonly BotServices _services;

        public MainDialog(IConfiguration configuration, ILogger<MainDialog> logger, BotServices botServices, UserState userState, ConversationState conversationState)
            : base(nameof(MainDialog), configuration["ConnectionName"], userState)
        {
            _logger = logger;

            _userState = userState ?? throw new ArgumentNullException(nameof(userState));
            _conversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));

            _userProfileStateAccessor = _userState.CreateProperty<UserProfileState>(nameof(UserProfileState));
            _dialogStateAccessor = _conversationState.CreateProperty<DialogState>(nameof(DialogState));


            AddDialog(new OAuthPrompt(
                nameof(OAuthPrompt),
                new OAuthPromptSettings
                {
                    ConnectionName = ConnectionName,
                    Text = "Please login",
                    Title = "Login",
                    Timeout = 300000, // User has 5 minutes to login
                }));

            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                PromptStepAsync,
                LoginStepAsync,
                CommandStepAsync,
                ProcessStepAsync
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> PromptStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.BeginDialogAsync(nameof(OAuthPrompt), null, cancellationToken);
        }

        private async Task<DialogTurnResult> LoginStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Get the token from the previous step. Note that we could also have gotten the
            // token directly from the prompt itself. There is an example of this in the next method.
            var tokenResponse = (TokenResponse)stepContext.Result;
            if (tokenResponse != null)
            {
                var uState = await _userProfileStateAccessor.GetAsync(stepContext.Context);
                ClaimsPrincipal jwtToken = null;
                if(uState != null && !string.IsNullOrWhiteSpace(uState.AccessToken))
                    jwtToken = Helper.Validate(uState.AccessToken);
                if (jwtToken == null)
                {
                    jwtToken = Helper.Validate(tokenResponse.Token);
                    if (uState != null)
                    {
                        uState.AccessToken = tokenResponse.Token;
                        uState.UserEmail = jwtToken.Identity.Name;
                    }
                    else
                        uState = new UserProfileState() { AccessToken = tokenResponse.Token, UserEmail = jwtToken.Identity.Name };

                    //var userProfileState = new UserProfileState() { AccessToken = tokenResponse.Token, CalAccessToken = tokenResponse.Token, UserEmail = jwtToken.Identity.Name };
                    await _userProfileStateAccessor.SetAsync(stepContext.Context, uState);
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"You are now logged in as {jwtToken.Identity.Name}."), cancellationToken);
                    return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("What would you like to do?") }, cancellationToken);
                }
                else
                {
                    return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("What would you like to do?") }, cancellationToken);
                }
            }

            await stepContext.Context.SendActivityAsync(MessageFactory.Text("Login was not successful please try again."), cancellationToken);
            return await stepContext.EndDialogAsync();
        }

        private async Task<DialogTurnResult> CommandStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["command"] = stepContext.Result;

            // Call the prompt again because we need the token. The reasons for this are:
            // 1. If the user is already logged in we do not need to store the token locally in the bot and worry
            // about refreshing it. We can always just call the prompt again to get the token.
            // 2. We never know how long it will take a user to respond. By the time the
            // user responds the token may have expired. The user would then be prompted to login again.
            //
            // There is no reason to store the token locally in the bot because we can always just call
            // the OAuth prompt to get the token or get a new token if needed.
            return await stepContext.BeginDialogAsync(nameof(OAuthPrompt), null, cancellationToken);
        }

        private async Task<DialogTurnResult> ProcessStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext.Result != null)
            {
                // We do not need to store the token in the bot. When we need the token we can
                // send another prompt. If the token is valid the user will not need to log back in.
                // The token will be available in the Result property of the task.
                var tokenResponse = stepContext.Result as TokenResponse;

                // If we have the token use the user is authenticated so we may use it to make API calls.
                if (tokenResponse?.Token != null)
                {
                    var parts = ((string)stepContext.Values["command"] ?? string.Empty).ToLowerInvariant().Split(' ');
                    //var command = parts[0];
                    var command = ((string)stepContext.Values["command"] ?? string.Empty);
                    return await stepContext.EndDialogAsync(command, cancellationToken: cancellationToken);
                }
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("We couldn't log you in. Please try again later."), cancellationToken);
            }

            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }
    }
}
