// Draws the shared campus backdrop layers in the "Chạy trốn thể chất" flat cartoon style:
// ink outlines, flat colours, no gradients. Palette shared with render-football-goalview-art.js.
// Run: TMP_NODE="$TEMP/kma-resvg"; npm install --prefix "$TMP_NODE" @resvg/resvg-js
//      NODE_PATH="$TMP_NODE/node_modules" node tools/render-campus-art.js Assets/_Project/Art/Environments/Campus
const fs = require('fs');
const path = require('path');
const { Resvg } = require('@resvg/resvg-js');

const OUT = process.argv[2];
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
    cloud(110, 200, 1.1) + cloud(420, 250, .8) + cloud(640, 175, 1.2) + cloud(880, 270, .7));
}

function tree(x, base, r, dark) {
  return `<rect x="${x - 5}" y="${base - r * .9}" width="10" height="${r * .9}" fill="#8a5a36" stroke="${C.ink}" stroke-width="3"/>` +
    blob([[x, base - r * 1.2, r], [x - r * .6, base - r * .8, r * .7], [x + r * .6, base - r * .8, r * .7]],
      dark ? C.treeDark : C.tree, 3);
}

function stand(x, base, w) {
  let rows = '';
  for (let i = 0; i < 4; i++)
    rows += `<rect x="${x + 6}" y="${base - 18 - i * 14}" width="${w - 12}" height="9" fill="${i % 2 ? C.seatYellow : C.seatBlue}"/>`;
  const light = `<rect x="${x + w - 16}" y="${base - 150}" width="8" height="90" fill="${C.ink}"/>` +
    rect(x + w - 34, base - 172, 44, 26, C.seatYellow, 3);
  return rect(x, base - 76, w, 76, C.stand) + rows +
    `<path d="M${x - 6} ${base - 76}H${x + w + 6}L${x + w - 10} ${base - 92}H${x + 10}Z" fill="${C.treeDark}" stroke="${C.ink}" stroke-width="4" stroke-linejoin="round"/>` + light;
}

function mainBuilding(x, base) {
  const w = 520, h = 175;
  let windows = '';
  for (let row = 0; row < 5; row++)
    for (let col = 0; col < 8; col++) {
      if (col === 3 || col === 4) continue; // atrium column
      windows += rect(x + 24 + col * 60, base - h + 14 + row * 31, 40, 20, C.glass, 3);
    }
  const atrium = rect(x + 200, base - h - 24, 120, h + 24, C.glass) +
    `<path d="M${x + 230} ${base - h - 24}V${base}M${x + 260} ${base - h - 24}V${base}M${x + 290} ${base - h - 24}V${base}" stroke="${C.ink}" stroke-width="3"/>`;
  const frame = rect(x - 8, base - h - 8, w + 16, 16, C.brick) + rect(x + 190, base - h - 38, 140, 14, C.brick);
  const door = rect(x + 232, base - 44, 56, 44, C.ink, 2);
  const flag = `<rect x="${x + 258}" y="${base - h - 100}" width="5" height="62" fill="${C.ink}"/>` +
    `<path d="M${x + 263} ${base - h - 98}L${x + 305} ${base - h - 87}L${x + 263} ${base - h - 76}Z" fill="${C.yellow}" stroke="${C.ink}" stroke-width="3" stroke-linejoin="round"/>`;
  return rect(x, base - h, w, h, C.wall) + windows + atrium + frame + door + flag;
}

function hall(x, base) {
  const w = 300, h = 110;
  let windows = '';
  for (let i = 0; i < 5; i++) windows += rect(x + 24 + i * 54, base - 70, 38, 26, C.glass, 3);
  return rect(x, base - h, w, h, C.wall) +
    `<path d="M${x - 8} ${base - h}Q${x + w / 2} ${base - h - 70} ${x + w + 8} ${base - h}Z" fill="${C.brick}" stroke="${C.ink}" stroke-width="4" stroke-linejoin="round"/>` +
    windows;
}

function skyline() {
  const W = 2048, H = 320, base = H - 24;
  let body = '';
  for (const [x, r, d] of [[40, 34, 0], [100, 44, 1], [170, 30, 0]]) body += tree(x, base, r, d);
  body += stand(210, base, 260);
  for (const [x, r, d] of [[520, 40, 1], [580, 30, 0]]) body += tree(x, base, r, d);
  body += mainBuilding(640, base);
  for (const [x, r, d] of [[1200, 36, 0], [1260, 46, 1]]) body += tree(x, base, r, d);
  body += hall(1310, base);
  for (const [x, r, d] of [[1650, 30, 0]]) body += tree(x, base, r, d);
  body += stand(1690, base, 260);
  for (const [x, r, d] of [[1990, 40, 1]]) body += tree(x, base, r, d);
  const hedge = `<rect x="-4" y="${base}" width="${W + 8}" height="28" fill="${C.grass}" stroke="${C.ink}" stroke-width="4"/>`;
  return svg(W, H, body + hedge);
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

const layers = { CampusSky: sky, CampusSkyline: skyline, CampusPixel: pixel, SprintTrack: sprintTrack, VolleyNet: volleyNet };
fs.mkdirSync(OUT, { recursive: true });
for (const [name, make] of Object.entries(layers)) {
  const image = make();
  const width = Number(/width="(\d+)"/.exec(image)[1]);
  fs.writeFileSync(path.join(OUT, name + '.svg'), image);
  const png = new Resvg(image, { fitTo: { mode: 'width', value: width } }).render().asPng();
  fs.writeFileSync(path.join(OUT, name + '.png'), png);
  console.log(name, width);
}
