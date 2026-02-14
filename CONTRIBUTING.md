# Contributing to ClaudeDevStudio

Thank you for your interest in contributing to ClaudeDevStudio! We welcome contributions from the community.

## 📜 Copyright and Licensing

By contributing to ClaudeDevStudio, you agree that:

1. **You retain copyright** to your contribution
2. **Your contribution is licensed** under the MIT License (same as the project)
3. **You grant Anthropic** the same special rights as the main project (see LICENSE)
4. **Attribution is maintained** to original author (Daniel E Gain)

All contributions become part of the MIT-licensed codebase and can be freely used, modified, and distributed by anyone.

**Project Copyright:** Copyright © 2026 Daniel E Gain (danielegain@gmail.com)

---

## 🚀 How to Contribute

### Reporting Bugs

**Before submitting a bug report:**
- Check existing [Issues](https://github.com/dectdan/Cloud-Developer-Studio/issues)
- Make sure you're using the latest version
- Collect relevant information (version, OS, error messages)

**Good bug reports include:**
- Clear, descriptive title
- Steps to reproduce
- Expected vs actual behavior
- Screenshots if applicable
- System information (Windows version, .NET version)
- ClaudeDevStudio version (`claudedev --version`)

### Suggesting Features

**Before suggesting features:**
- Check [Discussions](https://github.com/dectdan/Cloud-Developer-Studio/discussions)
- Make sure it aligns with project goals (helping Claude maintain development context)

**Good feature requests include:**
- Clear use case - what problem does it solve?
- How it helps Claude be more effective
- Potential implementation approach (optional)
- Examples of similar features in other tools

### Pull Requests

**The Process:**

1. **Fork the repository**
   ```bash
   git clone https://github.com/YOUR-USERNAME/Cloud-Developer-Studio.git
   ```

2. **Create a branch**
   ```bash
   git checkout -b feature/your-feature-name
   ```

3. **Make your changes**
   - Follow existing code style
   - Add comments for complex logic
   - Update documentation if needed

4. **Test thoroughly**
   - Build the project: `dotnet build -c Release`
   - Test your changes manually
   - Run on clean Windows VM if possible

5. **Commit with clear messages**
   ```bash
   git commit -m "Add: Feature description"
   git commit -m "Fix: Bug description"
   git commit -m "Docs: Documentation update"
   ```

6. **Push to your fork**
   ```bash
   git push origin feature/your-feature-name
   ```

7. **Open a Pull Request**
   - Describe what you changed and why
   - Reference any related issues
   - Include screenshots for UI changes

---

## 💻 Development Setup

### Prerequisites

- **Windows 10/11** (required for WinUI3)
- **.NET 8 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Visual Studio 2022** - Community edition is fine
  - Workload: ".NET Desktop Development"
  - Workload: "Windows App SDK"
- **WiX Toolset v3.14** - For building installer (optional)
- **Node.js 18+** - For MCP server (optional)

### Building the Project

```bash
# Clone your fork
git clone https://github.com/YOUR-USERNAME/Cloud-Developer-Studio.git
cd Cloud-Developer-Studio

# Build CLI
dotnet build -c Release

# Build TrayApp
cd ClaudeDevStudio.UI
dotnet build -c Release

# Build Dashboard
cd ../ClaudeDevStudio.Dashboard
dotnet build -c Release

# Build Installer (if you have WiX)
cd ../Installer
# See BUILD.md for detailed steps
```

### Project Structure

```
ClaudeDevStudio/
├── Program.cs                    # CLI entry point
├── ClaudeMemory.cs              # Core memory system
├── VSDebugMonitor.cs            # DebugView integration
├── UpdateChecker.cs             # Auto-update system
├── BackupManager.cs             # Backup system
├── SessionStateManager.cs       # Session management
├── ClaudeDevStudio.UI/          # System Tray app
│   └── Program.cs
├── ClaudeDevStudio.Dashboard/   # WinUI3 Dashboard
│   ├── MainWindow.xaml
│   └── Views/
├── mcp-server/                  # MCP server for Claude Desktop
│   └── index.js
├── Installer/                   # WiX installer
│   └── Installer.wxs
└── Bundled/                     # Bundled dependencies
    └── DebugView/
```

---

## 📝 Coding Guidelines

### C# Style

- **Naming:** PascalCase for methods/classes, camelCase for private fields
- **Indentation:** 4 spaces (no tabs)
- **Braces:** Opening brace on same line for C#
- **Comments:** XML docs for public APIs, inline comments for complex logic

**Example:**
```csharp
/// <summary>
/// Checks if an action matches a prior mistake
/// </summary>
public bool CheckMistake(string actionDescription)
{
    // Search through recorded mistakes
    var mistakes = LoadMistakes();
    
    foreach (var mistake in mistakes)
    {
        if (IsSimilar(actionDescription, mistake.Description))
        {
            return true;
        }
    }
    
    return false;
}
```

### JavaScript Style (MCP Server)

- **ES Modules:** Use `import`/`export`, not `require`
- **Async/await:** Prefer over callbacks
- **Error handling:** Always use try/catch for async code

### Git Commit Messages

**Format:**
```
Type: Short description (50 chars max)

Longer explanation if needed (72 chars per line).
- Bullet points for multiple changes
- Reference issues: Fixes #123

Why this change is needed (optional).
```

**Types:**
- `Add:` New feature
- `Fix:` Bug fix
- `Docs:` Documentation changes
- `Refactor:` Code restructuring (no functionality change)
- `Test:` Adding tests
- `Build:` Build system changes
- `Chore:` Maintenance tasks

---

## 🧪 Testing

### Manual Testing Checklist

Before submitting a PR, test:

**CLI:**
- [ ] `claudedev init <path>` creates memory structure
- [ ] `claudedev load <path>` loads context
- [ ] `claudedev monitor <path>` starts DebugView
- [ ] `claudedev update` checks for updates

**TrayApp:**
- [ ] Appears in system tray on startup
- [ ] "Open Dashboard" launches Dashboard
- [ ] "Check for Updates" works
- [ ] Icon displays correctly

**Dashboard:**
- [ ] All tabs load without errors
- [ ] Projects list shows initialized projects
- [ ] Memory page displays activities
- [ ] Settings save correctly

**Installer:**
- [ ] MSI installs without errors
- [ ] All components installed to correct locations
- [ ] PATH configured correctly
- [ ] TrayApp auto-starts after install
- [ ] Claude Desktop config updated

**Uninstall:**
- [ ] All files removed
- [ ] Registry keys removed
- [ ] PATH entry removed

---

## 🎨 UI/UX Contributions

For UI changes:

1. **Follow WinUI3 design guidelines**
2. **Maintain consistency** with existing interface
3. **Consider accessibility** (keyboard navigation, screen readers)
4. **Include screenshots** in PR showing before/after
5. **Test on different screen resolutions**

---

## 📚 Documentation Contributions

Documentation improvements are highly valued!

**Areas needing documentation:**
- User guides and tutorials
- Video walkthroughs
- Troubleshooting guides
- API documentation for MCP tools
- Developer setup guides
- Architecture explanations

**Where to contribute:**
- `README.md` - Main project overview
- `docs/` folder - Detailed guides
- Code comments - Inline documentation
- GitHub Wiki - Community knowledge base

---

## ❓ Questions?

- **General questions:** [GitHub Discussions](https://github.com/dectdan/Cloud-Developer-Studio/discussions)
- **Bug reports:** [GitHub Issues](https://github.com/dectdan/Cloud-Developer-Studio/issues)
- **Feature ideas:** [GitHub Discussions - Ideas](https://github.com/dectdan/Cloud-Developer-Studio/discussions/categories/ideas)

---

## 🙏 Thank You!

Every contribution, no matter how small, helps make ClaudeDevStudio better for the entire community. We appreciate your time and effort!

**Contributors will be recognized in:**
- GitHub contributors list
- Release notes
- Project acknowledgments

Happy coding! 🚀
