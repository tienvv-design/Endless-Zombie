import fs from "node:fs/promises";
import { SpreadsheetFile, Workbook } from "@oai/artifact-tool";

const outputDir = "D:/Project/Endless zombie/.artifact-work/output";
const workbook = Workbook.create();
await fs.mkdir(outputDir, { recursive: true });
const sheet = workbook.worksheets.add("Combat Roadmap");
sheet.showGridLines = false;

const rows = [
  ["Phase 1", "Player đứng cố định ở trung tâm", "☑", "Hoàn thành", "CharacterLogic đã khóa input di chuyển", "MVP Phase 1", "Kiểm tra vị trí spawn trong Play Mode"],
  ["Phase 1", "Một Gun với bộ chỉ số runtime", "☑", "Hoàn thành", "Đã có Base Stats, Modifiers và Current Stats", "Gun Stats", "Tách thành GunConfig ScriptableObject ở sprint sau"],
  ["Phase 1", "Auto-target zombie gần nhất trong range", "☑", "Hoàn thành", "PlayerAutoAttackSystem chọn mục tiêu gần nhất", "Target Selection", "Tối ưu spatial query khi mật độ zombie cao"],
  ["Phase 1", "Auto-fire theo fire interval", "☑", "Hoàn thành", "Cooldown tính từ Shots Per Second", "Gun Attack Flow", "Tinh chỉnh nhịp bắn"],
  ["Phase 1", "Projectile bay thẳng theo hướng snapshot", "☑", "Hoàn thành", "Không còn homing theo Entity Target", "Projectile Spawn", "Thay model kiếm bằng bullet prefab"],
  ["Phase 1", "Projectile tự hủy khi hết range", "☑", "Hoàn thành", "Có RemainingRange", "Projectile Layer", "Play Mode test ở tốc độ cao"],
  ["Phase 1", "Damage snapshot khi projectile spawn", "☑", "Hoàn thành", "Damage được lưu trên từng projectile", "Damage Formula", "Bổ sung damage multiplier theo weapon archetype"],
  ["Phase 1", "Critical hit snapshot một lần khi bắn", "☑", "Hoàn thành", "Có CriticalChance, CriticalDamage và IsCritical", "Critical Hit", "Thêm VFX/màu damage critical"],
  ["Phase 1", "Knockback và resistance", "☑", "Hoàn thành", "Mob nhận knockback và clamp trong combat radius", "Knockback", "Cân bằng Elite/Boss resistance"],
  ["Phase 1", "Zombie tiến về Player", "☑", "Hoàn thành", "Tái sử dụng ChaseTarget và UnitMover", "Enemy Movement", "Kiểm tra crowd congestion"],
  ["Phase 1", "Zombie dừng và đánh theo cooldown", "☑", "Hoàn thành", "Không còn tự hủy sau khi chạm Player", "Enemy Attack", "Thêm animation attack"],
  ["Phase 1", "Wave scaling HP và Damage", "☑", "Hoàn thành", "Wave 60 giây; HP x1.08; Damage x1.05", "Enemy Scaling", "Chuyển hệ số sang config asset"],
  ["Economy", "Zombie chết rơi vàng", "☑", "Hoàn thành", "Prefab XPCoin đang được tái sử dụng làm Gold pickup", "Gold Loop", "Đổi tên asset/code XP thành Gold"],
  ["Economy", "Ví vàng runtime", "☑", "Hoàn thành", "GoldWallet hỗ trợ Add và TrySpend", "Gold Loop", "Thêm lưu/khôi phục nếu cần"],
  ["Economy", "Mở menu nâng cấp khi đủ vàng", "☑", "Hoàn thành", "Shop cơ bản mở theo ngưỡng vàng", "Upgrade Loop", "Tách shop khỏi LevelUp state cũ"],
  ["Economy", "Upgrade dùng modifier cộng dồn", "☑", "Hoàn thành", "Damage, fire rate, range và projectile speed dùng GunModifiers", "Stacking Rule", "Thêm preview trước/sau"],
  ["Economy", "UI hiển thị số vàng", "☑", "Hoàn thành", "GoldCounter được tạo tự động trên HUD và cập nhật theo GoldWallet", "Upgrade Loop", "Kiểm tra vị trí và độ tương phản trong Play Mode"],
  ["Economy", "Giá riêng và cost curve cho từng upgrade", "☑", "Hoàn thành", "Mỗi upgrade có BaseCost, CostGrowth và level độc lập", "Upgrade Pool", "Cân bằng giá sau Play Mode test"],
  ["Phase 2", "Nhiều projectile mỗi lần bắn", "☑", "Hoàn thành logic", "ProjectileCount và spread đã hoạt động", "Build Variety", "Thêm upgrade Additional Projectile"],
  ["Phase 2", "Spread theo góc", "☑", "Hoàn thành logic", "Phân bố đều quanh hướng target", "Build Variety", "Thêm config riêng theo loại súng"],
  ["Phase 2", "Pierce và Hit List", "☑", "Hoàn thành logic", "Projectile không đánh lại cùng zombie", "Projectile Hit Flow", "Thêm upgrade Pierce"],
  ["Phase 2", "Magazine và Reload", "☑", "Hoàn thành logic", "Auto-fire trừ đạn và tự reload; Pistol 12/1.2s, Shotgun 6/1.8s, AR 30/1.6s", "Build Variety", "Thêm ammo/reload HUD và animation"],
  ["Phase 2", "Nhiều loại Gun", "☑", "Hoàn thành logic", "Đã có GunConfig cho Pistol, Shotgun và Assault Rifle; scene mặc định Pistol", "Weapon Profiles", "Thêm UI/chọn súng trước trận"],
  ["Phase 2", "Upgrade rarity", "☑", "Hoàn thành logic", "Upgrade có rarity, roll weight, cost multiplier và UI màu; shop quay weighted không trùng", "Build Variety", "Cân bằng tỷ lệ và thêm upgrade pool lớn hơn"],
  ["Phase 2", "Elite Enemy", "☑", "Hoàn thành logic", "Elite spawn từ wave 2 với chance tăng dần; HP x4, damage x2, kháng 75%, scale x1.4 và vàng x5", "Build Variety", "Play Mode cân bằng chance và chỉ số Elite"],
  ["Phase 3", "Explosive projectile", "☑", "Hoàn thành logic", "Rocket Launcher nổ khi va chạm, gây AoE damage và radial knockback; tái sử dụng explosion VFX", "Advanced Combat", "Play Mode cân bằng radius/damage và thay rocket model"],
  ["Phase 3", "Ricochet", "☑", "Hoàn thành logic", "Projectile tìm zombie gần nhất chưa bị trúng, đổi hướng tại impact và giữ hit list; RicochetSMG nảy 2 lần", "Advanced Combat", "Thêm trail/VFX khi đổi hướng và Play Mode cân bằng range"],
  ["Phase 3", "Chain Lightning", "☑", "Hoàn thành logic", "TeslaGun truyền tối đa 4 mục tiêu gần nhất chưa bị trúng; damage giảm 25% mỗi hop và có line VFX", "Advanced Combat", "Play Mode cân bằng chain range/falloff và hoàn thiện lightning material"],
  ["Phase 3", "Elemental effects", "☑", "Hoàn thành logic", "Fire gây burn theo tick; Frost giảm move speed có duration và tự khôi phục; có FlameRifle/CryoGun configs", "Advanced Combat", "Thêm status VFX/icon và cân bằng proc chance/duration"],
  ["Phase 3", "Gun và Skill synergy", "☑", "Hoàn thành logic", "Epic WeaponSynergy upgrade thích nghi theo gun: buff explosion, ricochet, chain hoặc elemental effect", "Advanced Combat", "Thêm mô tả preview riêng theo súng đang trang bị"],
  ["Phase 3", "Boss phase và Crowd Control resistance", "☑", "Hoàn thành logic", "Boss spawn mỗi 5 wave, có 3 phase theo HP; kháng knockback 90%, CC 85%, phase sau tăng speed/damage", "Advanced Combat", "Thêm boss health bar, phase VFX và Play Mode cân bằng"],
  ["Quality", "Combat metrics: DPS, TTK, pressure", "☑", "Hoàn thành logic", "Runtime HUD hiển thị DPS 1s, Avg TTK, active/near enemies, weighted pressure và kills", "Core Balance Metrics", "Play Mode ghi baseline cân bằng cho từng gun"],
  ["Quality", "Play Mode runtime validation", "☑", "Hoàn thành", "GameScene chạy 35s không Error/Exception; xác nhận spawn, auto-fire, damage, kill, ammo/reload và metrics", "QA", "Tiếp tục manual playtest để cân bằng và hoàn thiện presentation"]
];

