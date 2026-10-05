// Draws the 15 journey dialogue emoji in the shared flat cartoon style (dark navy outlines,
// few flat colours) and packs them into the TMP sprite atlas with the existing 5 x 3 layout,
// so JourneyEmoji.asset glyph rects stay valid.
// npm i @resvg/resvg-js; run: node tools/render-journey-emoji.js Assets/_Project/Art/Emoji
const fs = require('fs');
const path = require('path');
const { Resvg } = require('@resvg/resvg-js');

const OUT = process.argv[2];
const C = {
  ink: '#1c2546', face: '#f6c632', cream: '#fbf4e2', white: '#ffffff', coral: '#e85a48',
  blue: '#4fb3f0', navy: '#2a3f8f', skin: '#f5c9a0', pink: '#f49a8c', teal: '#2fb5a8', green: '#62b54a',
};
const W = 3.2; // outline width at 72 px

const face = (fill = C.face) => `<circle cx="36" cy="37" r="30" fill="${fill}" stroke="${C.ink}" stroke-width="${W}"/>`;
// Outlined stroke: ink underlay, colour on top.
const line = (d, color, w) => `<path d="${d}" fill="none" stroke="${C.ink}" stroke-width="${w + W * 1.6}" stroke-linecap="round" stroke-linejoin="round"/>` +
  `<path d="${d}" fill="none" stroke="${color}" stroke-width="${w}" stroke-linecap="round" stroke-linejoin="round"/>`;
const shape = (d, fill) => `<path d="${d}" fill="${fill}" stroke="${C.ink}" stroke-width="${W}" stroke-linejoin="round"/>`;
const inkLine = (d, w = W) => `<path d="${d}" fill="none" stroke="${C.ink}" stroke-width="${w}" stroke-linecap="round" stroke-linejoin="round"/>`;

function ballPatches(r) {
  const p = (cx, cy, rr, rot) => 'M' + [0, 1, 2, 3, 4].map(k => { const a = (rot + 72 * k) * Math.PI / 180; return `${(cx + rr * Math.cos(a)).toFixed(2)} ${(cy + rr * Math.sin(a)).toFixed(2)}`; }).join('L') + 'Z';
  let seams = '', patches = '';
  for (let k = 0; k < 5; k++) {
    const a = (-90 + 72 * k) * Math.PI / 180;
    seams += `M${(36 + r * .32 * Math.cos(a)).toFixed(2)} ${(37 + r * .32 * Math.sin(a)).toFixed(2)}L${(36 + r * .8 * Math.cos(a)).toFixed(2)} ${(37 + r * .8 * Math.sin(a)).toFixed(2)}`;
    patches += `<path d="${p(36 + r * 1.06 * Math.cos(a), 37 + r * 1.06 * Math.sin(a), r * .3, -90 + 72 * k + 180)}"/>`;
  }
  return `<clipPath id="ball"><circle cx="36" cy="37" r="${r}"/></clipPath>
<g clip-path="url(#ball)" fill="${C.ink}"><path d="${seams}" stroke="${C.ink}" stroke-width="2.2"/>${patches}<path d="${p(36, 37, r * .32, -90)}"/></g>`;
}

