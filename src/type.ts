import type { components } from "@octokit/openapi-types";

export interface Plugin {
  guid: string;
  name: string;
  description: string;
  overview: string;
  owner: string;
  category: string;
  versions: PluginVersion[];
}

export interface PluginVersion {
  version: string;
  changelog: string;
  targetAbi: string;
  sourceUrl: string;
  checksum: string;
  timestamp: string;
}

export type Manifest = Plugin[];
export type Asset = components["schemas"]["release-asset"];
