# CLAUDE.md — TwinFlux Clash Resolver

## Project Overview

TwinFlux is a full-stack BIM clash resolution platform for infrastructure BIM coordinators. It reads clash detection results from Navisworks Clash Detective or Autodesk Construction Cloud (ACC) Model Coordination, analyzes them against engineering constraints, proposes resolution options ranked by risk, and — only after explicit user approval — sends execution commands to Civil 3D through a locally installed agent.

**Target users:** BIM coordinators on highway, water/sewer, and utility infrastructure projects who work with Civil 3D pipe networks (gravity and pressure) and use Navisworks or ACC for clash detection.

**Core principle:** The platform NEVER modifies the Civil 3D model without explicit user consent. The workflow is always: **detect → analyze → propose → user approves → execute → validate**.

---

## Repository Structure

This is a **pnpm monorepo** with three main applications and a shared types package:

```
twinflux/
├── apps/
│   ├── web/                        # Next.js 14+ web dashboard (App Router)
│   │   ├── app/
│   │   │   ├── layout.tsx
│   │   │   ├── page.tsx            # Landing / login
│   │   │   ├── dashboard/
│   │   │   │   ├── page.tsx        # Project overview
│   │   │   │   ├── clashes/
│   │   │   │   │   ├── page.tsx    # Clash list
│   │   │   │   │   └── [id]/page.tsx  # Clash detail + resolution
│   │   │   │   ├── chat/
│   │   │   │   │   └── page.tsx    # Agent chat (also embeddable panel)
│   │   │   │   ├── audit/
│   │   │   │   │   └── page.tsx    # Audit log
│   │   │   │   └── settings/
│   │   │   │       └── page.tsx    # Project settings + constraints
│   │   ├── components/
│   │   │   ├── clash-list.tsx
│   │   │   ├── clash-detail.tsx
│   │   │   ├── resolution-options.tsx
│   │   │   ├── consent-bar.tsx
│   │   │   ├── cross-section-viz.tsx   # SVG cross section
│   │   │   ├── profile-viz.tsx         # SVG longitudinal profile
│   │   │   ├── agent-chat.tsx
│   │   │   ├── connection-status.tsx
│   │   │   └── audit-table.tsx
│   │   └── lib/
│   │       ├── api.ts              # Cloud API client
│   │       ├── agent-client.ts     # localhost agent client
│   │       └── types.ts            # Frontend TypeScript types
│   │
│   ├── api/                        # Backend API server (Node.js + Express)
│   │   ├── src/
│   │   │   ├── index.ts
│   │   │   ├── routes/
│   │   │   │   ├── auth.ts
│   │   │   │   ├── projects.ts
│   │   │   │   ├── clashes.ts
│   │   │   │   ├── chat.ts
│   │   │   │   └── audit.ts
│   │   │   ├── services/
│   │   │   │   ├── claude.ts       # Anthropic API integration
│   │   │   │   ├── acc.ts          # ACC/APS API integration
│   │   │   │   └── audit.ts
│   │   │   └── db/
│   │   │       ├── schema.prisma
│   │   │       └── migrations/
│   │   └── package.json
│   │
│   └── agent/                      # Local Windows agent (C# .NET 8)
│       ├── TwinFlux.Agent/
│       │   ├── Program.cs
│       │   ├── Server/AgentServer.cs         # REST API on localhost:3000
│       │   ├── Navisworks/NavisworksBridge.cs # COM automation
│       │   ├── Engine/
│       │   │   ├── ResolutionEngine.cs       # Constraint logic
│       │   │   └── Constraints.cs            # Configurable thresholds
│       │   ├── Consent/ConsentStore.cs
│       │   ├── Civil3D/Civil3DClient.cs      # HTTP to C3D plugin
│       │   └── Audit/AuditLogger.cs
│       ├── TwinFlux.Civil3DPlugin/
│       │   ├── ClashResolverExtension.cs     # IExtensionApplication
│       │   ├── PipeModifier.cs               # Gravity pipe modification
│       │   └── PressurePipeModifier.cs       # Pressure pipe modification
│       └── TwinFlux.Shared/
│           └── Models.cs                     # Shared DTOs
│
├── packages/
│   └── shared-types/               # TypeScript types shared between web and api
│       └── index.ts
│
├── .cursorrules
├── CLAUDE.md                       # This file
├── package.json                    # Monorepo root (pnpm workspaces)
└── README.md
```

---

## Tech Stack

