import fs from "node:fs/promises";
import { Presentation, PresentationFile } from "@oai/artifact-tool";

const W=1280,H=720,ROOT="D:/Project/Endless zombie";
const OUT=`${ROOT}/output/presentation/Endless_Zombie_Project_Presentation.pptx`;
const QA=`${ROOT}/tmp/ppt_builder/qa_pdf_flow`;
const PDF="C:/Users/Vu Tien/Desktop/Project2_GD1305_Endless_Zombie_Report_EN.pdf";
const A=`${ROOT}/tmp/ppt_builder/pdf_assets`;
const C={ink:"#121827",muted:"#60708A",navy:"#173A66",blue:"#3478C9",cyan:"#40B9C5",gold:"#E3B341",paper:"#F6F8FB",white:"#FFFFFF",line:"#D7DFEA",dark:"#08111F",green:"#36A56F",paleBlue:"#EAF2FB",paleGold:"#FFF5D8"};
const deck=Presentation.create({slideSize:{width:W,height:H}});

function shape(slide,name,x,y,w,h,fill=C.white,line=C.line,geometry="rect",radius){return slide.shapes.add({geometry,name,position:{left:x,top:y,width:w,height:h},fill,line:{style:"solid",fill:line,width:1},...(radius?{borderRadius:radius}:{})});}
function text(slide,name,value,x,y,w,h,size=22,color=C.ink,bold=false,align="left"){const s=slide.shapes.add({geometry:"textbox",name,position:{left:x,top:y,width:w,height:h},fill:"none",line:{style:"solid",fill:"none",width:0}});s.text=value;s.text.style={fontFamily:"Arial",fontSize:size,color,bold,alignment:align,verticalAlignment:"middle",wrap:true};return s;}
function header(slide,chapter,title,n){text(slide,`chapter-${n}`,chapter,72,34,600,24,14,C.blue,true);text(slide,`title-${n}`,title,72,70,1136,56,36,C.navy,true);shape(slide,`rule-${n}`,72,137,82,4,C.gold,C.gold);text(slide,`foot-${n}`,"GD1305  •  THE MOONSTONE",72,680,360,18,11,C.muted,true);text(slide,`page-${n}`,String(n).padStart(2,"0"),1160,680,48,18,11,C.muted,true,"right");}
function bullet(slide,name,value,x,y,w,size=18,color=C.ink){text(slide,name,`•  ${value}`,x,y,w,40,size,color,false);}
function notes(slide,pages){slide.speakerNotes.textFrame.setText(`[Sources]\n- ${PDF}${pages?` (pages ${pages})`:""}`);}
async function image(slide,name,path,x,y,w,h,fit="contain"){const b=await fs.readFile(path);const ab=b.buffer.slice(b.byteOffset,b.byteOffset+b.byteLength);return slide.images.add({name,blob:ab,contentType:"image/png",fit,position:{left:x,top:y,width:w,height:h}});}
function useCaseBlock(slide,i,id,title,actor,summary,x,y,w){shape(slide,`ucb-${i}`,x,y,w,118,i%2?C.paleGold:C.paleBlue,i%2?C.gold:C.line,"roundRect","rounded-xl");text(slide,`uci-${i}`,id,x+18,y+14,72,24,14,C.blue,true);text(slide,`uct-${i}`,title,x+18,y+41,w-36,28,20,C.navy,true);text(slide,`uca-${i}`,`${actor}  •  ${summary}`,x+18,y+76,w-36,30,15,C.ink,false);}

// Cover
{
 const s=deck.slides.add();s.background.fill=C.dark;shape(s,"bar",0,0,18,H,C.gold,C.gold);text(s,"cap","CAPSTONE PROJECT • GD1305",76,66,560,28,16,"#78D2D7",true);text(s,"main","ENDLESS\nZOMBIE",76,145,650,180,64,C.white,true);text(s,"sub","Survival Shooter / Action Roguelite",80,350,620,50,25,"#C7D6E8");shape(s,"info",790,80,390,520,"#101E31","#274565","roundRect","rounded-2xl");text(s,"project","PROJECT\nREPORT",850,170,270,110,40,C.gold,true,"center");text(s,"course","Unity 6000.3.10f1\nAndroid Mobile\nHybrid OOP + ECS",850,335,270,120,21,C.white,true,"center");text(s,"team","The MoonStone\nVu Viet Tien • Chu Van Thai",80,570,620,66,18,"#AFC1D8");notes(s,"1");
}

