﻿using Alba.CsConsoleFormat;
using Microsoft.Azure.Management.Kusto;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using dexcmd.Functions;
using dexcmd.Model;
using Kusto.Cloud.Platform.Data;
using Kusto.Data;
using Kusto.Data.Common;
using Microsoft.Identity.Client;
using kClient = Kusto.Data.Net.Client;
using Newtonsoft.Json;
using static System.ConsoleColor;
using AuthenticationResult = Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationResult;
using ClientCredential = Microsoft.IdentityModel.Clients.ActiveDirectory.ClientCredential;

namespace dexcmd
{
   public class KustoFunctions
   {
      internal readonly Options _options;
      public const string AUTHORITY = "https://login.microsoftonline.com/";

      public KustoFunctions(Options options)
      {
         _options = options;
      }

      #region List Databases 
      /// <summary>
      /// Given an ADX cluster returns a list of databases in that cluster with a row count and on disk compressed volume
      /// </summary>
      public async Task ListDatabases()
      {
         var db = new ListDatabases();
         await db.Execute(this);
      }

      #endregion

      #region List Tables 

      public async Task ListTables()
      {
         var tables = new ListTables();
         await tables.Execute(this);
      }

      #endregion

      #region Helpers

      public bool LoginInteractively => !String.IsNullOrEmpty(_options.InteractiveClientId);
      internal async Task<string> GetAadToken(Options options, string resource = "https://management.core.windows.net/")
      {
         if (LoginInteractively)
         {
            var publicApp = PublicClientApplicationBuilder.Create(options.InteractiveClientId)
               .WithAuthority(AadAuthorityAudience.None)
               .WithTenantId(options.TenantId)
               .WithDefaultRedirectUri()
               .Build();
            var scopes = new[] { $"{resource}/user_impersonation" };
            var resultInteractive = publicApp.AcquireTokenInteractive(scopes);

            return (await resultInteractive.WithPrompt(Prompt.SelectAccount).ExecuteAsync()).AccessToken;
         }
         // get a token for the Graph without triggering any user interaction (from the cache, via multi-resource refresh token, etc)
         ClientCredential creds = new ClientCredential(options.ClientId, options.ClientSecret);
         // Get auth token from auth code
         var authContext = new AuthenticationContext(AUTHORITY + options.TenantId);
         var result = await authContext.AcquireTokenAsync(resource, creds);
         // Acquire user token for the interactive user for Kusto:
         return result.AccessToken;
      }

      internal async Task<KustoManagementClient> GetManagementClient()
      {
         var token = await GetAadToken(_options);
         var client = new KustoManagementClient(credentials: new TokenCredentials(token))
         {
            SubscriptionId = _options.SubscriptionId
         };
         return client;
      }

      internal async Task<IDataReader> GetDataAdminReader(string databaseName, string query)
      {
         string resource = $"https://{_options.KustoClusterName}.northeurope.kusto.windows.net";
         KustoConnectionStringBuilder kcsb;
         if (!LoginInteractively)
         {
            kcsb = new KustoConnectionStringBuilder(resource)
            {
               ApplicationClientId = _options.ClientId,
               ApplicationKey = _options.ClientSecret,
               Authority = _options.TenantId,
               FederatedSecurity = true,
               InitialCatalog = databaseName,
            };
         }
         else
         {
            kcsb = new KustoConnectionStringBuilder(resource)
            {
               ApplicationToken = await GetAadToken(_options, resource),
               Authority = _options.TenantId,
               FederatedSecurity = true,
               InitialCatalog = databaseName,
            };
         }
         
         var admin = kClient.KustoClientFactory.CreateCslQueryProvider(kcsb);
         return await admin.ExecuteQueryAsync(databaseName, query, new ClientRequestProperties());
      }
      #endregion
   }
}
