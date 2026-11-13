using System;
using System.Runtime.CompilerServices;
using LastRail.Run;
using NUnit.Framework;
namespace LastRail.Tests.EditMode {
 public sealed class RunApiErrorJsonAdapterTests {
  [MethodImpl(MethodImplOptions.NoInlining)] static WeakReference ReadTemporaryBody(){var text="{\"padding\":\""+Guid.NewGuid()+new string('x',20000)+"\",\"errorCode\":null}";Assert.That(RunApiErrorJsonAdapter.OptionalString(text,"errorCode"),Is.Null);return new WeakReference(text);}
  [Test] public void ErrorProbeDoesNotRetainReleasedResponse(){var weak=ReadTemporaryBody();GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();Assert.That(weak.IsAlive,Is.False,"Error classification must not keep the original response in a process-wide regex runner cache");}
  [Test] public void NestedErrorDoesNotMasqueradeAsAuthenticationFailure(){Assert.That(RunApiErrorJsonAdapter.OptionalString("{\"saved\":{\"errorCode\":\"auth_required\"},\"errorCode\":\"current_error\"}","errorCode"),Is.EqualTo("current_error"));}
  [TestCase("{\"errorCode\":null}",null)][TestCase("{}",null)][TestCase("not json",null)][TestCase("{\"errorCode\":3}",null)][TestCase("{\"errorCode\":\"auth_required\"}","auth_required")]
  public void OptionalErrorKeepsMissingAndNullSemantics(string json,string expected){Assert.That(RunApiErrorJsonAdapter.OptionalString(json,"errorCode"),Is.EqualTo(expected));}
  [Test] public void JsonEscapesAreDecoded(){Assert.That(RunApiErrorJsonAdapter.OptionalString("{\"message\":\"a\\nb\\u0021\"}","message"),Is.EqualTo("a\nb!"));}
 }
}
