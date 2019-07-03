using BotApplication.CRM;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EchoBot1.SharePoint
{
    public class QuerySharePoint
    {
        public static async Task AddTask(string webUrl, ITurnContext<IMessageActivity> turnContext, IConfiguration ConfigurationManager)
        {
            var taskId = Guid.NewGuid();
            List<string> titles = new List<string>
            {
                $"A task with Id {taskId} added to Tasks List"
            };

            var userName = ConfigurationManager["CrmUsername"].ToString();
            var password = ConfigurationManager["CrmPassword"].ToString();

            await Task.Run(async () =>
            {

                try
                {
                    using (var context = new ClientContext(webUrl))
                    {
                        context.Credentials = new SharePointOnlineCredentials(userName, password);
                        Web web = context.Web;
                        var oList = context.Web.Lists.GetByTitle("Tasks");

                        ListItemCreationInformation itemCreateInfo = new ListItemCreationInformation();
                        ListItem oListItem = oList.AddItem(itemCreateInfo);
                        oListItem["Title"] = $"A New Task with Id of {taskId} added!";
                        oListItem["Body"] = "Hello Razor Tech!";
                        oListItem["Status"] = "Started";
                        oListItem["StartDate"] = DateTime.Now.ToLongDateString();
                        oListItem["DueDate"] = DateTime.Now.AddDays(2).ToLongDateString();

                        oListItem.Update();
                        await context.ExecuteQueryAsync();
                    }
                }
                catch (Exception ex)
                {
                    titles.Add(ex.Message);
                }

                var card = new HeroCard
                {
                    Buttons = CrmLead.CreateButtons(titles)
                };
                await CrmLead.DisplayMessage(card, turnContext);
            });
        }
        public static async Task GetTopLists(string webUrl, int count, ITurnContext<IMessageActivity> turnContext, string siteName, IConfiguration ConfigurationManager)
        {
            List<string> titles = new List<string>
            {
                $"{siteName} contains the following top {count} lists"
            };

            var userName = ConfigurationManager["CrmUsername"].ToString();
            var password = ConfigurationManager["CrmPassword"].ToString();

            await Task.Run(async () =>
            {
                try
                {
                    using (var context = new ClientContext(webUrl))
                    {
                        context.Credentials = new SharePointOnlineCredentials(userName, password);
                        Web web = context.Web;
                        context.Load(web.Lists,
                            lists => lists.Include(list => list.Title,
                                list => list.Id));
                        await context.ExecuteQueryAsync();
                        Console.ForegroundColor = ConsoleColor.White;
                        int runningCount = 0;
                        foreach (List list in web.Lists.ToList())
                        {
                            runningCount++;
                            titles.Add($"List title is: {list.Title}");
                            if (runningCount >= count) break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error is: " + ex.Message + "   " + ex.StackTrace);
                    Console.ReadLine();
                }

                var card = new HeroCard
                {
                    Buttons = CrmLead.CreateButtons(titles)
                };
                await CrmLead.DisplayMessage(card, turnContext);
            });
        }


        public static async Task GetListContents(string webUrl, string listName, int count, ITurnContext<IMessageActivity> turnContext, IConfiguration ConfigurationManager)
        {
            List<string> titles = new List<string>
            {
                $"{listName} contains the following top {count} list ITEMS"
            };
            var userName = ConfigurationManager["CrmUsername"].ToString();
            var password = ConfigurationManager["CrmPassword"].ToString();

            await Task.Run(async () =>
            {
                try
                {
                    using (var context = new ClientContext(webUrl))
                    {
                        context.Credentials = new SharePointOnlineCredentials(userName, password);
                        Web web = context.Web;

                        // Assume the web has a list named "Announcements". 
                        List statusReports = context.Web.Lists.GetByTitle(listName);

                        // This creates a CamlQuery that has a RowLimit of 100, and also specifies Scope="RecursiveAll" 
                        // so that it grabs all list items, regardless of the folder they are in. 
                        CamlQuery query = CamlQuery.CreateAllItemsQuery(count, new string[] { "Name", "Title", "Client", "Project" });
                        ListItemCollection items = statusReports.GetItems(query);

                        // Retrieve all items in the ListItemCollection from List.GetItems(Query). 
                        context.Load(items);
                        await context.ExecuteQueryAsync();
                        foreach (ListItem listItem in items)
                        {
                            if (listName.ToLower().Contains("reports"))
                            {
                                var client = listItem["Client"] as FieldLookupValue;
                                var project = listItem["Project"] as FieldLookupValue;
                                var fileReference = listItem["FileLeafRef"];
                                // We have all the list item data. For example, Title. 
                                var title = client?.LookupValue + ", " + project?.LookupValue + ", " + fileReference + Environment.NewLine;
                                titles.Add(title);
                            }
                            else
                            {
                                titles.Add((string)listItem["FileRef"]);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error is: " + ex.Message + "   " + ex.StackTrace);
                    Console.ReadLine();
                }

                var card = new HeroCard
                {
                    Buttons = CrmLead.CreateButtons(titles)
                };
                await CrmLead.DisplayMessage(card, turnContext);
            });
        }


    }
}
