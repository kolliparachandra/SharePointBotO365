// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio EchoBot v4.3.0

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Builder.AI.Luis;
using System;
using Microsoft.Extensions.Logging;
using EchoBot1.CRM;
using Microsoft.Bot.Builder.Dialogs;

namespace EchoBot1.Bots
{
    public class EchoBot : ActivityHandler
    {

        public static readonly string LuisConfiguration = "CRM Helper";

        private readonly IStatePropertyAccessor<UserProfileState> _greetingStateAccessor;
        private readonly IStatePropertyAccessor<DialogState> _dialogStateAccessor;
        private readonly UserState _userState;
        private readonly ConversationState _conversationState;
        private readonly BotServices _services;
        private DialogSet Dialogs { get; set; }
        private LuisRecognizer _recognizer { get; } = null;
        public EchoBot(LuisRecognizer recognizer, BotServices services, UserState userState, ConversationState conversationState, ILoggerFactory loggerFactory)
        {
            _recognizer = recognizer ?? throw new ArgumentNullException(nameof(recognizer));

            // Verify LUIS configuration.
            if (!_services.LuisServices.ContainsKey(LuisConfiguration))
            {
                throw new InvalidOperationException($"The bot configuration does not contain a service type of `luis` with the id `{LuisConfiguration}`.");
            }

            Dialogs = new DialogSet(_dialogStateAccessor);

        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var luisResult = await _recognizer.RecognizeAsync<CRMLuisModel>(turnContext, new CancellationToken());

            switch (luisResult.TopIntent().intent)
            {
                case CRMLuisModel.Intent.Accounts_AllAccounts:
                    //do something to handle the balance intent
                    break;
                case CRMLuisModel.Intent.Accounts_Top5:
                    //do something to handle the transfer intent
                    break;
                case CRMLuisModel.Intent.None:
                default:
                    await turnContext.SendActivityAsync(MessageFactory.Text($"I don't know what you want to do"), cancellationToken);
                    break;
            }


            await turnContext.SendActivityAsync(MessageFactory.Text($"Echo: {turnContext.Activity.Text}"), cancellationToken);
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text($"Hello and Welcome!"), cancellationToken);
                }
            }
        }
    }
}
