using System.Threading;
using System.Threading.Tasks;
using EchoBot1.CRM;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

namespace EchoBot1.Dialogs
{
    public class LogoutDialog : ComponentDialog
    {
        private readonly UserState _userState11;
        private readonly IStatePropertyAccessor<UserProfileState> _userProfileStateAccessor11;
        public LogoutDialog(string id, string connectionName, UserState userState)
            : base(id)
        {
            ConnectionName = connectionName;
            _userState11 = userState;
            _userProfileStateAccessor11 = _userState11.CreateProperty<UserProfileState>(nameof(UserProfileState));
        }

        protected string ConnectionName { get; private set; }

        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext innerDc, object options, CancellationToken cancellationToken)
        {
            var result = await InterruptAsync(innerDc, cancellationToken);
            if (result != null)
            {
                return result;
            }

            return await base.OnBeginDialogAsync(innerDc, options, cancellationToken);
        }

        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext innerDc, CancellationToken cancellationToken)
        {
            var result = await InterruptAsync(innerDc, cancellationToken);
            if (result != null)
            {
                return result;
            }

            return await base.OnContinueDialogAsync(innerDc, cancellationToken);
        }

        private async Task<DialogTurnResult> InterruptAsync(DialogContext innerDc, CancellationToken cancellationToken)
        {
            if (innerDc.Context.Activity.Type == ActivityTypes.Message)
            {
                var text = innerDc.Context.Activity.Text.ToLowerInvariant();

                if (text == "logout")
                {
                    // The bot adapter encapsulates the authentication processes.
                    var botAdapter = (BotFrameworkAdapter)innerDc.Context.Adapter;
                    await _userProfileStateAccessor11.SetAsync(innerDc.Context, null);
                    //await botAdapter.SignOutUserAsync(innerDc.Context, ConnectionName, null, cancellationToken);
                    await botAdapter.SignOutUserAsync(innerDc.Context, "crm", null, cancellationToken);
                    await botAdapter.SignOutUserAsync(innerDc.Context, "graph", null, cancellationToken);
                    await innerDc.Context.SendActivityAsync(MessageFactory.Text("You have been signed out."), cancellationToken);
                    return await innerDc.CancelAllDialogsAsync();
                }
            }
            return null;
        }
    }
}
