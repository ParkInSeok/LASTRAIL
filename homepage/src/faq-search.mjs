// Local display search only; never substitutes for a public catalog or API.
const normalize=value=>value.normalize('NFKC').toLocaleLowerCase('ko-KR');
export function searchFaq(entries,query){
 const terms=normalize(query).trim().split(/\s+/).filter(Boolean);
 return entries.flatMap(([question,answer],index)=>{
  const text=normalize(question+' '+answer);
  return terms.every(term=>text.includes(term))?[index]:[];
 });
}
