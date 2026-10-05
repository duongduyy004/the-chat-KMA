// Draws the shared campus backdrop layers in the "Chạy trốn thể chất" flat cartoon style:
// ink outlines, flat colours, no gradients. Palette shared with render-football-goalview-art.js.
// CampusSkyline is not drawn: it is cropped (pixels unchanged) from the Sprint campus illustration
// Assets/_Project/Art/Environments/Sprint/Campus.png to its opaque bounding box (alpha > 8; the source has alpha 1 noise).
// Run: TMP_NODE="$TEMP/kma-resvg"; npm install --prefix "$TMP_NODE" @resvg/resvg-js pngjs
//      NODE_PATH="$TMP_NODE/node_modules" node tools/render-campus-art.js Assets/_Project/Art/Environments/Campus
const fs = require('fs');
const path = require('path');
const { Resvg } = require('@resvg/resvg-js');
const { PNG } = require('pngjs');

const OUT = process.argv[2];
if (!OUT) {
  console.error('Usage: node tools/render-campus-art.js <output-dir>   (e.g. Assets/_Project/Art/Environments/Campus)');
  process.exit(2);
}
const C = {
  ink: '#1c2546', sky: '#2e9be6', cloud: '#fbf4e2', tree: '#5cb346', treeDark: '#3f8f3a',
  seatBlue: '#2f6fd1', seatYellow: '#f4c531', stand: '#efe6cf', wall: '#fbf4e2', coral: '#e85a48',
  grass: '#62b54a', line: '#fffbea', white: '#ffffff', yellow: '#f6c632',
  brick: '#d9614c', glass: '#5fb8e8', track: '#e0604a',
};

const blob = (circles, fill, w = 4) =>
  circles.map(([x, y, r]) => `<circle cx="${x}" cy="${y}" r="${r}" fill="${C.ink}" stroke="${C.ink}" stroke-width="${w * 2}"/>`).join('') +
  circles.map(([x, y, r]) => `<circle cx="${x}" cy="${y}" r="${r}" fill="${fill}"/>`).join('');
const cloud = (x, y, s) => blob([[x, y, 20 * s], [x + 26 * s, y - 12 * s, 26 * s], [x + 56 * s, y - 4 * s, 22 * s],
  [x + 78 * s, y + 8 * s, 15 * s], [x - 20 * s, y + 8 * s, 14 * s], [x + 4 * s, y + 14 * s, 16 * s],
  [x + 32 * s, y + 12 * s, 18 * s], [x + 58 * s, y + 14 * s, 15 * s]], C.cloud, 3);
const rect = (x, y, w, h, fill, sw = 4) =>
  `<rect x="${x}" y="${y}" width="${w}" height="${h}" fill="${fill}" stroke="${C.ink}" stroke-width="${sw}" stroke-linejoin="round"/>`;
const svg = (w, h, body) =>
  `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 ${w} ${h}" width="${w}" height="${h}">${body}</svg>`;

function sky() {
  // Clouds sit between 30 % and 60 % of the height; none crosses the left/right edge so it tiles.
  return svg(1024, 512, `<rect width="1024" height="512" fill="${C.sky}"/>` +
    cloud(110, 200, 1.1) + cloud(420, 250, .8) + cloud(640, 190, 1.2) + cloud(880, 270, .7));
}

const pixel = () => svg(8, 8, `<rect width="8" height="8" fill="${C.white}"/>`);

function sprintTrack() {
  const W = 1983, H = 875, top = 460;
  let lines = '';
  for (const row of [471.5, 553.5, 639, 722, 798])
    lines += `<rect x="-4" y="${row - 6}" width="${W + 8}" height="12" fill="${C.line}" stroke="${C.ink}" stroke-width="2"/>`;
  return svg(W, H, `<rect x="0" y="${top}" width="${W}" height="${H - top}" fill="${C.track}"/>` + lines);
}

function volleyNet() {
  const W = 64, H = 512;
  const mesh = `<defs><pattern id="m" width="10" height="10" patternUnits="userSpaceOnUse"><path d="M10 0H0V10" fill="none" stroke="${C.white}" stroke-opacity=".7" stroke-width="1.6"/></pattern></defs>` +
    `<rect x="12" y="40" width="40" height="${H - 80}" fill="url(#m)" stroke="${C.ink}" stroke-opacity=".6" stroke-width="2"/>`;
  const post = (y) => rect(22, y, 20, 40, C.ink, 2);
  return svg(W, H, mesh + post(0) + post(H - 40));
}

const layers = { CampusSky: sky, CampusPixel: pixel, SprintTrack: sprintTrack, VolleyNet: volleyNet };
fs.mkdirSync(OUT, { recursive: true });
for (const [name, make] of Object.entries(layers)) {
  const image = make();
  const width = Number(/width="(\d+)"/.exec(image)[1]);
  fs.writeFileSync(path.join(OUT, name + '.svg'), image);
  const png = new Resvg(image, { fitTo: { mode: 'width', value: width } }).render().asPng();
  fs.writeFileSync(path.join(OUT, name + '.png'), png);
  console.log(name, width);
}

// Skyline: crop the Sprint campus illustration to its opaque bounding box (alpha > 8; the source has alpha 1 noise).
{
  const src = PNG.sync.read(fs.readFileSync(path.join(__dirname, '..', 'Assets/_Project/Art/Environments/Sprint/Campus.png')));
  let x0 = src.width, y0 = src.height, x1 = -1, y1 = -1;
  for (let y = 0; y < src.height; y++)
    for (let x = 0; x < src.width; x++)
      if (src.data[(y * src.width + x) * 4 + 3] > 8) { x0 = Math.min(x0, x); x1 = Math.max(x1, x); y0 = Math.min(y0, y); y1 = Math.max(y1, y); }
  const w = x1 - x0 + 1, h = y1 - y0 + 1;
  const out = new PNG({ width: w, height: h });
  PNG.bitblt(src, out, x0, y0, w, h, 0, 0);
  fs.writeFileSync(path.join(OUT, 'CampusSkyline.png'), PNG.sync.write(out));
  console.log(`CampusSkyline ${w}x${h} (crop x ${x0}-${x1}, y ${y0}-${y1} of ${src.width}x${src.height})`);
}
