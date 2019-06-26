using EchoBot1.CRM;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using Xrm.Tools.WebAPI;
using Xrm.Tools.WebAPI.Requests;

namespace BotApplication.CRM
{
    [Serializable]
    public class CrmDataConnection
    {

        public static async Task<CRMWebAPI> GetAPI(IConfiguration ConfigurationManager)
        {
            string clientSecret = ConfigurationManager["ClientSecret"];
            string authority = ConfigurationManager["AdOath2AuthEndpoint"];// "https://login.microsoftonline.com/common?client_secret=";
            string clientId = ConfigurationManager["AdClientId"];
            string crmBaseUrl = ConfigurationManager["CrmServerUrl"]; //+ "?client_secret=" + clientSecret;
            string tenantID = ConfigurationManager["TenantId"];
            string authResource = "https://login.windows.net/" + tenantID + "/";

            var authContext = new AuthenticationContext(authResource, false);
            //UserCredential userCreds = new UserCredential(ConfigurationManager.AppSettings["CrmUsername"], ConfigurationManager.AppSettings["CrmPassword"]);
            var userCreds = new UserCredential();
            //var result = authContext.AcquireToken(crmBaseUrl, clientId, new Uri("http://localhost:3979"),PromptBehavior.Auto,UserIdentifier.AnyUser,"client_secret=" + clientSecret);
            var result = await authContext.AcquireTokenAsync(crmBaseUrl, clientId, userCreds);
            CRMWebAPI api = new CRMWebAPI(crmBaseUrl + "/api/data/v8.1/", result.AccessToken);

            return api;
        }

        public static async Task<CRMWebAPI> GetAPI2(ITurnContext<IMessageActivity> context, IConfiguration ConfigurationManager, IStatePropertyAccessor<UserProfileState> userState)
        {
            string authority = "https://login.microsoftonline.com/";
            string clientId = ConfigurationManager["AdClientId"];
            string crmBaseUrl = ConfigurationManager["CrmServerUrlProd"];
            string clientSecret = ConfigurationManager["ClientSecret"];
            string tenantID = ConfigurationManager["TenantId"];

            var clientcred = new Microsoft.IdentityModel.Clients.ActiveDirectory.ClientCredential(clientId, clientSecret);
            //var authContext = new AuthenticationContext(authority + tenantID);
            //var authenticationResult = authContext.AcquireToken(crmBaseUrl, clientcred);
            //var accessToken = GetAccessToken();
            var accessToken = await GetCurrentAccessToken(context, ConfigurationManager, userState);
            return new CRMWebAPI(crmBaseUrl + "/api/data/v8.1/", accessToken);
        }


        public static bool IsTokenValid(ITurnContext<IMessageActivity> context, string accessToken, IConfiguration ConfigurationManager)
        {
            var result = false;
            Task.Run(async () =>
            {
                string crmBaseUrl = ConfigurationManager["CrmServerUrlProd"];
                var api = new CRMWebAPI(crmBaseUrl + "/api/data/v8.1/", accessToken);
                try
                {
                    var results = await api.GetList("accounts", new CRMGetListOptions() { Top = 1 });
                    if (results != null && results.Count >= 0)
                        result = true;
                }
                catch (Exception ex)
                {
                }
            }).Wait();

            return result;
        }

        public static async Task<string> GetCurrentAccessToken(ITurnContext<IMessageActivity> context, IConfiguration ConfigurationManager, IStatePropertyAccessor<UserProfileState> userState)
        {
            var accesstoken = await userState.GetAsync(context);
            return accesstoken.AccessToken;
        }

        public static async Task<string> GetCurrentCalAccessToken(ITurnContext<IMessageActivity> context, IStatePropertyAccessor<UserProfileState> userState)
        {
            var accesstoken = await userState.GetAsync(context);
            return accesstoken.CalAccessToken;
        }

