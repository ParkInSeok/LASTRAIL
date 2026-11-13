using System.IO;
using System.Linq;
using LastRail.Battle;
using NUnit.Framework;
using TMPro;
using UnityEditor;
public class CombatEditorFontsTests {
 [Test] public void DynamicGlyphUsesTransientAtlasWithoutChangingSourceFile(){const string path="Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Pretendard-Regular SDF.asset";var source=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);var before=File.ReadAllBytes(path);int glyphs=source.characterTable.Count;var copy=CombatEditorFonts.Resolve(source);Assert.AreNotSame(source,copy);Assert.IsFalse(EditorUtility.IsPersistent(copy));Assert.AreNotSame(source.atlasTextures[0],copy.atlasTextures[0]);Assert.IsFalse(EditorUtility.IsPersistent(copy.atlasTextures[0]));Assert.AreNotSame(source.material,copy.material);char missing=Enumerable.Range(0xac00,0xd7a3-0xac00).Select(x=>(char)x).First(c=>!source.HasCharacter(c,false,false));Assert.IsTrue(copy.TryAddCharacters(missing.ToString(),out string unsupported));Assert.IsEmpty(unsupported);Assert.AreEqual(glyphs,source.characterTable.Count);Assert.IsFalse(source.HasCharacter(missing,false,false));Assert.IsTrue(copy.HasCharacter(missing,false,false));AssetDatabase.SaveAssets();CollectionAssert.AreEqual(before,File.ReadAllBytes(path));Assert.AreSame(copy,CombatEditorFonts.Resolve(source));}
}
