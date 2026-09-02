import fs from "node:fs/promises";
import { Presentation, PresentationFile } from "@oai/artifact-tool";

const W=1280,H=720;
const ROOT="D:/Project/Endless zombie";
const OUT=`${ROOT}/output/presentation/Endless_Zombie_Project_Presentation.pptx`;
const QA=`${ROOT}/tmp/ppt_builder/qa`;
const PDF="C:/Users/Vu Tien/Desktop/Project2_GD1305_Endless_Zombie_Report_EN.pdf";
const USECASE=`${ROOT}/output/report/diagrams/01_use_case_preview.png`;
const PAGE14=`${ROOT}/tmp/ppt_source/page-14.png`;
const PAGE15=`${ROOT}/tmp/ppt_source/page-15.png`;

const C={ink:"#121827",muted:"#60708A",navy:"#173A66",blue:"#3478C9",cyan:"#40B9C5",gold:"#E3B341",paper:"#F6F8FB",white:"#FFFFFF",line:"#D7DFEA",dark:"#08111F",green:"#36A56F",red:"#C95D5D",paleBlue:"#EAF2FB",paleGold:"#FFF5D8"};
const deck=Presentation.create({slideSize:{width:W,height:H}});

function shape(slide,name,x,y,w,h,fill=C.white,line=C.line,geometry="rect",radius){
  return slide.shapes.add({geometry,name,position:{left:x,top:y,width:w,height:h},fill,line:{style:"solid",fill:line,width:1},...(radius?{borderRadius:radius}:{})});
}
function text(slide,name,value,x,y,w,h,size=22,color=C.ink,bold=false,align="left"){
  const s=slide.shapes.add({geometry:"textbox",name,position:{left:x,top:y,width:w,height:h},fill:"none",line:{style:"solid",fill:"none",width:0}});
  s.text=value; s.text.style={fontFamily:"Arial",fontSize:size,color,bold,alignment:align,verticalAlignment:"middle",wrap:true}; return s;
}
function title(slide,value,kicker="ENDLESS ZOMBIE"){
  text(slide,"kicker",kicker,72,36,500,24,14,C.blue,true);
  text(slide,"title",value,72,70,1136,58,38,C.navy,true);
  shape(slide,"title-rule",72,137,82,4,C.gold,C.gold);
}
function footer(slide,n){ text(slide,"footer",`GD1305  •  THE MOONSTONE`,72,680,360,18,11,C.muted,true); text(slide,"page",String(n).padStart(2,"0"),1160,680,48,18,11,C.muted,true,"right"); }
function notes(slide,extra=""){ slide.speakerNotes.textFrame.setText(`[Sources]\n- ${PDF}\n${extra}`); }
function bullet(slide,value,x,y,w,size=20,color=C.ink){ text(slide,`bullet-${y}`,`•  ${value}`,x,y,w,42,size,color,false); }
function arrow(slide,name,x,y,w,h,color=C.gold){ shape(slide,name,x,y,w,h,color,color,"rightArrow"); }
async function image(slide,name,path,x,y,w,h,fit="contain"){
  const b=await fs.readFile(path); const ab=b.buffer.slice(b.byteOffset,b.byteOffset+b.byteLength);
  return slide.images.add({name,blob:ab,contentType:"image/png",fit,position:{left:x,top:y,width:w,height:h}});
}

// 1 — Cover
{
 const s=deck.slides.add(); s.background.fill=C.dark;
 shape(s,"accent-bar",0,0,18,H,C.gold,C.gold);
 text(s,"eyebrow","CAPSTONE PROJECT • GD1305",76,66,520,28,16,"#78D2D7",true);
 text(s,"cover-title","ENDLESS\nZOMBIE",76,145,650,180,64,C.white,true);
 text(s,"subtitle","A hybrid ECS survival shooter built with Unity",80,350,620,50,25,"#C7D6E8",false);
 shape(s,"visual-field",790,80,390,520,"#101E31","#274565","roundRect","rounded-2xl");
 text(s,"visual-symbol","EZ",865,170,245,150,92,C.gold,true,"center");
 text(s,"visual-copy","SURVIVE\nUPGRADE\nREPEAT",865,355,245,140,25,C.white,true,"center");
 text(s,"team","The MoonStone\nVu Viet Tien • Chu Van Thai",80,570,620,66,18,"#AFC1D8",false);
 notes(s); 
}

