// Flat cartoon chess pieces (cream White, navy Black, dark outline) and a white knight
// map icon. Usage:
//   npm install --prefix "$TMP_NODE" @resvg/resvg-js
//   NODE_PATH="$TMP_NODE/node_modules" node tools/render-chess-pieces.js
const fs = require('fs');
const path = require('path');
const { Resvg } = require('@resvg/resvg-js');

const ROOT = path.resolve(__dirname, '..');
const PIECES = path.join(ROOT, 'Assets/_Project/Art/Chess/Pieces');
const ICON = path.join(ROOT, 'Assets/_Project/Resources/Icons/SportIcon_Chess.png');
const INK = '#1c2546';
const FILL = { w: '#fbf4e2', b: '#2b3350' };
const SHINE = { w: '#ffffff', b: '#4a5680' };
const W = 5;

const base = 'M22 88 H78 V81 Q78 74 71 74 H29 Q22 74 22 81 Z';
const shapes = {
  P: ['M37 74 Q38 54 45 46 H55 Q62 54 63 74 Z', '<circle cx="50" cy="32" r="13"/>'],
  R: ['M31 74 L34 42 H66 L69 74 Z', 'M28 42 V22 H37 V30 H45 V22 H55 V30 H63 V22 H72 V42 Z'],
  N: ['M30 74 Q29 55 42 45 L33 41 Q28 31 39 22 L50 13 L55 22 Q73 31 70 54 L68 74 Z'],
  B: ['M38 74 Q40 57 44 50 H56 Q60 57 62 74 Z', 'M50 15 Q67 31 60 47 H40 Q33 31 50 15 Z', '<circle cx="50" cy="12" r="5"/>'],
  Q: ['M34 74 L29 33 L42 51 L50 25 L58 51 L71 33 L66 74 Z',
      '<circle cx="29" cy="31" r="5"/>', '<circle cx="50" cy="22" r="5"/>', '<circle cx="71" cy="31" r="5"/>'],
  K: ['M34 74 Q30 51 40 43 H60 Q70 51 66 74 Z', 'M38 43 H62 V36 H38 Z',
      'M47 9 H53 V17 H61 V23 H53 V36 H47 V23 H39 V17 H47 Z'],
};
const details = {
  N: '<circle cx="50" cy="31" r="3" fill="' + INK + '"/>',
  B: '<path d="M54 25 L46 36" stroke="' + INK + '" stroke-width="4" stroke-linecap="round"/>',
};

function part(d, fill) {
  return d.startsWith('<')
    ? d.replace('/>', ` fill="${fill}" stroke="${INK}" stroke-width="${W}"/>`)
    : `<path d="${d}" fill="${fill}" stroke="${INK}" stroke-width="${W}" stroke-linejoin="round"/>`;
}

function pieceSvg(color, type) {
  const fill = FILL[color];
  const body = [base, ...shapes[type]].map(d => part(d, fill)).join('');
  const shine = `<path d="M31 80 H45" stroke="${SHINE[color]}" stroke-width="3" stroke-linecap="round" opacity=".7"/>`;
  return `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100" width="128" height="128">${body}${shine}${details[type] || ''}</svg>`;
}

function iconSvg() {
  const d = [base, ...shapes.N].map(p => `<path d="${p}" fill="#ffffff"/>`).join('');
  return `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100" width="256" height="256">${d}</svg>`;
}

function write(svg, file, width) {
  const png = new Resvg(svg, { fitTo: { mode: 'width', value: width } }).render().asPng();
  fs.mkdirSync(path.dirname(file), { recursive: true });
  fs.writeFileSync(file, png);
  console.log('wrote', path.relative(ROOT, file));
}

for (const color of ['w', 'b'])
  for (const type of Object.keys(shapes))
    write(pieceSvg(color, type), path.join(PIECES, `${color}${type}.png`), 128);
write(iconSvg(), ICON, 256);
