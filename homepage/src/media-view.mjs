import {escapeHtml as e} from './html.mjs';
export function gameplayMedia(config){
 if(!config)return '';
 return `<figure class="gameplay-media"><video data-gameplay width="1920" height="1080" muted loop playsinline preload="none" poster="${e(config.poster)}" src="${e(config.src)}" aria-label="LAST RAIL 실제 플레이 영상"></video><figcaption><button type="button" data-video-toggle aria-pressed="false">영상 재생</button><span data-video-status role="status">무음 플레이 영상</span></figcaption></figure>`;
}
export function bgmControl(config){
 return config?'<div class="bgm-control"><button type="button" data-bgm-toggle aria-pressed="false">BGM 켜기</button><span data-bgm-status role="status"></span></div>':'';
}