const emoji = {
  sob: () => face() +
    shape('M18 40 Q15 58 20 66 L28 66 Q24 54 25 42Z', C.blue) + shape('M54 40 Q57 58 52 66 L44 66 Q48 54 47 42Z', C.blue) +
    inkLine('M17 32 Q23 26 30 32') + inkLine('M42 32 Q49 26 55 32') +
    shape('M26 46 Q36 40 46 46 Q44 58 36 58 Q28 58 26 46Z', C.ink) + `<path d="M30 54 Q36 50 42 54 Q39 57 36 57 Q33 57 30 54Z" fill="${C.pink}"/>`,

  skull: () => shape('M36 8 C54 8 64 20 64 34 C64 44 58 49 54 51 L54 60 Q54 64 50 64 L22 64 Q18 64 18 60 L18 51 C14 49 8 44 8 34 C8 20 18 8 36 8Z', C.cream) +
    shape('M18 30 Q18 24 26 24 Q32 25 32 32 Q31 39 24 39 Q18 38 18 30Z', C.ink) + shape('M54 30 Q54 24 46 24 Q40 25 40 32 Q41 39 48 39 Q54 38 54 30Z', C.ink) +
    shape('M36 40 L32 47 L40 47Z', C.ink) + inkLine('M27 54 V64 M36 54 V64 M45 54 V64'),

  sunglasses: () => face() +
    shape('M12 30 H60 L59 34 Q57 45 47 45 Q38 45 37 36 L35 36 Q34 45 25 45 Q15 45 13 34Z', C.ink) +
    `<path d="M18 33 L24 33 M43 33 L49 33" stroke="${C.blue}" stroke-width="2.6" stroke-linecap="round"/>` +
    inkLine('M26 53 Q36 59 48 50'),

  fire: () => shape('M36 66 C18 66 10 54 12 42 C14 32 22 28 22 18 C30 22 32 30 32 34 C34 26 38 16 36 6 C48 12 58 26 60 40 C62 56 52 66 36 66Z', C.coral) +
    shape('M36 66 C26 66 21 59 22 51 C23 45 28 42 29 36 C34 40 35 45 35 48 C38 44 40 39 40 33 C47 39 51 46 50 54 C49 62 44 66 36 66Z', C.face),

  scream: () => `<clipPath id="f"><circle cx="36" cy="37" r="30"/></clipPath>` + face() +
    `<g clip-path="url(#f)"><rect x="0" y="0" width="72" height="26" fill="${C.blue}"/></g><circle cx="36" cy="37" r="30" fill="none" stroke="${C.ink}" stroke-width="${W}"/>` +
    shape('M18 30 a7 8 0 1 0 14 0 a7 8 0 1 0 -14 0Z', C.white) + shape('M40 30 a7 8 0 1 0 14 0 a7 8 0 1 0 -14 0Z', C.white) +
    `<circle cx="25" cy="31" r="2.4" fill="${C.ink}"/><circle cx="47" cy="31" r="2.4" fill="${C.ink}"/>` +
    shape('M30 46 Q36 40 42 46 L42 56 Q36 64 30 56Z', C.ink) +
    shape('M4 40 Q4 52 12 60 Q18 62 18 54 Q14 46 14 38 Q9 34 4 40Z', C.face) + shape('M68 40 Q68 52 60 60 Q54 62 54 54 Q58 46 58 38 Q63 34 68 40Z', C.face),

  runner: () =>
    line('M33 42 L24 52 L12 51', C.navy, 7) + line('M33 42 L45 50 L41 63', C.navy, 7) +
    shape('M7 47 L15 47 L15 54 L5 54Z', C.white) + shape('M36 61 L46 61 L47 67 L35 67Z', C.white) +
    line('M36 27 L26 31 L21 39', C.skin, 5) +
    line('M40 24 L33 42', C.white, 12) +
    line('M41 27 L51 33 L58 26', C.skin, 5) +
    `<circle cx="44" cy="13" r="8.5" fill="${C.skin}" stroke="${C.ink}" stroke-width="${W}"/>` +
    shape('M35 13 Q35 3 45 4 Q54 4 53 12 L49 10 L46 13 L43 9 L39 13Z', C.ink),

  dash: () => line('M6 26 H30 M10 38 H26 M8 50 H28', '#bfe3f7', 4) +
    `<g>${[[34, 38, 13], [50, 30, 12], [58, 44, 11], [44, 48, 10]].map(([x, y, r]) => `<circle cx="${x}" cy="${y}" r="${r}" fill="${C.ink}" stroke="${C.ink}" stroke-width="${W * 2}"/>`).join('')}` +
    `${[[34, 38, 13], [50, 30, 12], [58, 44, 11], [44, 48, 10]].map(([x, y, r]) => `<circle cx="${x}" cy="${y}" r="${r}" fill="${C.cream}"/>`).join('')}</g>`,

  soccer: () => `<circle cx="36" cy="37" r="29" fill="${C.white}"/>` + ballPatches(29) + `<circle cx="36" cy="37" r="29" fill="none" stroke="${C.ink}" stroke-width="${W}"/>`,

  volleyball: () => `<clipPath id="v"><circle cx="36" cy="37" r="29"/></clipPath><circle cx="36" cy="37" r="29" fill="${C.white}"/>` +
    `<g clip-path="url(#v)">${shape('M36 37 C36 22 46 12 60 10 L72 24 C58 22 46 28 36 37Z', C.face)}${shape('M36 37 C24 30 12 32 4 42 L6 60 C12 48 24 40 36 37Z', C.navy)}${shape('M36 37 C38 50 34 62 26 70 L50 70 C50 56 44 44 36 37Z', C.face)}</g>` +
    `<circle cx="36" cy="37" r="29" fill="none" stroke="${C.ink}" stroke-width="${W}"/>`,

  eyes: () => shape('M6 36 C6 16 32 16 32 36 C32 56 6 56 6 36Z', C.white) + shape('M40 36 C40 16 66 16 66 36 C66 56 40 56 40 36Z', C.white) +
    `<circle cx="14" cy="38" r="7" fill="${C.ink}"/><circle cx="48" cy="38" r="7" fill="${C.ink}"/>` +
    `<circle cx="12" cy="35" r="2" fill="${C.white}"/><circle cx="46" cy="35" r="2" fill="${C.white}"/>`,

  clown: () => `${[[9, 24, 8], [7, 36, 7], [63, 24, 8], [65, 36, 7]].map(([x, y, r]) => `<circle cx="${x}" cy="${y}" r="${r}" fill="${C.blue}" stroke="${C.ink}" stroke-width="${W}"/>`).join('')}` +
    face(C.cream) +
    `<path d="M20 28 L30 28 M42 28 L52 28" stroke="${C.ink}" stroke-width="${W}" stroke-linecap="round"/>` +
    `<circle cx="25" cy="34" r="3" fill="${C.ink}"/><circle cx="47" cy="34" r="3" fill="${C.ink}"/>` +
    shape('M20 47 Q36 64 52 47 Q36 54 20 47Z', C.coral) +
    `<circle cx="36" cy="41" r="6" fill="${C.coral}" stroke="${C.ink}" stroke-width="${W}"/>`,

  salute: () => face() +
    `<circle cx="28" cy="36" r="3" fill="${C.ink}"/><circle cx="46" cy="36" r="3" fill="${C.ink}"/>` +
    inkLine('M28 50 H44') + inkLine('M40 28 L52 30') +
    shape('M4 26 L26 10 Q30 8 32 12 L34 16 Q35 20 31 22 L14 34 Q8 36 6 32Z', C.face) +
    inkLine('M24 14 L28 20 M18 18 L22 24', 2.2),

  100: () => line('M14 14 L8 44', C.coral, 6) +
    `<ellipse cx="29" cy="29" rx="8" ry="14" transform="rotate(12 29 29)" fill="none" stroke="${C.ink}" stroke-width="${6 + W * 1.6}"/>` +
    `<ellipse cx="54" cy="29" rx="8" ry="14" transform="rotate(12 54 29)" fill="none" stroke="${C.ink}" stroke-width="${6 + W * 1.6}"/>` +
    `<ellipse cx="29" cy="29" rx="8" ry="14" transform="rotate(12 29 29)" fill="none" stroke="${C.coral}" stroke-width="6"/>` +
    `<ellipse cx="54" cy="29" rx="8" ry="14" transform="rotate(12 54 29)" fill="none" stroke="${C.coral}" stroke-width="6"/>` +
    line('M8 56 Q36 50 64 52', C.coral, 5) + line('M14 65 Q38 60 60 62', C.coral, 4),

  tada: () => shape('M8 66 L22 26 L48 52Z', C.face) +
    `<path d="M14 49 L31 35 M11 58 L39 44" stroke="${C.coral}" stroke-width="4"/>` + shape('M8 66 L22 26 L48 52Z', 'none') +
    line('M28 22 Q32 10 42 12', C.blue, 3) + line('M46 30 Q58 22 64 30', C.coral, 3) + line('M50 46 Q60 46 64 54', C.green, 3) +
    `${[[36, 6, C.coral], [56, 12, C.face], [66, 40, C.blue], [26, 12, C.green]].map(([x, y, c]) => `<rect x="${x - 3}" y="${y - 3}" width="6" height="6" fill="${c}" stroke="${C.ink}" stroke-width="2" transform="rotate(20 ${x} ${y})"/>`).join('')}`,

  muscle: () => shape('M10 64 L14 44 Q16 36 24 34 L34 32 Q30 22 34 12 Q38 6 46 8 L52 10 Q56 14 52 18 L46 18 Q44 24 46 30 Q60 30 62 44 Q62 58 46 62 Q30 66 10 64Z', C.face) +
    inkLine('M34 32 Q40 40 50 40', 2.4) + inkLine('M46 18 Q42 16 40 20', 2.2),
};

