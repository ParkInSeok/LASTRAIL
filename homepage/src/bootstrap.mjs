import {HomepageController} from './controller.mjs';
import {UnconnectedPublicSource} from './read-source.mjs';
import {ReadingModel,UnavailableReadingPort} from './reading-model.mjs';
import {parseReadingLocation} from './reading-route.mjs';
const route=parseReadingLocation(new URL(location.href));
let port=new UnavailableReadingPort();
if(route.qa){
 const {QaReadingPort}=await import('/__qa/reading-source.mjs');
 port=new QaReadingPort(route.qa);
}
const controller=new HomepageController(document.querySelector('#app'),new UnconnectedPublicSource(),new ReadingModel(port));
controller.bind();
window.addEventListener('pagehide',()=>controller.unbind());
window.addEventListener('pageshow',event=>{if(event.persisted)controller.bind();});

