using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;
using Xrm.Tools.WebAPI;
using Xrm.Tools.WebAPI.Requests;
using AdaptiveCards;
using BotApplication.Forms;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Builder;
using System.Threading;

namespace BotApplication.CRM
{
    [Serializable]
    public class CrmLead
    {
        #region CRM Attributes
        public static string EntityName = "lead";
        public static string Field_Subject = "subject";
        public static string Field_Description = "description";
        public static string Field_FirstName = "firstname";
        #endregion


        #region private static List<CardAction> CreateButtons()
        public static List<CardAction> CreateButtons(List<string> names)
        {
            List<CardAction> cardButtons = new List<CardAction>();
            foreach (var item in names)
            {
                CardAction CardButton = new CardAction()
                {
                    Type = "imBack",
                    Title = item,
                    Value = item
                };
                cardButtons.Add(CardButton);
            }
            return cardButtons;
        }
        #endregion


        public static async Task ShowReport(ITurnContext<IMessageActivity> context, string url, string name)
        {
            await Task.Run(async () =>
            {
                var card = new HeroCard
                {
                    Buttons = new List<CardAction>()
                };
                var action = new CardAction();
                if (context.Activity.ChannelId.ToLowerInvariant() == "skypeforbusiness")
                {
                    action = new CardAction
                    {
                        Title = "",
                        Value = $"<a href='{url}' target='_blank'>{name}</a>",
                        Type = ActionTypes.Signin
                    };
                }
                else
                {

                    action = new CardAction
                    {
                        Title = name,
                        Value = url,
                        Type = ActionTypes.OpenUrl
                    };
                }
                card.Buttons.Add(action);
                await DisplayMessage(card, context);

            });
        }

        public static void AddReportColumn(AdaptiveColumnSet reports, string url, string name)
        {
            var column = new AdaptiveColumn();
            reports.Columns.Add(column);
            column.Items = new List<AdaptiveElement>();
            column.Items.Add(new AdaptiveTextBlock() { Text = "**" + name.ToUpper() + "**" });
            var action = new AdaptiveOpenUrlAction
            {
                Url = new Uri(url)
            };
            column.SelectAction = action;
        }

        public static async Task GetTopAccounts(CRMWebAPI api, ITurnContext<IMessageActivity> context, int count)
        {
            await Task.Run(async () =>
            {
                var card = new HeroCard();
                var names = new List<string>();
                //var results = await api.GetList("accounts", new CRMGetListOptions() { Top = 5, FormattedValues = true });
                string fetchXml = "<fetch mapping='logical' count='{0}'><entity name='account'><attribute name='accountid'/><attribute name='name'/></entity></fetch>";
                fetchXml = string.Format(fetchXml, count);
                var fetchResults = await api.GetList("accounts", QueryOptions: new CRMGetListOptions() { FetchXml = fetchXml });

                foreach (var item in fetchResults.List)
                {
                    IDictionary<string, object> propertyValues = item;

                    foreach (var property in propertyValues.Keys)
                    {
                        if (property.ToLowerInvariant() == "name")
                        {
                            names.Add(propertyValues[property].ToString());
                        }
                        //await context.PostAsync(String.Format("{0} : {1}", property, propertyValues[property]));
                    }
                }
                card.Buttons = CreateButtons(names);
                await DisplayMessage(card, context);
            });
        }

        public static async Task GetAccountCounts(CRMWebAPI api, ITurnContext<IMessageActivity> context)
        {
            await Task.Run(async () =>
            {
                dynamic whoamiResults = await api.ExecuteFunction("WhoAmI");
                var opt = new CRMGetListOptions
                {
                    Top = 5000,
                    Filter = "_createdby_value eq " + whoamiResults.UserId + " or _modifiedby_value eq " + whoamiResults.UserId
                };
                var count = await api.GetList("accounts", QueryOptions: opt);
                if (count == null)
                {
                    count = new Xrm.Tools.WebAPI.Results.CRMGetListResult<ExpandoObject>();
                    count.List = new List<ExpandoObject>();
                }
                else if (count.List == null)
                    count.List = new List<ExpandoObject>();

                var card = new HeroCard();
        
                var action = new CardAction()
                {
                    Type = ActionTypes.MessageBack,
                    Title = $"Accounts owned by me: {count.List.Count}",
                    Value = count.ToString()
                };
                card.Buttons = new List<CardAction>();
                card.Buttons.Add(action);
                await DisplayMessage(card, context);
            });
        }

        public static async Task GetAllAccountCounts(CRMWebAPI api, ITurnContext<IMessageActivity> context)
        {
            await Task.Run(async () =>
            {
                var count = await api.GetCount("accounts");
                var card = new HeroCard();

                var action = new CardAction()
                {
                    Type = ActionTypes.MessageBack,
                    Title = $"Accounts in CRM: {count}",
                    Value = count.ToString()
                };
                card.Buttons = new List<CardAction>();
                card.Buttons.Add(action);
                await DisplayMessage(card, context);
            });
        }

