#!/usr/bin/env node
/**
 * QuickCart Product Image Downloader
 * ───────────────────────────────────────────────────────────────────────────
 * Downloads real product photos for the products visible on the first screen
 * (above the fold) of both the Home page and the Products page.
 *
 * Source priority:  Open Food Facts  →  Pexels  →  Unsplash
 *
 * Output: The .svg files are REPLACED with SVG wrappers that embed a real
 * JPEG photo as a base64 data-URI.  This keeps every existing imageUrl
 * unchanged (no seeder / Angular / API edits needed).
 *
 * Usage
 * ─────
 *   node download-images.js
 *   PEXELS_KEY=xxx UNSPLASH_KEY=yyy node download-images.js
 *
 * See README at the bottom of this file for full setup instructions.
 */

'use strict';

const https  = require('https');
const http   = require('http');
const fs     = require('fs');
const path   = require('path');

// ── Configuration ─────────────────────────────────────────────────────────────
const OUTPUT_DIR   = path.join('D:', 'QuickCart', 'web', 'public', 'assets', 'images', 'products');
const IMAGE_SIZE   = 600;   // px — square crop
const JPEG_QUALITY = 88;
const TIMEOUT_MS   = 20_000;
const DELAY_MS     = 700;   // polite pause between API calls

const PEXELS_KEY   = process.env.PEXELS_KEY   || '';
const UNSPLASH_KEY = process.env.UNSPLASH_KEY || '';

