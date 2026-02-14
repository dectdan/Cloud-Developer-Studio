# ClaudeDevStudio

**AI-Powered Development Memory System for Claude**

Never lose context between coding sessions. ClaudeDevStudio gives Claude AI persistent memory of your projects, patterns, and development history.

## Features

- 🧠 **Persistent Memory** - Claude remembers your project context across sessions
- 🔄 **Auto-Backup** - Automatic backups to Documents folder (cloud-sync friendly)
- 🐛 **Debug Monitoring** - Integrates with Visual Studio DebugView output
- 📊 **Pattern Learning** - Tracks successful approaches and mistakes
- ✅ **Approval System** - Review critical actions before execution
- 🔌 **MCP Integration** - Seamless Claude Desktop integration via Model Context Protocol

## Installation

1. Download the latest `ClaudeDevStudio.msi` from [Releases](https://github.com/dectdan/Cloud-Developer-Studio/releases)
2. Run the installer
3. TrayApp automatically starts and integrates with Claude Desktop
4. Open Dashboard from Start menu or system tray

## Quick Start

```bash
# Initialize project memory
claudedev init

# Tell Claude to load your project
"Hey Claude, load the context for this project"

# Claude now has full project history and memory!
```

## Components

- **TrayApp** - Background service with system tray access
- **Dashboard** - GUI for oversight and management
- **CLI** - Command-line tools for power users
- **MCP Server** - Integration with Claude Desktop

## Requirements

- Windows 10/11
- .NET 8.0 Runtime (included in installer)
- Claude Desktop (optional, for MCP integration)

## Auto-Start

ClaudeDevStudio automatically starts with Windows by default. Toggle this in:
- Dashboard → Settings → "Start with Windows"

## Backup & Recovery

All project data is automatically backed up to:
```
Documents\ClaudeDevStudio\Backups\{ProjectName}\
```

Works with OneDrive, Dropbox, Google Drive sync!

**CLI Commands:**
```bash
claudedev backup              # Force manual backup
claudedev backups             # List available backups
claudedev restore file.backup # Restore from backup
```

## License

MIT License - Copyright © 2026 Daniel E Gain

## Contact

- Email: danielegain@gmail.com
- GitHub: [Cloud-Developer-Studio](https://github.com/dectdan/Cloud-Developer-Studio)

## Credits

Developed with assistance from Claude (Anthropic)
