using System;
using LastRail.Battle;
using LastRail.Presentation.MissionShell;
using Newtonsoft.Json.Linq;
namespace LastRail.Presentation.Battle {
 public static class OperatorInformationMiraSource {
  // Accepts an authenticated immutable trial context, never a preview/raw self-pinned packet.
  public static OperatorInformationSnapshot Capture(BMiraEquippedTrialContext context,OperatorInformationTarget target) {
   if(context==null||target==null||target.Scope!=context.RunId||target.OperatorId!="op_mira")throw new ArgumentException("Original Mira trial/run required.");
   var source=context.Capture()["start"]["source"];var result=new OperatorEquipmentInformation[3];
   for(int i=0;i<3;i++) {
    if(source["slots"][i].Type==JTokenType.Null)continue;
    var row=source["slots"][i];var instance=row["instance"];var enchant=row["effect"]["snapshot"]["enchantment"];
    bool has=enchant.Type!=JTokenType.Null;var selected=source["selection"];
    string state=!has?"NO_ENCHANTMENT":(string)selected["instanceId"]==(string)row["instanceId"]?"APPLIED":(int)selected["definitionSnapshot"]["coefficientBps"]==(int)enchant["definitionSnapshot"]["coefficientBps"]?"DUPLICATE_HIGHEST":"LOWER_EFFECT";
    var labels=EquipmentMultishotModel.RenderSlot(new JObject{["currentStar"]=row["currentStar"].DeepClone(),["appliedStar"]=has?enchant["appliedStar"].DeepClone():JValue.CreateNull(),["coefficientBps"]=has?enchant["definitionSnapshot"]["coefficientBps"].DeepClone():JValue.CreateNull(),["status"]=state},i);
    string description="기본 옵션 수치 확인 불가\n고유효과 없음\n"+labels["appliedStar"]+" "+labels["coefficient"]+"\n"+labels["statusLabel"];
    if(has)description+="\n다중 발사: 기본공격 주대상 주변의 적 최대 2명에게 추가 피해. 주대상 기준 0.5L와 원사거리·영역·시야의 교집합. 확정 본체·관통 대상 제외, 발사 후 재조준 없음. 장비 승급으로 기존 인챈트 수치는 바뀌지 않습니다.";
    description+="\n세트 태그: "+(string)instance["set_tag"]+" · 활성 단계 확인 불가";
    result[i]=new OperatorEquipmentInformation((string)row["instanceId"],(long)instance["revision"],null,(int)row["currentStar"],null,description,(long)row["effect"]["effectRevision"]);
   }
   return new OperatorInformationSnapshot(target,(long)source["inventoryRevision"],result,null,"출발 시 확정 장착 · 미라 LOCAL 시험");
  }
 }
}
