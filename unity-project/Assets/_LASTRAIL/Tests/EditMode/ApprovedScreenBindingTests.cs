using System.Threading;
using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using LastRail.Resources;
using NUnit.Framework;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace LastRail.Tests.EditMode
{
    public sealed class ApprovedScreenBindingTests
    {
        [Test]
        public void ScreenHostAcceptsPinnedLocationInsteadOfOnlyGlobalKey()
        {
            Assert.That(typeof(AddressableScreenHost).GetMethod("OpenAsync", new[] { typeof(IResourceLocation), typeof(CancellationToken) }),
                Is.Not.Null, "Actual screen instantiation must consume the approved locator");
        }

        [Test]
        public void PinnedInstantiationDoesNotEraseTheLocationOverload()
        {
            var method = typeof(AddressableScreenHost).GetMethod("OpenKeyAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            var state = method.GetCustomAttribute<AsyncStateMachineAttribute>().StateMachineType;
            var move = state.GetMethod("MoveNext", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            var code = move.GetMethodBody().GetILAsByteArray();
            var opcodes = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.FieldType == typeof(OpCode)).Select(f => (OpCode)f.GetValue(null)).ToDictionary(o => unchecked((ushort)o.Value));
            var found = false;
            for (var i = 0; i < code.Length;)
            {
                ushort value = code[i++]; if (value == 0xfe) value = (ushort)(0xfe00 | code[i++]);
                var op = opcodes[value];
                if (op.OperandType == OperandType.InlineMethod)
                {
                    var called = move.Module.ResolveMethod(BitConverter.ToInt32(code, i));
                    if (called.Name == "InstantiateAsync" && called.GetParameters().FirstOrDefault()?.ParameterType == typeof(IResourceLocation)) found = true;
                }
                switch (op.OperandType)
                {
                    case OperandType.InlineNone: break;
                    case OperandType.ShortInlineBrTarget: case OperandType.ShortInlineI: case OperandType.ShortInlineVar: i++; break;
                    case OperandType.InlineVar: i += 2; break;
                    case OperandType.InlineI8: case OperandType.InlineR: i += 8; break;
                    case OperandType.InlineSwitch: i += 4 + 4 * BitConverter.ToInt32(code, i); break;
                    default: i += 4; break;
                }
            }
            Assert.That(found, Is.True, "Casting a location to object selects global key lookup instead of the pinned-location overload");
        }
    }
}
