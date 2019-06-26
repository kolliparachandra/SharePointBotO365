// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio EchoBot v4.3.0

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using EchoBot1.Bots;
using Microsoft.Bot.Builder.AI.Luis;
using EchoBot1.Controllers;
using EchoBot1.Dialogs;
using Microsoft.Bot.Configuration;
using System;
using System.Linq;
using System.Collections.Generic;

namespace EchoBot1
{
    public class Startup
    {
        private bool _isProduction = false;
        public Startup(IConfiguration configuration, IHostingEnvironment env)
        {
            _isProduction = env.IsProduction();
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            // Create the credential provider to be used with the Bot Framework Adapter.
            services.AddSingleton<ICredentialProvider, ConfigurationCredentialProvider>();

            // Create the Bot Framework Adapter.
            services.AddSingleton<IBotFrameworkHttpAdapter, AdapterWithErrorHandler>();

                        // Un-comment the following lines to use Azure Blob Storage
            // // Storage configuration name or ID from the .bot file.
            // const string StorageConfigurationId = "<STORAGE-NAME-OR-ID-FROM-BOT-FILE>";
            // var blobConfig = botConfig.FindServiceByNameOrId(StorageConfigurationId);
            // if (!(blobConfig is BlobStorageService blobStorageConfig))
            // {
            //    throw new InvalidOperationException($"The .bot file does not contain an blob storage with name '{StorageConfigurationId}'.");
            // }
            // // Default container name.
            // const string DefaultBotContainer = "botstate";
            // var storageContainer = string.IsNullOrWhiteSpace(blobStorageConfig.Container) ? DefaultBotContainer : blobStorageConfig.Container;
            // IStorage dataStore = new Microsoft.Bot.Builder.Azure.AzureBlobStorage(blobStorageConfig.ConnectionString, storageContainer);

            // Create the storage we'll be using for User and Conversation state. (Memory is great for testing purposes.) 
            services.AddSingleton<IStorage, MemoryStorage>();

            // Create the User state. (Used in this bot's Dialog implementation.)
            services.AddSingleton<UserState>();

            // Create the Conversation state. (Used by the Dialog system itself.)
            services.AddSingleton<ConversationState>();

            // Loads .bot configuration file and adds a singleton that your Bot can access through dependency injection.
            BotConfiguration botConfig = null;
            try
            {
                botConfig = BotConfiguration.Load(@".\SharePointBot.bot", "PsORifD1iMKAL92f/1ucFVQVLU2fSV1h4Pc+ZarSjZk=");
            }
            catch
            {
                var msg = @"Error reading bot file.";
                throw new InvalidOperationException(msg);
            }

            services.AddSingleton(sp => botConfig ?? throw new InvalidOperationException($"The .bot config file could not be loaded. ({botConfig})"));

            // Add BotServices singleton.
            // Create the connected services from .bot file.
            services.AddSingleton(sp => new BotServices(botConfig));

            // Retrieve current endpoint.
            var environment = _isProduction ? "production" : "development";
            var service = botConfig.Services.Where(s => s.Type == "endpoint" && s.Name == environment).FirstOrDefault();
            if (!(service is EndpointService endpointService))
            {
                throw new InvalidOperationException($"The .bot file does not contain an endpoint with name '{environment}'.");
            }

            var luisService = botConfig.Services.Where(s => s.Type == "luis" && s.Name == "CRM Helper").FirstOrDefault();
            if (luisService == null || !(luisService is LuisService))
            {
                throw new InvalidOperationException($"The .bot file does not contain an Luis Service with name 'CRM Helper'.");
            }

            // Create and register a LUIS recognizer.
            services.AddSingleton(sp =>
            {
                var luisSvc = (LuisService)luisService;
                // Get LUIS information
                //var luisApp = new LuisApplication(Constants.CRM_HELPER_APP, "6587e88502964c1e9dca1af530ba4ffa", luisSvc.GetEndpoint());
                var luisApp = new LuisApplication(Constants.CRM_HELPER_APP, "c7516642105b4b1093c5fb9212b34653", luisSvc.GetEndpoint());

                // Specify LUIS options. These may vary for your bot.
                var luisPredictionOptions = new LuisPredictionOptions
                {
                    IncludeAllIntents = true, 
                    IncludeInstanceData = true,
                };

                // Create the recognizer
                var recognizer = new LuisRecognizer(luisApp, luisPredictionOptions, true, null);
                return recognizer;
            });

            // Create the bot as a transient. In this case the ASP Controller is expecting an IBot.
            //services.AddTransient<IBot, EchoBot>();

            // The Dialog that will be run by the bot.
            services.AddSingleton<MainDialog>();
            services.AddSingleton<CalDialog>();

            // Create the bot as a transient. In this case the ASP Controller is expecting an IBot.
            //services.AddTransient<IBot, AuthBot<MainDialog>>();
            //services.AddTransient<IBot, AuthBot<CalDialog>>();

            services.AddTransient<AuthBot<MainDialog>>();
            services.AddTransient<AuthBot<CalDialog>>();
            services.AddTransient<Func<string, IBot>>(serviceProvider => key =>
            {
                switch (key)
                {
                    case "main":
                        return serviceProvider.GetService<AuthBot<MainDialog>>();
                    case "graph":
                        return serviceProvider.GetService<AuthBot<CalDialog>>();
                    default:
                        throw new KeyNotFoundException(); // or maybe return null, up to you
                }
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseBotFramework();
            //app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}
