"""Bounded local contrast finish; does not synthesize missing drawing detail."""
import numpy as np
from PIL import Image,ImageFilter
from refinement import validate_mask

def finish_detail(image,mask):
 original=image.convert('RGBA');m=validate_mask(mask,original.size)
 a=np.asarray(original).copy();rgb=a[:,:,:3].astype(np.float32)
 luminance=original.convert('L');l=np.asarray(luminance,dtype=np.float32)
 low=np.asarray(luminance.filter(ImageFilter.GaussianBlur(1)),dtype=np.float32)
 delta=np.clip((l-low)*1.1,-10,10)
 delta[np.abs(l-low)<2]=0
 # Avoid bright halos in black or almost transparent background.
 delta[(l<20)|(a[:,:,3]<240)]=0
 delta*=np.asarray(m,dtype=np.float32)/255
 a[:,:,:3]=np.rint(np.clip(rgb+delta[:,:,None],0,255)).astype('uint8')
 changed=np.any(a!=np.asarray(original),axis=2)
 return Image.fromarray(a),{'changed_pixels':int(changed.sum()),'max_channel_delta':int(np.abs(a.astype(int)-np.asarray(original).astype(int)).max()),'method':'bounded local luminance contrast; no restored or generated detail','alpha_preserved':True,'outside_mask_preserved':True}
