"""Explicit approved-image conditioning for the installed SD1.5 IPAdapter profile."""
def attach_reference(w):
 w['20']={'class_type':'LoadImage','inputs':{'image':'{{ref:art}}'}}
 w['21']={'class_type':'CLIPVisionLoader','inputs':{'clip_name':'CLIP-ViT-H-14-laion2B-s32B-b79K.safetensors'}}
 w['22']={'class_type':'IPAdapterModelLoader','inputs':{'ipadapter_file':'ip-adapter-plus_sd15.safetensors'}}
 w['23']={'class_type':'IPAdapterAdvanced','inputs':{'model':['1',0],'ipadapter':['22',0],'image':['20',0],'clip_vision':['21',0],'weight':.7,'weight_type':'linear','combine_embeds':'concat','start_at':0.,'end_at':1.,'embeds_scaling':'V only'}}
 w['7']['inputs']['model']=['23',0]
