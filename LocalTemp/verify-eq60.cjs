
const fs=require('fs'),path=require('path');
const {chromium}=require('C:/Users/dlstj/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root='D:/Project/LASTRAIL/QA/DirectionImpact-20260921/equipment-composition-v60-review';
const checks=[],issues=[];
function check(name,value){checks.push({name,pass:!!value});if(!value)issues.push(name)}
(async()=>{
 const browser=await chromium.launch({headless:true,executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',args:['--allow-file-access-from-files']});
 try{
 for(const width of [360,430,720]){
  for(const mode of ['equipment','inventory','expanded','merchant']){
   const page=await browser.newPage({viewport:{width,height:900}});
   const errors=[];page.on('pageerror',e=>errors.push(e.message));page.on('requestfailed',r=>errors.push(r.url()+': '+r.failure()?.errorText));
   await page.goto('file:///'+root+'/'+mode+'.html');await page.waitForLoadState('load');
   check(mode+'-'+width+' no errors',errors.length===0);
   check(mode+'-'+width+' no page x overflow',await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1));
   check(mode+'-'+width+' DOM separate icon/badge/text',await page.locator('.gear-art').count()>0&&await page.locator('.set-badge').count()>0);
   if(mode==='equipment'){
    check('five owner portraits '+width,await page.locator('.operators .owner').count()===5);
    check('three owner slots '+width,await page.locator('.slots>.gear-card').count()===3);
    check('equipped badge visible '+width,await page.locator('.slots .set-badge').count()===2);
    check('variable tower horizontal overflow '+width,await page.locator('.tower-scroll').evaluate(e=>e.scrollWidth>e.clientWidth));
    await page.locator('[data-id="요원 2"]').click();
    check('unbound owner does not copy tower slots '+width,await page.locator('.slots').innerText().then(t=>t.includes('미연결')));
    await page.locator('[data-id="타워 1"]').click();
   }
   if(mode==='inventory'||mode==='expanded'){
    check('capacity fixture '+mode+'-'+width,await page.evaluate(()=>EQ_REVIEW.snapshot().used===(document.body.dataset.mode==='expanded'?54:30)));
    check('internal y scroll '+mode+'-'+width,await page.locator('.inventory-scroll').evaluate(e=>e.scrollHeight>e.clientHeight));
    const scroll=page.locator('.inventory-scroll');await scroll.evaluate(e=>e.scrollTop=150);
    const before=await scroll.evaluate(e=>e.scrollTop);
    await page.locator('[data-owned-unused]').count(); // no mutation
    await page.locator('[data-action="detail"][data-id="owned-1"]').evaluate(e=>e.click());
    await page.locator('[data-action="discard"]').click();
    check('one discard one item '+mode+'-'+width,await page.evaluate(()=>EQ_REVIEW.snapshot().used===(document.body.dataset.mode==='expanded'?53:29)));
    check('scroll retained after discard '+mode+'-'+width,Math.abs(await scroll.evaluate(e=>e.scrollTop)-before)<1);
    check('empty cells at list end '+mode+'-'+width,await page.locator('.gear-grid').first().evaluate(e=>{let empty=false;return [...e.children].every(c=>{if(c.classList.contains('empty-cell')){empty=true;return true}return !empty})}));
    await page.locator('[data-action="detail"][data-id="owned-8"]').evaluate(e=>e.click());
    check('locked discard disabled '+mode+'-'+width,await page.locator('[data-action="discard"]').isDisabled());
    await page.locator('[data-action="close-detail"]').click();
    if(mode==='inventory'){
     await page.locator('[data-action="claim"]').first().evaluate(e=>e.click());
     check('direct claim single reward '+width,await page.evaluate(()=>EQ_REVIEW.snapshot().used===30&&EQ_REVIEW.snapshot().pending===1));
     check('scroll retained after claim '+width,Math.abs(await scroll.evaluate(e=>e.scrollTop)-before)<1);
     check('full blocks remaining claim '+width,await page.locator('[data-action="claim"]').first().isDisabled());
    }
   }
   if(mode==='merchant'){
    check('three full-inventory gear purchases blocked '+width,await page.locator('.product:not(.service) button:disabled').count()===3);
    check('two service buttons remain enabled '+width,await page.locator('.product.service button:not(:disabled)').count()===2);
    const scroll=page.locator('.merchant-stock');await scroll.evaluate(e=>e.scrollTop=100);const before=await scroll.evaluate(e=>e.scrollTop);
    await page.locator('[data-action="storage"]').click();
    check('merchant storage no reward panel '+width,await page.locator('.storage-modal .pending-section').count()===0);
    await page.locator('.storage-modal [data-action="detail"][data-id="owned-1"]').evaluate(e=>e.click());
    await page.locator('[data-action="discard"]').click();
    await page.locator('[data-action="close-modal"]').click();
    check('same host stock count '+width,await page.locator('.product').count()===5&&await page.locator('.balance').innerText().then(t=>t.includes('40')));
    check('space re-evaluates gear availability '+width,await page.locator('.product:not(.service) button:not(:disabled)').count()===3);
    check('merchant scroll retained '+width,Math.abs(await scroll.evaluate(e=>e.scrollTop)-before)<1);
   }
   const badge=page.locator('.set-badge').first();
   const centered=await badge.evaluate(e=>{const a=e.getBoundingClientRect(),b=e.querySelector('svg').getBoundingClientRect();return Math.abs((a.x+a.width/2)-(b.x+b.width/2))<1&&Math.abs((a.y+a.height/2)-(b.y+b.height/2))<1});
   check('badge centered '+mode+'-'+width,centered);
   await page.locator('[data-layer="badges"]').click();
   check('badge independently hides '+mode+'-'+width,await badge.evaluate(e=>getComputedStyle(e).visibility==='hidden'));
   await page.locator('[data-layer="badges"]').click();
   await page.screenshot({path:root+'/'+mode+'-'+width+'.png',fullPage:true});
   await page.close();
  }
 }
 const qa={version:'eq60-composition-browser-20261002',checks,passed:checks.filter(c=>c.pass).length,total:checks.length,issues,scope:'headless Chrome actual browser DOM/render captures at 360/430/720; mouse click + simulated scroll; no physical device touch or drag/synthesis/economy/runtime',actualDeviceTouchRuntime:'NOT RUN',wholeTaskComplete:false};
 fs.writeFileSync(root+'/qa.json',JSON.stringify(qa,null,2));
 console.log(JSON.stringify({passed:qa.passed,total:qa.total,issues}));
 if(issues.length)process.exitCode=1;
 }finally{await browser.close()}
})().catch(e=>{console.error(e);process.exitCode=1});

