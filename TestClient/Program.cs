using Azure.AI.Agents.Persistent;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;

#pragma warning disable MEAI001 // Experimental

var endpoint = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROJECT_ENDPOINT") 
    ?? "https://pidebrui-foundry-7165.services.ai.azure.com/api/projects/my-foundry-project";
var model = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROJECT_DEPLOYMENT_NAME") ?? "gpt-4.1-mini";
var mcpServerUrl = "https://learn.microsoft.com/api/mcp";

// List available tools from the MCP server
Console.WriteLine("Available tools from Microsoft Learn MCP server:");
await using var mcpClient = await McpClient.CreateAsync(
    new HttpClientTransport(new()
    {
        Endpoint = new Uri(mcpServerUrl),
        Name = "Learn Docs MCP Server",
        TransportMode = HttpTransportMode.StreamableHttp,
    }));
foreach (var tool in await mcpClient.ListToolsAsync())
    Console.WriteLine($"  - {tool.Name}: {tool.Description?[..Math.Min(60, tool.Description?.Length ?? 0)]}...");
Console.WriteLine();

Console.WriteLine("Creating client...");
var client = new PersistentAgentsClient(endpoint, new AzureCliCredential());

// Create agent with hosted MCP tool (auto-approved)
var mcpTool = new HostedMcpServerTool("microsoft_learn", mcpServerUrl)
{
    AllowedTools = ["microsoft_docs_search"],
    ApprovalMode = HostedMcpServerToolApprovalMode.NeverRequire
};

Console.WriteLine("Creating agent...");
AIAgent agent = await client.CreateAIAgentAsync(
    model: model,
    options: new()
    {
        Name = "LearnMcpTestAgent",
        ChatOptions = new() { Instructions = "You answer questions by searching Microsoft Learn content.", Tools = [mcpTool] }
    });

Console.WriteLine(await agent.RunAsync("How to create a Foundry instance using az cli?"));

await client.Administration.DeleteAgentAsync(agent.Id);
