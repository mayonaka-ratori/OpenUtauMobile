---
name: mcp-github-setup
description: Guide for setting up GitHub MCP server. Load when you need to interact with GitHub Issues, Pull Requests, or upstream repository changes, and the gh CLI is not available or not authenticated.
user-invocable: false
---

# MCP GitHub Setup

## When to set up GitHub MCP

Set up the GitHub MCP server if ALL of these are true:

1. You need to interact with GitHub (Issues, PRs, upstream releases)
2. The gh CLI is not available or not authenticated

Always try gh CLI first:

```bash
gh auth status
```

If this succeeds, use Bash with gh commands instead of MCP.

## If gh CLI is not available

Create .mcp.json at the repository root:

```json
{
  "mcpServers": {
    "github": {
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-github"],
      "env": {
        "GITHUB_TOKEN": "${GITHUB_TOKEN}"
      }
    }
  }
}
```

Then tell the user:

1. A GITHUB_TOKEN environment variable is required.
   Create a personal access token at <https://github.com/settings/tokens>
   with repo scope.
2. Export it: export GITHUB_TOKEN=ghp_xxxxxxxxxxxx
3. Restart Claude Code for the MCP server to activate.

Do NOT proceed with GitHub operations without a valid token.
Do NOT fabricate or guess a token value.
