// ── Clash Data Models ──

export interface ClashLocation {
  x: number;
  y: number;
  z: number;
}

export interface ClashElement {
  elementId: string;
  modelFile: string;
  discipline: string;
  category: string;
  pipeNetworkName?: string;
  networkType?: 'gravity' | 'pressure';
  pipeName?: string;
  pipeDiameter?: number; // mm
  material?: string;
  alignmentName?: string;
  stationStart?: number;
  stationEnd?: number;
  invertElevation?: number; // meters
  crownElevation?: number;
  slope?: number; // fraction, e.g. 0.008
  cover?: number; // meters
}

export interface ClashData {
  id: string;
  source: 'navisworks' | 'acc';
  sourceRef: string;
  testName: string;
  type: 'hard' | 'clearance';
  status: 'new' | 'active' | 'reviewed' | 'resolved';
  severity: 'critical' | 'warning' | 'info';
  location: ClashLocation;
  distance: number; // negative = penetration, positive = gap
  gridLocation?: string;
  element1: ClashElement;
  element2: ClashElement;
  detectedAt: string;
}

// ── Resolution Engine Types ──

export type RiskLevel = 'low' | 'medium' | 'high';

export interface ConstraintResult {
  name: string;
  threshold: string;
  actual: string;
  passed: boolean;
  message: string;
}

export interface ResolutionOption {
  id: string;
  label: string;
  direction: 'lower' | 'raise' | 'horizontal';
  description: string;
  offset: number; // meters
  risk: RiskLevel;
  targetElement: ClashElement;
  affectedStationRange: [number, number];
  resultingCover: number;
  resultingClearance: number;
  resultingSlope: number | null;
  downstreamImpactCount: number;
  constraints: ConstraintResult[];
  warnings: string[];
}

// ── Consent Workflow ──

export type ConsentState =
  | 'analyzing'
  | 'options_ready'
  | 'option_selected'
  | 'approved'
  | 'executing'
  | 'executed'
  | 'validating'
  | 'resolved'
  | 'partially_resolved'
  | 'not_resolved'
  | 'failed';

export interface Proposal {
  id: string;
  clashId: string;
  options: ResolutionOption[];
  selectedOptionId: string | null;
  consentState: ConsentState;
  approvedBy: string | null;
  approvedAt: string | null;
  executedAt: string | null;
}

// ── Audit ──

export interface AuditEntry {
  id: string;
  timestamp: string;
  clashId: string;
  proposalId: string;
  optionApplied: string;
  approvedBy: string;
  elementsModified: {
    elementId: string;
    property: string;
    oldValue: string;
    newValue: string;
    unit: string;
  }[];
}

// ── Project ──

export interface ProjectConstraints {
  minCoverGravity: number;
  minCoverPressure: number;
  crossingClearance: number;
  minSlopeGravity: number;
  maxDepth: number;
}

export interface Project {
  id: string;
  name: string;
  location: string;
  clashSource: 'navisworks' | 'acc';
  constraints: ProjectConstraints;
  createdAt: string;
}

// ── Connection Status ──

export interface ConnectionStatus {
  agent: boolean;
  navisworks: boolean;
  civil3d: boolean;
  acc: boolean;
}

// ── Chat ──

export interface ChatMessage {
  id: string;
  role: 'user' | 'assistant';
  content: string;
  timestamp: string;
}

// ── ACC ──

export interface ACCProject {
  id: string;
  name: string;
  accountId: string;
}

export interface ACCClashTest {
  id: string;
  name: string;
  status: string;
  clashCount: number;
}
