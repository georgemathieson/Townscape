// Renders preview images of the generated town with three.js in headless Chromium.
//   node render.mjs <dir-with-town.gltf> [view:preset ...]
// Views and lighting presets are defined in page.html.
import { createServer } from 'node:http';
import { readFile, writeFile, mkdir } from 'node:fs/promises';
import { extname, join, resolve, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import { chromium } from 'playwright-core';

const here = dirname(fileURLToPath(import.meta.url));
const outDir = resolve(process.argv[2] ?? 'out');
const shots = process.argv.slice(3).length > 0
  ? process.argv.slice(3)
  : ['overview:dusk', 'bridge:dusk', 'street:dusk', 'junction:day', 'river:day', 'valley:day', 'lake:day', 'map:day', 'overview:night'];

const types = { '.html': 'text/html', '.js': 'text/javascript', '.mjs': 'text/javascript', '.gltf': 'model/gltf+json', '.bin': 'application/octet-stream' };
const server = createServer(async (request, response) => {
  const url = new URL(request.url, 'http://localhost');
  const path = url.pathname.startsWith('/out/') ? join(outDir, url.pathname.slice(5)) : join(here, url.pathname === '/' ? 'page.html' : url.pathname);
  try {
    const body = await readFile(path);
    response.writeHead(200, { 'content-type': types[extname(path)] ?? 'application/octet-stream' });
    response.end(body);
  } catch {
    response.writeHead(404);
    response.end();
  }
});
await new Promise((ok) => server.listen(0, ok));
const port = server.address().port;

const browser = await chromium.launch({
  executablePath: process.env.CHROMIUM_PATH ?? '/opt/pw-browsers/chromium',
  args: ['--use-gl=angle', '--use-angle=swiftshader', '--enable-unsafe-swiftshader', '--ignore-gpu-blocklist', '--disable-background-networking', '--disable-component-update'],
});
const page = await browser.newPage({ viewport: { width: 1600, height: 900 } });
page.on('console', (message) => console.log(`[page] ${message.text()}`));
page.on('pageerror', (error) => console.error(`[page error] ${error.message}`));
await page.goto(`http://localhost:${port}/`);
await page.waitForFunction(() => window.previewReady === true, null, { timeout: 120000 });

await mkdir(join(outDir, 'renders'), { recursive: true });
for (const shot of shots) {
  const [view, preset] = shot.split(':');
  const dataUrl = await page.evaluate(([v, p]) => window.renderView(v, p), [view, preset ?? 'dusk']);
  const file = join(outDir, 'renders', `${view}-${preset ?? 'dusk'}.png`);
  await writeFile(file, Buffer.from(dataUrl.split(',')[1], 'base64'));
  console.log(`wrote ${file}`);
}

await browser.close();
server.close();
