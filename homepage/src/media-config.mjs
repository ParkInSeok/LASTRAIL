// Approved media only. Keep null until the actual asset and poster are selected.
// Video: 10–15 seconds, landscape (1920×1080 reference; preserve the supplied game frame), no baked website UI. Poster is an actual frame.
// Put files in public/media and public/art; preview serves only configured paths.
export const homepageMedia = Object.freeze({
 gameplay:null, // {src:'/media/combat-v01.mp4',poster:'/art/combat-poster-v01.webp'}
 bgm:{src:'/media/home-theme-v01.mp3'} // Approved v01; defaults OFF, no saved autoplay consent.
});
export function mediaAssets(config=homepageMedia){
 const assets=[];
 for(const item of [config.gameplay,config.bgm]){
  if(!item)continue;
  if(!/^\/media\/[a-z0-9-]+\.(mp4|webm|ogg|mp3|wav)$/.test(item.src))throw new Error('Invalid local media path');
  const ext=item.src.split('.').pop();
  assets.push([item.src,['public'+item.src,({mp4:'video/mp4',webm:'video/webm',ogg:'audio/ogg',mp3:'audio/mpeg',wav:'audio/wav'})[ext]]]);
 }
 if(config.gameplay){
  const poster=config.gameplay.poster;
  if(!/^\/art\/[a-z0-9-]+\.(webp|png|jpg)$/.test(poster))throw new Error('Gameplay requires a local poster frame');
  assets.push([poster,['public'+poster,poster.endsWith('.webp')?'image/webp':poster.endsWith('.png')?'image/png':'image/jpeg']]);
 }
 return assets;
}