// ── Product list ───────────────────────────────────────────────────────────────
//
// HOME PAGE  — "Beverages" is the first alphabetical category and fills the
//              first row of product cards before the user scrolls.
//
// PRODUCTS PAGE — The grid orders all 112 products A→Z; the first 14 names
//                 all happen to start with A (Aashirvaad…Amul…Anker…Apple).
//
const PRODUCTS = [
  // ── HOME PAGE: Beverages (visible on first load) ─────────────────────────
  {
    file: 'mineral-water.svg',
    name: 'Bisleri Mineral Water 1L',
    off:  'bisleri mineral water',
    pex:  'mineral water bottle',
    uns:  'water bottle mineral clear',
  },
  {
    file: 'cold-brew.svg',
    name: 'Blue Tokai Cold Brew Coffee 200ml',
    off:  'cold brew coffee bottle',
    pex:  'cold brew coffee bottle',
    uns:  'cold brew iced coffee dark bottle',
  },
  {
    file: 'frooti.svg',
    name: 'Frooti Mango Drink 200ml',
    off:  'frooti mango drink',
    pex:  'mango juice tetra pack',
    uns:  'mango drink juice pack',
  },
  {
    file: 'nimbu-pani.svg',
    name: 'Minute Maid Nimbu Fresh 400ml',
    off:  'minute maid nimbu lemon',
    pex:  'lemon lime juice bottle',
    uns:  'lemon drink bottle',
  },
  {
    file: 'nescafe.svg',
    name: 'Nescafe Classic Coffee 50g',
    off:  'nescafe classic',
    pex:  'nescafe instant coffee glass jar',
    uns:  'instant coffee jar',
  },
  {
    file: 'coconut-water.svg',
    name: 'Raw Pressery Coconut Water 200ml',
    off:  'coconut water',
    pex:  'coconut water carton pack',
    uns:  'coconut water drink',
  },
  {
    file: 'red-bull.svg',
    name: 'Red Bull Energy Drink 250ml',
    off:  'red bull energy drink',
    pex:  'red bull energy drink can',
    uns:  'energy drink can silver',
  },
  {
    file: 'rooh-afza.svg',
    name: 'Rooh Afza Rose Sherbet 750ml',
    off:  'rooh afza',
    pex:  'rose sherbet syrup bottle',
    uns:  'syrup bottle pink red',
  },
  {
    file: 'green-tea.svg',
    name: 'Tetley Green Tea 25 Bags',
    off:  'tetley green tea',
    pex:  'green tea box tea bags',
    uns:  'green tea bags box',
  },
  {
    file: 'tropicana-orange.svg',
    name: 'Tropicana Orange Juice 1L',
    off:  'tropicana orange juice',
    pex:  'tropicana orange juice carton',
    uns:  'orange juice carton',
  },

  // ── PRODUCTS PAGE: First 14 alphabetically across all products ────────────
  {
    file: 'wheat-flour.svg',
    name: 'Aashirvaad Wheat Flour 5kg',
    off:  'aashirvaad wheat flour atta',
    pex:  'wheat flour atta bag india',
    uns:  'flour bag kitchen wheat',
  },
  {
    file: 'butter-popcorn.svg',
    name: 'Act II Butter Popcorn 100g',
    off:  'act ii butter popcorn',
    pex:  'microwave popcorn butter bag',
    uns:  'popcorn bag microwave',
  },
  {
    file: 'phone-stand.svg',
    name: 'Adjustable Desktop Phone Stand',
    off:  '',
    pex:  'adjustable phone stand desk holder',
    uns:  'phone holder stand desk',
  },
  {
    file: 'air-freshener.svg',
    name: 'Air Wick Freshmatic Spray 250ml',
    off:  'air wick freshmatic',
    pex:  'air freshener spray aerosol can',
    uns:  'air freshener aerosol can',
  },
  {
    file: 'mango.svg',
    name: 'Alphonso Mango 3pcs',
    off:  'alphonso mango',
    pex:  'alphonso mango fruit yellow',
    uns:  'mango fresh yellow fruit',
  },
  {
    file: 'amul-butter.svg',
    name: 'Amul Butter 100g',
    off:  'amul butter',
    pex:  'butter block yellow packet dairy',
    uns:  'butter block dairy',
  },
  {
    file: 'curd.svg',
    name: 'Amul Dahi 400g',
    off:  'amul dahi curd yogurt',
    pex:  'yogurt curd cup container',
    uns:  'yogurt container dairy',
  },
  {
    file: 'dark-chocolate.svg',
    name: 'Amul Dark Chocolate 150g',
    off:  'amul dark chocolate',
    pex:  'dark chocolate bar wrapper',
    uns:  'dark chocolate bar',
  },
  {
    file: 'amul-milk.svg',
    name: 'Amul Full Cream Milk 1L',
    off:  'amul milk carton',
    pex:  'milk carton dairy white fresh',
    uns:  'milk carton dairy',
  },
  {
    file: 'mango-lassi.svg',
    name: 'Amul Mango Lassi 200ml',
    off:  'amul mango lassi',
    pex:  'mango lassi drink bottle yellow',
    uns:  'mango lassi yogurt drink',
  },
  {
    file: 'paneer.svg',
    name: 'Amul Paneer 200g',
    off:  'paneer cottage cheese',
    pex:  'paneer cottage cheese block white',
    uns:  'paneer cheese block white',
  },
  {
    file: 'vanilla-ice-cream.svg',
    name: 'Amul Vanilla Ice Cream 1L',
    off:  'amul vanilla ice cream',
    pex:  'vanilla ice cream tub container',
    uns:  'ice cream tub vanilla white',
  },
  {
    file: 'usb-c-cable.svg',
    name: 'Anker USB-C Cable 1m 65W',
    off:  'anker usb-c cable',
    pex:  'USB-C charging cable braided',
    uns:  'usb cable charger phone',
  },
  {
    file: 'apple.svg',
    name: 'Apple (Red) 1kg',
    off:  'red apple',
    pex:  'fresh red apple fruit white background',
    uns:  'red apple fresh fruit',
  },
];

// ── ANSI colour helpers ────────────────────────────────────────────────────────
const C = {
  reset:  '\x1b[0m',
  bold:   '\x1b[1m',
  dim:    '\x1b[2m',
  green:  '\x1b[32m',
  red:    '\x1b[31m',
  yellow: '\x1b[33m',
  cyan:   '\x1b[36m',
};
const col = (c, s) => `${C[c] || ''}${s}${C.reset}`;

