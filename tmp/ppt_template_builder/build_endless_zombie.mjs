import fs from "node:fs/promises";
import { FileBlob, PresentationFile } from "@oai/artifact-tool";

const starter = "D:/Project/Endless zombie/tmp/ppt_template_builder/template-starter.pptx";
const output = "D:/Project/Endless zombie/output/presentation/Endless_Zombie_MoonStone_Structure_Presentation.pptx";
const assets = "D:/Project/Endless zombie/tmp/ppt_builder/pdf_assets";
const testPage = "D:/Project/Endless zombie/tmp/ppt_source/page-19.png";
const starterInspect = "D:/Project/Endless zombie/tmp/ppt_template_builder/template-starter.pptx.inspect.ndjson";
let anchorMap = new Map();

async function bytes(path) {
  const b = await fs.readFile(path);
  return b.buffer.slice(b.byteOffset, b.byteOffset + b.byteLength);
}

async function replaceImage(presentation, id, path, alt, fit = "contain") {
  const image = presentation.resolve(anchorMap.get(id) || id);
  const frame = image.frame;
  const crop = image.crop;
  const geometry = image.geometry;
  const radius = image.borderRadius;
  const rotation = image.rotation;
  const flipHorizontal = image.flipHorizontal;
  const flipVertical = image.flipVertical;
  const lockAspectRatio = image.lockAspectRatio;
  await image.replace({ blob: await bytes(path), contentType: "image/png", alt, fit });
  image.frame = frame;
  image.crop = crop;
  image.geometry = geometry;
  image.borderRadius = radius;
  image.rotation = rotation;
  image.flipHorizontal = flipHorizontal;
  image.flipVertical = flipVertical;
  image.lockAspectRatio = lockAspectRatio;
}

function setText(presentation, id, value) {
  presentation.resolve(anchorMap.get(id) || id).text = value;
}

function setNotes(presentation, id, sourceDetail) {
  presentation.resolve(anchorMap.get(id) || id).setText(`[Sources]\n- ${sourceDetail}`);
}

function parseNdjson(text) {
  return text.split(/\r?\n/).filter(Boolean).map((line) => JSON.parse(line));
}

function sameElement(a, b) {
  if (a.kind !== b.kind || a.slide !== b.slide) return false;
  if (a.kind === "notes") return true;
  if (a.name || b.name) return a.name === b.name;
  if (a.placeholder || b.placeholder) return a.placeholder === b.placeholder;
  return a.kind === "textbox" && a.textPreview === b.textPreview;
}

