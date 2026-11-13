using System;
using System.Reflection;
using LastRail.Presentation.MissionShell;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class AugmentSelectionContractTests
    {
        internal const string Initial = "{\"runId\":\"run\",\"eventId\":\"event\",\"stageId\":\"stage\",\"catalogVersion\":\"TEST_ONLY\",\"grantRevision\":0,\"state\":\"SELECTING\",\"slots\":[{\"slotIndex\":0,\"candidateId\":\"a\",\"freeRemaining\":3,\"extraRemaining\":1},{\"slotIndex\":1,\"candidateId\":\"b\",\"freeRemaining\":3,\"extraRemaining\":1},{\"slotIndex\":2,\"candidateId\":\"c\",\"freeRemaining\":3,\"extraRemaining\":1}],\"selectedCandidateId\":null}";
        static object Parse(string json)
        {
            var type=typeof(AccountScreenRuntime).Assembly.GetType("LastRail.Presentation.AugmentSelection.AugmentSelectionSnapshot");
            Assert.That(type,Is.Not.Null,"Immutable internal selection snapshot is missing");
            try {return type.GetMethod("Parse").Invoke(null,new object[]{json});}
            catch(TargetInvocationException e) {throw e.InnerException;}
        }
        [Test] public void InternalSnapshotPreservesServerFacts()
        {
            var snapshot=Parse(Initial);
            Assert.That(snapshot.GetType().GetProperty("EventId").GetValue(snapshot),Is.EqualTo("event"));
            Assert.That(snapshot.GetType().GetProperty("GrantRevision").GetValue(snapshot),Is.EqualTo(0L));
        }
        [TestCase("duplicateCandidate")][TestCase("duplicateSlot")][TestCase("freeRange")]
        [TestCase("extraBeforeFree")][TestCase("selectedWhileOpen")][TestCase("missingSelection")]
        [TestCase("fractionalRevision")][TestCase("unknownField")]
        public void MalformedBoundaryIsRejected(string defect)
        {
            var root=JObject.Parse(Initial);var slot=(JObject)root["slots"][0];
            switch(defect)
            {
                case "duplicateCandidate":slot["candidateId"]="b";break;
                case "duplicateSlot":slot["slotIndex"]=1;break;
                case "freeRange":slot["freeRemaining"]=4;break;
                case "extraBeforeFree":slot["extraRemaining"]=0;break;
                case "selectedWhileOpen":root["selectedCandidateId"]="a";break;
                case "missingSelection":root["state"]="CONFIRMED";break;
                case "fractionalRevision":root["grantRevision"]=0.5;break;
                case "unknownField":root["owned"]=true;break;
            }
            // Resolve the type outside Assert.Throws so a missing implementation is an ordinary red assertion.
            Parse(Initial);
            Assert.Throws<FormatException>(()=>Parse(root.ToString()));
        }
    }
}
