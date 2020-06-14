﻿using CommandLine;
using Microsoft.Extensions.Configuration;

namespace dexcmd
{
   public class Options
   {
      public string ClientId { get; set; }
      public string ClientSecret { get; set; }
      public string TenantId { get; set; }
      public string KustoClusterName { get; set; }
      public string ResourceGroup { get; set; }
      public string SubscriptionId { get; set; }
      [Option("list-databases", Required = false, HelpText = "Gets a list of databases")]
      public bool ListDatabases { get; set; }
      [Option("list-tables", Required = false, HelpText = "Gets a list of tables")]
      public bool ListTables { get; set; }
      [Option('d', "databaseName", Required = false, HelpText = "Gets a list of tables")]
      public string DatabaseName { get; set; }

      internal static Options GetConfigArguments(Options options)
      {
         IConfiguration config = new ConfigurationBuilder()
            .AddJsonFile("kusto.json", true, true)
            .Build();
         options.ClientId = config["application_id"];
         options.ClientSecret = config["application_key"];
         options.ResourceGroup = config["resource_group"];
         options.SubscriptionId = config["subscription_id"];
         options.TenantId = config["tenant_id"];
         options.KustoClusterName = config["cluster_name"];
         return options;
      }
   }
}