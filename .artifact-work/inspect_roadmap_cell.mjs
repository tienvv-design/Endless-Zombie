import { FileBlob, SpreadsheetFile } from "@oai/artifact-tool";
const file = await FileBlob.load("outputs/wave-spawn-roadmap/Wave_Spawn_System_Roadmap.xlsx");
const wb = await SpreadsheetFile.importXlsx(file);
console.log((await wb.inspect({ kind: "table", range: "Roadmap!A4:I12", include: "values,formulas", tableMaxRows: 12, tableMaxCols: 9 })).ndjson);