async function main() {
  await fs.mkdir("D:/Project/Endless zombie/output/presentation", { recursive: true });
  const p = await PresentationFile.importPptx(await FileBlob.load(starter));
  const sourceRecords = parseNdjson(await fs.readFile(starterInspect, "utf8"));
  const currentSnapshot = await p.inspect({ kind: "slide,textbox,shape,image,table,notes", maxChars: 50000 });
  const currentRecords = parseNdjson(currentSnapshot.ndjson);
  for (const old of sourceRecords) {
    if (!old.id || !["textbox", "image", "table", "notes"].includes(old.kind)) continue;
    const match = currentRecords.find((candidate) => sameElement(old, candidate));
    if (match?.id) anchorMap.set(old.id, match.id);
  }

  setText(p, "sh/tobudsf2", "ENDLESS ZOMBIE");
  setText(p, "sh/vy9wniho", "Class Name: GD1305\nProject II Presentation\nMembers: Vu Viet Tien & Chu Van Thai");

  setText(p, "sh/x4zihwv2", "OBJECTIVES");
  setText(p, "sh/w3qh8reh", "Project Introduction\nAnalyze System Requirements\nDesign Details\nTest\nInstallation Instructions");

  setText(p, "sh/srutcbad", "Project Introduction");
  setText(p, "sh/ts3ulgbi", "Project Name: Endless Zombie\n\nProject Description:\nEndless Zombie is a third-person survival shooter built with Unity. The player survives escalating enemy waves, switches weapons, gains EXP and gold, and selects upgrades during each run.\n\nObjective: survive as long as possible while managing movement, weapons, upgrades, and enemy pressure.");
  await replaceImage(p, "im/ilgz2h8b", `${assets}/main_menu.png`, "Endless Zombie main menu from the report");

  setText(p, "sh/dwr2t8n6", "Project Introduction");
  setText(p, "sh/cvylk36l", "\n\nCore Gameplay\nMove through the arena and avoid enemy attacks.\nAim and fire multiple weapon types.\nShotgun fires a pellet spread at close range.\nDefeat enemies to collect EXP and gold.\nChoose upgrades when leveling up.\nEnemy waves become increasingly difficult.");

  setText(p, "sh/qd076dwr", "Project Introduction");
  setText(p, "sh/be9ofixc", "\nDevelopment Tools");
  const table = p.resolve(anchorMap.get("tb/o7qdwfq9") || "tb/o7qdwfq9");
  const tableValues = [
    ["Tool", "Purpose"],
    ["Unity 6000.3.10f1", "Game engine"],
    ["Universal RP 17.3.0", "Rendering pipeline"],
    ["Entities / DOTS 1.4", "High-volume enemy simulation"],
    ["Input System 1.18.0", "Player input and controls"]
  ];
  for (let r = 0; r < tableValues.length; r++) for (let c = 0; c < 2; c++) table.cells.set(r, c, tableValues[r][c]);

  setText(p, "sh/1w761cjy", "II. ANALYZE SYSTEM REQUIREMENTS");
  setText(p, "sh/gvy5872d", "\nSystem Overview\n\nThe project is organized into five main areas:\nPlayer Control - movement, aiming, shooting, and weapon switching.\nEnemy System - spawning, navigation, collision, animation, and attacks.\nWave System - scales difficulty and coordinates spawn timing.\nProgression - EXP, gold, leveling, and upgrades.\nCanvas UI - main menu, HUD, settings, and level-up panels.");

  setText(p, "sh/4fihg7mh", "II. ANALYZE SYSTEM REQUIREMENTS");
  setText(p, "sh/5gry9c32", "Use Case Diagram");
  await replaceImage(p, "im/ze1kva10", `${assets}/use_case.png`, "Use case diagram from the Endless Zombie report");

  setText(p, "sh/8zuh8va9", "II. ANALYZE SYSTEM REQUIREMENTS");
  setText(p, "sh/t03i10ru", "Activity Diagram");
  await replaceImage(p, "im/2xwnmhkf", `${assets}/activity.png`, "Activity diagram from the Endless Zombie report");

  setText(p, "sh/sfqtona5", "III. DESIGN DETAIL");
  setText(p, "sh/tgzuxsrq", "Menu and Settings UI");
  await replaceImage(p, "im/ehwva1sr", `${assets}/main_menu.png`, "Main menu from the Endless Zombie report", "cover");
  await replaceImage(p, "im/5grex0rm", `${assets}/settings.png`, "Settings menu from the Endless Zombie report", "cover");

  setText(p, "sh/47epg3mx", "III. DESIGN DETAIL");
  setText(p, "sh/p876983i", "In-Game HUD and Level Up");
  await replaceImage(p, "im/2lwjq18r", `${assets}/gameplay_hud.png`, "Gameplay HUD from the Endless Zombie report", "cover");
  await replaceImage(p, "im/9kr2x07m", `${assets}/level_up.png`, "Level-up UI from the Endless Zombie report", "cover");

  setText(p, "sh/476lw7yx", "III. DESIGN DETAIL");
  setText(p, "sh/58z25sfi", "Class Diagram");
  await replaceImage(p, "im/qdkvq98j", `${assets}/class_component.png`, "Class and component diagram from the Endless Zombie report");

  setText(p, "sh/twnmhs3i", "III. DESIGN DETAIL");
  setText(p, "sh/svel8n2x", "System Architecture\n- Hybrid MonoBehaviour + DOTS");
  await replaceImage(p, "im/fixk3m1k", `${assets}/hybrid.png`, "Hybrid architecture diagram from the Endless Zombie report");

  setText(p, "sh/sbe5szi9", "III. DESIGN DETAIL");
  setText(p, "sh/tcnm14ju", "Sequence Diagram\n- Complete Gameplay Flow");
  await replaceImage(p, "im/a5sbih8j", `${assets}/sequence.png`, "Sequence diagram from the Endless Zombie report");

  setText(p, "sh/gj2dk3ax", "III. DESIGN DETAIL");
  setText(p, "sh/1kbet8ri", "Sequence Diagram\n- Wave Spawn and Enemy Update");
  await replaceImage(p, "im/yxgvixwv", `${assets}/sequence.png`, "Sequence diagram emphasizing enemy wave flow from the Endless Zombie report", "cover");

  setText(p, "sh/9gjapcz2", "III. DESIGN DETAIL");
  setText(p, "sh/ofa9w7ih", "Sequence Diagram\n- Combat, Death, and Rewards");
  await replaceImage(p, "im/zupoj21c", `${assets}/sequence.png`, "Sequence diagram emphasizing combat and reward flow from the Endless Zombie report", "cover");

  setText(p, "sh/bixk7udg", "III. DESIGN DETAIL");
  setText(p, "sh/axojypwv", "Data Model\n\nRuntime data stores:\nPlayer - health, weapons, EXP, and gold\nEnemy - health, speed, target, attack state\nWave - enemy types, count, timing, scaling\nUI - reads state without owning combat logic");
  await replaceImage(p, "im/l4vuxwfu", `${assets}/data_model.png`, "Data model diagram from the Endless Zombie report");

  setText(p, "sh/0b29kbqd", "III. DESIGN DETAIL");
  setText(p, "sh/1cbatg7i", "Data Model");
  setText(p, "sh/729cfut8", "Core Data Relationships");
  await replaceImage(p, "im/il0bah47", `${assets}/data_model.png`, "Detailed data model from the Endless Zombie report");

  setText(p, "sh/kvi9wryx", "IV. TEST");
  await replaceImage(p, "im/2d0bmdkn", testPage, "Testing page from the Endless Zombie report", "cover");
  await replaceImage(p, "im/pgrahsje", testPage, "Testing evidence from the Endless Zombie report", "cover");

  setText(p, "sh/itgvylk7", "IV. TEST");
  await replaceImage(p, "im/8bi9sby5", testPage, "Test cases from the Endless Zombie report", "cover");
  await replaceImage(p, "im/na98z6xk", testPage, "Test results from the Endless Zombie report", "cover");

  setText(p, "sh/j2tsvuhc", "V. Installation Instructions");
  setText(p, "sh/i1kr2pg7", "PREREQUISITES\nUnity Hub\nUnity Editor 6000.3.10f1\nAndroid Build Support\n\nINSTALLATION STEPS\n1. Open the Endless Zombie project in Unity Hub.\n2. Confirm URP, Entities/DOTS, and Input System packages.\n3. Open the main scene and test in Play Mode.\n4. Switch the build target to Android.\n5. Configure package name and player settings.\n6. Build the APK and install it on an Android device.");
  await replaceImage(p, "im/pona50fe", `${assets}/main_menu.png`, "Endless Zombie project verification screen", "contain");

  const report = "Project2_GD1305_Endless_Zombie_Report_EN.pdf (user-provided report)";
  const template = "GD1305 - Group MoonStone - Presentation Project II.pptx (user-provided structure and visual template)";
  const noteIds = ["nt/2k1fd9","nt/lxw40t","nt/daysbm","nt/rfaarr","nt/eqkt4n","nt/xgodb5","nt/e1peoj","nt/tqj6ug","nt/c5kmhr","nt/d74pod","nt/rte9nt","nt/oomq0w","nt/8ychvd","nt/ww5wxt","nt/kc7n60","nt/1yyhgu","nt/fyv2en","nt/4rzmv0","nt/a7iv4t","nt/skgyz5","nt/ntjeo3","nt/oco9om"];
  for (const id of noteIds) setNotes(p, id, `${report}\n- ${template}`);

  const pptx = await PresentationFile.exportPptx(p);
  await pptx.save(output);
}

main().catch((error) => { console.error(error); process.exitCode = 1; });
