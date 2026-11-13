import {expect,it} from 'vitest';
import {renderToStaticMarkup} from 'react-dom/server';
import {GameLandscapeReview} from './GameLandscapeReview';
it('shows all-game scope and unverified evidence without turning web orientation or build success into approval',()=>{
 const html=renderToStaticMarkup(<GameLandscapeReview/>);
 for(const value of ['전체 게임 화면 가로 검수','로그인','로비','전투','팝업','긴 가로','태블릿','3회','미확인','외부 OS','웹 반응형'])expect(html).toContain(value);
 expect(html).not.toContain('<input');expect(html).not.toContain('<button');
});