// 2 — Premise and stack
{
 const s=deck.slides.add(); s.background.fill=C.paper; title(s,"The game turns survival into a repeatable upgrade loop"); footer(s,2);
 text(s,"premise","A top-down Android survival shooter where the player moves, auto-targets, collects rewards, and adapts through weapon and stat upgrades.",72,170,520,130,25,C.ink,true);
 bullet(s,"Increasingly difficult enemy waves",72,325,520,20);
 bullet(s,"Automatic combat with weapon variety",72,375,520,20);
 bullet(s,"Persistent gold and meta progression",72,425,520,20);
 shape(s,"tech-field",670,170,538,390,C.white,C.line,"roundRect","rounded-2xl");
 text(s,"tech-head","DELIVERY STACK",710,200,450,30,18,C.blue,true);
 const tech=[["Unity","6000.3.10f1"],["Target","Android mobile"],["Rendering","URP 17.3.0"],["Simulation","Entities / DOTS 1.4"],["Input","Unity Input System"]];
 tech.forEach((r,i)=>{const y=255+i*57; text(s,`tk-${i}`,r[0],710,y,170,32,17,C.muted,true); text(s,`tv-${i}`,r[1],885,y,270,32,19,C.ink,true); if(i<4)shape(s,`tl-${i}`,710,y+42,445,1,C.line,C.line);});
 notes(s);
}

// 3 — Experience flow
{
 const s=deck.slides.add(); s.background.fill=C.white; title(s,"One loop connects every player-facing system"); footer(s,3);
 const nodes=[["01","PREPARE","Choose stage, weapon,\nand meta upgrades"],["02","SURVIVE","Move, avoid enemies,\nand auto attack"],["03","GROW","Collect XP and gold;\nselect upgrades"],["04","RESOLVE","Clear the final wave\nor reach Game Over"]];
 [315,590,865].forEach((x,i)=>arrow(s,`flow-arrow-${i}`,x,300,80,34,C.gold));
 nodes.forEach((n,i)=>{const x=72+i*280; shape(s,`flow-${i}`,x,220,235,240,i===1?C.paleGold:C.paper,i===1?C.gold:C.line,"roundRect","rounded-xl"); text(s,`num-${i}`,n[0],x+20,242,55,34,17,i===1?"#9A6B00":C.blue,true); text(s,`fh-${i}`,n[1],x+20,290,190,38,25,C.navy,true); text(s,`fb-${i}`,n[2],x+20,345,195,80,18,C.ink,false);});
 text(s,"loop-note","The result is a short feedback cycle: pressure creates rewards, rewards create build choices, and build choices change the next wave.",170,520,940,72,23,C.ink,true,"center");
 notes(s);
}

// 4 — Use Case
{
 const s=deck.slides.add(); s.background.fill=C.paper; title(s,"The player acts; the game system sustains the pressure"); footer(s,4);
 shape(s,"uc-frame",72,170,850,450,C.white,C.line,"roundRect","rounded-xl");
 await image(s,"use-case",USECASE,92,192,810,410,"contain");
 text(s,"uc-side-head","WHAT THIS SHOWS",970,180,238,34,18,C.blue,true);
 bullet(s,"Player controls entry, movement, weapon choice, and upgrades.",970,235,235,17);
 bullet(s,"Game System owns wave generation, death processing, and stage completion.",970,335,235,17);
 bullet(s,"Include / extend relations keep optional behavior explicit.",970,455,235,17);
 notes(s,`- ${ROOT}/output/report/diagrams/01_use_case.puml`);
}

