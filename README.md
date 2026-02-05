# foundrycli

A small experiment repository for working with Microsoft Learn MCP, Azure Foundry resources, and a test client.

**Overview**
- **Purpose:** capture user-agent interactions, provide a repeatable script to create Foundry resources, and include a .NET test client which lists MCP tools and runs a docs search.
- **Experiment log:** See [experiment-log.md](experiment-log.md) for a copy of the user's messages recorded during the experiment.
- **Experiment script:** See [experiment-scripts.sh](experiment-scripts.sh) for the scripted `az` CLI commands used to create and validate Foundry resources.
- **Test client:** The `TestClient` folder contains a .NET console app that lists MCP tools and performs a Microsoft Docs search via the MCP server.

**Files**
- [experiment-log.md](experiment-log.md): chronological log of user messages sent to the agent during the experiment.
- [experiment-scripts.sh](experiment-scripts.sh): bash script with `az` CLI commands to create the Foundry deployment and verify readiness.
- `TestClient/`: .NET 10 console application. Run it to list MCP tools and exercise the docs search tool.

**Prerequisites**
- `dotnet` (net10.0 SDK) installed.
- `az` (Azure CLI) installed and signed in.
- Network access to the Microsoft Learn MCP endpoint used by the test client (the test client sample targets https://learn.microsoft.com/api/mcp).

**Run the experiment script**
1. Inspect `experiment-scripts.sh` to review the `az` commands and any requirements.
2. Run the script (it is interactive and includes checks):

```bash
bash experiment-scripts.sh
```

**Run the TestClient**
1. Change into the `TestClient` folder:

```bash
cd TestClient
```

2. Restore/build and run the console app:

```bash
dotnet restore
dotnet run --project TestClient.csproj
```

The app will attempt to list available MCP tools from the configured MCP endpoint and then perform a docs search. If you hit provider rate limits, retry after the suggested backoff.

**Notes & troubleshooting**
- The `TestClient` uses preview MCP and agent packages; package versions may require pre-release feeds and up-to-date SDKs.
- If `dotnet run` fails due to package or API changes, check `TestClient/TestClient.csproj` for package versions and update accordingly.
- For Azure authentication the script and test client assume an interactive Azure CLI login (`az login`) or `AzureCliCredential` available.