import { Workbook } from "@oai/artifact-tool";
const wb = Workbook.create();
wb.worksheets.add("Sheet1");
console.log(wb.help("*", { search: "checkbox|check box|form control|boolean cell", include: "index,examples,notes", maxChars: 6000 }).ndjson);