// 5 — Architecture
{
 const s=deck.slides.add(); s.background.fill=C.white; title(s,"A hybrid architecture separates presentation from simulation"); footer(s,5);
 // arrows first
 arrow(s,"a1",384,300,90,36,C.gold); arrow(s,"a2",806,300,90,36,C.gold);
 const cols=[{x:72,w:300,c:C.paleBlue,h:"OOP / GAMEOBJECT",items:"Input • State machine\nCanvas UI • Audio\nMeta progression"},{x:490,w:300,c:C.paleGold,h:"BRIDGE LAYER",items:"Mob visuals • Damage\nXP events • Weapon VFX\nEntity ↔ GameObject sync"},{x:908,w:300,c:"#EAF7F1",h:"ECS / DOTS",items:"Wave scheduling • Spawn\nMovement • Projectiles\nDamage • XP • Metrics"}];
 cols.forEach((c,i)=>{shape(s,`arch-${i}`,c.x,190,c.w,340,c.c,i===1?C.gold:C.line,"roundRect","rounded-2xl"); text(s,`arch-h-${i}`,c.h,c.x+24,225,c.w-48,50,23,C.navy,true,"center"); text(s,`arch-b-${i}`,c.items,c.x+30,310,c.w-60,130,19,C.ink,false,"center");});
 text(s,"arch-note","High-volume gameplay remains data-oriented, while UI and visuals stay editable in the Unity Inspector.",160,570,960,54,23,C.ink,true,"center");
 notes(s);
}

// 6 — Wave pipeline
{
 const s=deck.slides.add(); s.background.fill=C.paper; title(s,"Wave spawning is data-driven from configuration to prefab"); footer(s,6);
 const stages=[["1","StageConfig","Wave list and limits"],["2","WaveProgression","Activates current wave"],["3","SpawnScheduler","Creates timed requests"],["4","SpawnProcessor","Resolves EnemyId"],["5","Entity Prefab","Enemy enters simulation"]];
 [270,500,730,960].forEach((x,i)=>arrow(s,`wa-${i}`,x,330,62,28,C.cyan));
 stages.forEach((n,i)=>{const x=58+i*230; shape(s,`wave-${i}`,x,240,200,210,C.white,C.line,"roundRect","rounded-xl"); text(s,`wn-${i}`,n[0],x+15,255,38,38,18,C.white,true,"center"); shape(s,`wc-${i}`,x+14,252,42,42,i===0?C.gold:C.blue,i===0?C.gold:C.blue,"ellipse"); text(s,`wn2-${i}`,n[0],x+14,255,42,36,18,C.white,true,"center"); text(s,`wh-${i}`,n[1],x+18,315,164,50,20,C.navy,true,"center"); text(s,`wb-${i}`,n[2],x+18,380,164,42,16,C.muted,false,"center");});
 text(s,"wave-note","Invalid entries are skipped or marked terminal so one bad record cannot block the entire stage.",190,515,900,56,22,C.ink,true,"center");
 notes(s);
}

// 7 — Combat
{
 const s=deck.slides.add(); s.background.fill=C.dark;
 text(s,"combat-kicker","COMBAT DESIGN",72,36,500,24,14,"#78D2D7",true);
 text(s,"combat-title","Shotgun behavior emerges from reusable weapon parameters",72,70,1136,58,38,C.white,true);
 shape(s,"combat-title-rule",72,137,82,4,C.gold,C.gold);
 text(s,"combat-footer","GD1305  •  THE MOONSTONE",72,680,360,18,11,"#91A8C2",true); text(s,"combat-page","07",1160,680,48,18,11,"#91A8C2",true,"right");
 text(s,"weapon-head","WEAPON MANAGER",72,180,390,42,26,C.white,true);
 const params=[["Damage","Per projectile"],["Projectile Count","One or many pellets"],["Spread Angle","Width of the firing cone"],["Cooldown","Time between attacks"],["Range / Speed","Targeting and travel"]];
 params.forEach((p,i)=>{const y=240+i*62; text(s,`pk-${i}`,p[0],72,y,190,30,17,"#79C7D0",true); text(s,`pv-${i}`,p[1],265,y,240,30,18,C.white,false);});
 shape(s,"combat-field",590,165,618,430,"#101E31","#274565","roundRect","rounded-2xl");
 text(s,"shotgun","SHOTGUN",635,205,220,38,23,C.gold,true);
 shape(s,"player-dot",675,372,46,46,C.white,C.white,"ellipse");
 // pellet trajectories first visually behind labels
 [270,310,350,390,430].forEach((y,i)=>{const a=shape(s,`pellet-${i}`,735,390,330,8,i===2?C.gold:"#5BBAC4",i===2?C.gold:"#5BBAC4","rightArrow"); a.rotation=(y-350)/4;});
 text(s,"spread","Projectile Count > 1\n+\nSpread Angle > 0",835,455,300,82,20,C.white,true,"center");
 text(s,"combat-note","The same auto-attack system supports single-shot and pellet weapons without a separate combat pipeline.",630,540,540,44,18,"#C7D6E8",false,"center");
 notes(s);
}

