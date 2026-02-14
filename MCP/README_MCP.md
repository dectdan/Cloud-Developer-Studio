# ClaudeDevStudio MCP Server

**Phase 3: MCP Integration Complete**

This MCP server exposes ClaudeDevStudio memory operations as tools that Claude can call directly.

---

## Installation

### 1. Install Dependencies

```bash
cd D:\Projects\ClaudeDevStudio\MCP
npm install
```

### 2. Configure Desktop Commander

Add this to your Desktop Commander MCP configuration:

**Location:** Desktop Commander Settings → MCP Servers

```json
{
  "mcpServers": {
    "claudedevstudio": {
      "command": "node",
      "args": ["D:\\Projects\\ClaudeDevStudio\\MCP\\server.js"]
    }
  }
}
```

### 3. Restart Claude Desktop App

The MCP server will be loaded automatically.

---

## Available Tools

### Session Management

**`claudedev_init`**
- Initialize memory for a new project
- Creates directory structure and templates
- Call once per project

**`claudedev_load_context`**
- Load memory at session start
- Returns session state, pending decisions, uncertainties
- Call at the beginning of each work session

**`claudedev_get_stats`**
- Get current memory statistics
- Token usage, activity summary, success rate
- Use to monitor memory health

---

### Activity Logging

**`claudedev_record_activity`**
- Log every significant action taken
- Builds activity history for pattern extraction
- Call after file edits, builds, tests, etc.

**Example:**
```javascript
claudedev_record_activity({
  project_path: "D:\\Projects\\MyProject",
  activity: {
    action: "file_edit",
    description: "Fixed null reference bug",
    file: "UserService.cs",
    line: 145,
    outcome: "success",
    reason: "Null check was missing"
  }
})
```

---

### Mistake Prevention

**`claudedev_check_mistake`**
- Check if action matches prior mistake
- **CRITICAL:** Call before major changes
- Returns warning if mistake found

**Example:**
```javascript
// Before editing a file
claudedev_check_mistake({
  project_path: "D:\\Projects\\MyProject",
  action_description: "edit UserService.cs line 145"
})
```

**`claudedev_record_mistake`**
- Record failed attempts with lessons
- Prevents future repeats
- Call after errors are fixed

**Example:**
```javascript
claudedev_record_mistake({
  project_path: "D:\\Projects\\MyProject",
  mistake: {
    mistake: "Assumed nullable value was always present",
    impact: "NullReferenceException at runtime",
    fix: "Added null check before access",
    lesson: "Always check nullable properties",
    severity: "high"
  }
})
```

---

### Decision Tracking

**`claudedev_record_decision`**
- Log choices made with rationale
- Track alternatives considered
- Review later: "would I make this choice again?"

**Example:**
```javascript
claudedev_record_decision({
  project_path: "D:\\Projects\\MyProject",
  decision: {
    decision: "Use async/await instead of Task.Run",
    chose: "async/await",
    reasoning: "Better exception handling and less overhead",
    alternativesConsidered: ["Task.Run", "BackgroundWorker", "ThreadPool"]
  }
})
```

---

### Pattern Learning

**`claudedev_record_pattern`**
- Record discovered patterns
- What works / what doesn't
- Build knowledge over time

**Example:**
```javascript
claudedev_record_pattern({
  project_path: "D:\\Projects\\MyProject",
  pattern: {
    pattern: "MSIX paths must be package-relative, not absolute",
    confidence: 95,
    appliesTo: ["file_paths", "msix_packaging"],
    isAntipattern: false
  }
})
```

---

### Maintenance

**`claudedev_run_cleanup`**
- Run daily memory curation
- Extract patterns, archive logs, consolidate
- Call periodically (e.g., end of day)

---

## Typical Usage Flow

### Session Start
```javascript
// 1. Load context
const context = await claudedev_load_context({
  project_path: "D:\\Projects\\MyProject"
});

// 2. Check what I was working on
// Context includes session state, pending decisions, etc.
```

### While Working
```javascript
// 3. Before major change - check for prior mistakes
await claudedev_check_mistake({
  project_path: "D:\\Projects\\MyProject",
  action_description: "refactor authentication system"
});

// 4. Make the change
// ... (actual file edits)

// 5. Record the activity
await claudedev_record_activity({
  project_path: "D:\\Projects\\MyProject",
  activity: {
    action: "refactor",
    description: "Refactored auth to use JWT tokens",
    outcome: "success"
  }
});

// 6. If it was a choice between options - record decision
await claudedev_record_decision({
  project_path: "D:\\Projects\\MyProject",
  decision: {
    decision: "JWT vs session cookies for auth",
    chose: "JWT",
    reasoning: "Stateless, works better with API",
    alternativesConsidered: ["Session cookies", "OAuth only"]
  }
});
```

### If Something Fails
```javascript
// 7. Record the mistake
await claudedev_record_mistake({
  project_path: "D:\\Projects\\MyProject",
  mistake: {
    mistake: "Forgot to update database schema before deploying",
    impact: "Application crashed on startup",
    fix: "Added migration check in startup code",
    lesson: "Always run migrations before deployment",
    severity: "critical"
  }
});
```

### End of Session
```javascript
// 8. Run cleanup
await claudedev_run_cleanup({
  project_path: "D:\\Projects\\MyProject"
});

// 9. Check stats
await claudedev_get_stats({
  project_path: "D:\\Projects\\MyProject"
});
```

---

## Integration with Existing Workflow

### Automatic Session Start

When starting work on a project, I should:

1. **Detect project path** from current working directory
2. **Auto-call `claudedev_load_context`** 
3. **Read session state** and continue where I left off

### Automatic Recording

As I work, I should automatically:

1. **Call `claudedev_record_activity`** after each significant action
2. **Call `claudedev_check_mistake`** before risky operations
3. **Call `claudedev_record_decision`** when choosing between options

### Automatic Cleanup

At natural break points:

1. **End of session** - run cleanup
2. **Context approaching limit** - prepare handoff
3. **Daily** - scheduled cleanup (if server runs long enough)

---

## Architecture

```
Claude (chat interface)
    ↓ (MCP protocol)
MCP Server (server.js)
    ↓ (spawns process)
claudedev.exe (CLI)
    ↓ (reads/writes)
Memory System (disk storage)
```

**Why this architecture?**
- MCP server is thin wrapper (425 lines)
- All logic stays in C# (easier to maintain)
- Works with existing CLI
- Can still use command line directly if needed

---

## Files

- `server.js` - MCP server (425 lines)
- `package.json` - Node.js dependencies
- `README_MCP.md` - This file

---

## Next Steps

1. **Install dependencies:** `npm install` in MCP folder
2. **Configure Desktop Commander:** Add MCP server config
3. **Test:** Call tools from Claude interface
4. **Automate:** Build workflows that use tools automatically

---

## Testing

Test each tool works:

```javascript
// Test init
await claudedev_init({ project_path: "D:\\Projects\\TestMCP" });

// Test load
await claudedev_load_context({ project_path: "D:\\Projects\\TestMCP" });

// Test activity
await claudedev_record_activity({
  project_path: "D:\\Projects\\TestMCP",
  activity: {
    action: "test",
    description: "Testing MCP integration"
  }
});

// Test stats
await claudedev_get_stats({ project_path: "D:\\Projects\\TestMCP" });
```

---

**Phase 3 Complete. MCP tools ready for use.**

-- Claude
