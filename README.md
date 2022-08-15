# Problem space / Solution overview:

Azure Logic Apps and Azure Functions are a perfect pairing for Serverless. Azure Functions are serverless compute, while Logic Apps act as serverless workflows and integrations. However one challenge comes from the fact that Azure Functions can run for up to 10 minutes in the serverless ‘consumption’ plan, but a single HTTP request from Logic Apps will time out after 2 minutes. Attached is an example of a HTTP Triggered Azure durable function which demonstrates the use of async request/response with polling to get around the 2 minute HTTP request timeout on Logic App side.


In my sample scenario, you invoke an HTTP Function which kicks off a durable orchestration. The durable orchestrator can now take on the work of managing function calls (that may take > 2 minutes - **in my case a slow stored procedure**) and returning a status back to the logic app. When all work is completed, the durable function orchestrator will respond with a 200.

1. Durable Function (Http Trigger) gets the request from Logic Apps
2. Durable Function (Http Trigger invokes a durable orchestrator function and returns the status (202 Accepted with location header for status updates)
3. The durable orchestrator calls the RunSQLStoredProc Function that need to do work which may be > 2 minutes
4. Logic App will continue to check the location header (automatically) until a 200 status code is received.
5. Below is a sample successful 202 result body:

```json
{
    "id": "cb041dd2ba06421cbff251f04c9ebd04",
    "statusQueryGetUri": "http://localhost:7258/runtime/webhooks/durabletask/instances/cb041dd2ba06421cbff251f04c9ebd04?taskHub=TestHubName&connection=Storage&code=UTVwW_4hUyRBclnBm2ehOY8gqnWzSC_45Qw-CtZ7VtSyAzFuOcv4fA==",
    "sendEventPostUri": "http://localhost:7258/runtime/webhooks/durabletask/instances/cb041dd2ba06421cbff251f04c9ebd04/raiseEvent/{eventName}?taskHub=TestHubName&connection=Storage&code=UTVwW_4hUyRBclnBm2ehOY8gqnWzSC_45Qw-CtZ7VtSiAzFuOcv4gA==",
    "terminatePostUri": "http://localhost:7258/runtime/webhooks/durabletask/instances/cb041dd2ba06421cbff251f04c9ebd04/terminate?reason={text}&taskHub=TestHubName&connection=Storage&code=UTVwW_4hUyRBclnBm2ehOY8gqnWzSC_45Qw-CtZ7VtSiAzFuscv4fA==",
    "purgeHistoryDeleteUri": "http://localhost:7258/runtime/webhooks/durabletask/instances/cb041dd2ba06421cbff251f04c9ebd04?taskHub=TestHubName&connection=Storage&code=UTVwW_4hUyRBclnBm2ehOY8gqnWzSC_45Qw-CtZ7VtSiAzFuOcx4fA==",
    "restartPostUri": "http://localhost:7258/runtime/webhooks/durabletask/instances/cb041dd2ba06421cbff251f04c9ebd04/restart?taskHub=TestHubName&connection=Storage&code=UTVwW_4hUyRBclnBm2ehOY8gqnWzSC_45Qw-CtZ7VtSiAzFuOc34fA=="
}
```


## Setup prerequisites

1. Stand up an Azure function in Azure. Enable private endpoint/vnet integration as per your organisation requirements.
2. Enable **System Assigned Managed Idenity** on function app and grant access to SQL DB. More instructions on this step are documented here: https://docs.microsoft.com/en-us/azure/azure-functions/functions-identity-access-azure-sql-with-managed-identity
3. Deploy the sample code.


## HTTP Call requirements

Please ensure that form your Logic App Workflow you invoke the sample azure function via **HTTP Request connector** and that you supply the following body JSON:


```json
{
    "connectionString":"Server=sqlserver20220311t071913z.database.windows.net; Authentication=Active Directory Managed Identity; Database=arian-db-001",
    "storedProcName":"Test",
    "params":[
      {
          "paramName":"@first_name",
          "value":"Arian"
      },
       {
          "paramName":"@last_name",
          "value":"Nevjestic"
      }
    ]
}
```


Please note that the **connectionString** in above example is using the **System assigned managed idenity** - so please ensure you follow step 2 guidance in the prerequisite section carefully.

As always - this is a proof of concept code - designed to highlight the principles of the pattern rather than something you deploy straight to prodution. Use at your own risk.