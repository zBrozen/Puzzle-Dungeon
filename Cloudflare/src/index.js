// Le build Unity est compressé en Brotli avec « Decompression Fallback » : les fichiers de Build/
// finissent en .unityweb. Si le navigateur reçoit Content-Encoding: br, il les décompresse lui-même
// (rapide) ; sinon le loader Unity le fait en JavaScript (plus lent, mais ça marche quand même).
//
// Sur Cloudflare, poser Content-Encoding via _headers ne suffit pas : Cloudflare ne sait pas que le
// corps est déjà compressé et le navigateur reçoit des octets illisibles. On sert donc ces fichiers
// depuis le Worker avec encodeBody: "manual", qui transmet le corps tel quel avec l'en-tête.
//
// Workers refuse aussi les fichiers de plus de 25 Mio : scripts/build.mjs découpe les gros fichiers
// en morceaux, et on les recolle ici en flux, sans jamais tout charger en mémoire.

import splitManifest from "./split-manifest.json";

const CONTENT_TYPES = [
  [".wasm.unityweb", "application/wasm"],
  [".js.unityweb", "application/javascript"],
  [".json.unityweb", "application/json"],
  [".data.unityweb", "application/octet-stream"],
];

function unitywebHeaders(pathname, headers) {
  const match = CONTENT_TYPES.find(([ext]) => pathname.endsWith(ext));
  headers.set("Content-Type", match ? match[1] : "application/octet-stream");
  headers.set("Content-Encoding", "br");
}

function serveSplit(request, env, ctx, pathname, entry) {
  // Les fichiers n'ont pas de hash dans leur nom : no-cache force la revalidation par ETag.
  const headers = new Headers({ "Cache-Control": "no-cache", ETag: entry.etag });
  if (pathname.endsWith(".unityweb")) unitywebHeaders(pathname, headers);

  if (request.headers.get("If-None-Match") === entry.etag) {
    return new Response(null, { status: 304, headers });
  }
  if (request.method === "HEAD") {
    headers.set("Content-Length", String(entry.size));
    return new Response(null, { status: 200, headers });
  }

  // FixedLengthStream annonce la taille totale (Content-Length), utile à la barre de progression Unity.
  const { readable, writable } = new FixedLengthStream(entry.size);
  const pump = (async () => {
    for (const part of entry.parts) {
      const response = await env.ASSETS.fetch(new URL(part, request.url));
      if (!response.ok) throw new Error(`Morceau manquant : ${part} (${response.status})`);
      await response.body.pipeTo(writable, { preventClose: true });
    }
    await writable.close();
  })().catch((error) => writable.abort(error));
  ctx.waitUntil(pump);

  return new Response(readable, { status: 200, headers, encodeBody: "manual" });
}

export default {
  async fetch(request, env, ctx) {
    const { pathname } = new URL(request.url);

    const entry = splitManifest[pathname];
    if (entry) return serveSplit(request, env, ctx, pathname, entry);

    const response = await env.ASSETS.fetch(request);

    // Les fichiers n'ont pas de hash dans leur nom : on force la revalidation (ETag) pour qu'un
    // nouveau déploiement ne mélange jamais l'ancien .wasm avec le nouveau .data.
    const headers = new Headers(response.headers);
    headers.set("Cache-Control", "no-cache");

    if (!pathname.endsWith(".unityweb") || response.status !== 200) {
      return new Response(response.body, { status: response.status, headers });
    }

    unitywebHeaders(pathname, headers);
    return new Response(response.body, { status: 200, headers, encodeBody: "manual" });
  },
};