// I Introduction
{
 const s=deck.slides.add();s.background.fill=C.paper;header(s,"I. PROJECT INTRODUCTION","Endless Zombie combines survival pressure with persistent growth",2);
 text(s,"overview","PROJECT OVERVIEW",72,175,380,28,18,C.blue,true);text(s,"overview-copy","A top-down Android survival shooter where the player survives increasingly difficult waves through movement, automatic targeting, weapon choice, XP, and upgrades.",72,215,510,112,23,C.ink,true);
 text(s,"scope","PROJECT SCOPE",72,365,260,28,18,C.blue,true);bullet(s,"s1","Canvas-based Main Menu, HUD, Settings, and Level Up UI",72,405,530,18);bullet(s,"s2","Data-driven enemy waves and prefab spawning",72,450,530,18);bullet(s,"s3","Weapon profiles including multi-projectile Shotgun",72,495,530,18);bullet(s,"s4","DogMutant visual and synchronized run animation",72,540,530,18);
 shape(s,"stack",650,175,558,410,C.white,C.line,"roundRect","rounded-2xl");text(s,"stack-h","DEPLOYMENT ENVIRONMENT",690,205,450,30,18,C.blue,true);const rows=[["Platform","Android mobile devices"],["Engine","Unity 6000.3.10f1"],["Rendering","URP 17.3.0"],["Data stack","Entities / DOTS 1.4"],["Language","C#"],["Input","Unity Input System 1.18"]];rows.forEach((r,i)=>{const y=260+i*48;text(s,`rk-${i}`,r[0],690,y,150,28,16,C.muted,true);text(s,`rv-${i}`,r[1],845,y,300,28,17,C.ink,true);});notes(s,"3");
}

// II Requirements
{
 const s=deck.slides.add();s.background.fill=C.white;header(s,"II. SYSTEM REQUIREMENTS ANALYSIS","The requirements cover gameplay, scalability, UI, and persistence",3);
 text(s,"fr","FUNCTIONAL REQUIREMENTS",72,175,420,28,18,C.blue,true);const fr=["Start a selected stage and load GameScene","Acquire targets and process projectiles, damage, death, and XP","Spawn enemies by wave, prefab, schedule, and alive limit","Display Canvas HUD and level-up choices","Pause, resume, configure settings, and persist progression","Finish a stage with Win or Game Over"];fr.forEach((v,i)=>bullet(s,`fr-${i}`,v,72,220+i*55,540,17));
 shape(s,"nfr-box",660,175,548,390,C.paper,C.line,"roundRect","rounded-2xl");text(s,"nfr","NON-FUNCTIONAL REQUIREMENTS",700,205,450,28,18,C.blue,true);const nf=["Stable performance with many Entity enemies","Inspector-editable Canvas prefabs","ScriptableObject-driven gameplay values","Movement-synchronized visual animation","Safe local defaults and wave validation"];nf.forEach((v,i)=>bullet(s,`nf-${i}`,v,700,255+i*58,440,17));notes(s,"5");
}

// Use Case Diagram from PDF
{
 const s=deck.slides.add();s.background.fill=C.paper;header(s,"II. SYSTEM REQUIREMENTS ANALYSIS","4. Use Case Diagram",4);shape(s,"frame",140,160,1000,485,C.white,C.line,"roundRect","rounded-xl");await image(s,"pdf-use-case",`${A}/use_case.png`,170,180,940,445,"contain");notes(s,"6");
}

