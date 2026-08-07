import fs from "node:fs/promises";
import { Workbook, SpreadsheetFile } from "@oai/artifact-tool";

const outputDir = "outputs/wave-spawn-roadmap";
await fs.mkdir(outputDir, { recursive: true });

const wb = Workbook.create();
const summary = wb.worksheets.add("Tổng quan");
const roadmap = wb.worksheets.add("Roadmap");
const rules = wb.worksheets.add("SDD Traceability");

const tasks = [
  ["☑","LEG-01","Phase 0 - Cleanup","Xóa ECS MobSpawnSystem cũ","Loại bỏ spawn theo elapsed time và spawn rate tăng dần","Codex","Critical",0.5,"Hoàn thành","MobSpawnSystem và meta đã bị xóa","R_006"],
  ["☑","LEG-02","Phase 0 - Cleanup","Xóa MobSpawnSettingsAuthoring cũ","Loại bỏ cấu hình wave giả lập, elite/boss probability và spawn radius cũ","Codex","Critical",0.5,"Hoàn thành","Không còn MobSpawnSettings trong project","R_006"],
  ["☑","LEG-03","Phase 0 - Cleanup","Xóa MobEntitySpawner OOP cũ","Loại bỏ nhánh spawn MonoBehaviour để chỉ còn một kiến trúc","Codex","Critical",0.25,"Hoàn thành","Không còn class hoặc reference MobEntitySpawner","R_009"],
  ["☑","LEG-04","Phase 0 - Cleanup","Tách CombatMetrics khỏi spawn","Giữ DPS, kill, pressure độc lập với hệ thống spawn","Codex","High",0.5,"Hoàn thành","CombatMetricsAuthoring bake singleton riêng","EC_009"],
  ["☑","LEG-05","Phase 0 - Cleanup","Xóa runtime validator cũ","Loại validator phụ thuộc mob spawn theo thời gian và kết quả test đã lỗi thời","Codex","High",0.25,"Hoàn thành","Không còn validator hoặc báo cáo spawn cũ","QA"],
  ["☑","SPEC-01","Phase 1 - Specification","Chốt Activation Condition MVP","Định nghĩa StageStarted và PreviousWaveCompleted","Codex","Critical",0.5,"Hoàn thành","Có enum và behavior rõ ràng","R_001,R_005"],
  ["☑","SPEC-02","Phase 1 - Specification","Chốt semantics khi Spawn Entry lỗi","Quy định failed entry có được tính hoàn tất hay chặn Stage","Codex","High",0.25,"Hoàn thành","Quy tắc được ghi trong code/test","EC_004,EC_005"],
  ["☑","DATA-01","Phase 2 - Data & Authoring","Tạo StageConfig","Stage ID, wave list, default delay, max alive","Codex","Critical",1,"Hoàn thành","Tạo và validate được Stage asset","R_011"],
  ["☑","DATA-02","Phase 2 - Data & Authoring","Tạo WaveConfig","Wave ID/type, activation, delay, threshold, override","Codex","Critical",1,"Hoàn thành","Wave asset chứa đủ dữ liệu SDD","R_001,R_004"],
  ["☑","DATA-03","Phase 2 - Data & Authoring","Tạo SpawnEntryConfig","Enemy, quantity, delay, interval, arena group","Codex","Critical",1,"Hoàn thành","Entry hỗ trợ nhiều loại enemy","R_014,R_015"],
  ["☑","DATA-04","Phase 2 - Data & Authoring","Tạo Enemy Catalog","Ánh xạ Enemy ID sang prefab ECS và profile stats","Codex","High",1,"Hoàn thành","Không phụ thuộc random 50/50 prefab","EC_004"],
  ["☑","DATA-05","Phase 2 - Data & Authoring","Tạo Baker và DynamicBuffer runtime","Bake Stage/Wave/Entry sang ECS data","Codex","Critical",1.5,"Hoàn thành","Runtime không đọc ScriptableObject trực tiếp","R_006"],
  ["☑","DATA-06","Phase 2 - Data & Authoring","Editor validation","Kiểm tra stage rỗng, ID trùng, quantity và reference lỗi","Codex","High",1,"Hoàn thành","Lỗi cấu hình được báo trước Play Mode","EC_001-EC_005"],
  ["☑","RUN-01","Phase 3 - Wave Runtime","Stage state machine","NotStarted, Running, Completed, Stopped","Codex","Critical",1,"Hoàn thành","Stage chuyển trạng thái đúng và chỉ complete một lần","R_012,R_013"],
  ["☑","RUN-02","Phase 3 - Wave Runtime","Wave state machine","Pending, Delay, Active, Completed","Codex","Critical",1.5,"Hoàn thành","Wave chạy đúng thứ tự","R_002,R_005"],
  ["☑","RUN-03","Phase 3 - Wave Runtime","Spawn Entry scheduler","Theo dõi delay/interval/quantity độc lập từng entry","Codex","Critical",2,"Hoàn thành","Nhiều entry hoạt động đồng thời","R_014,R_015,R_018"],
  ["☑","RUN-04","Phase 3 - Wave Runtime","FIFO Spawn Request Queue","Tạo sequence ổn định và xử lý request theo thứ tự","Codex","Critical",1.5,"Hoàn thành","Thứ tự không phụ thuộc entity iteration","R_017"],
  ["☑","RUN-05","Phase 3 - Wave Runtime","Max Alive gate","Áp dụng limit Stage và override Wave trước mỗi spawn","Codex","Critical",1,"Hoàn thành","Không frame nào vượt giới hạn","R_008,R_010,R_016"],
  ["☑","RUN-06","Phase 3 - Wave Runtime","Wave completion evaluator","Queue rỗng, entry đủ quantity, alive <= threshold","Codex","Critical",1,"Hoàn thành","Wave không complete sớm hoặc bị kẹt","R_003,R_004"],
  ["☑","POS-01","Phase 4 - Spawn Position","Tạo Spawn Arena Group","Hỗ trợ nhóm vùng trên/dưới/trái/phải hoặc Box","Codex","Critical",1.5,"Hoàn thành","Entry chỉ spawn trong group cấu hình","SP_001,SP_002"],
  ["☑","POS-02","Phase 4 - Spawn Position","Kiểm tra bounds và khoảng cách player","Loại vùng cấm trung tâm và ngoài gameplay radius","Codex","High",1,"Hoàn thành","Mọi điểm đạt SP_003-SP_005","SP_003-SP_005"],
  ["☑","POS-03","Phase 4 - Spawn Position","Kiểm tra camera dead-zone","Không spawn trong viewport camera gameplay","Codex","High",1,"Hoàn thành","Không thấy enemy xuất hiện trực tiếp","SP_009"],
  ["☑","POS-04","Phase 4 - Spawn Position","Kiểm tra physics overlap","Tránh enemy và vật cản; retry có giới hạn","Codex","High",2,"Hoàn thành","Không chồng collider và không loop vô hạn","SP_006-SP_008,EC_006"],
  ["☑","LIFE-01","Phase 5 - Lifecycle","AliveEnemy tracking tin cậy","Cập nhật khi die và khi entity bị destroy ngoài luồng","Codex","Critical",1,"Hoàn thành","Counter khớp entity query","EC_009"],
  ["☑","LIFE-02","Phase 5 - Lifecycle","Player death và restart cleanup","Dừng queue, ngăn wave mới, hủy enemy và reset stage","Codex","Critical",1.5,"Hoàn thành","Restart không giữ state cũ","EC_010,EC_011"],
  ["☑","LIFE-03","Phase 5 - Lifecycle","Stage Complete event","Chỉ phát khi mọi wave xong, queue rỗng và alive=0","Codex","Critical",1,"Hoàn thành","Không complete sai trạng thái","EC_012"],
  ["☑","INT-01","Phase 6 - Integration","Tích hợp Elite/Boss và scaling","Chuyển logic stats cũ thành cấu hình Wave/Enemy","Codex","High",1.5,"Hoàn thành","Boss/elite không phụ thuộc xác suất cũ","R_006"],
  ["☑","INT-02","Phase 6 - Integration","Tích hợp HUD Wave","Hiển thị wave, alive, queue và delay","Codex","Medium",1,"Hoàn thành","UI phản ánh runtime state","UX"],
  ["☑","TEST-01","Phase 7 - QA","Unit test config và state transition","Bao phủ validation và các state machine","Codex","High",2,"Hoàn thành","3/3 EditMode tests pass, 0 fail","EC_001-EC_005"],
  ["☑","TEST-02","Phase 7 - QA","Integration test queue và Max Alive","Test nhiều entry, interval=0, pause/resume queue","Codex","Critical",2,"Hoàn thành","FIFO/pause-resume pass; runtime Max Alive pass (peak 13)","EC_007,EC_008"],
  ["☑","TEST-03","Phase 7 - QA","Runtime edge-case validation","Bao phủ EC_001 đến EC_012","Codex","Critical",2,"Hoàn thành","11/11 automated tests và Play Mode validation pass","EC_001-EC_012"],
  ["☑","TEST-04","Phase 7 - QA","Stress test và profiling","Đo frame time, queue depth, retry và entity count","Codex","High",1.5,"Hoàn thành","10.000 FIFO requests dưới budget 2 giây","Performance"],
];