sheet.getRange("A1:G1").merge();
sheet.getRange("A1").values = [["ENDLESS ZOMBIE — COMBAT & WEAPON ROADMAP"]];
sheet.getRange("A2:G2").merge();
sheet.getRange("A2").values = [["Theo System Design Document — cập nhật trạng thái sau Phase 1 vertical slice"]];

sheet.getRange("A4:B4").values = [["Tổng hạng mục", "Đã hoàn thành"]];
sheet.getRange("A5").formulas = [["=COUNTA(B8:B40)"]];
sheet.getRange("B5").formulas = [["=COUNTIF(C8:C40,\"☑\")"]];
sheet.getRange("C4:D4").values = [["Tiến độ", "Cập nhật"]];
sheet.getRange("C5").formulas = [["=B5/A5"]];
sheet.getRange("D5").values = [[new Date("2026-08-05T00:00:00")]];

sheet.getRange("A7:G7").values = [["Phase", "Hạng mục roadmap", "Check", "Trạng thái", "Hiện trạng triển khai", "Nguồn thiết kế", "Bước tiếp theo"]];
sheet.getRange(`A8:G${7 + rows.length}`).values = rows;

sheet.getRange("I2:K2").merge();
sheet.getRange("I2").values = [["TIẾN ĐỘ THEO NHÓM"]];
sheet.getRange("I3:K3").values = [["Nhóm", "Đã xong", "Tổng"]];
const phases = ["Phase 1", "Economy", "Phase 2", "Phase 3", "Quality"];
sheet.getRange("I4:I8").values = phases.map(x => [x]);
for (let r = 4; r <= 8; r++) {
  sheet.getRange(`J${r}`).formulas = [[`=COUNTIFS($A$8:$A$40,I${r},$C$8:$C$40,"☑")`]];
  sheet.getRange(`K${r}`).formulas = [[`=COUNTIF($A$8:$A$40,I${r})`]];
}

