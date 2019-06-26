using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BotApplication.Forms;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Newtonsoft.Json.Linq;

namespace EchoBot1.Dialogs
{
    public static class DialogExtensions
    {
        public static async Task<DialogTurnResult> Run(this Dialog dialog, ITurnContext turnContext, IStatePropertyAccessor<DialogState> accessor, CancellationToken cancellationToken = default(CancellationToken), DialogContext ctx = null)
        {
            var dialogSet = new DialogSet(accessor);
            dialogSet.Add(dialog);

            var dialogContext = await dialogSet.CreateContextAsync(turnContext, cancellationToken);
            var results = await dialogContext.ContinueDialogAsync(cancellationToken);
            if (results.Status == DialogTurnStatus.Empty)
            {
                object options = null;
                if (dialog is LeadRegisterDialog)
                    options = new LeadRegisterForm();
                return await dialogContext.BeginDialogAsync(dialog.Id, options, cancellationToken);
            }
            return results;
        }

        public static T GetEntity<T>(this RecognizerResult luisResult, string entityKey, string valuePropertyName = "text")
        {
            if (luisResult != null)
            {
                //// var value = (luisResult.Entities["$instance"][entityKey][0]["text"] as JValue).Value;
                var data = luisResult.Entities as IDictionary<string, JToken>;

                if (data.TryGetValue("$instance", out JToken value))
                {
                    var entities = value as IDictionary<string, JToken>;
                    if (entities.TryGetValue(entityKey, out JToken targetEntity))
                    {
                        var entityArray = targetEntity as JArray;
                        if (entityArray.Count > 0)
                        {
                            var values = entityArray[0] as IDictionary<string, JToken>;
                            if (values.TryGetValue(valuePropertyName, out JToken textValue))
                            {
                                var text = textValue as JValue;
                                if (text != null)
                                {
                                    return (T)text.Value;
                                }
                            }
                        }
                    }
                }
            }

            return default(T);
        }
    }
}