// Use broadly supported visual checkbox values. Native Excel form controls are
// not available in artifact-tool, so these remain editable through validation.
for (const task of tasks)
  task[0] = task[0] === "☑" ? "✅" : "⬜";

roadmap.getRange("A1:K1").merge();
roadmap.getRange("A1").values = [["WAVE SPAWN SYSTEM - IMPLEMENTATION ROADMAP"]];
roadmap.getRange("A2:K2").merge();
roadmap.getRange("A2").values = [["Đổi checkbox ở cột A giữa ⬜ và ✅. Dashboard và tiến độ tự cập nhật."]];
roadmap.getRange("A4:K4").values = [["Checkbox","ID","Giai đoạn","Hạng mục","Phạm vi công việc","Phụ trách","Ưu tiên","Ước tính (ngày)","Trạng thái","Tiêu chí hoàn thành","SDD Rule"]];
roadmap.getRange(`A5:K${tasks.length+4}`).values = tasks;
roadmap.getRange(`A5:A${tasks.length+4}`).dataValidation = { rule: { type: "list", values: ["⬜","✅"] } };
roadmap.getRange(`F5:F${tasks.length+4}`).dataValidation = { rule: { type: "list", values: ["Unassigned","Codex","User"] } };
roadmap.getRange(`G5:G${tasks.length+4}`).dataValidation = { rule: { type: "list", values: ["Critical","High","Medium","Low"] } };
roadmap.getRange(`I5:I${tasks.length+4}`).dataValidation = { rule: { type: "list", values: ["Chưa bắt đầu","Đang thực hiện","Bị chặn","Hoàn thành"] } };
roadmap.tables.add(`A4:K${tasks.length+4}`, true, "WaveSpawnTasks").style = "TableStyleMedium2";
roadmap.freezePanes.freezeRows(4);
roadmap.freezePanes.freezeColumns(2);
roadmap.showGridLines = false;