sheet.freezePanes.freezeRows(7);
sheet.getRange("D5").format.numberFormat = "yyyy-mm-dd";
sheet.getRange("C5").format.numberFormat = "0%";
sheet.getRange("C8:C40").dataValidation = { rule: { type: "list", values: ["☑", "☐"] } };
sheet.getRange("D8:D40").dataValidation = { rule: { type: "list", values: ["Hoàn thành", "Hoàn thành logic", "Đang làm", "Chờ kiểm tra", "Chưa làm"] } };

sheet.getRange("A1:G1").format = { fill: "#172554", font: { bold: true, color: "#FFFFFF", size: 18 }, horizontalAlignment: "center", verticalAlignment: "center" };
sheet.getRange("A2:G2").format = { fill: "#DBEAFE", font: { italic: true, color: "#1E3A8A" }, horizontalAlignment: "center" };
sheet.getRange("A4:D4").format = { fill: "#334155", font: { bold: true, color: "#FFFFFF" }, horizontalAlignment: "center" };
sheet.getRange("A5:D5").format = { fill: "#F1F5F9", font: { bold: true, color: "#0F172A", size: 12 }, horizontalAlignment: "center", borders: { preset: "outside", style: "thin", color: "#94A3B8" } };
sheet.getRange("A7:G7").format = { fill: "#1D4ED8", font: { bold: true, color: "#FFFFFF" }, horizontalAlignment: "center", verticalAlignment: "center", wrapText: true };
sheet.getRange(`A8:G${7 + rows.length}`).format = { verticalAlignment: "center", wrapText: true, borders: { insideHorizontal: { style: "thin", color: "#E2E8F0" } } };
sheet.getRange(`C8:C${7 + rows.length}`).format = { horizontalAlignment: "center", font: { size: 15, bold: true } };
sheet.getRange(`D8:D${7 + rows.length}`).format = { horizontalAlignment: "center" };
sheet.getRange("I2:K2").format = { fill: "#0F766E", font: { bold: true, color: "#FFFFFF" }, horizontalAlignment: "center" };
sheet.getRange("I3:K3").format = { fill: "#CCFBF1", font: { bold: true, color: "#134E4A" }, horizontalAlignment: "center" };
sheet.getRange("I4:K8").format = { borders: { preset: "all", style: "thin", color: "#CBD5E1" } };
sheet.getRange("J4:K8").format.horizontalAlignment = "center";

