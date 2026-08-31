# Pretendard Launch Font Assets

Source copied from the user's local 영물대소동 project:
`E:\Projects\fangame\SpiritCreatureWork\Assets\SpiritCreature\Fonts\Pretendard\`

Imported font family/files:
- `Pretendard-Regular.otf`
- `Pretendard-SemiBold.otf`
- `Pretendard-Bold.otf`

Imported TextMeshPro SDF assets:
- `TMP/Pretendard-Regular SDF.asset`
- `TMP/Pretendard-SemiBold SDF.asset`
- `TMP/Pretendard-Bold SDF.asset`

Imported TMP material presets:
- `TMP/Materials/Pretendard-Regular SDF UI.mat`
- `TMP/Materials/Pretendard-SemiBold SDF UI.mat`
- `TMP/Materials/Pretendard-Bold SDF UI.mat`

SDF/material handling:
- The source project already contained working TMP SDF font assets and UI material presets.
- LASTRAIL copies those TMP SDF assets, material presets, and `.meta` files instead of relying only on raw OTF/default TMP material.
- `Pretendard-Regular SDF.asset` contains its atlas texture as a Unity sub-asset; no separate external atlas texture file was found in the source Pretendard folder.
- Seven launch screen prefabs should bind `Pretendard-Regular SDF.asset` and `Pretendard-Regular SDF UI.mat` through `LaunchScreenPrefabBindingBuilder`.

License and release compliance audit:
- Local source license file: `LICENSE-Pretendard-OFL.txt`.
- License name: SIL Open Font License, Version 1.1.
- Official Pretendard documentation also identifies Pretendard as SIL Open Font License and describes commercial use, modification, and redistribution as allowed.
- Imported OTF files are copied from the source local project without font-outline/name edits.
- Imported TMP SDF assets/material presets are rendering assets derived from the unmodified Pretendard font for Unity TextMeshPro use.
- Release package must include the copied `LICENSE-Pretendard-OFL.txt` notice with the app/font distribution.
- Do not sell the font files by themselves.
- Do not rename or market a modified font using the Reserved Font Name `Pretendard` unless that future modification is explicitly allowed by the license holder.
- This is an engineering/release compliance note, not legal advice.