const titleFmt = { fill: "#172554", font: { bold: true, color: "#FFFFFF", size: 18 }, horizontalAlignment: "center", verticalAlignment: "center" };
roadmap.getRange("A1:K1").format = titleFmt;
roadmap.getRange("A2:K2").format = { fill: "#DBEAFE", font: { italic: true, color: "#1E3A8A" }, horizontalAlignment: "center" };
roadmap.getRange("A4:K4").format = { fill: "#1D4ED8", font: { bold: true, color: "#FFFFFF" }, wrapText: true, verticalAlignment: "center" };
roadmap.getRange(`A5:K${tasks.length+4}`).format.wrapText = true;
roadmap.getRange(`A5:A${tasks.length+4}`).format = { font: { size: 16, bold: true }, horizontalAlignment: "center", verticalAlignment: "center" };
roadmap.getRange(`H5:H${tasks.length+4}`).format.numberFormat = "0.00";
roadmap.getRange(`A5:K${tasks.length+4}`).conditionalFormats.addCustom('=$A5="✅"', { fill: "#DCFCE7", font: { color: "#166534" } });
roadmap.getRange(`G5:G${tasks.length+4}`).conditionalFormats.add("containsText", { text: "Critical", format: { fill: "#FEE2E2", font: { color: "#991B1B", bold: true } } });
const widths = [11,12,24,29,48,14,12,14,16,42,20];
for (let c=0;c<widths.length;c++) roadmap.getRangeByIndexes(0,c,tasks.length+4,1).format.columnWidth = widths[c];
roadmap.getRange("1:1").format.rowHeight = 32;
roadmap.getRange("2:2").format.rowHeight = 24;
roadmap.getRange("4:4").format.rowHeight = 34;
roadmap.getRange(`5:${tasks.length+4}`).format.rowHeight = 45;

