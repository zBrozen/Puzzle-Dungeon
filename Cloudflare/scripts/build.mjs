// Prépare le build Unity pour Cloudflare Workers, qui refuse les fichiers de plus de 25 Mio.
// Copie le build dans Cloudflare/dist en découpant les gros fichiers en morceaux de 20 Mio
// (fichier.part0, fichier.part1…). Le Worker les recolle à la volée (voir src/index.js) grâce au
// manifeste src/split-manifest.json généré ici.
//
// Usage : node scripts/build.mjs [dossier du build]   (par défaut ../Builds/WebGL)

import { createHash } from "node:crypto";
import { existsSync } from "node:fs";
import { copyFile, mkdir, open, readdir, rm, stat, writeFile } from "node:fs/promises";
import { dirname, join, relative, resolve, sep } from "node:path";
import { fileURLToPath } from "node:url";

const root = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const src = resolve(root, process.argv[2] ?? "../Builds/WebGL");
const dist = join(root, "dist");
const manifestPath = join(root, "src", "split-manifest.json");

const PART_SIZE = 20 * 1024 * 1024;

if (!existsSync(join(src, "index.html"))) {
  console.error(`Build WebGL introuvable dans ${src} (pas d'index.html). Lance d'abord le build Unity.`);
  process.exit(1);
}

await rm(dist, { recursive: true, force: true });

const manifest = {};

async function walk(dir) {
  for (const entry of await readdir(dir, { withFileTypes: true })) {
    const from = join(dir, entry.name);
    const to = join(dist, relative(src, from));
    if (entry.isDirectory()) {
      await walk(from);
      continue;
    }
    await mkdir(dirname(to), { recursive: true });
    const { size } = await stat(from);
    if (size <= PART_SIZE) {
      await copyFile(from, to);
      continue;
    }
    manifest["/" + relative(src, from).split(sep).join("/")] = await split(from, to, size);
  }
}

async function split(from, to, size) {
  const hash = createHash("sha256");
  const input = await open(from, "r");
  const parts = [];
  try {
    for (let offset = 0, i = 0; offset < size; offset += PART_SIZE, i++) {
      const buffer = Buffer.alloc(Math.min(PART_SIZE, size - offset));
      await input.read(buffer, 0, buffer.length, offset);
      hash.update(buffer);
      const partPath = `${to}.part${i}`;
      await writeFile(partPath, buffer);
      parts.push("/" + relative(dist, partPath).split(sep).join("/"));
    }
  } finally {
    await input.close();
  }
  const mib = (size / 1024 / 1024).toFixed(1);
  console.log(`Découpé : ${relative(src, from)} (${mib} Mio) → ${parts.length} morceaux`);
  return { size, parts, etag: `"${hash.digest("hex").slice(0, 32)}"` };
}

await walk(src);
await writeFile(manifestPath, JSON.stringify(manifest, null, 2) + "\n");
console.log(`Build prêt dans ${relative(process.cwd(), dist) || dist}`);