| Layer | Technology |
|---|---|
| Frontend | Next.js 14+ (App Router), TypeScript, Tailwind CSS, React Query |
| Real-time | Socket.io or SSE (agent → dashboard updates) |
| Backend API | Node.js + Express (or Next.js API routes) |
| Database | PostgreSQL via Prisma ORM |
| AI | Anthropic Claude API (claude-sonnet-4-20250514) |
| Local Agent | C# .NET 8 (Windows, system tray app) |
| C3D Plugin | .NET class library, loaded via NETLOAD |
| ACC Integration | Autodesk Platform Services (APS) REST API, 3-legged OAuth |
| Navisworks | COM automation (ProgID: `Navisworks.Application`) |
| Monorepo | pnpm workspaces |

---

## Development Workflow

### Build Order (Phases)

**Phase 1 — Core Web Dashboard:** Next.js setup, dark theme, clash list, clash detail, SVG visualizations, resolution options panel, consent bar. Use mock/sample data.

**Phase 2 — Agent Chat + Claude:** API server with auth, Anthropic Claude integration, chat component, context-aware system prompts.

**Phase 3 — Local Agent + Navisworks Bridge:** C# agent with REST API, Navisworks COM bridge (mock first, real COM when DLLs available), Resolution Engine, Consent Store, connection status UI.

**Phase 4 — ACC Integration:** APS OAuth flow, Model Coordination API adapter, unified clash model mapping, source selection UI.

**Phase 5 — Civil 3D Execution:** C3D plugin (.dll), pipe profile modification, structure adjustment, downstream propagation, audit trail, validation flow.

**Phase 6 — Polish + Production:** Audit log export, project settings, agent installer, error handling, auth/billing/onboarding.

### Common Commands

```bash
# Install dependencies (from repo root)
pnpm install

# Run the web dashboard (development)
pnpm --filter web dev

# Run the API server (development)
pnpm --filter api dev

# Build all packages
pnpm build

# Run linting
pnpm lint

# Run tests
pnpm test

# Database migrations (from api directory)
pnpm --filter api prisma migrate dev
pnpm --filter api prisma generate
```

### For the C# agent and C3D plugin

```bash
# Build agent (from apps/agent directory)
dotnet build TwinFlux.Agent/

# Build Civil 3D plugin
dotnet build TwinFlux.Civil3DPlugin/

# Run agent locally
dotnet run --project TwinFlux.Agent/
```

---

## System Architecture

```
CLOUD (Server)
├── Web Dashboard (Next.js) ──── port configurable
├── API Server (Node/Express) ── port configurable
├── PostgreSQL ────────────────── Projects, Clash history, Audit logs, Users
└── Claude API (Anthropic) ────── Analysis & NL responses

          │ HTTPS (clash metadata only, model files stay local)
          ▼

USER'S MACHINE (Windows)
├── TwinFlux Local Agent ─────── localhost:3000
│   ├── Resolution Engine (deterministic constraint math)
│   ├── Consent Store (in-memory approval gate)
│   ├── Navisworks Bridge (COM, read-only)
│   └── Audit Logger
├── Civil 3D Plugin ──────────── localhost:5100
│   └── Executes approved modifications inside C3D Transactions
└── ACC Model Coordination ───── Cloud-to-cloud (read clashes via APS REST)
```

**Data flow rule:** No model data (DWG/NWD files) leaves the user's machine. Only structured clash metadata (IDs, coordinates, properties) goes to the cloud.

---

## Key Domain Concepts

### Clash Sources (Dual Input)

The platform supports two clash data sources, selected per project:

1. **Navisworks (local):** COM automation via `Navisworks.Application` ProgID. Reads Clash Detective results. Requires local agent on same machine as Navisworks.
2. **ACC Model Coordination (cloud):** APS REST API with 3-legged OAuth. Endpoints at `developer.api.autodesk.com/construction/mc/v3/...`. Does NOT require local agent for reading clashes, but STILL requires it for executing fixes.

Both sources normalize into the unified `ClashData` / `ClashElement` interfaces (see Data Models below).

### Pipe Network Types

- **Gravity networks** (storm sewers, sanitary sewers): Slope-constrained. Non-negotiable slope requirements. Use `PipeNetwork` → `Pipe` + `Structure` in Civil 3D.
- **Pressure networks** (water mains, force mains): More flexible vertically, no slope constraint. Use `PressurePipeNetwork` → `PressurePipe` + `PressureFitting` in Civil 3D.

### Resolution Engine (Deterministic — NOT LLM-Driven)

The resolution engine runs locally in the agent. All engineering decisions are deterministic math, never LLM-generated.

