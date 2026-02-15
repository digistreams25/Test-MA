/**
 * Autodesk Construction Cloud (ACC) Model Coordination Integration
 *
 * Uses Autodesk Platform Services (APS, formerly Forge) REST API
 * to read clash detection results from Model Coordination.
 *
 * Auth: 3-legged OAuth 2.0
 * Scopes: data:read, data:write, account:read
 */

import type { ClashData, ClashElement } from '@twinflux/shared-types';

const APS_BASE = 'https://developer.api.autodesk.com';
const CLIENT_ID = process.env.APS_CLIENT_ID || '';
const CLIENT_SECRET = process.env.APS_CLIENT_SECRET || '';
const CALLBACK_URL = process.env.APS_CALLBACK_URL || 'http://localhost:4000/projects/{id}/acc-callback';

interface APSTokens {
  access_token: string;
  refresh_token: string;
  expires_in: number;
  token_type: string;
}

interface ACCClashRaw {
  id: string;
  status: string;
  distance: number;
  clashPoint: { x: number; y: number; z: number };
  lDocVersionUrn: string;
  rDocVersionUrn: string;
  lObjId: number;
  rObjId: number;
}

interface ACCElementProperties {
  objectid: number;
  name: string;
  properties: Record<string, Record<string, string | number>>;
}

// In-memory token store (in production, use encrypted DB storage)
const tokenStore: Record<string, APSTokens> = {};

class ACCService {
  /**
   * Generate the APS OAuth authorization URL for 3-legged auth
   */
  getAuthorizationUrl(projectId: string): string {
    const scopes = 'data:read data:write account:read';
    const callbackUrl = CALLBACK_URL.replace('{id}', projectId);
    return (
      `${APS_BASE}/authentication/v2/authorize` +
      `?response_type=code` +
      `&client_id=${CLIENT_ID}` +
      `&redirect_uri=${encodeURIComponent(callbackUrl)}` +
      `&scope=${encodeURIComponent(scopes)}` +
      `&state=${projectId}`
    );
  }

  /**
   * Exchange authorization code for access/refresh tokens
   */
  async exchangeCodeForTokens(code: string): Promise<APSTokens> {
    if (!CLIENT_ID || !CLIENT_SECRET) {
      // Return mock tokens for development
      const mockTokens: APSTokens = {
        access_token: 'mock-aps-access-token',
        refresh_token: 'mock-aps-refresh-token',
        expires_in: 3600,
        token_type: 'Bearer',
      };
      tokenStore['default'] = mockTokens;
      return mockTokens;
    }

    const response = await fetch(`${APS_BASE}/authentication/v2/token`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
      body: new URLSearchParams({
        grant_type: 'authorization_code',
        code,
        client_id: CLIENT_ID,
        client_secret: CLIENT_SECRET,
        redirect_uri: CALLBACK_URL,
      }),
    });

    if (!response.ok) {
      throw new Error(`APS token exchange failed: ${response.status}`);
    }

