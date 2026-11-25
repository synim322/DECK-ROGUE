import fs from 'node:fs';
import path from 'node:path';
import { createHash } from 'node:crypto';
import { fileURLToPath } from 'node:url';

// Eight bars per record. Bass and hook rows are sixteenth-note scores:
// digits are A natural minor scale degrees (0 = tonic), dots are rests.
const scores = [
  // record, voice, kick steps, bass line, hook, phrase roots, shuffle
  ['warehouse_line','steel','048c','..0..0.4..0..2..','0..4..7...2..1..','00030040',.05],
  ['smoke_column','dub','048c','..0...0...2...0.','..0.......3.....','00050030',.10],
  ['press_line','motor','048bc','..00..4..0...24.','0.....2..0....4.','00000320',.03],
  ['torque_drum','tom','048c','..0..4...0.2..4.','0..2.4..2..4.7..','00020040',.13],
  ['voltage_alley','acid','048c','..0.0..4..2..04.','024.7.4.20.42.17','00030040',.07],
  ['mutabor_pulse','bell','048c','..0..0..0..4..2.','0..7.4..2.4..1..','00003040',.04],
  ['foundry_drop','anvil','048c','..0..0...0..4.0.','0.......0..4..7.','00000200',.02],
  ['afterglow_key','keys','048c','..0...4...2..4..','..0....4..2....1','03540354',.14],
  ['signal_carrier','radio','038ac','0..4..0...2...4.','7.4...1..7..2...','00030040',.04],
  ['signal_breach','alarm','0468bc','..0..00...0..40.','0.1.4...7.4.1.0.','00000010',.02],
  ['carbon_copy','wire','036ac','0..0..4..2....4.','7..4..2...0..4..','00040030',.09],
  ['cold_cathode','glass','048c','..0...4..0...2..','..4...7....1..2.','03350040',.06],
  ['night_tram','keys','048c','..0..24...0..40.','0....4..2....7..','00530043',.16],
  ['freight_lift','anvil','048c','..00..2...0.4..2','0..0..4....2.1..','00020050',.08],
  ['return_duct','dub','048c','..0....4..0..2..','...0......4...2.','00050043',.12],
  ['ratchet_works','tom','0368bc','0..2..0..4..02..','0.2..4.2..7.4..1','00040020',.11],
  ['phase_fault','wire','038ad','0...2..0..4..02.','7..2..4..1..0.4.','00010040',.03],
  ['mirror_shunt','prism','0368ac','0..4..0..2..4..0','047.2.7.4.1.7.42','03540054',.09],
];
const dir = fileURLToPath(new URL('../../DJ/Assets/_Game/Audio/Resources/TestLoops/', import.meta.url));
const rate = 48000, frames = rate * 16, step = rate / 8, tau = Math.PI * 2;
const scale = [0,2,3,5,7,8,10];
const hz = (degree, base) => base * 2 ** (Math.floor(degree/7) + scale[degree%7]/12);
const sin = Math.sin, exp = Math.exp;
const meta = fs.readFileSync(path.join(dir,'neon_pulse_low.wav.meta'),'utf8');
const masters = fileURLToPath(new URL('./Mixes/',import.meta.url));
fs.mkdirSync(masters,{recursive:true});
const editorShelf = fileURLToPath(new URL('../../DJ/Assets/_Game/Editor/Records/',import.meta.url));
const rewardIds = new Set(['voltage_alley','mutabor_pulse','foundry_drop','afterglow_key','mirror_shunt','signal_carrier']);
const preview = [];
function wav(samples) {
  const b = Buffer.alloc(44 + samples.length*2);
  b.write('RIFF'); b.writeUInt32LE(b.length-8,4); b.write('WAVEfmt ',8);
  b.writeUInt32LE(16,16); b.writeUInt16LE(1,20); b.writeUInt16LE(1,22);
  b.writeUInt32LE(rate,24); b.writeUInt32LE(rate*2,28); b.writeUInt16LE(2,32);
  b.writeUInt16LE(16,34); b.write('data',36); b.writeUInt32LE(samples.length*2,40);
  for(let i=0;i<samples.length;i++) b.writeInt16LE(Math.round(32767*samples[i]),44+i*2);
  return b;
}
function render([id,voice,kicks,bass,hook,roots,swing],index) {
  if(bass.length!==16 || hook.length!==16 || roots.length!==8) throw Error(id);
  const [low,perc,music] = Array.from({length:3},()=>new Float32Array(frames));
  let seed = 12577 + index;
  function noise() { seed=(Math.imul(seed,1664525)+1013904223)>>>0; return seed/2147483648-1; }
  function hit(stem, at, duration, amp, tone) {
    const n=Math.ceil(duration*rate), start=Math.round(at);
    for(let i=0;i<n;i++) {
      const env=Math.min(i/40,1,(n-i)/160);
      stem[(start+i)%frames] += amp*env*tone(i/rate);
    }
  }
  const metal=['steel','anvil','bell','glass','radio','prism'].includes(voice);
  const chord=['keys','dub'].includes(voice);
  for(let bar=0;bar<8;bar++) for(let s=0;s<16;s++) {
    const at=(16*bar+s)*step, groove=at+(s%2)*swing*step, root=Number(roots[bar]);
    const pickup=bar%4===3 && s>=14, stripped=bar===6 && s<8;
    if(kicks.includes(s.toString(16)) && !(bar===7 && s===12)) {
      const f=45+(index%6)*2;
      hit(low,at,.35,1,t=>Math.tanh(1.65*sin(tau*(f*t+2.8*(1-exp(-t*55)))))*exp(-t*(11+index%4)));
    }
    if(bass[s]!=='.' && !stripped) {
      const f=hz(root+Number(bass[s]),55);
      hit(low,groove,.22,.64,t=>exp(-t*16)*(sin(tau*f*t)+.22*sin(2*tau*f*t)+.09*sin(3*tau*f*t)));
    }
    if(s===4 || s===12 || (pickup && s===15)) {
      let last=0;
      hit(perc,groove,.20,pickup?.44:.78,t=>{
        const n=noise(), hp=n-last; last=n;
        return .52*hp*(exp(-t*25)+.6*exp(-Math.abs(t-.016)*230))+.56*sin(tau*(178*t+.7*(1-exp(-40*t))))*exp(-35*t);
      });
    }
    if((s%2===0 || (s+index)%5===0 || pickup) && !(stripped && s%4!==2)) {
      let last=0; const open=s%4===2;
      hit(perc,groove,open?.16:.055,open?.37:.17,t=>{
        const n=noise(), h=n-last; last=n;
        return (h+.22*sin(32641*t)*sin(21789*t))*exp(-t*(open?26:85));
      });
    }
    if((s*3+index)%8===0 || (pickup && s===14)) {
      hit(perc,groove,.14,.32,t=>sin(tau*(350+17*(index%8))*t+2.7*sin(2013*t)*exp(-40*t))*exp(-34*t));
    }
    if(hook[s]==='.' || (stripped && s%4===1)) continue;
    const degree=root+Number(hook[s])+(pickup && bar===7?7:0);
    const f=hz(degree,voice==='motor'||voice==='tom'?110:220);
    const decay=chord?4.4:voice==='acid'?14:metal?8:11;
    const amp=.7+.3*((s+bar+index)%3===0);
    hit(music,groove,chord?.9:.55,amp,t=>{
      const p=tau*f*t, env=exp(-decay*t), mod=exp(-17*t);
      if(chord) return env*(1-exp(-t*(voice==='keys'?140:33)))*(
        sin(p)+.67*sin(tau*hz(degree+2,220)*t)+.45*sin(tau*hz(degree+4,220)*t)+.16*sin(2.003*p));
      if(voice==='acid') return env*(sin(p)+.55*mod*sin(2*p)+.4*sin(3*p+3*mod)+.28*mod*sin(5*p));
      if(metal) {
        const ratio={steel:1.43,anvil:.72,bell:2.71,glass:3.02,radio:1.007,prism:1.51}[voice];
        return env*(sin(p+(1.7+index%4)*mod*sin(ratio*p))+.23*sin(2.007*p));
      }
      if(voice==='alarm') return env*sin(p+4*sin(31*t)+1.6*mod*sin(2*p));
      if(voice==='tom') return env*sin(p+3*(1-exp(-29*t)));
      return env*(sin(p+mod*2.6*sin(p*.5))+.32*sin(p*2.01)+.15*mod*sin(4*p));
    });
  }
  // Circular tempo delay keeps the last phrase's repeats alive at the loop point.
  const dry=music.slice();
  for(let i=0;i<frames;i++) music[i]+= .28*dry[(i+frames-3*step)%frames]+.12*dry[(i+frames-6*step)%frames];
  for(const [stem,target] of [[low,.49],[perc,.16],[music,.30]]) {
    let peak=0; for(const v of stem) peak=Math.max(peak,Math.abs(v));
    for(let i=0;i<frames;i++) stem[i]*=target/peak;
  }
  const mix=new Float32Array(frames); let peak=0;
  for(let i=0;i<frames;i++) { mix[i]=low[i]+perc[i]+music[i]; peak=Math.max(peak,Math.abs(mix[i])); }
  const gain=Math.min(1,.666/peak);
  for(const [role,stem] of [['low',low],['perc',perc],['music',music]]) {
    for(let i=0;i<frames;i++) stem[i]*=gain;
    const name=id+'_'+role+'.wav';
    const target=path.join(dir,name), bytes=wav(stem);
    if(!fs.existsSync(target) || !fs.readFileSync(target).equals(bytes))
      fs.writeFileSync(target,bytes);
    const guid=createHash('sha256').update('deckrogue/clubrecords/'+name).digest('hex').slice(0,32);
    if(!fs.existsSync(path.join(dir,name+'.meta'))) fs.writeFileSync(path.join(dir,name+'.meta'),meta.replace(/guid: \w+/,'guid: '+guid));
  }
  for(let i=0;i<frames;i++) mix[i]*=gain;
  const master = wav(mix);
  fs.writeFileSync(path.join(masters,id+'.wav'),master);
  const shelf = path.join(editorShelf,rewardIds.has(id) ? 'Rewards' : 'Club');
  fs.mkdirSync(shelf,{recursive:true});
  const title=id.split('_').map(word=>word[0].toUpperCase()+word.slice(1)).join(' ');
  fs.writeFileSync(path.join(shelf,title+'.wav'),master);
  // Two bars from each record for the listening reel.
  const excerpt=mix.slice(2*rate,6*rate);
  for(let i=0;i<excerpt.length;i++) excerpt[i]*=Math.min(1,i/240,(excerpt.length-1-i)/480);
  preview.push(excerpt);
}
scores.forEach(render);
// Club records first, then the six rewards in unlock order.
const reelOrder=[0,1,2,3,10,11,12,13,14,15,16,9,4,5,6,7,17,8];
const reel=new Float32Array(preview.length*preview[0].length);
reelOrder.forEach((record,i)=>reel.set(preview[record],i*preview[record].length));
fs.writeFileSync(new URL('club-records-preview.wav',import.meta.url),wav(reel));
