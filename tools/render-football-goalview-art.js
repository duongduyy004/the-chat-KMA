// Redraws the Football GoalView layers in the "Chạy trốn thể chất" flat cartoon style:
// dark navy outlines, few flat colours, no gradients. Geometry (goal mouth, field lines,
// penalty spot, viewBoxes) is kept identical to the previous layers so gameplay alignment holds.
const fs = require('fs');
const path = require('path');
const { Resvg } = require('@resvg/resvg-js');
const { PNG } = require('pngjs');
// Run: TMP_NODE="$TEMP/kma-resvg"; npm install --prefix "$TMP_NODE" @resvg/resvg-js pngjs
//      NODE_PATH="$TMP_NODE/node_modules" node tools/render-football-goalview-art.js Assets/_Project/Art/Football/GoalView

const OUT = process.argv[2];
const C = {
  ink: '#1c2546', sky: '#2e9be6', cloud: '#fbf4e2', tree: '#5cb346', treeDark: '#3f8f3a',
  seatBlue: '#2f6fd1', seatYellow: '#f4c531', stand: '#efe6cf', wall: '#fbf4e2', coral: '#e85a48',
  grass: '#62b54a', grassLight: '#73c257', line: '#fffbea', white: '#ffffff', yellow: '#f6c632',
};

// Outlined union of circles: all strokes first, then all fills on top.
const blob = (circles, fill, w = 4) =>
  circles.map(([x, y, r]) => `<circle cx="${x}" cy="${y}" r="${r}" fill="${C.ink}" stroke="${C.ink}" stroke-width="${w * 2}"/>`).join('') +
  circles.map(([x, y, r]) => `<circle cx="${x}" cy="${y}" r="${r}" fill="${fill}"/>`).join('');

const cloud = (x, y, s) => blob([[x, y, 20 * s], [x + 26 * s, y - 12 * s, 26 * s], [x + 56 * s, y - 4 * s, 22 * s],
  [x + 78 * s, y + 8 * s, 15 * s], [x - 20 * s, y + 8 * s, 14 * s], [x + 4 * s, y + 14 * s, 16 * s], [x + 32 * s, y + 12 * s, 18 * s], [x + 58 * s, y + 14 * s, 15 * s]], C.cloud, 3);

// Shared campus skyline (cropped from the Sprint campus art), full width, hedge row tucked behind the stands.
function trees() {
  const png = fs.readFileSync(path.join(__dirname, '..', 'Assets/_Project/Art/Environments/Campus/CampusSkyline.png'));
  const { width, height } = PNG.sync.read(png);
  const w = 1200, h = Math.round(w * height / width), base = 176;
  return `<image x="0" y="${base - h}" width="${w}" height="${h}" href="data:image/png;base64,${png.toString('base64')}"/>`;
}

function bleachers() {
  const shape = 'M0 143Q600 205 1200 143V267H0Z';
  let rows = '';
  for (let i = 0; i < 9; i++) {
    const y = 140 + i * 14;
    rows += `<rect x="0" y="${y}" width="1200" height="9" fill="${i % 3 === 1 ? C.seatYellow : C.seatBlue}"/>`;
    rows += `<rect x="0" y="${y + 9}" width="1200" height="5" fill="${C.stand}"/>`;
  }
  let aisles = '';
  for (let x = 100; x < 1200; x += 200) aisles += `<rect x="${x}" y="130" width="14" height="140" fill="${C.stand}" stroke="${C.ink}" stroke-width="2"/>`;
  return `<clipPath id="stand"><path d="${shape}"/></clipPath>
<g clip-path="url(#stand)">${rows}${aisles}</g>
<path d="${shape}" fill="none" stroke="${C.ink}" stroke-width="5" stroke-linejoin="round"/>`;
}

function field() {
  let mow = '';
  const bands = [[273, 297], [314, 341], [378, 421], [477, 546], [636, 675]];
  for (const [a, b] of bands) mow += `<rect x="0" y="${a}" width="1200" height="${b - a}" fill="${C.grassLight}"/>`;
  return `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1200 675" width="1200" height="675">
<rect width="1200" height="320" fill="${C.sky}"/>
${cloud(110, 60, 1.1)}${cloud(470, 42, 0.8)}${cloud(860, 70, 1.2)}${cloud(1110, 34, 0.7)}
${trees()}
${bleachers()}
<rect x="-5" y="230" width="1210" height="45" fill="${C.wall}" stroke="${C.ink}" stroke-width="5"/>
<rect x="0" y="246" width="1200" height="10" fill="${C.coral}"/>
<path d="M0 246H1200M0 256H1200" stroke="${C.ink}" stroke-width="2"/>
<rect x="0" y="273" width="1200" height="402" fill="${C.grass}"/>
${mow}
<path d="M0 273H1200" stroke="${C.ink}" stroke-width="5"/>
<g fill="none" stroke="${C.line}" stroke-width="4" stroke-linejoin="round"><path d="M0 297H1200M255 297L80 538H1120L945 297M420 297L373 365H827L780 297"/><path d="M350 538Q600 660 850 538"/></g>
<ellipse cx="600" cy="488" rx="8" ry="3.5" fill="${C.line}"/>
</svg>`;
}