**Movement calculation:**
- Hard clash: `|penetration depth| + 150mm clearance buffer`
- Clearance clash: `150mm minimum − current gap`

**Which element to move (priority order):**
1. If one pipe is Pressure and the other Gravity → move Pressure (gravity has non-negotiable slope)
2. If both same type → move the smaller diameter pipe
3. User can override via `prefer_element` parameter

**Three resolution options generated per clash:**

| Option | Direction | Key Checks |
|---|---|---|
| OPT-1 | Lower target pipe (↓) | Cover > min, depth < max, slope maintained (gravity) |
| OPT-2 | Raise target pipe (↑) | Cover > min |
| OPT-3 | Shift horizontally (→) | Within ROW, utility corridor spacing |

**Configurable constraint thresholds (defaults):**

| Constraint | Default Threshold | On Fail |
|---|---|---|
| Min cover (gravity) | ≥ 1.2m | Warning |
| Min cover (pressure) | ≥ 0.9m | Warning |
| Crossing clearance | ≥ 150mm | Must fix |
| Min slope (gravity) | ≥ 0.5% | Downstream impact warning |
| Max depth | < 6.0m | Reject option |

Thresholds are configurable per project (different jurisdictions have different standards).

**Risk levels:** LOW (all pass, no downstream impact), MEDIUM (pass but downstream propagation needed, or horizontal shift), HIGH (constraint warnings).

### Consent Workflow (Critical Safety Gate)

```
Clash Detected
  → Analyzing
    → Options Ready (OPT-1, 2, 3)
      → Option Selected
        → Approved (user clicked APPROVE)
          → Executing (C3D transaction in progress)
            → Executed (audit logged)
              → Validating (re-run clash check)
                → Resolved ✅
                → Partially Resolved ⚠️ (new clashes created → re-enter pipeline)
                → Not Resolved ❌ (re-analyze with bigger magnitude)
            → Failed (C3D error → try different option)
        ↺ Revoke → Option Selected
      ↺ Change mind → Options Ready
```

**Rules:**
- `apply_resolution` MUST check `consent state == APPROVED` before executing
- If state is not APPROVED, refuse and return error
- After execution, automatically run validation
- All state transitions logged with timestamp and user ID

### Claude AI Role

Claude is used for **explanation, clarification, prioritization, and report formatting only**. It does NOT make engineering decisions or calculate offsets — the deterministic Resolution Engine handles that.

When sending chat messages to Claude, the API builds a system prompt containing: project config, active clash data, resolution engine results, constraint thresholds, and pipe network context.

---

## Data Models

### Unified Clash Model

```typescript
interface ClashData {
  id: string;
  source: 'navisworks' | 'acc';
  sourceRef: string;           // NW clash GUID or ACC issue ID
  testName: string;
  type: 'hard' | 'clearance';
  status: 'new' | 'active' | 'reviewed' | 'resolved';
  severity: 'critical' | 'warning' | 'info';
  location: { x: number; y: number; z: number };
  distance: number;            // negative = penetration, positive = gap
  gridLocation?: string;
  element1: ClashElement;
  element2: ClashElement;
  detectedAt: Date;
}

interface ClashElement {
  elementId: string;
  modelFile: string;
  discipline: string;
  category: string;
  pipeNetworkName?: string;
  networkType?: 'gravity' | 'pressure';
  pipeName?: string;
  pipeDiameter?: number;       // mm
  material?: string;
  alignmentName?: string;
  stationStart?: number;
  stationEnd?: number;
  invertElevation?: number;    // meters
  crownElevation?: number;
  slope?: number;              // fraction, e.g. 0.008
  cover?: number;              // meters
}
```

### Civil 3D Modification Audit Record

Every modification must record:
- Modification ID (unique)
- Source clash ID and proposal ID
- Approved by (user ID)
- For each element changed: element ID (Handle), property name, old value, new value, unit
- Execution timestamp

This is required for **ISO 19650 compliance**.

---

## API Endpoints

### Cloud API Server

```
POST /auth/login                  — User authentication
POST /auth/register               — New account
GET  /projects                    — List user's projects
POST /projects                    — Create project
GET  /projects/:id                — Project details
POST /projects/:id/connect-acc    — Start OAuth flow for ACC
GET  /projects/:id/acc-clashes    — Fetch clashes from ACC
POST /chat                        — Send message to Claude agent
GET  /audit/:projectId            — Audit log
GET  /audit/:projectId/export     — Export audit log (CSV/PDF)
```

### Local Agent (localhost:3000)