// ── HTTP fetch with redirect following ────────────────────────────────────────
function fetchBuffer(url, headers = {}, hops = 0) {
  return new Promise((resolve, reject) => {
    if (hops > 5) return reject(new Error('Too many redirects'));
    const lib = url.startsWith('https') ? https : http;
    const req = lib.get(url, { headers, timeout: TIMEOUT_MS }, res => {
      if ([301, 302, 307, 308].includes(res.statusCode) && res.headers.location) {
        res.resume();
        // Handle relative redirects
        let loc = res.headers.location;
        if (loc.startsWith('/')) {
          const u = new URL(url);
          loc = `${u.protocol}//${u.host}${loc}`;
        }
        return fetchBuffer(loc, headers, hops + 1).then(resolve, reject);
      }
      if (res.statusCode !== 200) {
        res.resume();
        return reject(new Error(`HTTP ${res.statusCode}`));
      }
      const chunks = [];
      res.on('data', c => chunks.push(c));
      res.on('end',  () => resolve(Buffer.concat(chunks)));
      res.on('error', reject);
    });
    req.on('error',   reject);
    req.on('timeout', () => { req.destroy(); reject(new Error('Timeout')); });
  });
}

async function fetchJson(url, headers = {}) {
  const buf = await fetchBuffer(url, { Accept: 'application/json', ...headers });
  return JSON.parse(buf.toString('utf8'));
}

const sleep = ms => new Promise(r => setTimeout(r, ms));