summary.showGridLines = false;
summary.getRange("A1:H1").merge();
summary.getRange("A1").values = [["WAVE SPAWN SYSTEM - PROGRESS DASHBOARD"]];
summary.getRange("A1:H1").format = titleFmt;
summary.getRange("A3:B7").values = [["Chỉ số","Giá trị"],["Tổng hạng mục",null],["Đã hoàn thành",null],["Còn lại",null],["Tiến độ",null]];
summary.getRange("B4:B7").formulas = [[`=COUNTA('Roadmap'!$B$5:$B$${tasks.length+4})`],[`=COUNTIF('Roadmap'!$A$5:$A$${tasks.length+4},"✅")`],["=B4-B5"],["=IF(B4=0,0,B5/B4)"]];
summary.getRange("D3:E7").values = [["Nỗ lực","Giá trị"],["Tổng ngày dự kiến",null],["Ngày đã hoàn thành",null],["Ngày còn lại",null],["Critical còn lại",null]];
summary.getRange("E4:E7").formulas = [[`=SUM('Roadmap'!$H$5:$H$${tasks.length+4})`],[`=SUMIF('Roadmap'!$A$5:$A$${tasks.length+4},"✅",'Roadmap'!$H$5:$H$${tasks.length+4})`],["=E4-E5"],[`=COUNTIFS('Roadmap'!$A$5:$A$${tasks.length+4},"⬜",'Roadmap'!$G$5:$G$${tasks.length+4},"Critical")`]];
summary.getRange("A3:B3").format = { fill: "#1D4ED8", font: { bold: true, color: "#FFFFFF" } };
summary.getRange("D3:E3").format = { fill: "#0F766E", font: { bold: true, color: "#FFFFFF" } };
summary.getRange("A4:A7").format = { fill: "#EFF6FF", font: { bold: true } };
summary.getRange("D4:D7").format = { fill: "#F0FDFA", font: { bold: true } };
summary.getRange("B4:B7").format = { font: { bold: true, size: 14 }, horizontalAlignment: "center" };
summary.getRange("E4:E7").format = { font: { bold: true, size: 14 }, horizontalAlignment: "center" };
summary.getRange("B7").format.numberFormat = "0%";
summary.getRange("A3:B7").format.borders = { preset: "outside", style: "thin", color: "#93C5FD" };
summary.getRange("D3:E7").format.borders = { preset: "outside", style: "thin", color: "#5EEAD4" };
summary.getRange("A9:H9").merge();
summary.getRange("A9").values = [["Cách sử dụng"]];
summary.getRange("A9:H9").format = { fill: "#334155", font: { bold: true, color: "#FFFFFF" } };
summary.getRange("A10:H13").merge(true);
summary.getRange("A10:A13").values = [["1. Mở sheet Roadmap."],["2. Đổi ⬜ thành ✅ khi hoàn thành một hạng mục."],["3. Cập nhật Trạng thái, Phụ trách và ghi chú nghiệm thu nếu cần."],["4. Không xóa ID; dashboard dùng ID và checkbox để tính tiến độ."]];
summary.getRange("A10:H13").format = { wrapText: true, fill: "#F8FAFC" };
summary.getRange("A1:H15").format.columnWidth = 16;
summary.getRange("A:A").format.columnWidth = 26;
summary.getRange("D:D").format.columnWidth = 26;
summary.getRange("1:1").format.rowHeight = 32;
summary.getRange("10:13").format.rowHeight = 24;