    const tokens: APSTokens = await response.json();
    tokenStore['default'] = tokens;
    return tokens;
  }

  /**
   * Refresh an expired access token
   */
  async refreshAccessToken(refreshToken: string): Promise<APSTokens> {
    const response = await fetch(`${APS_BASE}/authentication/v2/token`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
      body: new URLSearchParams({
        grant_type: 'refresh_token',
        refresh_token: refreshToken,
        client_id: CLIENT_ID,
        client_secret: CLIENT_SECRET,
      }),
    });

    if (!response.ok) {
      throw new Error(`APS token refresh failed: ${response.status}`);
    }

    const tokens: APSTokens = await response.json();
    tokenStore['default'] = tokens;
    return tokens;
  }

  /**
   * Get clash tests from Model Coordination
   */
  async getClashTests(
    containerId: string,
    accessToken: string
  ): Promise<{ id: string; name: string; status: string; clashCount: number }[]> {
    const response = await fetch(
      `${APS_BASE}/construction/mc/v3/containers/${containerId}/clash-tests`,
      {
        headers: { Authorization: `Bearer ${accessToken}` },
      }
    );

    if (!response.ok) {
      throw new Error(`Failed to fetch clash tests: ${response.status}`);
    }

    const data = await response.json();
    return data.clashTests || [];
  }

  /**
   * Get clashes from a specific clash test
   */
  async getClashesFromTest(
    containerId: string,
    testId: string,
    accessToken: string
  ): Promise<ACCClashRaw[]> {
    const response = await fetch(
      `${APS_BASE}/construction/mc/v3/containers/${containerId}/clash-tests/${testId}/clashes`,
      {
        headers: { Authorization: `Bearer ${accessToken}` },
      }
    );

    if (!response.ok) {
      throw new Error(`Failed to fetch clashes: ${response.status}`);
    }

    const data = await response.json();
    return data.clashes || [];
  }

  /**
   * Get element properties from Model Derivative API
   */
  async getElementProperties(
    urn: string,
    objectId: number,
    accessToken: string
  ): Promise<ACCElementProperties | null> {
    // First get the metadata GUID
    const metaResponse = await fetch(
      `${APS_BASE}/modelderivative/v2/designdata/${urn}/metadata`,
      {
        headers: { Authorization: `Bearer ${accessToken}` },
      }
    );

    if (!metaResponse.ok) return null;

    const metaData = await metaResponse.json();
    const guid = metaData.data?.metadata?.[0]?.guid;
    if (!guid) return null;

    // Then get properties for the specific object
    const propResponse = await fetch(
      `${APS_BASE}/modelderivative/v2/designdata/${urn}/metadata/${guid}/properties?objectid=${objectId}`,
      {
        headers: { Authorization: `Bearer ${accessToken}` },
      }
    );

    if (!propResponse.ok) return null;

    const propData = await propResponse.json();
    const collection = propData.data?.collection;
    return collection?.find((item: ACCElementProperties) => item.objectid === objectId) || null;
  }

  /**
   * Map ACC element properties to our ClashElement interface
   */
  mapToClashElement(
    props: ACCElementProperties | null,
    objectId: number,
    docUrn: string
  ): ClashElement {
    if (!props) {
      return {
        elementId: `ACC-${objectId}`,
        modelFile: docUrn,
        discipline: 'Unknown',
        category: 'Unknown',
      };
    }

    // Extract properties from APS property categories
    const identity = props.properties['Identity Data'] || {};
    const dimensions = props.properties['Dimensions'] || {};
    const constraints = props.properties['Constraints'] || {};
    const mechanical = props.properties['Mechanical'] || {};

    return {
      elementId: `ACC-${objectId}`,
      modelFile: String(identity['Source File'] || docUrn),
      discipline: String(identity['Discipline'] || 'Civil'),
      category: String(identity['Category'] || 'Pipe'),
      pipeNetworkName: String(identity['System Name'] || identity['Network'] || ''),
      networkType: this.inferNetworkType(props),
      pipeName: String(props.name || `Element ${objectId}`),
      pipeDiameter: Number(dimensions['Diameter'] || dimensions['Nominal Diameter'] || 0),
      material: String(identity['Material'] || mechanical['Material'] || ''),
      alignmentName: String(constraints['Alignment'] || ''),
      stationStart: Number(constraints['Start Station'] || 0),
      stationEnd: Number(constraints['End Station'] || 0),
      invertElevation: Number(constraints['Invert Elevation'] || constraints['Start Invert Elevation'] || 0),
      crownElevation: Number(constraints['Crown Elevation'] || 0),
      slope: this.parseSlope(constraints['Slope'] || constraints['Grade']),
      cover: Number(constraints['Cover'] || 0),
    };
  }

  /**
   * Infer network type from element properties
   */
  private inferNetworkType(props: ACCElementProperties): 'gravity' | 'pressure' {
    const name = (props.name || '').toLowerCase();
    const systemName = String(
      props.properties['Identity Data']?.['System Name'] || ''
    ).toLowerCase();

    const gravityKeywords = ['storm', 'sanitary', 'sewer', 'stm', 'san', 'gravity'];
    const pressureKeywords = ['water', 'force', 'pressure', 'wtr', 'fm'];

    for (const kw of gravityKeywords) {
      if (name.includes(kw) || systemName.includes(kw)) return 'gravity';
    }
    for (const kw of pressureKeywords) {
      if (name.includes(kw) || systemName.includes(kw)) return 'pressure';
    }

    return 'pressure'; // Default to pressure (more flexible)
  }

  private parseSlope(value: string | number | undefined): number | undefined {
    if (value === undefined || value === null || value === '') return undefined;
    const num = typeof value === 'string' ? parseFloat(value) : value;
    return isNaN(num) ? undefined : num;
  }

  /**
   * Map raw ACC clash to our unified ClashData model
   */
  mapToClashData(
    raw: ACCClashRaw,
    testName: string,
    element1: ClashElement,
    element2: ClashElement
  ): ClashData {
    return {
      id: `ACC-${raw.id}`,
      source: 'acc',
      sourceRef: raw.id,
      testName,
      type: raw.distance < 0 ? 'hard' : 'clearance',
      status: this.mapStatus(raw.status),
      severity: raw.distance < 0 ? 'critical' : Math.abs(raw.distance) < 0.1 ? 'warning' : 'info',
      location: raw.clashPoint,
      distance: raw.distance,
      element1,
      element2,
      detectedAt: new Date().toISOString(),
    };
  }

  private mapStatus(accStatus: string): 'new' | 'active' | 'reviewed' | 'resolved' {
    switch (accStatus?.toLowerCase()) {
      case 'new':
        return 'new';
      case 'active':
        return 'active';
      case 'reviewed':
      case 'approved':
        return 'reviewed';
      case 'resolved':
      case 'closed':
        return 'resolved';
      default:
        return 'active';
    }
  }

  /**
   * Main method: fetch and normalize all clashes for a project
   * In production, projectId maps to stored ACC containerId + tokens
   */
  async getClashes(projectId: string): Promise<ClashData[]> {
    const tokens = tokenStore['default'];

    // If no real tokens, return mock data normalized as ACC source
    if (!tokens || tokens.access_token.startsWith('mock-')) {
      return this.getMockACCClashes();
    }

    // Real ACC API flow
    const containerId = projectId; // In production: look up from DB
    const accessToken = tokens.access_token;

    const tests = await this.getClashTests(containerId, accessToken);
    const allClashes: ClashData[] = [];

    for (const test of tests) {
      const rawClashes = await this.getClashesFromTest(
        containerId,
        test.id,
        accessToken
      );

      for (const raw of rawClashes) {
        const el1Props = await this.getElementProperties(
          raw.lDocVersionUrn,
          raw.lObjId,
          accessToken
        );
        const el2Props = await this.getElementProperties(
          raw.rDocVersionUrn,
          raw.rObjId,
          accessToken
        );

        const element1 = this.mapToClashElement(el1Props, raw.lObjId, raw.lDocVersionUrn);
        const element2 = this.mapToClashElement(el2Props, raw.rObjId, raw.rDocVersionUrn);

        allClashes.push(this.mapToClashData(raw, test.name, element1, element2));
      }
    }

    return allClashes;
  }

  /**
   * Mock ACC clashes for development
   */
  private getMockACCClashes(): ClashData[] {
    return [
      {
        id: 'CLH-002',
        source: 'acc',
        sourceRef: 'ACC-ISSUE-042',
        testName: 'Underground Utilities',
        type: 'clearance',
        status: 'active',
        severity: 'warning',
        location: { x: 623520.1, y: 4834095.4, z: 183.2 },
        distance: 0.08,
        gridLocation: 'H-18',
        element1: {
          elementId: 'E-SAN-001',
          modelFile: 'Sanitary_Network.dwg',
          discipline: 'Civil — Sanitary',
          category: 'Pipe',
          pipeNetworkName: 'SAN-Trunk-01',
          networkType: 'gravity',
          pipeName: 'Sanitary Trunk T-301',
          pipeDiameter: 450,
          material: 'PVC',
          alignmentName: 'San-Trunk-AL',
          stationStart: 3210.0,
          stationEnd: 3215.8,
          invertElevation: 182.75,
          crownElevation: 183.2,
          slope: 0.006,
          cover: 2.1,
        },
        element2: {
          elementId: 'E-FM-001',
          modelFile: 'ForceMains.dwg',
          discipline: 'Civil — Force Main',
          category: 'Pipe',
          pipeNetworkName: 'FM-PS2-01',
          networkType: 'pressure',
          pipeName: 'Force Main FM-102',
          pipeDiameter: 300,
          material: 'HDPE',
          alignmentName: 'FM-PS2-AL',
          stationStart: 845.2,
          stationEnd: 849.0,
          invertElevation: 183.05,
          crownElevation: 183.35,
          slope: undefined,
          cover: 1.35,
        },
        detectedAt: '2025-12-02T09:15:00Z',
      },
      {
        id: 'CLH-004',
        source: 'acc',
        sourceRef: 'ACC-ISSUE-055',
        testName: 'Underground Utilities',
        type: 'clearance',
        status: 'new',
        severity: 'warning',
        location: { x: 623500.0, y: 4834100.0, z: 182.5 },
        distance: 0.05,
        gridLocation: 'H-17',
        element1: {
          elementId: 'E-SAN-002',
          modelFile: 'Sanitary_Network.dwg',
          discipline: 'Civil — Sanitary',
          category: 'Pipe',
          pipeNetworkName: 'SAN-Trunk-01',
          networkType: 'gravity',
          pipeName: 'Sanitary Trunk T-305',
          pipeDiameter: 375,
          material: 'PVC',
          alignmentName: 'San-Trunk-AL',
          stationStart: 3250.0,
          stationEnd: 3255.0,
          invertElevation: 182.125,
          crownElevation: 182.5,
          slope: 0.005,
          cover: 1.8,
        },
        element2: {
          elementId: 'E-WTR-003',
          modelFile: 'Water_Network.dwg',
          discipline: 'Civil — Water',
          category: 'Pipe',
          pipeNetworkName: 'WTR-Dist-03',
          networkType: 'pressure',
          pipeName: 'Water Main W-310',
          pipeDiameter: 250,
          material: 'Ductile Iron',
          alignmentName: 'Water-Dist-AL',
          stationStart: 1300.0,
          stationEnd: 1303.5,
          invertElevation: 182.4,
          crownElevation: 182.65,
          slope: undefined,
          cover: 1.2,
        },
        detectedAt: '2025-12-03T11:00:00Z',
      },
    ];
  }
}

export const accService = new ACCService();
