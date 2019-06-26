using System;
using System.Threading;
using System.Threading.Tasks;
using Bot.Builder.Community.Dialogs.FormFlow;
using BotApplication.CRM;
using BotApplication.Forms;
using EchoBot1.CRM;
using EchoBot1.Dialogs;
using EchoBot1.SharePoint;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EchoBot1.Bots
{
    // This IBot implementation can run any type of Dialog. The use of type parameterization is to allows multiple different bots
    // to be run at different endpoints within the same project. This can be achieved by defining distinct Controller types
    // each with dependency on distinct IBot types, this way ASP Dependency Injection can glue everything together without ambiguity.
    // The ConversationState is used by the Dialog system. The UserState isn't, however, it might have been used in a Dialog implementation,
    // and the requirement is that all BotState objects are saved at the end of a turn.
    public class DialogBot<T> : ActivityHandler where T : Dialog
    {
        protected readonly BotState ConversationState;
        protected readonly Dialog Dialog;
        protected readonly Dialog CalDialog;
        protected readonly ILogger Logger;
        protected readonly BotState UserState;
        protected readonly IConfiguration _configuration;

        public static readonly string LuisConfiguration = "CRM Helper";

        private readonly IStatePropertyAccessor<UserProfileState> _userProfileStateAccessor;
        private readonly IStatePropertyAccessor<DialogState> _mainDialogStateAccessor;
        private readonly IStatePropertyAccessor<DialogState> _calDialogStateAccessor;
        private readonly IStatePropertyAccessor<DialogState> _leadDialogStateAccessor;
        private readonly BotServices _services;
        private DialogSet Dialogs { get; set; }
        private LuisRecognizer _recognizer { get; } = null;
        public DialogBot(ConversationState conversationState, UserState userState, T dialog, ILogger<DialogBot<T>> logger, LuisRecognizer recognizer, BotServices services, IConfiguration configuration, CalDialog calDialog)
        {
            ConversationState = conversationState;
            UserState = userState;
            Dialog = dialog;
            CalDialog = calDialog;
            Logger = logger;

            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _recognizer = recognizer ?? throw new ArgumentNullException(nameof(recognizer));
            _services = services ?? throw new ArgumentNullException(nameof(services));
            UserState = userState ?? throw new ArgumentNullException(nameof(userState));
            ConversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));

            _userProfileStateAccessor = UserState.CreateProperty<UserProfileState>(nameof(UserProfileState));
            _mainDialogStateAccessor = ConversationState.CreateProperty<DialogState>("mainDialogState");
            _calDialogStateAccessor = ConversationState.CreateProperty<DialogState>("calDialogState");
            _leadDialogStateAccessor = ConversationState.CreateProperty<DialogState>("leadDialogState");
            // Verify LUIS configuration.
            if (!_services.LuisServices.ContainsKey(LuisConfiguration))
            {
                throw new InvalidOperationException($"The bot configuration does not contain a service type of `luis` with the id `{LuisConfiguration}`.");
            }

        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (turnContext?.Activity?.Type == ActivityTypes.Invoke && turnContext.Activity.ChannelId == "msteams")
                //await Dialog.Run(turnContext, ConversationState.CreateProperty<DialogState>(nameof(DialogState)), cancellationToken);
                await OnMessageReceived(turnContext, cancellationToken);
            else
                await base.OnTurnAsync(turnContext, cancellationToken);

            // Save any state changes that might have occured during the turn.
            await ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            await UserState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            Logger.LogInformation("Running dialog with Message Activity.");

            // Run the Dialog with the new message Activity.
            //await Dialog.Run(turnContext, ConversationState.CreateProperty<DialogState>(nameof(DialogState)), cancellationToken);
            await OnMessageReceived(turnContext, cancellationToken);
        }

        private async Task OnMessageReceived(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            var leadDialogState = await _leadDialogStateAccessor.GetAsync(turnContext);
            var mainDialogState = await _mainDialogStateAccessor.GetAsync(turnContext);
            var calDialogState = await _calDialogStateAccessor.GetAsync(turnContext);
            var luisResultRaw = await _recognizer.RecognizeAsync(turnContext, cancellationToken);

            if (mainDialogState != null && mainDialogState.DialogStack != null && mainDialogState.DialogStack.Count > 0)
            {
                await ProcessLuisIntent(CRMLuisModel.Intent.Greet_Welcome, turnContext, luisResultRaw);
            }
            else if (calDialogState != null && calDialogState.DialogStack != null && calDialogState.DialogStack.Count > 0)
            {
                await ProcessLuisIntent(CRMLuisModel.Intent.Calendar_Event, turnContext, luisResultRaw);
            }
            else if (leadDialogState != null && leadDialogState.DialogStack != null && leadDialogState.DialogStack.Count > 0)
            {
                await ProcessLuisIntent(CRMLuisModel.Intent.Lead_Registration, turnContext, luisResultRaw);
            }
            else if (!string.IsNullOrWhiteSpace(turnContext?.Activity?.Text))
            {
                var luisResult = await _recognizer.RecognizeAsync<CRMLuisModel>(turnContext, cancellationToken);
                //var luisResultRaw = await _recognizer.RecognizeAsync(turnContext, cancellationToken);
                if (luisResult.TopIntent().intent != CRMLuisModel.Intent.Greet_Welcome)
                {
                    var userState = await _userProfileStateAccessor.GetAsync(turnContext);
                    if (userState == null || string.IsNullOrWhiteSpace(userState.AccessToken))
                    {
                        await ProcessLuisIntent(CRMLuisModel.Intent.Greet_Welcome, turnContext, luisResultRaw);
                    }
                    else if (luisResult.TopIntent().intent == CRMLuisModel.Intent.None && (userState == null || string.IsNullOrWhiteSpace(userState.CalAccessToken)))
                    {
                        await ProcessLuisIntent(CRMLuisModel.Intent.Calendar_Event, turnContext, luisResultRaw);
                    }
                    else
                        await ProcessLuisIntent(luisResult.TopIntent().intent, turnContext, luisResultRaw, cancellationToken);
                }
                else
                    await ProcessLuisIntent(luisResult.TopIntent().intent, turnContext, luisResultRaw, cancellationToken);
            }
            else
            {
                var usrState = await _userProfileStateAccessor.GetAsync(turnContext);
                if (usrState == null) usrState = new UserProfileState() { CurrentDialog = "mainDialog" };
                if (usrState.CurrentDialog == "graphDialog")
                    await ProcessLuisIntent(CRMLuisModel.Intent.Calendar_Event, turnContext, new RecognizerResult());
                else
                    await ProcessLuisIntent(CRMLuisModel.Intent.Greet_Welcome, turnContext, new RecognizerResult());
            }
            //await turnContext.SendActivityAsync(MessageFactory.Text($"Echo: {turnContext.Activity.Text}"), cancellationToken);
        }


        private async Task ProcessLuisIntent(CRMLuisModel.Intent intent, ITurnContext turnContext, RecognizerResult luisResult0, CancellationToken cancellationToken = default(CancellationToken))
        {
            var msgContext = turnContext as ITurnContext<IMessageActivity>;
            switch (intent)
            {
                case CRMLuisModel.Intent.Greet_Welcome:
                    var usrState = await _userProfileStateAccessor.GetAsync(turnContext);
                    if (usrState == null) usrState = new UserProfileState();
                    usrState.CurrentDialog = "mainDialog";
                    await _userProfileStateAccessor.SetAsync(turnContext, usrState);
                    if(msgContext != null)
                        Logger.LogInformation($"[Inside the Greet.Welcome Intent] " + msgContext.Activity.AsMessageActivity()?.Text, msgContext.Activity.Conversation.Id);
                    var dialogResult = await Dialog.Run(turnContext, _mainDialogStateAccessor, cancellationToken);
                    if(dialogResult.Result != null && !string.IsNullOrWhiteSpace(dialogResult.Result.ToString()))
                    {
                        turnContext.Activity.Text = dialogResult.Result.ToString();
                        var luisResult = await _recognizer.RecognizeAsync<CRMLuisModel>(turnContext, cancellationToken);
                        var luisResultRaw = await _recognizer.RecognizeAsync(turnContext, cancellationToken);
                        await ProcessLuisIntent(luisResult.TopIntent().intent, turnContext, luisResultRaw, cancellationToken);
                    }
                    break;
                case CRMLuisModel.Intent.Accounts_Total:
                    Logger.LogInformation($"[Inside the Accounts.Total Intent] " + msgContext.Activity.AsMessageActivity()?.Text, msgContext.Activity.Conversation.Id);
                    await CrmLead.GetAccountCounts(await CrmDataConnection.GetAPI2(msgContext, _configuration, _userProfileStateAccessor), msgContext);
                    break;
                case CRMLuisModel.Intent.Accounts_AllAccounts:
                    Logger.LogInformation($"[Inside the Accounts.AllAccounts Intent] " + msgContext.Activity.AsMessageActivity()?.Text, msgContext.Activity.Conversation.Id);
                    await CrmLead.GetAllAccountCounts(await CrmDataConnection.GetAPI2(msgContext,_configuration,_userProfileStateAccessor), msgContext);
                    break;
                case CRMLuisModel.Intent.Accounts_Top5:
                    Logger.LogInformation($"[Inside the Accounts.Top5 Intent] " + msgContext.Activity.AsMessageActivity()?.Text, msgContext.Activity.Conversation.Id);
                    var count = GetNumber(luisResult0.Text);
                    await CrmLead.GetTopAccounts(await CrmDataConnection.GetAPI2(msgContext, _configuration, _userProfileStateAccessor), msgContext, count);
                    break;
                case CRMLuisModel.Intent.Products_Top5:
                    Logger.LogInformation($"[Inside the Products.Top5 Intent] " + msgContext.Activity.AsMessageActivity()?.Text, msgContext.Activity.Conversation.Id);
                    await CrmLead.GetTopProducts(await CrmDataConnection.GetAPI2(msgContext, _configuration, _userProfileStateAccessor), msgContext);
                    break;
                case CRMLuisModel.Intent.Calendar_Event:
                    var usrState1 = await _userProfileStateAccessor.GetAsync(turnContext);
                    if (usrState1 == null) usrState1 = new UserProfileState();
                    usrState1.CurrentDialog = "graphDialog";
                    await _userProfileStateAccessor.SetAsync(turnContext, usrState1);

                    var dialogResult1 = await CalDialog.Run(turnContext, _calDialogStateAccessor, cancellationToken);
                    if(dialogResult1 != null && dialogResult1.Status == DialogTurnStatus.Complete && luisResult0.GetTopScoringIntent().intent.ToLower() != "none")
                    {
                        var crmCalendar = new CRMCalendar();
                        Logger.LogInformation($"[Inside the Calendar.Event Intent] " + msgContext.Activity.AsMessageActivity()?.Text, msgContext.Activity.Conversation.Id);
                        await crmCalendar.AddEventToCalendar(luisResult0, msgContext, cancellationToken, _userProfileStateAccessor, Logger);
                    }
                    break;
                case CRMLuisModel.Intent.Greet_Farewell:
                    Logger.LogInformation($"[Inside the Greet.Farewell Intent] " + msgContext.Activity.AsMessageActivity()?.Text, msgContext.Activity.Conversation.Id);
                    var botAdapter = (BotFrameworkAdapter)turnContext.Adapter;
                    await botAdapter.SignOutUserAsync(turnContext, "crm", null, cancellationToken);
                    await botAdapter.SignOutUserAsync(turnContext, "graph", null, cancellationToken);
                    await turnContext.SendActivityAsync(MessageFactory.Text("b'bye \U0001F44B Take care"), cancellationToken);
                    await turnContext.SendActivityAsync(MessageFactory.Text("You have been signed out."), cancellationToken);
                    break;
                case CRMLuisModel.Intent.QueryProduct:
                    var product = luisResult0.GetEntity<string>("Product_Name");
                    Logger.LogInformation($"[Inside the QueryProduct Intent] " + msgContext.Activity.AsMessageActivity()?.Text, msgContext.Activity.Conversation.Id);
                    await CrmLead.GetProductInfo(await CrmDataConnection.GetAPI2(msgContext, _configuration,_userProfileStateAccessor), msgContext, product);
                    break;
                case CRMLuisModel.Intent.Lead_Registration:
                    var leadDialogState = await _leadDialogStateAccessor.GetAsync(turnContext);
                    if(leadDialogState == null || leadDialogState.DialogStack.Count == 0)
                        await turnContext.SendActivityAsync(MessageFactory.Text($"A new Lead is now being registered in the system."), cancellationToken);
                    Logger.LogInformation($"[Inside the Lead.Registration Intent] " + msgContext.Activity.AsMessageActivity()?.Text, msgContext.Activity.Conversation.Id);
                    var dialogResult2 = await MakeRootDialog(msgContext.Activity.ChannelId).Run(turnContext, _leadDialogStateAccessor, cancellationToken);
                    if (dialogResult2 != null && dialogResult2.Result != null)
                    {
                        if (dialogResult2.Result is LeadRegisterForm completedForm)
                        {
                            CrmLead.CreateNewLead(CrmDataConnection.GetAPIStaging(_configuration), completedForm);
                            await turnContext.SendActivityAsync(MessageFactory.Text($"The Lead has been registered in the System"), cancellationToken);
                        }
                    }
                    break;
                case CRMLuisModel.Intent.Report_ActiveCases:
                    Logger.LogInformation($"[Inside the Report.ActiveCases Intent] " + msgContext.Activity.AsMessageActivity()?.Text, msgContext.Activity.Conversation.Id);
                    var actionUrl3 = "https://app.powerbi.com/view?r=eyJrIjoiODk3ODQxNDgtMzU2Ni00YTRkLThjMGMtOTliMzNiMDBhMjc4IiwidCI6Ijc0YzNhNGIxLWEyYTUtNGU0OC05ZDdiLTQzNGYzNmQzMzVlZCIsImMiOjF9";
                    await CrmLead.ShowReport(msgContext, actionUrl3, "Active Cases");
                    break;
                case CRMLuisModel.Intent.Report_PerformanceKpi:
                    Logger.LogInformation($"[Inside the Report.PerformanceKpi Intent] " + msgContext.Activity.AsMessageActivity()?.Text, msgContext.Activity.Conversation.Id);
                    var actionUrl2 = "https://app.powerbi.com/view?r=eyJrIjoiZWVjODlhNjktZTcwYy00ZDU3LTllYjAtOTRlMjY1MTk0NGQwIiwidCI6Ijc0YzNhNGIxLWEyYTUtNGU0OC05ZDdiLTQzNGYzNmQzMzVlZCIsImMiOjF9";
                    await CrmLead.ShowReport(msgContext, actionUrl2, "Org Performance KPIs");
                    break;
                case CRMLuisModel.Intent.Report_SalesAnalytics:
                    Logger.LogInformation($"[Inside the Report.SalesAnalytics Intent] " + msgContext.Activity.AsMessageActivity()?.Text, msgContext.Activity.Conversation.Id);
                    var actionUrl1 = "https://app.powerbi.com/view?r=eyJrIjoiZTljNDg5MjctNjljYi00MTQ2LWJiMTktM2M1OTBkMzQ0OWVkIiwidCI6Ijc0YzNhNGIxLWEyYTUtNGU0OC05ZDdiLTQzNGYzNmQzMzVlZCIsImMiOjF9";
                    await CrmLead.ShowReport(msgContext, actionUrl1, "Sales Analytics Reports");
                    break;
                case CRMLuisModel.Intent.Report_SalesLeaderboard:
                    Logger.LogInformation($"[Inside the Report.SalesLeaderboard Intent] " + msgContext.Activity.AsMessageActivity()?.Text, msgContext.Activity.Conversation.Id);
                    var actionUrl = "https://app.powerbi.com/view?r=eyJrIjoiNzMxZmRlNjEtYmFkMy00NDMyLThlY2MtMzI3ZDc2ODE5MTdiIiwidCI6Ijc0YzNhNGIxLWEyYTUtNGU0OC05ZDdiLTQzNGYzNmQzMzVlZCIsImMiOjF9";
                    await CrmLead.ShowReport(msgContext, actionUrl, "Sales Leaderboard Reports");
                    break;
                case CRMLuisModel.Intent.SharePoint_TopLists:
                    Logger.LogInformation($"[Inside the SharePoint.TopLists Intent] " + msgContext.Activity.AsMessageActivity()?.Text, msgContext.Activity.Conversation.Id);
                    var count2 = GetNumber(luisResult0.Text);
                    var siteName = luisResult0.GetEntity<string>("Site_Name");
                    if (string.IsNullOrEmpty(siteName)) siteName = "DBS";
                    string siteUrl = "https://atidan2.sharepoint.com/sites/myrazor";
                    if (siteName.ToLower().Contains("dbs")) siteUrl += "/" + siteName;
                    if (siteName.ToLower().Contains("elwyn")) siteUrl += "/RazorPMO/Elwyn/ElwynCRM";
                    if (siteName.ToLower().Contains("elwyncrm")) siteUrl += "/RazorPMO/Elwyn/ElwynCRM";
                    await QuerySharePoint.GetTopLists(siteUrl, count2, msgContext, siteName, _configuration);
                    break;
                case CRMLuisModel.Intent.SharePoint_ListItems:
                    Logger.LogInformation($"[Inside the SharePoint.ListItems Intent] " + msgContext.Activity.AsMessageActivity()?.Text, msgContext.Activity.Conversation.Id);
                    var count1 = GetNumber(luisResult0.Text);
                    var listName = luisResult0.GetEntity<string>("List_Name");
                    if (string.IsNullOrEmpty(listName)) listName = "Documents";
                    if (listName.ToLower() == "status" || listName.ToLower() == "statusreports") listName = "Status Reports";
                    if (listName.ToLower() == "billing" || listName.ToLower() == "billingreports") listName = "Billing Reports";
                    await QuerySharePoint.GetListContents("https://atidan2.sharepoint.com/sites/myrazor/DBS", listName, count1, msgContext, _configuration);
                    break;
                case CRMLuisModel.Intent.None:
                default:
                    Logger.LogInformation($"[Inside the None Intent] " + msgContext.Activity.AsMessageActivity()?.Text, msgContext.Activity.Conversation.Id);
                    await turnContext.SendActivityAsync(MessageFactory.Text($"Sorry, I did not understand you. \U0001F44D"), cancellationToken);
                    await turnContext.SendActivityAsync(MessageFactory.Text($"I don't know what you want to do"), cancellationToken);
                    break;
            }
        }

        private int GetNumber(string query)
        {
            if (!string.IsNullOrWhiteSpace(query))
            {
                var values = query.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var item in values)
                {
                    int count = 0;
                    if (int.TryParse(item, out count) && count > 0)
                        return count;
                }
            }
            return 10;
        }

        internal static FormDialog<LeadRegisterForm> MakeRootDialog(string channelId)
        {
            BuildFormDelegate<LeadRegisterForm> MakeLeadRegisterForm = null;
            var isSkype = channelId.ToLowerInvariant() == "skypeforbusiness";
            if (isSkype)
                MakeLeadRegisterForm = () => LeadRegisterForm.BuildFormSkype();
            else
                MakeLeadRegisterForm = () => LeadRegisterForm.BuildForm();
            return FormDialog.FromForm(MakeLeadRegisterForm, options: FormOptions.PromptInStart);
        }

    }
}
