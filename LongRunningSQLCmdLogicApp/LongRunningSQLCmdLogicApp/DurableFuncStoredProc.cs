using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using System.Data;
using System;
using Microsoft.Data.SqlClient;
using Azure.Core;
using System.IO;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Dynamitey;

namespace LongRunningSQLCmdLogicApp
{
    public static class DurableFuncStoredProc
    {
        [FunctionName("Orcherstrator")]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var outputs = new List<string>();

            string data = context.GetInput<string>();

            Root inputData = JsonConvert.DeserializeObject<Root>(data);


            outputs.Add(await context.CallActivityAsync<string>("RunSQLStoredProc", inputData));

            return outputs;
        }

        [FunctionName("RunSQLStoredProc")]
        public static string RunSQLStoredProc([ActivityTrigger] Root inputData, ILogger log)
        {



            log.LogInformation("Running [RunSQLStoredProc] with following data: {0}, storedProc: {1}", inputData.connectionString, inputData.storedProcName);
            //System.Threading.Thread.Sleep(30000);


            using (SqlConnection conn = new SqlConnection(inputData.connectionString))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(inputData.storedProcName, conn);
                cmd.CommandType = CommandType.StoredProcedure;

                foreach (var param in inputData.@params)
                {
                    log.LogInformation("adding in param {0}, {1}", param.paramName, param.value);
                    cmd.Parameters.Add(new SqlParameter(param.paramName, param.value));
                }

                using (SqlDataReader rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        Console.WriteLine("result: {0}", rdr);
                    }
                }
            }
            return $"Hello {0}!";
        }

        [FunctionName("HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {

            string requestBody = String.Empty;

            requestBody = await req.Content.ReadAsStringAsync();

            //dynamic data = JsonConvert.DeserializeObject(requestBody);

            string instanceId = await starter.StartNewAsync("Orcherstrator", null, requestBody);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}