// Use Cases 1-4
{
 const s=deck.slides.add();s.background.fill=C.white;header(s,"II. SYSTEM REQUIREMENTS ANALYSIS","5. Use Case Descriptions",5);text(s,"uc-range-1","UC-01 to UC-04  •  Player entry and progression",72,145,600,24,15,C.muted,true);
 useCaseBlock(s,1,"UC-01","Play Game","Player","Enter the gameplay loop and initialize the stage.",72,175,550);
 useCaseBlock(s,2,"UC-02","Action","Player","Move, avoid enemies, and interact during Running state.",658,175,550);
 useCaseBlock(s,3,"UC-03","Switch Weapon","Player","Select another valid weapon and apply its current statistics.",72,325,550);
 useCaseBlock(s,4,"UC-04","Upgrade Weapon","Player","Choose a weapon modifier at an upgrade opportunity.",658,325,550);
 text(s,"uc-note","Player actions are ignored while paused, during Level Up selection, or after Win / Game Over.",180,520,920,50,21,C.ink,true,"center");notes(s,"6–9");
}

// Use Cases 5-8
{
 const s=deck.slides.add();s.background.fill=C.paper;header(s,"II. SYSTEM REQUIREMENTS ANALYSIS","5. Use Case Descriptions",6);text(s,"uc-range-2","UC-05 to UC-08  •  Session and enemy lifecycle",72,145,600,24,15,C.muted,true);
 useCaseBlock(s,5,"UC-05","Pause & Resume","Player","Suspend the simulation, adjust settings, and return to Running.",120,175,502);
 useCaseBlock(s,16,"UC-06","Generate & Spawn Enemy Wave","Game System","Schedule entries, resolve EnemyId, and instantiate prefabs.",658,175,550);
 useCaseBlock(s,7,"UC-07","Auto Attack & Collect EXP, Gold","Game System","Acquire targets, fire projectiles, and generate rewards.",72,325,550);
 useCaseBlock(s,8,"UC-08","Process Enemy Death & Reward","Game System","Remove defeated enemies and continue wave progression.",658,325,550);
 text(s,"uc-note2","Invalid spawn or reward data must never leave the current wave permanently blocked.",180,520,920,50,21,C.ink,true,"center");notes(s,"9–12");
}

// Activity from PDF
{
 const s=deck.slides.add();s.background.fill=C.white;header(s,"II. SYSTEM REQUIREMENTS ANALYSIS","6. Activity Diagram",7);shape(s,"af",345,155,590,500,C.paper,C.line,"roundRect","rounded-xl");await image(s,"pdf-activity",`${A}/activity.png`,390,170,500,470,"contain");text(s,"activity-side","The diagram follows the complete path from Main Menu to Stage Win or Game Over, including the repeated wave branch.",70,250,230,150,19,C.ink,true);notes(s,"13");
}

// UI Design from PDF
{
 const s=deck.slides.add();s.background.fill=C.paper;header(s,"III. DETAILED DESIGN","1. UI Design",8);
 const imgs=[["main_menu","Main Menu Canvas",80,175,230,405],["gameplay_hud","Gameplay HUD",350,175,230,405],["settings","Settings Menu",620,175,260,190],["level_up","Level Up Menu",920,175,260,190]];
 imgs.forEach((it,i)=>{shape(s,`uif-${i}`,it[2],it[3],it[4],it[5],C.white,C.line,"roundRect","rounded-xl");});
 await image(s,"pdf-main-menu",`${A}/main_menu.png`,100,192,190,330,"contain");await image(s,"pdf-gameplay",`${A}/gameplay_hud.png`,370,192,190,330,"contain");await image(s,"pdf-settings",`${A}/settings.png`,640,190,220,140,"contain");await image(s,"pdf-level-up",`${A}/level_up.png`,940,190,220,140,"contain");
 text(s,"l1","Main Menu Canvas",90,535,210,28,16,C.blue,true,"center");text(s,"l2","Gameplay HUD",360,535,210,28,16,C.blue,true,"center");text(s,"l3","Settings Menu",640,335,220,28,16,C.blue,true,"center");text(s,"l4","Level Up Menu",940,335,220,28,16,C.blue,true,"center");
 text(s,"ui-copy","MainMenuManager and HUDManager bind data and events; the Canvas prefabs own layout and remain editable in the Inspector.",620,415,560,110,20,C.ink,true,"center");notes(s,"14–15");
}

