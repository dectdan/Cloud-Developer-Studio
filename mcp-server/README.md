# ClaudeDevStudio MCP Server

This MCP server enables Claude Desktop to access ClaudeDevStudio memory and debugging tools during development sessions.

## Installation

1. **Install Node.js** (if not already installed)
   - Download from: https://nodejs.org/
   - Verify: `node --version`

2. **Install dependencies** (already done if you're reading this)
   ```bash
   cd D:\Projects\ClaudeDevStudio\mcp-server
   npm install
   ```

## Configuration for Claude Desktop

Add this to your Claude Desktop configuration file:

**Windows:** `%APPDATA%\Claude\claude_desktop_config.json`

```json
{
  "mcpServers": {
    "claudedevstudio": {
      "command": "node",
      "args": [
        "D:\\Projects\\ClaudeDevStudio\\mcp-server\\index.js"
      ]
    }
  }
}
```

## Available Tools

Once configured, Claude can use these tools during development:

### `claudedev_init`
Initialize memory system for a project
```javascript
{
  "project_path": "D:\\MyProject"
}
```

### `claudedev_load`
Load context at session start (shows pending decisions, current tasks, etc.)
```javascript
{
  "project_path": "D:\\MyProject"
}
```

### `claudedev_record_activity`
Record development activities
```javascript
{
  "project_path": "D:\\MyProject",
  "action": "bug_fix",
  "description": "Fixed null reference exception in UserController",
  "file": "Controllers/UserController.cs",
  "outcome": "success"
}
```

### `claudedev_record_mistake`
Record mistakes with lessons learned (prevents repeating errors)
```javascript
{
  "project_path": "D:\\MyProject",
  "mistake": "Used .First() instead of .FirstOrDefault()",
  "impact": "Caused runtime exception when collection was empty",
  "fix": "Changed to .FirstOrDefault() and added null check",
  "lesson": "Always use .FirstOrDefault() for LINQ queries that might return empty"
}
```

### `claudedev_check_mistake`
Check if action matches a prior mistake (call BEFORE taking action)
```javascript
{
  "project_path": "D:\\MyProject",
  "action_description": "Using .First() to get user from database"
}
```

### `claudedev_stats`
Get memory statistics
```javascript
{
  "project_path": "D:\\MyProject"
}
```

### `claudedev_monitor_start`
Start monitoring Visual Studio debug output
```javascript
{
  "project_path": "D:\\MyProject"
}
```

## Usage in Claude Desktop

Once configured, Claude will automatically have access to these tools. Example conversation:

**You:** "I'm working on D:\\MyProject - load the context"  
**Claude:** *[Calls claudedev_load]*  
"I can see you were working on the UserController bug fix. The pending decision is about error handling strategy..."

**You:** "I'm about to use .First() to get the user"  
**Claude:** *[Calls claudedev_check_mistake]*  
"⚠️ Warning: This matches a prior mistake! You previously had issues with .First() causing exceptions. Consider using .FirstOrDefault() instead."

## Testing the Server

Test manually:
```bash
cd D:\Projects\ClaudeDevStudio\mcp-server
node index.js
```

The server should output: `ClaudeDevStudio MCP server running on stdio`

## Troubleshooting

**Server not appearing in Claude Desktop:**
1. Check config file path is correct
2. Verify Node.js is in PATH
3. Restart Claude Desktop completely
4. Check Claude Desktop logs

**Commands failing:**
1. Ensure claudedev.exe is built: `cd D:\Projects\ClaudeDevStudio; dotnet build -c Release`
2. Check project path uses double backslashes in JSON: `D:\\MyProject`
3. Verify DebugView is installed at `D:\Tools\DebugView\dbgview64.exe`

## Next Steps

After configuration:
1. Restart Claude Desktop
2. Start a new conversation
3. Tell Claude about your project path
4. Claude will automatically use ClaudeDevStudio to maintain context!
