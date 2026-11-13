// User-approved unified artwork; original PNG and independent source assets preserved.
// Mobile crop matches the approved 55% center composition. UI remains separate HTML.
export function heroScene(){return `<div class="home-scene" aria-hidden="true">
<picture>
<source media="(max-width:600px)" type="image/webp" srcset="/art/lastrail-keyvisual-v01-mobile-480.webp 480w, /art/lastrail-keyvisual-v01-mobile-960.webp 960w" sizes="90vw" width="960" height="720">
<source type="image/webp" srcset="/art/lastrail-keyvisual-v01-1280.webp 1280w, /art/lastrail-keyvisual-v01-1840.webp 1840w" sizes="90vw">
<img class="home-keyvisual" src="/art/lastrail-keyvisual-v01.png" alt="" width="1840" height="855" fetchpriority="high">
</picture></div>`;}