// Hybrid Diagram
{
 const s=deck.slides.add();s.background.fill=C.white;header(s,"III. DETAILED DESIGN","2. Hybrid Architecture Diagram",9);shape(s,"hf",72,165,1136,455,C.paper,C.line,"roundRect","rounded-xl");await image(s,"pdf-hybrid",`${A}/hybrid.png`,92,185,1096,410,"contain");notes(s,"16");
}

// Class Diagram
{
 const s=deck.slides.add();s.background.fill=C.paper;header(s,"III. DETAILED DESIGN","3. Class / Component Diagram",10);shape(s,"cf",140,155,1000,500,C.white,C.line,"roundRect","rounded-xl");await image(s,"pdf-class",`${A}/class_component.png`,175,175,930,455,"contain");notes(s,"16");
}

// Sequence
{
 const s=deck.slides.add();s.background.fill=C.white;header(s,"III. DETAILED DESIGN","4. Sequence Diagram",11);shape(s,"sf",72,160,1136,475,C.paper,C.line,"roundRect","rounded-xl");await image(s,"pdf-sequence",`${A}/sequence.png`,100,185,1080,420,"contain");notes(s,"17");
}

// Data Model
{
 const s=deck.slides.add();s.background.fill=C.paper;header(s,"III. DETAILED DESIGN","6. Data Model",12);shape(s,"df",72,160,1136,475,C.white,C.line,"roundRect","rounded-xl");await image(s,"pdf-data",`${A}/data_model.png`,100,185,1080,420,"contain");notes(s,"17–18");
}

// Testing
{
 const s=deck.slides.add();s.background.fill=C.white;header(s,"IV. TESTING","Static checks are complete; runtime evidence remains explicit",13);
 shape(s,"m1",72,180,290,190,C.paleBlue,C.line,"roundRect","rounded-2xl");text(s,"n1","9",100,205,234,78,58,C.green,true,"center");text(s,"nt1","PASS — STATIC",100,290,234,26,18,C.navy,true,"center");
 shape(s,"m2",390,180,290,190,C.paleGold,C.gold,"roundRect","rounded-2xl");text(s,"n2","3",418,205,234,78,58,C.gold,true,"center");text(s,"nt2","PLAY MODE REQUIRED",408,290,254,26,17,C.navy,true,"center");
 shape(s,"checks",710,180,498,190,C.paper,C.line,"roundRect","rounded-2xl");text(s,"ch","RUNTIME CHECKS",745,205,400,28,18,C.blue,true);bullet(s,"c1","Player movement",745,250,400,17);bullet(s,"c2","Enemy ground contact",745,292,400,17);bullet(s,"c3","Pause and resume",745,334,400,17);
 text(s,"acc","ACCEPTANCE CRITERIA",72,430,350,28,18,C.blue,true);const ac=["No compile errors","No spawn-time visual flashing","DogMutant speed stays synchronized","Settings sliders remain clamped","Final wave reaches a terminal state"];ac.forEach((v,i)=>bullet(s,`ac-${i}`,v,72+(i%2)*550,475+Math.floor(i/2)*48,500,17));notes(s,"19");
}