// 8 — UI
{
 const s=deck.slides.add(); s.background.fill=C.paper; title(s,"Canvas prefabs keep every interface editable without layout code"); footer(s,8);
 shape(s,"ui-left",72,165,430,450,C.white,C.line,"roundRect","rounded-xl");
 shape(s,"ui-right",530,165,678,450,C.white,C.line,"roundRect","rounded-xl");
 await image(s,"ui-page14",PAGE14,90,185,392,410,"contain");
 await image(s,"ui-page15",PAGE15,550,185,638,410,"contain");
 text(s,"ui-tag1","MAIN MENU + HUD",110,550,240,26,15,C.blue,true);
 text(s,"ui-tag2","SETTINGS + LEVEL UP",570,550,270,26,15,C.blue,true);
 notes(s);
}

// 9 — Data model
{
 const s=deck.slides.add(); s.background.fill=C.white; title(s,"Configuration assets define behavior; PlayerPrefs preserve progress"); footer(s,9);
 // connectors first
 arrow(s,"d1",305,260,70,24,C.cyan); arrow(s,"d2",610,260,70,24,C.cyan); arrow(s,"d3",915,260,70,24,C.cyan);
 const top=[[72,"StageConfig","Stage ID • delay • limits"],[377,"WaveDefinition","Type • activation • entries"],[682,"SpawnEntry","EnemyId • quantity • interval"],[987,"EnemyCatalog","EnemyId → prefab"]];
 top.forEach((n,i)=>{shape(s,`data-${i}`,n[0],205,220,135,C.paleBlue,C.line,"roundRect","rounded-xl"); text(s,`dh-${i}`,n[1],n[0]+18,228,184,34,20,C.navy,true,"center"); text(s,`db-${i}`,n[2],n[0]+18,280,184,38,15,C.muted,false,"center");});
 shape(s,"persist-line",72,412,1136,2,C.line,C.line);
 text(s,"persist-head","LOCAL PERSISTENCE",72,445,250,28,17,C.blue,true);
 const pitems=[["Audio","Music • Sound • Vibration"],["Economy","Gold Wallet"],["Progression","Meta upgrades"]];
 pitems.forEach((p,i)=>{const x=360+i*270; text(s,`ph-${i}`,p[0],x,435,220,32,20,C.navy,true); text(s,`pb-${i}`,p[1],x,480,220,40,17,C.ink,false);});
 text(s,"data-note","No database server is required for the current offline scope.",72,570,700,36,21,C.ink,true);
 notes(s);
}