        public static async Task DisplayMessage(AdaptiveCard card, ITurnContext<IMessageActivity> context, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Create the attachment with adapative card.  
            Attachment attachment = new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = card
            };
            var reply = MessageFactory.Attachment(attachment);
            await context.SendActivityAsync(reply, cancellationToken);
        }

        public static async Task DisplayMessage(HeroCard card, ITurnContext<IMessageActivity> context, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Create the attachment with adapative card.  
            Attachment attachment = new Attachment()
            {
                ContentType = HeroCard.ContentType,
                Content = card
            };
            var reply = MessageFactory.Attachment(attachment);
            await context.SendActivityAsync(reply, cancellationToken);
        }

        public static void CreateNewLead(CRMWebAPI api, LeadRegisterForm result)
        {
            Task.Run(async () =>
            {
                dynamic data = new ExpandoObject();
                data.subject = $"New Lead Registration Request by {result.Name}";
                data.firstname = result.Name;
                data.description = $@"New Lead registration request summary:
                                    {Environment.NewLine}Product Requested : {result.Product},
                                    {Environment.NewLine}Accounts Interested In: {result.Accounts},
                                    {Environment.NewLine}Requester's Gender: {result.Gender},
                                    {Environment.NewLine}Customer Name: {result.Name},
                                    {Environment.NewLine}Total Registrations: {result.TotalAttendees},
                                    {Environment.NewLine}Complementary Drink: {result.ComplementoryDrink}";

                var leadGuid = await api.Create("leads", data);
            }).Wait();                        

        }


        public static async Task GetTopProducts(CRMWebAPI api, ITurnContext<IMessageActivity> context)
        {
            await Task.Run(async () =>
            {
                var card = new HeroCard();
                var names = new List<string>();
                //var results = await api.GetList("accounts", new CRMGetListOptions() { Top = 5, FormattedValues = true });
                string fetchXml = "<fetch mapping='logical' count='100'><entity name='product'><attribute name='productid'/><attribute name='name'/></entity></fetch>";
                var fetchResults = await api.GetList("products", QueryOptions: new CRMGetListOptions() { FetchXml = fetchXml });

                foreach (var item in fetchResults.List)
                {
                    IDictionary<string, object> propertyValues = item;

                    foreach (var property in propertyValues.Keys)
                    {
                        if (property.ToLowerInvariant() == "name")
                        {
                            names.Add(propertyValues[property].ToString());
                        }
                        //await context.PostAsync(String.Format("{0} : {1}", property, propertyValues[property]));
                    }
                }
                card.Buttons = CreateButtons(names);
                await DisplayMessage(card, context);
            });
        }

        public static async Task GetProductInfo(CRMWebAPI api, ITurnContext<IMessageActivity> context, string product)
        {
            var isTeams = context.Activity.ChannelId.ToLowerInvariant() == "msteams";
            var isSkype = context.Activity.ChannelId.ToLowerInvariant() == "skypeforbusiness";
            await Task.Run(async () =>
            {
                var card = new HeroCard();
                var adaptCard = new AdaptiveCard(new AdaptiveSchemaVersion());
                var factSet = new AdaptiveChoiceSetInput() { Choices = new List<AdaptiveChoice>(), Spacing = AdaptiveSpacing.Medium };
                //card.Body.Add(factSet);

                var names = new List<string>();
                string fetchXml = @"<fetch mapping='logical'>
                                        <entity name='product'> 
                                          <attribute name='productid'/> 
                                          <attribute name='name'/> 
                                        <filter type='and'> 
                                            <condition attribute='name' operator='eq' value='{0}' /> 
                                        </filter> 
                                       </entity> 
                                </fetch>";
                fetchXml = string.Format(fetchXml, product);
                var fetchResults = await api.GetList("products", QueryOptions: new CRMGetListOptions() { FetchXml = fetchXml });

                foreach (var item in fetchResults.List)
                {
                    IDictionary<string, object> propertyValues = item;

                    foreach (var property in propertyValues.Keys)
                    {
                        string text = $"*{property}* : {propertyValues[property].ToString()}";
                        if (isTeams || isSkype)
                            card.Buttons.Add(new CardAction() { Text = text, Value = text, Type = ActionTypes.MessageBack });
                        else
                            adaptCard.Body.Add(new AdaptiveTextBlock() { Text = text });
                    }
                }
                if(isTeams || isSkype)
                    await DisplayMessage(card, context);
                else
                    await DisplayMessage(adaptCard, context);
            });
        }

    }
}