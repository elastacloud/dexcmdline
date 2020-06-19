﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Alba.CsConsoleFormat;
using dexcmd.Model;
using Microsoft.Azure.Management.Kusto;
using static System.ConsoleColor;

namespace dexcmd.Functions
{
   internal class ListDatabases : IKustoFunction
   {
      public async Task Execute(KustoFunctions functions)
      {
         try
         {
            var client = await functions.GetManagementClient();
            var databases =
               await client.Databases.ListByClusterAsync(resourceGroupName: functions._options.ResourceGroup,
                  clusterName: $"{functions._options.KustoClusterName}");

            var headerThickness = new LineThickness(LineWidth.Single, LineWidth.Single);

            var doc = new Document(
               new Grid
               {
                  Color = Gray,
                  Columns = { GridLength.Auto, GridLength.Char(20), GridLength.Char(20) },
                  Children = {
                     new Cell("Name") { Stroke = headerThickness},
                     new Cell("Row Count") { Stroke = headerThickness },
                     new Cell("Cache Size (GB)") { Stroke = headerThickness },
                     databases.Select(item =>
                     {
                        var databasesQuery = functions.GetDataAdminReader(item.Name.Split('/')[1], ".show database datastats").Result;
                        var dataStats = new KustoDatastats(databasesQuery);
                        return new[]
                        {
                           new Cell(dataStats.DatabaseName) {Color = Yellow},
                           new Cell(dataStats.HotRowCount),
                           new Cell(dataStats.HotCompressedSize.ToString("##,##0.00000000", CultureInfo.InvariantCulture)) {Align = Align.Right},
                        };
                     })
                  }
               }
            );

            ConsoleRenderer.RenderDocument(doc);
         }
         catch (Exception ex)
         {
            Console.WriteLine(ex);
         }
      }
   }
}