// ── Source 1: Open Food Facts (free, no API key) ──────────────────────────────
async function offSearch(query) {
  if (!query) return null;
  const url =
    'https://world.openfoodfacts.org/cgi/search.pl' +
    `?search_terms=${encodeURIComponent(query)}` +
    '&search_simple=1&action=process&json=true&page_size=8' +
    '&fields=product_name,image_front_url,image_url';

  const data = await fetchJson(url, {
    'User-Agent': 'QuickCart-Downloader/1.0 (educational; contact@quickcart.dev)',
  });

  for (const p of (data.products || [])) {
    const u = p.image_front_url || p.image_url;
    if (u && /^https?:\/\//.test(u)) return u;
  }
  return null;
}

// ── Source 2: Pexels (free API key required) ──────────────────────────────────
async function pexelsSearch(query) {
  if (!PEXELS_KEY || !query) return null;
  const url =
    'https://api.pexels.com/v1/search' +
    `?query=${encodeURIComponent(query)}&per_page=5&orientation=square`;
  const data = await fetchJson(url, { Authorization: PEXELS_KEY });
  const p = (data.photos || [])[0];
  return p?.src?.large2x || p?.src?.large || p?.src?.medium || null;
}

// ── Source 3: Unsplash (free API key required) ────────────────────────────────
async function unsplashSearch(query) {
  if (!UNSPLASH_KEY || !query) return null;
  const url =
    'https://api.unsplash.com/search/photos' +
    `?query=${encodeURIComponent(query)}&per_page=5&orientation=squarish`;
  const data = await fetchJson(url, { Authorization: `Client-ID ${UNSPLASH_KEY}` });
  const r = (data.results || [])[0];
  return r?.urls?.regular || r?.urls?.small || null;
}

// ── Image processing + save ───────────────────────────────────────────────────
async function processAndSave(imageBuffer, outputPath) {
  // lazy-require so the missing-module error message is friendlier
  let sharp;
  try { sharp = require('sharp'); }
  catch { throw new Error('sharp is not installed — run: npm install sharp'); }

  const jpeg = await sharp(imageBuffer)
    .resize(IMAGE_SIZE, IMAGE_SIZE, {
      fit:        'cover',
      position:   'attention',         // smart-crop to the most interesting area
      background: { r: 248, g: 249, b: 251, alpha: 1 },
    })
    .jpeg({ quality: JPEG_QUALITY, mozjpeg: false })
    .toBuffer();

  // Wrap in SVG so the existing .svg filename is preserved verbatim.
  // The <image> element with a data-URI is valid SVG 1.1 and renders
  // correctly in every modern browser via <img src="...svg">.
  const b64 = jpeg.toString('base64');
  const svg =
    `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 ${IMAGE_SIZE} ${IMAGE_SIZE}">\n` +
    `  <image href="data:image/jpeg;base64,${b64}" ` +
        `width="${IMAGE_SIZE}" height="${IMAGE_SIZE}" ` +
        `preserveAspectRatio="xMidYMid slice"/>\n` +
    `</svg>`;

  fs.writeFileSync(outputPath, svg, 'utf8');
  return jpeg.length;
}

// ── Single product ─────────────────────────────────────────────────────────────
async function downloadOne(prod, idx, total) {
  const tag = `[${String(idx + 1).padStart(2)}/${total}]`;
  const out  = path.join(OUTPUT_DIR, prod.file);

  // Skip files that are already real images (contain embedded JPEG)
  if (fs.existsSync(out)) {
    const existing = fs.readFileSync(out, 'utf8');
    if (existing.includes('data:image/jpeg;base64')) {
      console.log(`${tag} ${col('dim', '⏭  skip')}  ${prod.name}`);
      return { status: 'skipped', name: prod.name };
    }
  }

  // Label for progress line — pad name so columns align
  process.stdout.write(`${tag} ${col('yellow', '↓')}  ${prod.name.padEnd(40)}`);

  const sources = [
    { label: 'OFF',      fn: () => offSearch(prod.off) },
    { label: 'Pexels',   fn: () => pexelsSearch(prod.pex) },
    { label: 'Unsplash', fn: () => unsplashSearch(prod.uns) },
  ];

  for (const { label, fn } of sources) {
    let imgUrl = null;
    try {
      process.stdout.write(col('cyan', `[${label}] `));
      imgUrl = await fn();
    } catch { /* source unavailable */ }
    await sleep(DELAY_MS);

    if (!imgUrl) continue;

    try {
      const buf  = await fetchBuffer(imgUrl);
      const bytes = await processAndSave(buf, out);
      console.log(col('green', `✓  ${label}  ${(bytes / 1024).toFixed(0)}KB JPEG → SVG`));
      return { status: 'ok', name: prod.name, source: label };
    } catch (e) {
      process.stdout.write(col('yellow', '(err) '));
    }
  }

  console.log(col('red', '✗  no image found'));
  return { status: 'failed', name: prod.name };
}

// ── Entry point ────────────────────────────────────────────────────────────────
async function main() {
  console.log(`\n${col('bold', 'QuickCart  Product Image Downloader')}`);
  console.log(col('dim', '═'.repeat(60)));

  // Dependency guard
  try { require('sharp'); }
  catch {
    console.error(col('red', '\n  ✗  sharp not found.\n  Run:  npm install sharp\n'));
    process.exit(1);
  }

  // API key status
  console.log('  Open Food Facts  ' + col('green', '✓  free (no key needed)'));
  console.log('  Pexels           ' + (PEXELS_KEY
    ? col('green', '✓  key loaded')
    : col('yellow', '⚠  no key — set PEXELS_KEY env var')));
  console.log('  Unsplash         ' + (UNSPLASH_KEY
    ? col('green', '✓  key loaded')
    : col('yellow', '⚠  no key — set UNSPLASH_KEY env var')));

  fs.mkdirSync(OUTPUT_DIR, { recursive: true });

  console.log(`\n  Output   ${OUTPUT_DIR}`);
  console.log(`  Products ${PRODUCTS.length}`);
  console.log(col('dim', '─'.repeat(60)) + '\n');

  const results = [];
  for (let i = 0; i < PRODUCTS.length; i++) {
    results.push(await downloadOne(PRODUCTS[i], i, PRODUCTS.length));
  }

  // ── Summary ─────────────────────────────────────────────────────────────────
  const ok      = results.filter(r => r.status === 'ok');
  const skipped = results.filter(r => r.status === 'skipped');
  const failed  = results.filter(r => r.status === 'failed');

  console.log('\n' + col('dim', '─'.repeat(60)));
  console.log(col('bold', 'Summary'));
  console.log(col('green',  `  ✓  Downloaded  : ${ok.length}`));
  console.log(col('dim',    `  ⏭  Skipped     : ${skipped.length}  (already real images)`));

  if (failed.length) {
    console.log(col('red', `  ✗  Not found   : ${failed.length}`));
    failed.forEach(r => console.log(col('dim', `       • ${r.name}`)));
  }

  console.log(`\n  📁  ${OUTPUT_DIR}`);

  // Source breakdown
  const srcCount = {};
  ok.forEach(r => { srcCount[r.source] = (srcCount[r.source] || 0) + 1; });
  if (Object.keys(srcCount).length) {
    console.log('\n  Sources used:');
    Object.entries(srcCount).forEach(([s, n]) =>
      console.log(`       ${s.padEnd(12)} ${n} image${n > 1 ? 's' : ''}`));
  }
  console.log('');

  if (failed.length === 0 && ok.length > 0) {
    console.log(col('green', '  All images downloaded successfully.\n'));
    console.log('  Restart the Angular dev server (ng serve) to see the changes.\n');
  }
}

main().catch(err => {
  console.error(col('red', `\n  ✗  Fatal: ${err.message}`));
  process.exit(1);
});

/*
 * ─────────────────────────────────────────────────────────────────────────────
 * SETUP INSTRUCTIONS
 * ─────────────────────────────────────────────────────────────────────────────
 *
 * Step 1 — Install the ONE dependency
 * ─────────────────────────────────────
 *   Open a terminal in D:\QuickCart\ and run:
 *
 *     npm init -y
 *     npm install sharp
 *
 *   sharp does the image resizing/cropping. It installs native binaries
 *   automatically. Node 18+ is required.
 *
 *
 * Step 2 — Run with Open Food Facts only (FREE, no key needed)
 * ─────────────────────────────────────────────────────────────
 *   node download-images.js
 *
 *   Open Food Facts contains real packaging photos for most branded grocery
 *   products (Amul, Nescafe, Red Bull, etc.). No API key is required.
 *
 *
 * Step 3 — Add Pexels for better fallback coverage (recommended)
 * ───────────────────────────────────────────────────────────────
 *   a) Sign up free at https://www.pexels.com/api/ (takes 2 minutes, no card)
 *   b) Copy your API key
 *   c) Run:
 *
 *   Windows CMD:
 *     set PEXELS_KEY=your_key_here
 *     node download-images.js
 *
 *   Windows PowerShell:
 *     $env:PEXELS_KEY="your_key_here"
 *     node download-images.js
 *
 *   Mac / Linux:
 *     PEXELS_KEY=your_key_here node download-images.js
 *
 *
 * Step 4 — Add Unsplash as extra fallback (optional)
 * ────────────────────────────────────────────────────
 *   a) Sign up free at https://unsplash.com/developers
 *   b) Create an app → copy the "Access Key"
 *   c) Add UNSPLASH_KEY the same way as PEXELS_KEY above
 *
 *
 * Step 5 — What happens
 * ──────────────────────
 *   • The script tries Open Food Facts first (best for branded packaged goods)
 *   • Falls back to Pexels, then Unsplash
 *   • Crops every image to a 600 × 600 px square (product-focused smart-crop)
 *   • Embeds the real JPEG inside a tiny SVG wrapper (so .svg filenames stay)
 *   • Skips any file that already contains a real JPEG (safe to re-run)
 *   • Prints progress + final summary
 *
 *
 * Note on image filenames
 * ────────────────────────
 *   Each downloaded image is saved with the exact filename already stored in
 *   the product imageUrl (e.g. mineral-water.svg, nescafe.svg).  The file
 *   contains a valid SVG that wraps the real JPEG — browsers display the photo
 *   correctly via the <img> tag without any code changes.
 *
 *
 * Products covered by this script
 * ─────────────────────────────────
 *   HOME PAGE  (Beverages — first category row, above the fold)
 *     Bisleri Mineral Water, Blue Tokai Cold Brew, Frooti Mango Drink,
 *     Minute Maid Nimbu Fresh, Nescafe Classic, Raw Pressery Coconut Water,
 *     Red Bull Energy Drink, Rooh Afza Rose Sherbet, Tetley Green Tea,
 *     Tropicana Orange Juice
 *
 *   PRODUCTS PAGE  (First 14 alphabetically across all products)
 *     Aashirvaad Wheat Flour, Act II Butter Popcorn, Adjustable Phone Stand,
 *     Air Wick Freshmatic, Alphonso Mango, Amul Butter, Amul Dahi,
 *     Amul Dark Chocolate, Amul Full Cream Milk, Amul Mango Lassi,
 *     Amul Paneer, Amul Vanilla Ice Cream, Anker USB-C Cable, Apple (Red)
 */