const goalBox = 'viewBox="290 145 620 170" width="620" height="170"';
const goal = () => `<svg xmlns="http://www.w3.org/2000/svg" ${goalBox}>
<path d="M324 295L355 176H845L876 295Z" fill="${C.ink}" opacity=".32"/>
<path d="M300 302V153H900V302" fill="none" stroke="${C.ink}" stroke-width="16" stroke-linejoin="round"/>
<path d="M300 302V153H900V302" fill="none" stroke="${C.white}" stroke-width="9" stroke-linejoin="round"/>
<path d="M300 302H900" stroke="${C.line}" stroke-width="4"/>
</svg>`;

const net = () => `<svg xmlns="http://www.w3.org/2000/svg" ${goalBox}>
<defs><pattern id="net" width="22" height="20" patternUnits="userSpaceOnUse"><path d="M22 0H0V20" fill="none" stroke="${C.white}" stroke-opacity=".6" stroke-width="1.6"/></pattern></defs>
<path d="M324 295L355 176H845L876 295Z" fill="url(#net)"/>
<path d="M300 302V153H900V302L876 295L845 176H355L324 295Z" fill="url(#net)" stroke="${C.ink}" stroke-opacity=".55" stroke-width="2.5" stroke-linejoin="round"/>
</svg>`;

function ball() {
  const pt = (r, deg) => { const a = deg * Math.PI / 180; return `${(r * Math.cos(a)).toFixed(2)} ${(r * Math.sin(a)).toFixed(2)}`; };
  const penta = (cx, cy, r, rot) => 'M' + [0, 1, 2, 3, 4].map(k => { const a = (rot + 72 * k) * Math.PI / 180; return `${(cx + r * Math.cos(a)).toFixed(2)} ${(cy + r * Math.sin(a)).toFixed(2)}`; }).join('L') + 'Z';
  let seams = '', patches = '';
  for (let k = 0; k < 5; k++) {
    const a = -90 + 72 * k;
    seams += `M${pt(5, a)}L${pt(13, a)}`;
    const b = a * Math.PI / 180;
    patches += `<path d="${penta(16.8 * Math.cos(b), 16.8 * Math.sin(b), 4.6, a + 180)}"/>`;
  }
  return `<svg xmlns="http://www.w3.org/2000/svg" viewBox="-18 -18 36 36" width="36" height="36">
<clipPath id="b"><circle r="15.5"/></clipPath>
<circle r="15.5" fill="${C.white}"/>
<g clip-path="url(#b)" fill="${C.ink}"><path d="${seams}" stroke="${C.ink}" stroke-width="1.1" fill="none"/>${patches}<path d="${penta(0, 0, 5, -90)}"/></g>
<circle r="15.5" fill="none" stroke="${C.ink}" stroke-width="2.2"/>
</svg>`;
}

const crosshair = () => {
  const g = (stroke, w, dot) => `<g stroke="${stroke}" fill="none" stroke-width="${w}" stroke-linecap="round"><circle r="18"/><circle r="${dot}" fill="${stroke}"/><path d="M-27 0H-12M12 0H27M0 -27V-12M0 12V27"/></g>`;
  return `<svg xmlns="http://www.w3.org/2000/svg" viewBox="-30 -30 60 60" width="60" height="60">${g(C.ink, 7, 6.5)}${g(C.yellow, 3.2, 4.5)}</svg>`;
};

const shadow = () => `<svg xmlns="http://www.w3.org/2000/svg" viewBox="576 494 48 16" width="48" height="16">
<ellipse cx="600" cy="502" rx="21" ry="7" fill="${C.ink}" opacity=".35"/></svg>`;

const layers = { field, goal, net, ball, crosshair, shadow };
for (const [name, make] of Object.entries(layers)) {
  const svg = make();
  const w = Number(/width="(\d+)"/.exec(svg)[1]);
  fs.writeFileSync(path.join(OUT, name + '.svg'), svg);
  const png = new Resvg(svg, { fitTo: { mode: 'width', value: w * 2 } }).render().asPng();
  fs.writeFileSync(path.join(OUT, name + '.png'), png);
  console.log(name, w * 2);
}
