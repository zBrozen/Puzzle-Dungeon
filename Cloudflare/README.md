# Déploiement WebGL sur Cloudflare — vestige.brozen.fr

Le build WebGL est hébergé sur **Cloudflare Workers** (fichiers statiques + un petit Worker pour `Build/*`).

## Première fois

```bash
cd Cloudflare
npm install
npx wrangler login
```

## À chaque version

1. Unity → Build Profiles → Web, template **Mobile**, puis Build dans `Builds/WebGL` (à la racine du projet).
2. Déployer :

   ```bash
   cd Cloudflare
   npm run deploy
   ```

Au premier déploiement, Cloudflare crée lui-même l'enregistrement DNS `vestige.brozen.fr` et le certificat HTTPS.

## Pourquoi un Worker ?

Le build est compressé en Brotli avec « Decompression Fallback » (`.unityweb`). Sur Cloudflare, un
`Content-Encoding: br` posé via `_headers` casse le chargement : le navigateur reçoit des octets qu'il ne
peut pas lire. Le Worker (`src/index.js`) sert ces fichiers avec `encodeBody: "manual"`, ce qui transmet
le corps tel quel avec le bon en-tête. Le navigateur décompresse alors lui-même, ce qui est plus rapide.

## Fichiers de plus de 25 Mio

Cloudflare Workers refuse les fichiers de plus de 25 Mio, et le `.data.unityweb` les dépasse. `npm run deploy`
lance d'abord `scripts/build.mjs`, qui copie `Builds/WebGL` dans `Cloudflare/dist` en découpant les gros
fichiers en morceaux de 20 Mio (`.part0`, `.part1`…). Il écrit aussi `src/split-manifest.json`. Le Worker
recolle ensuite les morceaux en flux : le navigateur reçoit un seul fichier, avec sa taille totale et son ETag.
`dist/` et le manifeste sont régénérés à chaque déploiement, et ils ne sont pas versionnés.

## Cache

- Les noms de fichiers n'ont pas de hash : tout `Build/` est servi en `no-cache` (revalidation par ETag)
  pour qu'un nouveau déploiement ne mélange jamais l'ancien et le nouveau build.

## Tester en local

```bash
npm run dev
```

Le site est alors servi sur http://localhost:8787 avec le build présent dans `Builds/WebGL`.
Si `wrangler dev` affiche « The Workers runtime failed to start », d'anciens processus `workerd.exe` tournent
encore : ferme-les depuis le Gestionnaire des tâches.
