
const fs=require('fs');const {chromium}=require('C:/Users/dlstj/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root="D:/Project/LASTRAIL/QA/DirectionImpact-20260921/equipment-drag-independent-v62-review",checks=[],issues=[];const check=(name,v)=>{checks.push({name,pass:!!v});if(!v)issues.push(name)};
(async()=>{const browser=await chromium.launch({headless:true,executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',args:['--allow-file-access-from-files']});
try{for(const width of [360,430,720]){
 const page=await browser.newPage({viewport:{width,height:1050}});const errors=[];page.on('pageerror',e=>errors.push(e.message));page.on('requestfailed',r=>errors.push(r.url()));
 for(const mode of ['ready','valid','invalid']){
  await page.goto('file:///'+root+'/'+mode+'.html');
  check(mode+'-'+width+' no errors',errors.length===0);
  check(mode+'-'+width+' no x overflow',await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1));
  check(mode+'-'+width+' eight owned cards',await page.locator('#screen [data-item]').count()===8);
  check(mode+'-'+width+' empty cells at end',await page.locator('.drag-grid').evaluate(e=>{let empty=false;return [...e.children].every(x=>{if(x.classList.contains('empty-cell')){empty=true;return true}return !empty})}));
  if(mode!=='ready'){check(mode+'-'+width+' ghost visible',await page.locator('.drag-ghost').count()===1);check(mode+'-'+width+' original dim',await page.locator('.moving').count()===1);check(mode+'-'+width+' non-consuming preview',await page.evaluate(()=>DRAG_REVIEW.snapshot().used===8));check(mode+'-'+width+' ghost inside width',await page.locator('.drag-ghost').evaluate(e=>{const r=e.getBoundingClientRect();return r.left>=0&&r.right<=innerWidth}));}
  await page.screenshot({path:root+'/'+mode+'-'+width+'.png',fullPage:true});
 }
 await page.goto('file:///'+root+'/ready.html');
 const a=await page.locator('#screen [data-item="fixture-source"]').boundingBox(),b=await page.locator('#screen [data-item="fixture-target"]').boundingBox(),l=await page.locator('#screen [data-item="locked"]').boundingBox();
 await page.mouse.move(a.x+a.width/2,a.y+a.height/2);await page.mouse.down();await page.mouse.move(b.x+b.width/2,b.y+b.height/2,{steps:7});
 check('mouse ghost identity '+width,await page.locator('.drag-ghost').innerText().then(t=>t.includes('강화 장갑')&&t.includes('★★')));
 check('valid receiver highlights '+width,await page.locator('#screen [data-item="fixture-target"]').getAttribute('class').then(t=>t.includes('target-yes')));
 check('different tag allowed '+width,await page.locator('#screen [data-item="fixture-target"] .reason').innerText().then(t=>t.includes('3성')));
 await page.mouse.up();
 check('one drop one preview '+width,await page.locator('.preview-veil').count()===1&&await page.evaluate(()=>DRAG_REVIEW.snapshot().previews===1));
 check('no material consumed '+width,await page.evaluate(()=>DRAG_REVIEW.snapshot().used===8));
 await page.locator('[data-preview="cancel"]').click();
 check('cancel cleans ghost/reason '+width,await page.locator('.drag-ghost,.preview-veil,#screen .reason').count()===0);
 await page.mouse.move(a.x+a.width/2,a.y+a.height/2);await page.mouse.down();await page.mouse.move(l.x+l.width/2,l.y+l.height/2,{steps:7});await page.mouse.up();
 check('locked receiver no preview '+width,await page.locator('.preview-veil').count()===0&&await page.evaluate(()=>DRAG_REVIEW.snapshot().previews===1));
 await page.mouse.move(l.x+l.width/2,l.y+l.height/2);await page.mouse.down();await page.mouse.move(a.x+a.width/2,a.y+a.height/2,{steps:7});await page.mouse.up();
 check('locked source no ghost '+width,await page.locator('.drag-ghost').count()===0);
 await page.mouse.move(a.x+a.width/2,a.y+a.height/2);await page.mouse.down();await page.mouse.move(b.x+b.width/2,b.y+b.height/2);await page.keyboard.press('Escape');await page.mouse.up();
 check('escape cancels no additional preview '+width,await page.locator('.drag-ghost,.preview-veil').count()===0&&await page.evaluate(()=>DRAG_REVIEW.snapshot().previews===1));
 await page.mouse.move(a.x+a.width/2,a.y+a.height/2);await page.mouse.down();await page.mouse.move(b.x+b.width/2,b.y+b.height/2);await page.evaluate(()=>document.dispatchEvent(new PointerEvent('pointercancel')));await page.mouse.up();
 check('pointer cancellation clears '+width,await page.locator('.drag-ghost,.preview-veil').count()===0);
 await page.evaluate(()=>DRAG_REVIEW.dispose());await page.mouse.move(a.x+a.width/2,a.y+a.height/2);await page.mouse.down();await page.mouse.move(b.x+b.width/2,b.y+b.height/2);await page.mouse.up();
 check('unbind no further calls '+width,await page.locator('.drag-ghost,.preview-veil').count()===0&&await page.evaluate(()=>!DRAG_REVIEW.snapshot().alive&&DRAG_REVIEW.snapshot().previews===1));
 await page.close();
}
const q={checks,passed:checks.filter(x=>x.pass).length,total:checks.length,issues,scope:'actual headless Chrome mouse input and rendered fixture variants 360/430/720; physical touch/native game/economy NOT RUN',wholeTaskComplete:false};fs.writeFileSync(root+'/qa.json',JSON.stringify(q,null,2));console.log(JSON.stringify({passed:q.passed,total:q.total,issues}));if(issues.length)process.exitCode=1;
}finally{await browser.close()}})().catch(e=>{console.error(e);process.exitCode=1});

