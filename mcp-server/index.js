#!/usr/bin/env node

import { Server } from '@modelcontextprotocol/sdk/server/index.js';
import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js';
import {
  CallToolRequestSchema,
  ListToolsRequestSchema,
} from '@modelcontextprotocol/sdk/types.js';
import { exec } from 'child_process';
import { promisify } from 'util';
import path from 'path';
import { fileURLToPath } from 'url';
import https from 'https';
import http from 'http';

const execAsync = promisify(exec);
const __dirname = path.dirname(fileURLToPath(import.meta.url));

// Path to claudedev.exe
const CLAUDEDEV_PATH = path.join(__dirname, '..', 'bin', 'Release', 'net8.0', 'claudedev.exe');

/**
 * Execute claudedev command and return result
 */
async function runClaudeDevCommand(args) {
  try {
    const command = `"${CLAUDEDEV_PATH}" ${args}`;
    const { stdout, stderr } = await execAsync(command, {
      maxBuffer: 10 * 1024 * 1024, // 10MB buffer
      timeout: 30000 // 30 second timeout
    });
    
    return {
      success: true,
      output: stdout || stderr,
      error: null
    };
  } catch (error) {
    return {
      success: false,
      output: error.stdout || '',
      error: error.message
    };
  }
}

/**
 * Fetch URL content - allows Claude to verify its own work and fetch documentation
 */
async function fetchUrl(url) {
  return new Promise((resolve, reject) => {
    const urlObj = new URL(url);
    const client = urlObj.protocol === 'https:' ? https : http;
    
    const options = {
      headers: {
        'User-Agent': 'ClaudeDevStudio/1.0.0'
      }
    };
    
    client.get(url, options, (res) => {
      let data = '';
      
      res.on('data', (chunk) => {
        data += chunk;
      });
      
      res.on('end', () => {
        resolve({
          success: true,
          statusCode: res.statusCode,
          headers: res.headers,
          body: data
        });
      });
    }).on('error', (err) => {
      resolve({
        success: false,
        error: err.message
      });
    });
  });
}

/**
 * MCP Server for ClaudeDevStudio
 */
class ClaudeDevStudioServer {
  constructor() {
    this.server = new Server(
      {
        name: 'claudedevstudio',
        version: '1.0.0',
      },
      {
        capabilities: {
          tools: {},
        },
      }
    );

    this.setupHandlers();
  }

  setupHandlers() {
    // List available tools
    this.server.setRequestHandler(ListToolsRequestSchema, async () => ({
      tools: [
        {
          name: 'claudedev_init',
          description: 'Initialize ClaudeDevStudio memory for a project',
          inputSchema: {
            type: 'object',
            properties: {
              project_path: {
                type: 'string',
                description: 'Absolute path to the project directory',
              },
            },
            required: ['project_path'],
          },
        },
        {
          name: 'claudedev_load',
          description: 'Load context from ClaudeDevStudio memory (call at session start)',
          inputSchema: {
            type: 'object',
            properties: {
              project_path: {
                type: 'string',
                description: 'Absolute path to the project directory',
              },
            },
            required: ['project_path'],
          },
        },
        {
          name: 'claudedev_record_activity',
          description: 'Record an activity/action taken during development',
          inputSchema: {
            type: 'object',
            properties: {
              project_path: {
                type: 'string',
                description: 'Absolute path to the project directory',
              },
              action: {
                type: 'string',
                description: 'Type of action (e.g., "code_change", "debug", "fix")',
              },
              description: {
                type: 'string',
                description: 'Description of what was done',
              },
              file: {
                type: 'string',
                description: 'File that was modified (optional)',
              },
              outcome: {
                type: 'string',
                description: 'Result of the action (e.g., "success", "failed")',
              },
            },
            required: ['project_path', 'action', 'description'],
          },
        },
        {
          name: 'claudedev_record_mistake',
          description: 'Record a mistake/failed attempt with lesson learned',
          inputSchema: {
            type: 'object',
            properties: {
              project_path: {
                type: 'string',
                description: 'Absolute path to the project directory',
              },
              mistake: {
                type: 'string',
                description: 'What went wrong',
              },
              impact: {
                type: 'string',
                description: 'How it affected the project',
              },
              fix: {
                type: 'string',
                description: 'How it was fixed',
              },
              lesson: {
                type: 'string',
                description: 'What was learned',
              },
            },
            required: ['project_path', 'mistake', 'impact', 'fix', 'lesson'],
          },
        },
        {
          name: 'claudedev_check_mistake',
          description: 'Check if an action matches a prior mistake (prevents repeating errors)',
          inputSchema: {
            type: 'object',
            properties: {
              project_path: {
                type: 'string',
                description: 'Absolute path to the project directory',
              },
              action_description: {
                type: 'string',
                description: 'Description of the action you plan to take',
              },
            },
            required: ['project_path', 'action_description'],
          },
        },
        {
          name: 'claudedev_stats',
          description: 'Get memory statistics for current project',
          inputSchema: {
            type: 'object',
            properties: {
              project_path: {
                type: 'string',
                description: 'Absolute path to the project directory',
              },
            },
            required: ['project_path'],
          },
        },
        {
          name: 'claudedev_monitor_start',
          description: 'Start monitoring Visual Studio debug output (captures exceptions/errors)',
          inputSchema: {
            type: 'object',
            properties: {
              project_path: {
                type: 'string',
                description: 'Absolute path to the project directory',
              },
            },
            required: ['project_path'],
          },
        },
        {
          name: 'fetch_url',
          description: 'Fetch content from a URL - allows Claude to verify websites it built, fetch documentation, or get current information without asking the user',
          inputSchema: {
            type: 'object',
            properties: {
              url: {
                type: 'string',
                description: 'URL to fetch (http:// or https://)',
              },
            },
            required: ['url'],
          },
        },
      ],
    }));

    // Handle tool calls
    this.server.setRequestHandler(CallToolRequestSchema, async (request) => {
      const { name, arguments: args } = request.params;

      try {
        switch (name) {
          case 'claudedev_init':
            return await this.handleInit(args);
          
          case 'claudedev_load':
            return await this.handleLoad(args);
          
          case 'claudedev_record_activity':
            return await this.handleRecordActivity(args);
          
          case 'claudedev_record_mistake':
            return await this.handleRecordMistake(args);
          
          case 'claudedev_check_mistake':
            return await this.handleCheckMistake(args);
          
          case 'claudedev_stats':
            return await this.handleStats(args);
          
          case 'claudedev_monitor_start':
            return await this.handleMonitorStart(args);
          
          case 'fetch_url':
            return await this.handleFetchUrl(args);
          
          default:
            throw new Error(`Unknown tool: ${name}`);
        }
      } catch (error) {
        return {
          content: [
            {
              type: 'text',
              text: `Error: ${error.message}`,
            },
          ],
          isError: true,
        };
      }
    });
  }

