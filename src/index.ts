import { Octokit } from "@octokit/action";
import { fileURLToPath } from "node:url";
import { createWriteStream } from "node:fs";
import { readFile } from "node:fs/promises";
import { join } from "node:path";
import { Readable } from "node:stream";
import { finished } from "node:stream/promises";
import nodeStreamZip from "node-stream-zip";

import type { Asset, Manifest, PluginVersion } from "./type.js";

const rootDir = fileURLToPath(new URL("..", import.meta.url));

async function getChecksum(assets: Asset[]): Promise<string> {
  const asset = assets.find(a => a.name === "checksum");
  if (!asset) {
    throw new Error("Cannot find checksum");
  }
  const url = asset.browser_download_url;
  const res = await fetch(url, { method: "GET" });
  if (!res.ok) {
    throw new Error(`Cannot fetch ${url}`);
  }
  return res.text();
}

async function getTimestamp(assets: Asset[]): Promise<[string, string]> {
  const asset = assets.find(a => a.name === "media-data-hub.zip");
  if (!asset) {
    throw new Error("Cannot find checksum");
  }
  const url = asset.browser_download_url;
  const res = await fetch(url, { method: "GET" });
  if (!res.ok || !res.body) {
    throw new Error(`Cannot fetch ${url}`);
  }
  const ws = createWriteStream("media-data-hub.zip");
  await finished(Readable.fromWeb(res.body).pipe(ws));
  const zip = new nodeStreamZip.async({ file: "media-data-hub.zip" });
  const data = await zip.entryData("meta.json");
  const meta = JSON.parse(data.toString("utf-8"));
  return [asset.browser_download_url, meta.timestamp];
}

async function main(): Promise<void> {
  const octokit = new Octokit();
  const owner = "media-data-hub";
  const repo = "jellyfin-plugin";

  const { data } = await octokit.request(
    "GET /repos/{owner}/{repo}/releases/latest",
    {
      owner,
      repo,
      headers: {
        "X-GitHub-Api-Version": "2022-11-28"
      }
    }
  );
  if (!data) {
    console.warn("Empty data");
    return;
  }
  const versionRegex = /^v(?<version>\d\.\d\.\d)$/u;
  const regexResult = versionRegex.exec(data.tag_name);
  const version = regexResult?.groups?.version;
  if (!version) {
    console.warn(`Invalid tag name ${data.tag_name}. Skipping`);
    return;
  }
  const csVersion = `${version}.0`;
  const manifestPath = join(rootDir, "manifest.json");
  const manifestText = await readFile(manifestPath, { encoding: "utf-8" });
  const manifest = JSON.parse(manifestText) as Manifest;
  const plugin = manifest.find(
    p => p.guid === "88ce23bd-f56f-4269-9949-e734326e9797"
  );
  if (!plugin) {
    console.warn("Cannot find plugin by GUID");
    return;
  }
  // if (plugin.versions.find(v => v.version === csVersion)) {
  //   console.warn(`Version already exists (${csVersion})`);
  //   return;
  // }
  const [checksum, [timestamp, sourceUrl]] = await Promise.all([
    getChecksum(data.assets),
    getTimestamp(data.assets)
  ]);
  const newVersion: PluginVersion = {
    version: csVersion,
    changelog: `See https://github.com/media-data-hub/jellyfin-plugin/releases/tag/${data.tag_name}`,
    targetAbi: "10.8.11.0",
    sourceUrl,
    checksum,
    timestamp
  };
  console.log(newVersion);
}

main();