        public static string GetAccessToken(IConfiguration ConfigurationManager)
        {
            string tenantID = ConfigurationManager["TenantId"];
            string crmBaseUrl = ConfigurationManager["CrmServerUrl"];
            string authResource = "https://login.windows.net/" + tenantID + "/";

            var username = ConfigurationManager["CrmUsername"].ToString();
            var password = ConfigurationManager["CrmPassword"].ToString();

            string clientId = ConfigurationManager["AdClientId"];

            HttpClientHandler clientHandler = new HttpClientHandler()
            {
                UseDefaultCredentials = true
            };
            using (HttpClient httpClient = new HttpClient(clientHandler))
            {
                httpClient.BaseAddress = new Uri(crmBaseUrl);
                httpClient.Timeout = new TimeSpan(0, 0, 15);  // 2 minutes

                string clientSecret = ConfigurationManager["ClientSecret"];

                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("client_id", clientId),
                    new KeyValuePair<string, string>("client_secret", clientSecret),
                    new KeyValuePair<string, string>("resource", crmBaseUrl),
                    new KeyValuePair<string, string>("username", username),
                    new KeyValuePair<string, string>("password", password),
                    new KeyValuePair<string, string>("grant_type", "password")
                });
                var result = httpClient.PostAsync(authResource + "oauth2/token", content);
                string responseBody = result.Result.Content.ReadAsStringAsync().Result;
                var accessToken = JObject.Parse(responseBody).GetValue("access_token").ToString();

