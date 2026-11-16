/* Owns only the editable polygon draft; Studio owns saved masks and revisions. */
class MaskEditorView {
  constructor(root, save) {
    this.root=root;this.save=save;this.points=[];this.generation=0;
    root.innerHTML=`<p>그림의 경계를 클릭해 점을 놓으세요. 닫힌 다각형으로 저장합니다.</p><label>확대 <select data-zoom><option>0.25</option><option>0.5</option><option>1</option><option>2</option><option>4</option></select></label>
      <label><input type="checkbox" data-pick>원본 RGB 추출</label><output data-rgb></output>
      <label>작업 <select data-mode><option value="add">추가</option><option value="erase">지우기</option><option value="replace">전체 교체</option></select></label>
      <button data-undo>마지막 점 취소</button><button data-clear>점 초기화</button><button data-save>경계 저장</button>
      <div style="overflow:auto;max-height:650px;background:#777"><canvas style="display:block;touch-action:none;image-rendering:pixelated" aria-label="부위 경계 편집 그림"></canvas></div><p data-status></p>`;
    this.el=k=>root.querySelector(`[data-${k}]`);this.canvas=root.querySelector('canvas');
    this.canvas.onclick=e=>{if(!this.source)return;const b=this.canvas.getBoundingClientRect(),x=Math.min(this.canvas.width-1,Math.floor((e.clientX-b.left)*this.canvas.width/b.width)),y=Math.min(this.canvas.height-1,Math.floor((e.clientY-b.top)*this.canvas.height/b.height));
      if(this.el('pick').checked){const c=this.sample.getContext('2d').getImageData(x,y,1,1).data;this.el('rgb').textContent=`(${x}, ${y}) #${[...c].slice(0,3).map(n=>n.toString(16).padStart(2,'0')).join('').toUpperCase()} · α ${c[3]}`;return}
      this.points.push([x,y]);this.draw()};
    this.el('undo').onclick=()=>{this.points.pop();this.draw()};this.el('clear').onclick=()=>{this.points=[];this.draw()};this.el('zoom').onchange=()=>this.draw();
    this.el('save').onclick=async()=>{if(this.points.length<3||!this.context)return;this.el('save').disabled=true;try{await this.save({...this.context,points:this.points.map(p=>[...p]),mode:this.el('mode').value})}catch(e){this.el('status').textContent=e.message}finally{this.el('save').disabled=false}};
  }
  async load(context, source, mask) {
    const generation=++this.generation;this.source=null;this.context=context;this.points=[];this.canvas.getContext('2d').clearRect(0,0,this.canvas.width,this.canvas.height);
    const read=async asset=>{if(!asset)return null;const im=new Image();im.src='/assets/'+asset;await im.decode();return im};
    try{const [a,b]=await Promise.all([read(source),read(mask)]);if(generation!==this.generation)return;if(!a){this.el('status').textContent='표시할 원본이 없습니다.';return}
      this.context=context;this.source=a;this.mask=b;this.canvas.width=a.width;this.canvas.height=a.height;
      this.sample=document.createElement('canvas');this.sample.width=a.width;this.sample.height=a.height;this.sample.getContext('2d').drawImage(a,0,0);this.draw();
    }catch(e){if(generation===this.generation)this.el('status').textContent='마스크 표시 실패: '+e.message}
  }
  draw(){if(!this.source)return;const c=this.canvas.getContext('2d'),z=Number(this.el('zoom').value);this.canvas.style.width=this.canvas.width*z+'px';this.canvas.style.height=this.canvas.height*z+'px';c.clearRect(0,0,this.canvas.width,this.canvas.height);c.drawImage(this.source,0,0);
    if(this.mask){const overlay=document.createElement('canvas');overlay.width=this.canvas.width;overlay.height=this.canvas.height;const oc=overlay.getContext('2d');oc.drawImage(this.mask,0,0);const pixels=oc.getImageData(0,0,overlay.width,overlay.height);for(let i=0;i<pixels.data.length;i+=4){const d=pixels.data,a=Math.round((d[i]*.299+d[i+1]*.587+d[i+2]*.114)*d[i+3]/255);d[i]=0;d[i+1]=255;d[i+2]=180;d[i+3]=a}oc.putImageData(pixels,0,0);c.globalAlpha=.45;c.drawImage(overlay,0,0);c.globalAlpha=1}
    if(this.points.length){c.beginPath();this.points.forEach(([x,y],i)=>i?c.lineTo(x,y):c.moveTo(x,y));c.closePath();c.fillStyle='#ff00ff44';c.strokeStyle='#ffff00';c.lineWidth=1;c.fill();c.stroke();for(const [x,y] of this.points)c.fillRect(x-1,y-1,3,3)}this.el('status').textContent=`경계점 ${this.points.length}개 · 초록: 저장된 면 · 분홍: 수정할 면`;
  }
  destroy(){this.generation++;this.context=null;this.source=null;this.root.replaceChildren()}
}
