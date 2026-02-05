using Azure;
using Azure.AI.Agents.Persistent;
using Azure.Identity;

// Configuration - using our Foundry project endpoint
var endpoint = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROJECT_ENDPOINT") 
    ?? "https://pidebrui-foundry-7165.services.ai.azure.com/api/projects/my-foundry-project";
var model = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROJECT_DEPLOYMENT_NAME") 
    ?? "gpt-4.1-mini";

Console.WriteLine("=== Microsoft Learn MCP Test Client ===");
Console.WriteLine($"Endpoint: {endpoint}");
Console.WriteLine($"Model: {model}");
Console.WriteLine();

// Create client using Azure CLI credential
PersistentAgentsClient client = new(endpoint, new AzureCliCredential());

// Create an MCP tool definition for Microsoft Learn
var mcpTool = new MCPToolDefinition(
    serverLabel: "microsoft_learn",
    serverUrl: "https://learn.microsoft.com/api/mcp");

// Configure allowed tools
mcpTool.AllowedTools.Add("microsoft_docs_search");
mcpTool.AllowedTools.Add("microsoft_docs_fetch");
mcpTool.AllowedTools.Add("microsoft_code_sample_search");

Console.WriteLine("MCP Server: microsoft_learn");
Console.WriteLine("Server Address: https://learn.microsoft.com/api/mcp");
Console.WriteLine("Available Tools:");
Console.WriteLine("  - microsoft_docs_search");
Console.WriteLine("  - microsoft_docs_fetch");
Console.WriteLine("  - microsoft_code_sample_search");
Console.WriteLine();

// Create an agent with the MCP tool
Console.WriteLine("Creating agent with MCP tools...");
PersistentAgent agent = client.Administration.CreateAgent(
    model: model,
    name: "LearnMcpTestAgent",
    instructions: "You answer questions by searching Microsoft Learn content using the microsoft_docs_search tool. Always cite your sources.",
    tools: [mcpTool]);

Console.WriteLine($"Agent created: {agent.Id}");
Console.WriteLine();

// Create thread for communication
PersistentAgentThread thread = client.Threads.CreateThread();

// Test the docs search tool
Console.WriteLine("Testing microsoft_docs_search tool...");
Console.WriteLine("Query: 'How to create a Foundry instance using az cli?'");
Console.WriteLine();

// Create message to thread
PersistentThreadMessage messageResponse = client.Messages.CreateMessage(
    thread.Id,
    MessageRole.User,
    "How to create a Foundry instance using az cli?");

// Run the agent
ThreadRun run = client.Runs.CreateRun(thread, agent);

// Wait for the run to complete, handle MCP tool approvals
Console.WriteLine("Waiting for response...");
while (run.Status == RunStatus.Queued || run.Status == RunStatus.InProgress || run.Status == RunStatus.RequiresAction)
{
    Thread.Sleep(TimeSpan.FromMilliseconds(500));
    run = client.Runs.GetRun(thread.Id, run.Id);
    
    // Handle MCP tool approvals
    if (run.Status == RunStatus.RequiresAction && run.RequiredAction is SubmitToolApprovalAction toolApprovalAction)
    {
        var toolApprovals = new List<ToolApproval>();
        foreach (var toolCall in toolApprovalAction.SubmitToolApproval.ToolCalls)
        {
            if (toolCall is RequiredMcpToolCall mcpToolCall)
            {
                Console.WriteLine($"  Approving MCP tool call: {mcpToolCall.Name}");
                toolApprovals.Add(new ToolApproval(mcpToolCall.Id, approve: true));
            }
        }
        if (toolApprovals.Count > 0)
        {
            run = client.Runs.SubmitToolOutputsToRun(thread.Id, run.Id, toolApprovals: toolApprovals);
        }
    }
    else
    {
        Console.Write(".");
    }
}
Console.WriteLine();

if (run.Status != RunStatus.Completed)
{
    Console.WriteLine($"Run did not complete successfully. Status: {run.Status}");
    if (run.LastError != null)
    {
        Console.WriteLine($"Error: {run.LastError.Message}");
    }
}
else
{
    // Get the messages
    Pageable<PersistentThreadMessage> messages = client.Messages.GetMessages(
        threadId: thread.Id,
        order: ListSortOrder.Ascending);

    Console.WriteLine("=== Response ===");
    foreach (PersistentThreadMessage threadMessage in messages)
    {
        if (threadMessage.Role.ToString() == "assistant")
        {
            foreach (MessageContent contentItem in threadMessage.ContentItems)
            {
                if (contentItem is MessageTextContent textItem)
                {
                    Console.WriteLine(textItem.Text);
                }
            }
        }
    }
}

Console.WriteLine();

// Cleanup
Console.WriteLine("Cleaning up...");
client.Threads.DeleteThread(thread.Id);
client.Administration.DeleteAgent(agent.Id);
Console.WriteLine("Done!");
