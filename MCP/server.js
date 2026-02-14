#!/usr/bin/env node

/**
 * ClaudeDevStudio MCP Server
 * 
 * Exposes memory system operations as MCP tools.
 * Acts as a bridge between Claude and the claudedev.exe CLI.
 */

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

const execAsync = promisify(exec);

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

// Path to claudedev.exe
const CLAUDEDEV_PATH = path.join(__dirname, '..', 'bin', 'Release', 'net8.0', 'claudedev.exe');

/**
 * Execute claudedev command and return result
 */
async function runClaudedev(args) {
  try {
    const { stdout, stderr } = await execAsync(`"${CLAUDEDEV_PATH}" ${args}`);
    return { success: true, output: stdout, error: stderr };
  } catch (error) {
    return { success: false, output: error.stdout || '', error: error.stderr || error.message };
  }
}

/**
 * Create the MCP server
 */
const server = new Server(
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

/**
 * List available tools
 */
server.setRequestHandler(ListToolsRequestSchema, async () => {
  return {
    tools: [
      {
        name: 'claudedev_init',
        description: 'Initialize ClaudeDevStudio memory for a project. Creates directory structure and templates.',
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
        name: 'claudedev_load_context',
        description: 'Load memory context for a project. Returns session state, pending decisions, and flagged uncertainties. Call this at the start of each session.',
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
        description: 'Record an activity/action taken. Use this to log every significant action.',
        inputSchema: {
          type: 'object',
          properties: {
            project_path: {
              type: 'string',
              description: 'Absolute path to the project directory',
            },
            activity: {
              type: 'object',
              description: 'Activity details (action, description, file, outcome, etc)',
              properties: {
                action: { type: 'string' },
                description: { type: 'string' },
                file: { type: 'string' },
                line: { type: 'number' },
                outcome: { type: 'string' },
                reason: { type: 'string' },
              },
              required: ['action', 'description'],
            },
          },
          required: ['project_path', 'activity'],
        },
      },
      {
        name: 'claudedev_check_mistake',
        description: 'Check if an action matches a prior mistake. ALWAYS call this before major changes to prevent repeating errors.',
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
        name: 'claudedev_record_mistake',
        description: 'Record a mistake/failed attempt with lesson learned. This prevents repeating the same error.',
        inputSchema: {
          type: 'object',
          properties: {
            project_path: {
              type: 'string',
              description: 'Absolute path to the project directory',
            },
            mistake: {
              type: 'object',
              description: 'Mistake details',
              properties: {
                mistake: { type: 'string' },
                impact: { type: 'string' },
                fix: { type: 'string' },
                lesson: { type: 'string' },
                severity: { type: 'string', enum: ['low', 'medium', 'high', 'critical'] },
              },
              required: ['mistake', 'impact', 'fix', 'lesson'],
            },
          },
          required: ['project_path', 'mistake'],
        },
      },
      {
        name: 'claudedev_record_decision',
        description: 'Record a decision made with rationale and alternatives considered.',
        inputSchema: {
          type: 'object',
          properties: {
            project_path: {
              type: 'string',
              description: 'Absolute path to the project directory',
            },
            decision: {
              type: 'object',
              description: 'Decision details',
              properties: {
                decision: { type: 'string' },
                chose: { type: 'string' },
                reasoning: { type: 'string' },
                alternativesConsidered: { type: 'array', items: { type: 'string' } },
              },
              required: ['decision', 'chose', 'reasoning'],
            },
          },
          required: ['project_path', 'decision'],
        },
      },
      {
        name: 'claudedev_record_pattern',
        description: 'Record a discovered pattern (what works/doesn\'t work).',
        inputSchema: {
          type: 'object',
          properties: {
            project_path: {
              type: 'string',
              description: 'Absolute path to the project directory',
            },
            pattern: {
              type: 'object',
              description: 'Pattern details',
              properties: {
                pattern: { type: 'string' },
                confidence: { type: 'number', minimum: 0, maximum: 100 },
                appliesTo: { type: 'array', items: { type: 'string' } },
                isAntipattern: { type: 'boolean' },
              },
              required: ['pattern', 'confidence', 'appliesTo'],
            },
          },
          required: ['project_path', 'pattern'],
        },
      },
      {
        name: 'claudedev_get_stats',
        description: 'Get memory statistics for a project (token usage, session info, activity summary).',
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
        name: 'claudedev_run_cleanup',
        description: 'Run daily memory cleanup (extract patterns, archive logs, consolidate duplicates). Should be run periodically.',
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
        name: 'claudedev_update_tokens',
        description: 'Update token usage count for current session. Use this to track context usage.',
        inputSchema: {
          type: 'object',
          properties: {
            project_path: {
              type: 'string',
              description: 'Absolute path to the project directory',
            },
            tokens_used: {
              type: 'number',
              description: 'Current token count',
            },
          },
          required: ['project_path', 'tokens_used'],
        },
      },
    ],
  };
});

/**
 * Handle tool calls
 */
server.setRequestHandler(CallToolRequestSchema, async (request) => {
  const { name, arguments: args } = request.params;

  try {
    switch (name) {
      case 'claudedev_init': {
        const result = await runClaudedev(`init "${args.project_path}"`);
        return {
          content: [
            {
              type: 'text',
              text: result.success ? result.output : `Error: ${result.error}`,
            },
          ],
        };
      }

      case 'claudedev_load_context': {
        const result = await runClaudedev(`load "${args.project_path}"`);
        return {
          content: [
            {
              type: 'text',
              text: result.success ? result.output : `Error: ${result.error}`,
            },
          ],
        };
      }

      case 'claudedev_record_activity': {
        const activityJson = JSON.stringify(args.activity).replace(/"/g, '\\"');
        const result = await runClaudedev(`record activity "${activityJson}"`);
        return {
          content: [
            {
              type: 'text',
              text: result.success ? result.output : `Error: ${result.error}`,
            },
          ],
        };
      }

      case 'claudedev_check_mistake': {
        const result = await runClaudedev(`check "${args.action_description}"`);
        // Exit code 1 means mistake found (intentional)
        const mistakeFound = !result.success;
        return {
          content: [
            {
              type: 'text',
              text: mistakeFound ? result.error : '✓ No prior mistakes found',
            },
          ],
        };
      }

      case 'claudedev_record_mistake': {
        const mistakeJson = JSON.stringify(args.mistake).replace(/"/g, '\\"');
        const result = await runClaudedev(`record mistake "${mistakeJson}"`);
        return {
          content: [
            {
              type: 'text',
              text: result.success ? result.output : `Error: ${result.error}`,
            },
          ],
        };
      }

      case 'claudedev_record_decision': {
        const decisionJson = JSON.stringify(args.decision).replace(/"/g, '\\"');
        const result = await runClaudedev(`record decision "${decisionJson}"`);
        return {
          content: [
            {
              type: 'text',
              text: result.success ? result.output : `Error: ${result.error}`,
            },
          ],
        };
      }

      case 'claudedev_record_pattern': {
        const patternJson = JSON.stringify(args.pattern).replace(/"/g, '\\"');
        const result = await runClaudedev(`record pattern "${patternJson}"`);
        return {
          content: [
            {
              type: 'text',
              text: result.success ? result.output : `Error: ${result.error}`,
            },
          ],
        };
      }

      case 'claudedev_get_stats': {
        const result = await runClaudedev(`stats "${args.project_path}"`);
        return {
          content: [
            {
              type: 'text',
              text: result.success ? result.output : `Error: ${result.error}`,
            },
          ],
        };
      }

      case 'claudedev_run_cleanup': {
        const result = await runClaudedev(`cleanup "${args.project_path}"`);
        return {
          content: [
            {
              type: 'text',
              text: result.success ? result.output : `Error: ${result.error}`,
            },
          ],
        };
      }

      case 'claudedev_update_tokens': {
        // This would require adding a new command to the CLI
        // For now, return a note
        return {
          content: [
            {
              type: 'text',
              text: 'Token tracking will be implemented in the CLI',
            },
          ],
        };
      }

      default:
        throw new Error(`Unknown tool: ${name}`);
    }
  } catch (error) {
    return {
      content: [
        {
          type: 'text',
          text: `Error executing ${name}: ${error.message}`,
        },
      ],
      isError: true,
    };
  }
});

/**
 * Start the server
 */
async function main() {
  const transport = new StdioServerTransport();
  await server.connect(transport);
  console.error('ClaudeDevStudio MCP Server running on stdio');
}

main().catch((error) => {
  console.error('Server error:', error);
  process.exit(1);
});