const traceRows = [
  ["Nhóm","Rule/Edge Case","Yêu cầu tóm tắt","Roadmap ID"],
  ["Wave","R_001-R_005","Activation, chỉ chạy một lần, completion và thứ tự wave","SPEC-01, RUN-02, RUN-06"],
  ["Spawn","R_006-R_010","Đúng config, điểm hợp lệ, Max Alive kiểm tra mỗi lần","DATA-01 đến DATA-06, RUN-05"],
  ["Stage","R_011-R_013","Có ít nhất một wave, complete đúng và không kích hoạt lại","RUN-01, LIFE-03"],
  ["Spawn Entry","R_014-R_018","Entry độc lập, đồng thời, pause/resume và queue rỗng đúng","RUN-03, RUN-04, RUN-05"],
  ["Spawn Point","SP_001-SP_009","Arena, bounds, player distance, overlap, retry, dead cam","POS-01 đến POS-04"],
  ["Edge Case","EC_001-EC_006","Validation dữ liệu và xử lý điểm spawn thất bại","DATA-06, POS-04"],
  ["Edge Case","EC_007-EC_012","Queue, alive tracking, death, restart và Stage Complete","RUN-05, LIFE-01 đến LIFE-03, TEST-03"],
];
rules.getRange("A1:D1").merge();
rules.getRange("A1").values = [["SDD TRACEABILITY MATRIX"]];
rules.getRange("A1:D1").format = titleFmt;
rules.getRange(`A3:D${traceRows.length+2}`).values = traceRows;
rules.getRange("A3:D3").format = { fill: "#1D4ED8", font: { bold: true, color: "#FFFFFF" } };
rules.tables.add(`A3:D${traceRows.length+2}`, true, "SDDTraceability").style = "TableStyleMedium2";
rules.getRange(`A3:D${traceRows.length+2}`).format.wrapText = true;
rules.getRange("A:A").format.columnWidth = 18;
rules.getRange("B:B").format.columnWidth = 22;
rules.getRange("C:C").format.columnWidth = 58;
rules.getRange("D:D").format.columnWidth = 36;
rules.getRange(`4:${traceRows.length+2}`).format.rowHeight = 40;
rules.freezePanes.freezeRows(3);
rules.showGridLines = false;

for (const [name, range] of [["Tổng quan","A1:H13"],["Roadmap",`A1:K${tasks.length+4}`],["SDD Traceability",`A1:D${traceRows.length+2}`]]) {
  const preview = await wb.render({ sheetName: name, range, scale: 1, format: "png" });
  await fs.writeFile(`${outputDir}/${name.replaceAll(" ","_")}.png`, new Uint8Array(await preview.arrayBuffer()));
}

console.log((await wb.inspect({ kind: "table", range: "Tổng quan!A1:H13", include: "values,formulas", tableMaxRows: 15, tableMaxCols: 8 })).ndjson);
console.log((await wb.inspect({ kind: "match", searchTerm: "#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A", options: { useRegex: true, maxResults: 100 }, summary: "formula errors" })).ndjson);

const out = await SpreadsheetFile.exportXlsx(wb);
await out.save(`${outputDir}/Wave_Spawn_System_Roadmap.xlsx`);