```
GET  /health                      — Agent + connections status
GET  /clashes                     — Read clashes from Navisworks
GET  /clashes/:id                 — Single clash detail
POST /clashes/:id/propose         — Run resolution engine, generate options
POST /proposals/:id/approve       — Set consent state to APPROVED
POST /proposals/:id/execute       — Execute in C3D (requires APPROVED state)
POST /proposals/:id/validate      — Re-run clash check after execution
GET  /audit-log                   — Modification history
```

### Civil 3D Plugin (localhost:5100)

Receives modification commands from the local agent. Executes within C3D's document thread using `Application.Idle` marshaling. All modifications happen inside a `Transaction`.

---

## UI/Design Conventions

- **Dark theme** — engineering tooling aesthetic
- **Monospace font** for data: JetBrains Mono or similar
- **Color coding:**
  - Red (`#ef4444` range) = critical / hard clash
  - Amber (`#f59e0b` range) = warning / clearance clash
  - Green (`#22c55e` range) = resolved
- **Brand accent:** Gold `#d4a853`
- **Layout:** Minimal, data-dense. BIM coordinators want information, not decoration.
- **Clash detail:** Two-column layout — left: element details/properties grid, right: resolution options with risk indicators
- **Visualizations:** Cross-section SVG (pipes in ground, animated on option selection) and profile view SVG (longitudinal section with inverts and ground surface)

---

## Security Rules

1. **Local agent listens on localhost only** — never exposed to the network
2. **No model data leaves the machine** — only structured metadata goes to the cloud
3. **ACC OAuth tokens:** Store refresh tokens encrypted in the database
4. **Consent gate is sacred:** The `apply_resolution` function MUST verify `consent state == APPROVED` before any C3D execution. If not approved, refuse and return an error.
5. **CORS:** Configure so the web dashboard can talk to localhost agent endpoints

---

## Inviolable Rules for AI Assistants

1. **NEVER auto-execute.** The consent gate is sacred. The resolution engine proposes, the user approves, only then execute.
2. **Model data stays local.** Only structured clash metadata (IDs, coordinates, properties) goes to the cloud. DWG/NWD files never leave the machine.
3. **Resolution engine is deterministic.** Claude explains and helps prioritize. It does NOT calculate offsets or check constraints. The engine does that with hard math.
4. **Audit everything.** Every modification must be traceable to a clash ID, proposal ID, approved-by user, and timestamped before/after values. ISO 19650 compliance.
5. **Support both gravity and pressure networks.** Gravity pipes have slope constraints. Pressure pipes are more flexible vertically. The engine must handle both correctly.
6. **Configurable thresholds.** Min cover, min slope, max depth, clearance — all configurable per project.
7. **Never put engineering decision logic in the LLM path.** All constraint checking, offset calculation, and risk assessment is deterministic code in the Resolution Engine.

---

## Sample Test Data

Two sample clashes are available for development before real data sources are connected:

- **CLH-001:** Hard clash (critical), Storm Pipe S-101 (600mm RCP, gravity) vs Water Main W-205 (200mm DI, pressure), 45mm penetration at grid G-14, station ~2450
- **CLH-002:** Clearance clash (warning), Sanitary Trunk T-301 (450mm PVC, gravity) vs Force Main FM-102 (300mm HDPE, pressure), 80mm gap at grid H-18, station ~3212

Use these for building the UI, testing the resolution engine, and validating the consent workflow before connecting to Navisworks or ACC.

---

## ACC Integration Notes

- **Auth:** APS 3-legged OAuth 2.0, scopes: `data:read`, `data:write`, `account:read`
- **Clash reading:** `GET /mc/v3/containers/{containerId}/clash-tests` and `GET /mc/v3/containers/{containerId}/clash-tests/{testId}/clashes`
- **Element properties:** `GET /modelderivative/v2/designdata/{urn}/metadata/{guid}/properties`
- **Adapter pattern:** Map ACC clash data to the same `ClashData` interface used by Navisworks. The rest of the pipeline (resolution engine, consent, execution) is source-agnostic.

## Civil 3D Execution Notes

- Plugin is a .NET class library loaded via `NETLOAD` command
- Starts a lightweight REST API on `localhost:5100` (Kestrel)
- All modifications happen inside a C3D `Transaction`
- Uses `Application.Idle` marshaling to execute on the document thread
- **Gravity networks:** Modify `Profile.PVIs` (Point of Vertical Intersection elevations)
- **Pressure networks:** Modify `PressurePipe` positions
- **Structure adjustment:** Connected manholes/catch basins need rim and sump elevation updates
- **Downstream propagation:** Cascade elevation changes to maintain minimum slope