                return accessToken;
            }
        }

        public static CRMWebAPI GetAPIStaging(IConfiguration ConfigurationManager)
        {
            string crmBaseUrl = ConfigurationManager["CrmServerUrl"];
            var accessToken = GetAccessToken(ConfigurationManager);
            return new CRMWebAPI(crmBaseUrl + "/api/data/v8.1/", accessToken);
        }
        public static CRMWebAPI GetAPIProd(IConfiguration ConfigurationManager)
        {
            string crmBaseUrl = ConfigurationManager["CrmServerUrlProd"];
            var accessToken = GetAccessTokenProd("", ConfigurationManager);
            return new CRMWebAPI(crmBaseUrl + "/api/data/v8.1/", accessToken);
        }

        public static string GetAccessTokenProd(string resourceUrl, IConfiguration ConfigurationManager)
        {
            string tenantID = ConfigurationManager["TenantId"];
            string crmBaseUrl = ConfigurationManager["CrmServerUrlProd"];
            string authResource = "https://login.windows.net/" + tenantID + "/";

            if (string.IsNullOrWhiteSpace(resourceUrl))
                resourceUrl = crmBaseUrl;

            var username = ConfigurationManager["CrmUsername"].ToString();
            var password = ConfigurationManager["CrmPassword"].ToString();

            string clientId = ConfigurationManager["AdClientIdProd"];

            HttpClientHandler clientHandler = new HttpClientHandler()
            {
                UseDefaultCredentials = true
            };
            using (HttpClient httpClient = new HttpClient(clientHandler))
            {
                httpClient.BaseAddress = new Uri(crmBaseUrl);
                httpClient.Timeout = new TimeSpan(0, 0, 15);  // 2 minutes

                string clientSecret = ConfigurationManager["ClientSecretProd"];

                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("client_id", clientId),
                    new KeyValuePair<string, string>("client_secret", clientSecret),
                    new KeyValuePair<string, string>("resource", resourceUrl),
                    new KeyValuePair<string, string>("username", username),
                    new KeyValuePair<string, string>("password", password),
                    new KeyValuePair<string, string>("grant_type", "password")
                });
                var result = httpClient.PostAsync(authResource + "oauth2/token", content);
                string responseBody = result.Result.Content.ReadAsStringAsync().Result;
                var accessToken = JObject.Parse(responseBody).GetValue("access_token").ToString();

                return accessToken;
            }
        }


        public static async Task<HttpResponseMessage> CrmWebApiRequest(string apiRequest, HttpContent requestContent, string requestType,  IConfiguration ConfigurationManager)
        {
            AuthenticationContext authContext = new AuthenticationContext(ConfigurationManager["AdOath2AuthEndpoint"], false);
            UserCredential credentials = new UserCredential(ConfigurationManager["CrmUsername"]);
            Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationResult tokenResult = await authContext.AcquireTokenAsync(ConfigurationManager["CrmServerUrl"],
                ConfigurationManager["AdClientId"], credentials);

            HttpResponseMessage apiResponse;

            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(ConfigurationManager["CrmServerUrl"]);
                httpClient.Timeout = new TimeSpan(0, 2, 0);
                httpClient.DefaultRequestHeaders.Add("OData-MaxVersion", "4.0");
                httpClient.DefaultRequestHeaders.Add("OData-Version", "4.0");
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.AccessToken);

                if (requestType == "retrieve")
                {
                    apiResponse = await httpClient.GetAsync(apiRequest);
                }
                else if (requestType == "create")
                {
                    apiResponse = await httpClient.PostAsync(apiRequest, requestContent);
                }
                else
                {
                    apiResponse = null;
                }
            }
            return apiResponse;
        }

        public static async Task<string> GetAccessTokenCal(ITurnContext<IMessageActivity> context, IConfiguration ConfigurationManager, IStatePropertyAccessor<UserProfileState> userState)
        {
            string accessToken = null;

            // Load the app config from web.config
            string appId = ConfigurationManager["ida:AppId"];
            string appPassword = ConfigurationManager["ida:AppPassword"];
            string redirectUri = ConfigurationManager["ida:RedirectUri"];
            string[] scopes = ConfigurationManager["ida:AppScopes"]
                .Replace(' ', ',').Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            // Get the current user's ID
            string userId = ClaimsPrincipal.Current.Claims.FirstOrDefault(x => x.Type == "iss").Value;
            userId = userId.Replace("https://login.microsoftonline.com/", "");
            userId = userId.Substring(0, userId.IndexOf("/"));
            if (!string.IsNullOrEmpty(userId))
            {
                var accessCode = await GetCurrentAccessCode(context, userState);
                var ccd = ConfidentialClientApplicationBuilder.Create(appId).WithRedirectUri(redirectUri).WithClientSecret(appPassword).Build();

                //ConfidentialClientApplication cca = new ConfidentialClientApplication(
                //    appId, redirectUri, new Microsoft.Identity.Client.ClientCredential(appPassword), null, null);

                // Call AcquireTokenSilentAsync, which will return the cached
                // access token if it has not expired. If it has expired, it will
                // handle using the refresh token to get a new one.
                Microsoft.Identity.Client.AuthenticationResult result = await ccd.AcquireTokenByAuthorizationCode(scopes, accessCode).ExecuteAsync();

                accessToken = result.AccessToken;
            }

            return accessToken;
        }
        public static async Task<string> GetCurrentAccessCode(ITurnContext<IMessageActivity> context, IStatePropertyAccessor<UserProfileState> userState)
        {
            var accesstoken = await userState.GetAsync(context);
            return accesstoken.AccessCode;
        }

        public static async Task<string> GetCurrentEmail(ITurnContext<IMessageActivity> context, IStatePropertyAccessor<UserProfileState> userState)
        {
            var accesstoken = await userState.GetAsync(context);
            return accesstoken.UserEmail;
        }

        public static async Task<string> GetUserEmail(string accessToken)
        {
            GraphServiceClient client = new GraphServiceClient(
                new DelegateAuthenticationProvider(
                    async (requestMessage) =>
                    {
                        requestMessage.Headers.Authorization =
                            new AuthenticationHeaderValue("Bearer", accessToken);
                    }));

            // Get the user's email address
            try
            {
                Microsoft.Graph.User user = await client.Me.Request().GetAsync();
                return user.Mail;
            }
            catch (ServiceException ex)
            {
                return string.Format("#ERROR#: Could not get user's email address. {0}", ex.Message);
            }
        }

        public static async Task AddEventToCalendar(ITurnContext<IMessageActivity> context, IStatePropertyAccessor<UserProfileState> userState, DateTime? currentDate = null)
        {
            //string token = GetAccessTokenProd("https://graph.microsoft.com");
            string token = await GetCurrentCalAccessToken(context, userState);
            string userEmail = await GetCurrentEmail(context, userState);

            GraphServiceClient client = new GraphServiceClient(
                new DelegateAuthenticationProvider(
                    (requestMessage) =>
                    {
                        requestMessage.Headers.Authorization =
                            new AuthenticationHeaderValue("Bearer", token);

                        requestMessage.Headers.Add("X-AnchorMailbox", userEmail);

                        return Task.FromResult(0);
                    }));

            try
            {
                var start = "2017-12-09T00:00:00";
                var end = "2017-12-10T00:00:00";

                if(currentDate != null && currentDate.Value != DateTime.MinValue)
                {
                    start = currentDate.Value.ToString("yyyy-MM-ddTHH:mm:ss");
                    end = currentDate.Value.AddDays(1).ToString("yyyy-MM-ddTHH:mm:ss");
                }

                var event1 = new Event();
                event1.Start = new DateTimeTimeZone() { DateTime = start, TimeZone = "Pacific Standard Time" };
                event1.End = new DateTimeTimeZone() { DateTime = end, TimeZone = "Pacific Standard Time" };
                event1.IsAllDay = true;
                event1.Location = new Location() { Address = new PhysicalAddress() { Street = "21731 Saticoy Street", City = "Los Angeles", State = "CA" } };
                event1.Subject = "Scheduled appointment For ";
                event1.Subject += currentDate != null ? currentDate.Value.ToLongDateString() : "09th Dec 2017";

                await client.Me.Events.Request().AddAsync(event1);
            }
            catch (ServiceException ex)
            {

            }
        }


    }
}