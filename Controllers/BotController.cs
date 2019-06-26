// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio EchoBot v4.3.0

using System;
using System.Threading.Tasks;
using EchoBot1.Bots;
using EchoBot1.CRM;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EchoBot1.Controllers
{
    // This ASP Controller is created to handle a request. Dependency Injection will provide the Adapter and IBot
    // implementation at runtime. Multiple different IBot implementations running at different endpoints can be
    // achieved by specifying a more specific type for the bot constructor argument.
    [Route("api/azure")]
    [ApiController]
    public class BotController : ControllerBase
    {
        private readonly IBotFrameworkHttpAdapter Adapter;
        private IBot Bot;

        protected readonly BotState ConversationState;
        protected readonly Dialog Dialog;
        protected readonly Dialog CalDialog;
        protected readonly ILogger Logger;
        protected readonly BotState UserState;
        protected readonly IConfiguration _configuration;

        public static readonly string LuisConfiguration = "CRM Helper";

        private readonly IStatePropertyAccessor<UserProfileState> _userProfileStateAccessor;
        private readonly IStatePropertyAccessor<DialogState> _dialogStateAccessor;
        private readonly BotServices _services;
        private readonly Func<string, IBot> _serviceAccessor;
        private LuisRecognizer _recognizer { get; } = null;

        public BotController(IBotFrameworkHttpAdapter adapter, ConversationState conversationState, UserState userState, ILogger<BotController> logger, LuisRecognizer recognizer, BotServices services, IConfiguration configuration, Func<string, IBot> serviceAccessor)
        {
            Adapter = adapter;
            _serviceAccessor = serviceAccessor;
            ConversationState = conversationState;
            UserState = userState;
            Logger = logger;

            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _recognizer = recognizer ?? throw new ArgumentNullException(nameof(recognizer));
            _services = services ?? throw new ArgumentNullException(nameof(services));
            UserState = userState ?? throw new ArgumentNullException(nameof(userState));
            ConversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));

            _userProfileStateAccessor = UserState.CreateProperty<UserProfileState>(nameof(UserProfileState));
            _dialogStateAccessor = ConversationState.CreateProperty<DialogState>(nameof(DialogState));

            // Verify LUIS configuration.
            if (!_services.LuisServices.ContainsKey(LuisConfiguration))
            {
                throw new InvalidOperationException($"The bot configuration does not contain a service type of `luis` with the id `{LuisConfiguration}`.");
            }
        }

        [HttpPost]
        public async Task PostAsync()
        {
            Bot = _serviceAccessor("main");
            // Delegate the processing of the HTTP POST to the adapter.
            // The adapter will invoke the bot.
            await Adapter.ProcessAsync(Request, Response, Bot);
        }
    }
}
