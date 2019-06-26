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
    public class CalDialog : LogoutDialog
    {
        protected readonly ILogger _logger;
        private readonly IStatePropertyAccessor<UserProfileState> _userProfileStateAccessor;
        private readonly IStatePropertyAccessor<DialogState> _dialogStateAccessor;
        private readonly UserState _userState;
        private readonly ConversationState _conversationState;
        private readonly BotServices _services;

        public CalDialog(IConfiguration configuration, ILogger<CalDialog> logger, BotServices botServices, UserState userState, ConversationState conversationState)
            : base(nameof(CalDialog), configuration["ConnectionNameCal"], userState)
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
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> PromptStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            try
            {
                return await stepContext.BeginDialogAsync(nameof(OAuthPrompt), null, cancellationToken);
            }
            catch (Exception ex)
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Login NOT successful please try again. {ex.Message}"), cancellationToken);
                _logger.LogError(ex.Message, ex.StackTrace);
            }
            return new DialogTurnResult(DialogTurnStatus.Complete);
        }

        private async Task<DialogTurnResult> LoginStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Get the token from the previous step. Note that we could also have gotten the
            // token directly from the prompt itself. There is an example of this in the next method.
            try
            {
                var tokenResponse = (TokenResponse)stepContext.Result;
                if (tokenResponse != null)
                {
                    var uState = await _userProfileStateAccessor.GetAsync(stepContext.Context);
                    var calTokenExists = uState != null && !string.IsNullOrWhiteSpace(uState.CalAccessToken);
                    if (uState != null)
                        uState.CalAccessToken = tokenResponse.Token;
                    else
                        uState = new UserProfileState() { CalAccessToken = tokenResponse.Token };
                    await _userProfileStateAccessor.SetAsync(stepContext.Context, uState);

                    if (!calTokenExists)
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text($"You are now logged in {uState.UserEmail}. Please renter the command."), cancellationToken);
                    //await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Would you like to do?") }, cancellationToken);
                    return await stepContext.EndDialogAsync();
                }
            }
            catch (Exception ex)
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Login NOT successful please try again. {ex.Message}"), cancellationToken);
                _logger.LogError(ex.Message, ex.StackTrace);
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
                    var command = parts[0];
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
