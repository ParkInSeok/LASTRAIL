"""Conservative red fringe correction near black, inside an explicit mask."""
import numpy as np
from PIL import Image,ImageFilter
from refinement import validate_mask

def smooth_alpha_edge(image,mask):
 """Single subpixel coverage pass, bounded to alpha edges and the selected mask."""
 original=image.convert('RGBA');m=validate_mask(mask,original.size);a=np.asarray(original,dtype=np.float32)
 alpha=original.getchannel('A');lo=np.asarray(alpha.filter(ImageFilter.MinFilter(3)));hi=np.asarray(alpha.filter(ImageFilter.MaxFilter(3)))
 edge=(lo<250)&(hi>0)&(hi!=lo)
 weight=(np.asarray(m,dtype=np.float32)/255*edge)[:,:,None]
 premul=a.copy();premul[:,:,:3]*=premul[:,:,3:4]/255
 # Separable float filter avoids mixing invisible RGB or rounding dark edge colors.
 padded=np.pad(premul,((0,0),(1,1),(0,0)));horizontal=padded[:,:-2]*.16+padded[:,1:-1]*.68+padded[:,2:]*.16
 padded=np.pad(horizontal,((1,1),(0,0),(0,0)));blurred=padded[:-2]*.16+padded[1:-1]*.68+padded[2:]*.16
 result=premul*(1-weight)+blurred*weight
 result[:,:,:3]/=np.maximum(result[:,:,3:4]/255,1/255)
 result=result.round().clip(0,255).astype('uint8');result[weight[:,:,0]==0]=a.astype('uint8')[weight[:,:,0]==0]
 return Image.fromarray(result),{'changed_pixels':int(np.any(result!=a,axis=2).sum()),'method':'one-pass premultiplied alpha-edge antialiasing','outside_mask_preserved':True,'opaque_interior_preserved':True,'alpha_preserved':False,'new_ai_generation':False}

def clean_red_fringe(image,mask):
 original=image.convert('RGBA');m=validate_mask(mask,original.size)
 a=np.asarray(original).copy();rgb=a[:,:,:3].astype(np.float32)
 peak=rgb.max(axis=2)
 near_black=np.asarray(Image.fromarray(peak.astype('uint8')).filter(ImageFilter.MinFilter(5)))<12
 red,green,blue=rgb[:,:,0],rgb[:,:,1],rgb[:,:,2]
 selected=(np.asarray(m)>0)&near_black&(red>1.6*green)&(red>1.1*blue)&(red-green>15)&(a[:,:,3]>0)
 # Neutralize red/magenta contamination; no resampling, shape warp or alpha edit.
 weight=np.asarray(m,dtype=np.float32)/255
 target=green*1.5
 corrected=np.rint(red+(target-red)*weight).clip(0,255).astype('uint8')
 a[:,:,0][selected]=corrected[selected]
 corrected_blue=np.rint(blue+(np.minimum(blue,green*.85)-blue)*weight).clip(0,255).astype('uint8')
 a[:,:,2][selected]=corrected_blue[selected]
 return Image.fromarray(a),{'changed_pixels':int(np.count_nonzero(np.any(a!=np.asarray(original),axis=2))),'method':'masked red-magenta fringe correction near black','alpha_preserved':True,'outside_mask_preserved':True}
