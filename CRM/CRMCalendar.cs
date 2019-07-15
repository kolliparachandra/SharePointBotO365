using Bot.Builder.Community.Dialogs.FormFlow.Luis.Models;
using EchoBot1.Bots;
using Microsoft.Bot.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EchoBot1.Dialogs;
using System.Threading;
using Microsoft.Bot.Schema;
using BotApplication.CRM;
using Microsoft.Extensions.Logging;

namespace EchoBot1.CRM
{
    public class CRMCalendar
    {

        public async Task AddEventToCalendar(RecognizerResult luisResult, ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken, IStatePropertyAccessor<UserProfileState> userStateAccessor, ILogger Logger)
        {
            bool eventAdded = false;
            
            object entitydate = null;
            entitydate = luisResult.GetEntity<object>("datetime", "text");
            if (entitydate != null)
            {
                DateTime currentDate;
                if (entitydate != null)
                {
                    if (DateTime.TryParse(entitydate.ToString(), out currentDate))
                    {
                        eventAdded = true;
                        Logger.LogInformation($"[Inside the Calendar.Event Intent success event] " + turnContext.Activity.AsMessageActivity()?.Text, turnContext.Activity.Conversation.Id);
                        await CrmDataConnection.AddEventToCalendar(turnContext, userStateAccessor, currentDate);
                        await turnContext.SendActivityAsync(MessageFactory.Text($"Added event to your calendar for : {currentDate.ToLongDateString()}"), cancellationToken);
                    }
                    else if (Enum.TryParse<DayOfWeek>(entitydate.ToString(), out DayOfWeek currentDay))
                    {
                        //var dayOfWeek = (DayOfWeek)Enum.Parse(typeof(DayOfWeek), entitydate.ToString(), true);
                        currentDate = DateTime.Now.ClosestWeekDay(currentDay);
                        eventAdded = true;
                        Logger.LogInformation($"[Inside the Calendar.Event Intent success event] " + turnContext.Activity.AsMessageActivity()?.Text, turnContext.Activity.Conversation.Id);
                        await CrmDataConnection.AddEventToCalendar(turnContext, userStateAccessor, currentDate);
                        await turnContext.SendActivityAsync(MessageFactory.Text($"Added event to your calendar for : {currentDate.ToLongDateString()}"), cancellationToken);
                    }
                }
            }
            if (!eventAdded)
            {
                Logger.LogInformation($"[Inside the Calendar.Event Intent success event] " + turnContext.Activity.AsMessageActivity()?.Text, turnContext.Activity.Conversation.Id);
                await CrmDataConnection.AddEventToCalendar(turnContext, userStateAccessor);
                await turnContext.SendActivityAsync(MessageFactory.Text($"Added event to your calendar for : 9th Dec 2017"), cancellationToken);
            }
        }

    }
}
