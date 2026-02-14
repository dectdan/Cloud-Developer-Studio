# Auto-configure Claude Desktop for ClaudeDevStudio
# This runs during MSI installation

param(
    [string]$MCPServerPath
)

$ErrorActionPreference = "Stop"

try {
    $configPath = "$env:APPDATA\Claude\claude_desktop_config.json"
    $configDir = Split-Path $configPath -Parent
    
    # Create Claude config directory if it doesn't exist
    if (!(Test-Path $configDir)) {
        New-Item -ItemType Directory -Path $configDir -Force | Out-Null
    }
    
    # Read existing config or create new
    if (Test-Path $configPath) {
        $config = Get-Content $configPath -Raw | ConvertFrom-Json
    } else {
        $config = @{
            mcpServers = @{}
            preferences = @{}
        }
    }
    
    # Add/update ClaudeDevStudio MCP server
    if (!$config.mcpServers) {
        $config | Add-Member -MemberType NoteProperty -Name "mcpServers" -Value @{} -Force
    }
    
    $config.mcpServers.claudedevstudio = @{
        command = "node"
        args = @($MCPServerPath)
    }
    
    # Save config
    $config | ConvertTo-Json -Depth 10 | Set-Content $configPath -Encoding UTF8
    
    Write-Host "✓ Claude Desktop configured successfully"
    Write-Host "  Config: $configPath"
    Write-Host "  MCP Server: $MCPServerPath"
    
    exit 0
} catch {
    Write-Host "Error configuring Claude Desktop: $_"
    # Don't fail installation if config fails
    exit 0
}
