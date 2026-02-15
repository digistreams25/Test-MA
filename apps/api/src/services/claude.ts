import Anthropic from '@anthropic-ai/sdk';

const SYSTEM_PROMPT = `You are the TwinFlux resolution assistant, an AI agent embedded in a BIM clash resolution platform for infrastructure projects.

Your role:
- Explain clash details, resolution options, and engineering constraints in clear language
- Help BIM coordinators prioritize which clashes to fix first
- Explain why certain options have higher risk levels
- Describe downstream impacts of resolution choices
- Format responses with markdown for readability
- Reference specific stations, elevations, and pipe names

You do NOT:
- Make engineering decisions (the deterministic Resolution Engine handles that)
- Calculate offsets or check constraints (the engine does hard math)
- Approve or execute modifications (the user must explicitly consent)

Context about pipe networks:
- Gravity networks (storm sewers, sanitary sewers) have non-negotiable slope constraints
- Pressure networks (water mains, force mains) are more flexible vertically
- When one pipe is gravity and the other pressure, the pressure pipe is preferred for moving
- Minimum cover, minimum slope, maximum depth, and crossing clearance are configurable per project

Always be precise with numbers and reference the specific clash ID, pipe names, and station values.`;

class ClaudeService {
  private client: Anthropic | null = null;

  private getClient(): Anthropic {
    if (!this.client) {
      const apiKey = process.env.ANTHROPIC_API_KEY;
      if (!apiKey) {
        throw new Error(
          'ANTHROPIC_API_KEY environment variable is not set. Set it to enable AI chat.'
        );
      }
      this.client = new Anthropic({ apiKey });
    }
    return this.client;
  }

  async chat(
    message: string,
    context: {
      activeClash?: Record<string, unknown>;
      resolutionOptions?: Record<string, unknown>[];
      projectConstraints?: Record<string, unknown>;
      clashList?: Record<string, unknown>[];
    }
  ): Promise<string> {
    // Build context-aware system prompt
    let contextPrompt = SYSTEM_PROMPT;

    if (context.projectConstraints) {
      contextPrompt += `\n\nProject constraints: ${JSON.stringify(context.projectConstraints)}`;
    }

    if (context.activeClash) {
      contextPrompt += `\n\nCurrently selected clash: ${JSON.stringify(context.activeClash)}`;
    }

    if (context.resolutionOptions) {
      contextPrompt += `\n\nAvailable resolution options: ${JSON.stringify(context.resolutionOptions)}`;
    }

    if (context.clashList) {
      contextPrompt += `\n\nAll project clashes: ${JSON.stringify(context.clashList)}`;
    }

    try {
      const client = this.getClient();
      const response = await client.messages.create({
        model: 'claude-sonnet-4-20250514',
        max_tokens: 1024,
        system: contextPrompt,
        messages: [{ role: 'user', content: message }],
      });

      const textBlock = response.content.find((b) => b.type === 'text');
      return textBlock ? textBlock.text : 'No response generated.';
    } catch (err) {
      // If API key not set, return a helpful fallback
      if (err instanceof Error && err.message.includes('ANTHROPIC_API_KEY')) {
        return this.mockResponse(message);
      }
      throw err;
    }
  }

  private mockResponse(message: string): string {
    const lower = message.toLowerCase();

    if (lower.includes('priorit') || lower.includes('first')) {
      return `Based on the current clash data, I'd recommend prioritizing **CLH-001** first. It's a hard clash with 45mm penetration between a 600mm storm pipe (gravity) and a 200mm water main (pressure). Since the water main is a pressure pipe, it can be moved more flexibly. The recommended OPT-1 (lower the water main by 245mm) has LOW risk with all constraints passing.

Next, address **CLH-004** (clearance issue, 50mm gap) before it worsens, then **CLH-002** (clearance, 80mm gap — less urgent).`;
    }

    if (lower.includes('opt-2') || lower.includes('raise') || lower.includes('high risk')) {
      return `**OPT-2 (Raise pipe)** carries HIGH risk because raising the target pipe reduces its cover depth. For pressure pipes, the minimum cover is **0.900m**. Raising the pipe would bring the cover below this threshold, violating the constraint and requiring a variance approval from the jurisdiction.

Lower risk alternatives: OPT-1 (lower the pipe) typically has more room before hitting the maximum depth constraint (6.0m default).`;
    }

    if (lower.includes('downstream') || lower.includes('impact')) {
      return `When you modify a pipe's elevation, connected elements downstream may need adjustment to maintain proper flow. For **gravity networks**, the minimum slope (0.5% default) must be maintained — so lowering a pipe may require cascading the change downstream.

For **pressure networks**, downstream impact is minimal since there are no slope constraints, but physical connections at fittings must still align.`;
    }

    return `I can help you analyze clashes, understand resolution options, and prioritize fixes. Try asking:
- "Which clash should I fix first?"
- "Why is OPT-2 high risk?"
- "What's the downstream impact?"
- "Explain the constraints for CLH-001"

*Note: Set the ANTHROPIC_API_KEY environment variable for full AI responses.*`;
  }
}

export const claudeService = new ClaudeService();