  async handleInit(args) {
    const result = await runClaudeDevCommand(`init "${args.project_path}"`);
    return {
      content: [
        {
          type: 'text',
          text: result.success ? result.output : `Error: ${result.error}\n${result.output}`,
        },
      ],
    };
  }

  async handleLoad(args) {
    const result = await runClaudeDevCommand(`load "${args.project_path}"`);
    return {
      content: [
        {
          type: 'text',
          text: result.success ? result.output : `Error: ${result.error}\n${result.output}`,
        },
      ],
    };
  }

  async handleRecordActivity(args) {
    const activityJson = JSON.stringify({
      action: args.action,
      description: args.description,
      file: args.file || '',
      outcome: args.outcome || 'unknown'
    });
    
    const result = await runClaudeDevCommand(`record "${args.project_path}" activity '${activityJson}'`);
    return {
      content: [
        {
          type: 'text',
          text: result.success ? '✓ Activity recorded' : `Error: ${result.error}`,
        },
      ],
    };
  }

  async handleRecordMistake(args) {
    const mistakeJson = JSON.stringify({
      mistake: args.mistake,
      impact: args.impact,
      fix: args.fix,
      lesson: args.lesson
    });
    
    const result = await runClaudeDevCommand(`record "${args.project_path}" mistake '${mistakeJson}'`);
    return {
      content: [
        {
          type: 'text',
          text: result.success ? '✓ Mistake recorded' : `Error: ${result.error}`,
        },
      ],
    };
  }

  async handleCheckMistake(args) {
    const result = await runClaudeDevCommand(`check "${args.project_path}" "${args.action_description}"`);
    return {
      content: [
        {
          type: 'text',
          text: result.output,
        },
      ],
    };
  }

  async handleStats(args) {
    const result = await runClaudeDevCommand(`stats "${args.project_path}"`);
    return {
      content: [
        {
          type: 'text',
          text: result.success ? result.output : `Error: ${result.error}`,
        },
      ],
    };
  }

  async handleMonitorStart(args) {
    const result = await runClaudeDevCommand(`monitor "${args.project_path}"`);
    return {
      content: [
        {
          type: 'text',
          text: result.success ? 
            'Debug monitor started. Capturing exceptions and errors from Visual Studio.' : 
            `Error: ${result.error}`,
        },
      ],
    };
  }

  async handleFetchUrl(args) {
    const result = await fetchUrl(args.url);
    
    if (!result.success) {
      return {
        content: [
          {
            type: 'text',
            text: `Failed to fetch ${args.url}: ${result.error}`,
          },
        ],
        isError: true,
      };
    }
    
    return {
      content: [
        {
          type: 'text',
          text: `Status: ${result.statusCode}\nContent-Type: ${result.headers['content-type']}\n\n${result.body}`,
        },
      ],
    };
  }

  async run() {
    const transport = new StdioServerTransport();
    await this.server.connect(transport);
    console.error('ClaudeDevStudio MCP server running on stdio');
  }
}

// Start the server
const server = new ClaudeDevStudioServer();
server.run().catch(console.error);