// Assignment
{
 const s=deck.slides.add();s.background.fill=C.paper;header(s,"V. TASK ASSIGNMENT","The work is divided across core systems, UI, and delivery",14);
 shape(s,"vt",72,180,520,340,C.paleBlue,C.line,"roundRect","rounded-2xl");text(s,"vtn","VU VIET TIEN",105,215,450,42,25,C.navy,true);bullet(s,"v1","Requirement analysis and architecture design",105,290,430,18);bullet(s,"v2","OOP/ECS gameplay, waves, combat, and XP",105,345,430,18);bullet(s,"v3","Testing, bug fixing, and report writing",105,400,430,18);
 shape(s,"ct",616,180,592,340,C.paleGold,C.gold,"roundRect","rounded-2xl");text(s,"ctn","CHU VAN THAI",650,215,520,42,25,C.navy,true);bullet(s,"t1","OOP/ECS implementation support",650,290,510,18);bullet(s,"t2","Main Menu, HUD, and Settings Canvas",650,345,510,18);bullet(s,"t3","Enemy and DogMutant visuals / animation",650,400,510,18);text(s,"pending","Final Play Mode regression remains pending before delivery.",230,565,820,44,21,C.ink,true,"center");notes(s,"20");
}

// Installation
{
 const s=deck.slides.add();s.background.fill=C.white;header(s,"VI. INSTALLATION INSTRUCTIONS","Build and verify the project on Android",15);
 const cols=[[72,"1. PREREQUISITES",["Unity Hub and Editor 6000.3.10f1","Android Build Support","SDK, NDK, OpenJDK","Android device or emulator"]],[420,"2. OPEN & RUN",["Add project from disk","Restore packages","Open MainMenu.unity","Press Play and verify systems"]],[768,"3. BUILD AND TEST",["Switch platform to Android","Add MainMenu and GameScene","Configure Player Settings","Build APK/AAB or Build and Run"]]];
 cols.forEach((c,i)=>{shape(s,`inst-${i}`,c[0],180,310,370,i===2?C.paleGold:C.paper,i===2?C.gold:C.line,"roundRect","rounded-2xl");text(s,`ih-${i}`,c[1],c[0]+24,215,262,42,19,C.navy,true);c[2].forEach((v,j)=>bullet(s,`ib-${i}-${j}`,v,c[0]+24,285+j*58,262,16));});text(s,"verify","Device verification must cover UI scaling, touch input, audio, wave spawning, and game states.",160,585,960,40,20,C.ink,true,"center");notes(s,"21");
}

// Conclusion
{
 const s=deck.slides.add();s.background.fill=C.dark;text(s,"ck","VII. CONCLUSION AND FUTURE DEVELOPMENT",72,55,700,24,14,"#78D2D7",true);text(s,"ctitle","The complete survival loop is in place",72,120,800,64,46,C.white,true);text(s,"cbody","Stage entry, wave spawning, automatic combat, XP and upgrades, and Win / Game Over now operate within a hybrid Unity architecture.",72,210,780,78,22,"#C7D6E8");shape(s,"cr",72,315,90,5,C.gold,C.gold);const fs=[["Bosses","New archetypes and explicit attack states"],["Performance","Pooling, profiling, and GC reduction"],["Depth","More stages, weapon synergies, and progression"],["Quality","Automated Play Mode and accessibility tests"]];fs.forEach((f,i)=>{const x=72+(i%2)*560,y=360+Math.floor(i/2)*120;text(s,`fh-${i}`,f[0].toUpperCase(),x,y,190,28,19,C.white,true);text(s,`fb-${i}`,f[1],x,y+38,480,38,16,"#C7D6E8");});text(s,"cf","THE MOONSTONE  •  GD1305 CAPSTONE PROJECT",72,665,700,20,12,"#91A8C2",true);notes(s,"21–22");
}

await fs.rm(QA,{recursive:true,force:true});await fs.mkdir(QA,{recursive:true});
for(const[i,s]of deck.slides.items.entries()){const png=await deck.export({slide:s,format:"png",scale:1});await fs.writeFile(`${QA}/slide-${String(i+1).padStart(2,"0")}.png`,new Uint8Array(await png.arrayBuffer()));const l=await s.export({format:"layout"});await fs.writeFile(`${QA}/slide-${String(i+1).padStart(2,"0")}.layout.json`,await l.text());}
const pptx=await PresentationFile.exportPptx(deck);await pptx.save(OUT);console.log(OUT);
