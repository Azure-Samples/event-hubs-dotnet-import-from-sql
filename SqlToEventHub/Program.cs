using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure.ServiceRuntime;
using Newtonsoft.Json;

using WorkerHost.Extensions;
using WorkerHost.SQL;

namespace WorkerHost
{
    public class WorkerHost : RoleEntryPoint
    {
        static void Main()
        {
            StartPushingUpdates("readerLocal");
        }

        public override void Run()
        {
            StartPushingUpdates("readerWorkerRole");
        }

        private static void StartPushingUpdates(string readeName)
        {
            const int SEND_GROUP_SIZE = 30;

            int sleepTimeMs;
            if (!int.TryParse(CloudConfigurationManager.GetSetting("SleepTimeMs"), out sleepTimeMs))
            {
                sleepTimeMs = 10000;
            }

            string sqlDatabaseConnectionString = CloudConfigurationManager.GetSetting("sqlDatabaseConnectionString");
            string serviceBusConnectionString = CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.ServiceBusConnectionString");
            string hubName = CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.EventHubToUse");

            string dataTableName = CloudConfigurationManager.GetSetting("DataTableName");
            string offsetKey = CloudConfigurationManager.GetSetting("OffsetKey");

            string selectDataQueryTemplate =
                ReplaceDataTableName(
                    ReplaceOffsetKey(ReplaceReader(CloudConfigurationManager.GetSetting("DataQuery"), readeName),
                        offsetKey), dataTableName);

            string createOffsetTableQuery = CloudConfigurationManager.GetSetting("CreateOffsetTableQuery");
            string selectOffsetQueryTemplate = ReplaceReader(CloudConfigurationManager.GetSetting("OffsetQuery"), readeName);
            string updateOffsetQueryTemplate = ReplaceReader(CloudConfigurationManager.GetSetting("UpdateOffsetQuery"), readeName);
            string insertOffsetQueryTemplate = ReplaceReader(CloudConfigurationManager.GetSetting("InsertOffsetQuery"), readeName);

            SqlTextQuery queryPerformer = new SqlTextQuery(sqlDatabaseConnectionString);
            queryPerformer.PerformQuery(createOffsetTableQuery);

            EventHubClient eventHubClient = EventHubClient.CreateFromConnectionString(serviceBusConnectionString,
                hubName);

            for (; ; )
            {
                try
                {
                    Dictionary<string, object> offsetQueryResult = queryPerformer.PerformQuery(selectOffsetQueryTemplate).FirstOrDefault();
                    string offsetString;
                    if (offsetQueryResult == null || offsetQueryResult.Count == 0)
                    {
                        offsetString = "-1";
                        queryPerformer.PerformQuery(ReplaceOffset(insertOffsetQueryTemplate, offsetString));
                    }
                    else
                    {
                        offsetString = offsetQueryResult.Values.First().ToString();
                    }

                    string selectDataQuery = ReplaceOffset(selectDataQueryTemplate, offsetString);

                    IEnumerable<Dictionary<string, object>> resultCollection =
                        queryPerformer.PerformQuery(selectDataQuery);

                    if (resultCollection.Any())
                    {
                        IEnumerable<Dictionary<string, object>> orderedByOffsetKey =
                            resultCollection.OrderBy(r => r[offsetKey]);

                        foreach (var resultGroup in orderedByOffsetKey.Split(SEND_GROUP_SIZE))
                        {
                            SendRowsToEventHub(eventHubClient, resultGroup).Wait();

                            string nextOffset = resultGroup.Max(r => r[offsetKey]).ToString();
                            queryPerformer.PerformQuery(ReplaceOffset(updateOffsetQueryTemplate, nextOffset));
                        }
                    }
                }
                catch (Exception)
                {
                    //TODO: log exception details
                }
                Thread.Sleep(sleepTimeMs);
            }
        }

        private static async Task SendRowsToEventHub(EventHubClient eventHubClient, IEnumerable<object> rows)
        {
            var memoryStream = new MemoryStream();

            using (var sw = new StreamWriter(memoryStream, new UTF8Encoding(false), 1024, leaveOpen: true))
            {
                string serialized = JsonConvert.SerializeObject(rows);
                sw.Write(serialized);
                sw.Flush();
            }

            Debug.Assert(memoryStream.Position > 0, "memoryStream.Position > 0");

            memoryStream.Position = 0;
            EventData eventData = new EventData(memoryStream);

            await eventHubClient.SendAsync(eventData);
        }

        private static string ReplaceDataTableName(string query, string tableName)
        {
            return query.Replace("{tableName}", tableName);
        }

        private static string ReplaceOffsetKey(string query, string offsetKey)
        {
            return query.Replace("{offsetKey}", offsetKey);
        }

        private static string ReplaceOffset(string query, string offsetValue)
        {
            return query.Replace("{offsetValue}", offsetValue);
        }

        private static string ReplaceReader(string query, string readerValue)
        {
            return query.Replace("{readerValue}", "\'" + readerValue + "\'");
        }
    }
}