const order = ['sob', 'skull', 'sunglasses', 'fire', 'scream', 'runner', 'dash', 'soccer', 'volleyball', 'eyes', 'clown', 'salute', '100', 'tada', 'muscle'];
const wrap = (body, size) => `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 72 72" width="${size}" height="${size}">${body}</svg>`;
const render = (svg, width) => new Resvg(svg, { fitTo: { mode: 'width', value: width } }).render().asPng();

fs.mkdirSync(path.join(OUT, 'Source~'), { recursive: true });
let atlas = '<svg xmlns="http://www.w3.org/2000/svg" width="360" height="216">';
order.forEach((name, i) => {
  const svg = wrap(emoji[name](), 72);
  fs.writeFileSync(path.join(OUT, 'Source~', name + '.png'), render(svg, 72));
  atlas += `<svg x="${(i % 5) * 72}" y="${Math.floor(i / 5) * 72}" width="72" height="72" viewBox="0 0 72 72">${emoji[name]()}</svg>`;
});
fs.writeFileSync(path.join(OUT, 'JourneyEmojiAtlas.png'), render(atlas + '</svg>', 360));
if (process.argv[3]) fs.writeFileSync(process.argv[3], render(atlas + '</svg>', 1440));
console.log('rendered', order.length, 'emoji');