// 10 — Testing
{
 const s=deck.slides.add(); s.background.fill=C.paper; title(s,"Static coverage is strong; three behaviors still need runtime evidence"); footer(s,10);
 shape(s,"metric1",72,185,310,210,C.white,C.line,"roundRect","rounded-2xl"); text(s,"m1","9",105,215,244,88,64,C.green,true,"center"); text(s,"m1t","PASS — STATIC",105,310,244,30,19,C.navy,true,"center");
 shape(s,"metric2",410,185,310,210,C.white,C.line,"roundRect","rounded-2xl"); text(s,"m2","3",443,215,244,88,64,C.gold,true,"center"); text(s,"m2t","PLAY MODE REQUIRED",443,310,244,30,18,C.navy,true,"center");
 shape(s,"metric3",748,185,460,210,C.white,C.line,"roundRect","rounded-2xl"); text(s,"m3h","RUNTIME CHECKS",785,215,380,34,20,C.blue,true); bullet(s,"Player movement",785,265,360,18); bullet(s,"Enemy ground contact",785,310,360,18); bullet(s,"Pause / resume",785,355,360,18);
 text(s,"accept","ACCEPTANCE FOCUS",72,445,250,30,18,C.blue,true);
 const acc=["No compile errors","No spawn-time visual flashing","DogMutant run speed stays synchronized","Settings sliders remain clamped","Final wave always reaches a terminal state"];
 acc.forEach((a,i)=>bullet(s,a,72+(i%2)*540,490+Math.floor(i/2)*48,500,17));
 notes(s);
}

// 11 — Team and delivery
{
 const s=deck.slides.add(); s.background.fill=C.white; title(s,"Two contributors split core systems, UI, testing, and reporting"); footer(s,11);
 shape(s,"tien",72,180,520,340,C.paleBlue,C.line,"roundRect","rounded-2xl");
 text(s,"tien-name","VU VIET TIEN",105,215,450,42,26,C.navy,true);
 bullet(s,"Requirements and architecture",105,290,430,19);
 bullet(s,"OOP/ECS gameplay, waves, combat, XP",105,345,430,19);
 bullet(s,"Testing, bug fixing, and report",105,410,430,19);
 shape(s,"thai",616,180,592,340,C.paleGold,C.gold,"roundRect","rounded-2xl");
 text(s,"thai-name","CHU VAN THAI",650,215,520,42,26,C.navy,true);
 bullet(s,"OOP/ECS implementation support",650,290,510,19);
 bullet(s,"Main Menu, HUD, and Settings Canvas",650,345,510,19);
 bullet(s,"Enemy and DogMutant visuals / animation",650,410,510,19);
 text(s,"delivery","Current delivery target: Android build with final Play Mode regression and device verification.",190,570,900,48,22,C.ink,true,"center");
 notes(s);
}

// 12 — Close
{
 const s=deck.slides.add(); s.background.fill=C.dark;
 text(s,"close-kicker","ENDLESS ZOMBIE",72,60,430,24,15,"#78D2D7",true);
 text(s,"close-title","The core loop works.\nThe next step is scale.",72,130,760,150,52,C.white,true);
 shape(s,"close-rule",72,310,90,5,C.gold,C.gold);
 const future=[["01","BOSSES","Attack state machines and new archetypes"],["02","PERFORMANCE","Pooling, profiling, and GC reduction"],["03","DEPTH","More stages, synergies, and progression"],["04","QUALITY","Automated Play Mode and accessibility tests"]];
 future.forEach((f,i)=>{const x=72+(i%2)*560,y=360+Math.floor(i/2)*120; text(s,`fn-${i}`,f[0],x,y,48,28,15,C.gold,true); text(s,`fh-${i}`,f[1],x+55,y,180,28,19,C.white,true); text(s,`fb-${i}`,f[2],x+55,y+36,440,42,16,"#C7D6E8",false);});
 text(s,"close-team","THE MOONSTONE  •  GD1305 CAPSTONE PROJECT",72,665,700,20,12,"#91A8C2",true);
 notes(s);
}

await fs.mkdir(QA,{recursive:true});
for (const [i,s] of deck.slides.items.entries()) {
  const png=await deck.export({slide:s,format:"png",scale:1});
  await fs.writeFile(`${QA}/slide-${String(i+1).padStart(2,"0")}.png`,new Uint8Array(await png.arrayBuffer()));
  const layout=await s.export({format:"layout"});
  await fs.writeFile(`${QA}/slide-${String(i+1).padStart(2,"0")}.layout.json`,await layout.text());
}
const montage=await deck.export({format:"webp",montage:true,scale:1});
await fs.writeFile(`${QA}/montage.webp`,new Uint8Array(await montage.arrayBuffer()));
const pptx=await PresentationFile.exportPptx(deck); await pptx.save(OUT);
console.log(OUT);
