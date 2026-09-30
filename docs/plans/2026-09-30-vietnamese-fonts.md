# Vietnamese TMP fonts

Use only the four supplied static TTF files. Preserve gameplay, text content,
RectTransforms, scene/prefab GUIDs and migrated component fileIDs.

1. Create reusable Editor setup: Saira title and Barlow button/HUD static assets
   at 90 pt, 9 px padding, 2048 SDFAA, Optimum packing; Be Vietnam Regular/Bold
   dynamic multi-atlas assets at 2048. Preserve existing asset GUIDs on reruns.
   Verify the supplied sample, configure TMP defaults/fallbacks and materials.
2. Convert legacy components in place, retain settings and IDs, assign roles to
   existing TMP and update runtime/Editor UI factories. Keep input/game state
   ownership unchanged. Normalize incoming strings to NFC in the existing text
   assignment paths; normalize input on end edit without disrupting the IME.
3. Generate a separate FontTest scene and an Editor coverage audit of serialized
   text, ScriptableObject strings, data files and runtime source literals. Audit
   unsupported characters with exact code points, never silence TMP warnings.
4. Verify source font tables, Unity compilation, EditMode/PlayMode suites,
   setup idempotence, migration IDs/RectTransforms and rendered screenshots.
   Commit setup, application and QA in separate concern groups after checks.

Review focus: Vietnamese combining marks; static atlas capacity versus glyphs
absent in the source font; role materials must match font atlases; runtime
builders must not restore old fonts; input normalization must preserve IME
composition and avoid recursive callbacks. Existing Sprint arrows are supported
by Saira, so bake U+2190/U+2192 as well and use its plain material for those labels.
No old font assets will be deleted. Device Telex/VNI and Android GPU QA remain
distinct from desktop Editor evidence.