for (let i = 0; i < rows.length; i++) {
  const excelRow = i + 8;
  if (rows[i][2] === "☑") {
    sheet.getRange(`A${excelRow}:G${excelRow}`).format.fill = "#ECFDF5";
    sheet.getRange(`C${excelRow}`).format.font = { size: 15, bold: true, color: "#047857" };
  } else {
    sheet.getRange(`A${excelRow}:G${excelRow}`).format.fill = "#FFF7ED";
    sheet.getRange(`C${excelRow}`).format.font = { size: 15, bold: true, color: "#C2410C" };
  }
}

sheet.getRange("A1:K40").format.font.name = "Aptos";
sheet.getRange("A1:G1").format.rowHeight = 32;
sheet.getRange("A2:G2").format.rowHeight = 24;
sheet.getRange("A7:G7").format.rowHeight = 34;
sheet.getRange("A8:G40").format.rowHeight = 42;
sheet.getRange("A:A").format.columnWidth = 13;
sheet.getRange("B:B").format.columnWidth = 36;
sheet.getRange("C:C").format.columnWidth = 9;
sheet.getRange("D:D").format.columnWidth = 18;
sheet.getRange("E:E").format.columnWidth = 42;
sheet.getRange("F:F").format.columnWidth = 22;
sheet.getRange("G:G").format.columnWidth = 38;
sheet.getRange("H:H").format.columnWidth = 3;
sheet.getRange("I:I").format.columnWidth = 16;
sheet.getRange("J:K").format.columnWidth = 12;

const preview = await workbook.render({ sheetName: "Combat Roadmap", range: "A1:K40", scale: 1, format: "png" });
await fs.writeFile(`${outputDir}/roadmap-preview.png`, new Uint8Array(await preview.arrayBuffer()));

const check = await workbook.inspect({ kind: "table", range: "Combat Roadmap!A1:K40", include: "values,formulas", tableMaxRows: 40, tableMaxCols: 11, maxChars: 8000 });
await fs.writeFile(`${outputDir}/inspection.ndjson`, check.ndjson, "utf8");
const errors = await workbook.inspect({ kind: "match", searchTerm: "#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A", options: { useRegex: true, maxResults: 100 }, summary: "formula error scan" });
await fs.writeFile(`${outputDir}/errors.ndjson`, errors.ndjson, "utf8");

const output = await SpreadsheetFile.exportXlsx(workbook);
await output.save(`${outputDir}/Combat_Weapon_Roadmap.xlsx`);
