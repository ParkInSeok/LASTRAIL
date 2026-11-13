import type initSqlJs from "sql.js";
/** Explicit database time for synthetic historical combat fixtures. */
export function setCombatSqlClock(sql:initSqlJs.Database,now:()=>number){
 sql.create_function("strftime",(format:string,source:string)=>{
  if(source!=="now")throw new Error("Unexpected clock source");
  const at=now();
  if(format==="%s")return String(Math.floor(at/1000));
  if(format==="%f")return String(Math.floor(at/1000)%60).padStart(2,"0")+"."+String(at%1000).padStart(3,"0");
  throw new Error("Unexpected clock format");
 });
}
