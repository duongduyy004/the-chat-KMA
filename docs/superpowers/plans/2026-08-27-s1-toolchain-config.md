# S1 — Toolchain & Config Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Đưa project Unity từ trạng thái chưa từng mở bằng Editor sang trạng thái compile sạch, 158 test pass, ProjectSettings đúng spec, URP 2D đã gán, và build ra được APK Android chạy trên máy thật.

**Architecture:** ProjectSettings **không** sửa bằng cách vá YAML — sửa bằng một Editor script dùng `PlayerSettings` API, chạy headless qua `-executeMethod`, và được canh bởi EditMode test assert từng giá trị. Lý do: giá trị enum trong YAML (`scriptingBackend: {}`, `apiCompatibilityLevel: 6`) không đọc được nghĩa từ file, đoán sai là hỏng build; API thì Unity tự ghi YAML đúng. Package thì ngược lại — sửa `Packages/manifest.json` trực tiếp với version đã pin, vì như vậy diff review được và tái lập được, khác với `Client.AddAndRemove` cho version không xác định.

**Tech Stack:** Unity `6000.3.23f1` (đã cài), URP `17.3.0`, `com.unity.ugui` `2.0.0`, `com.unity.2d.sprite` `1.0.0`, Input System `1.20.0`, Unity Test Framework `1.6.0`, IL2CPP/ARM64, CLI `unity` tại `~/.local/bin/unity`.

**Spec:** `docs/superpowers/specs/2026-08-27-kma-game-completion-design.md` (mục 5, phần S1)

## Global Constraints

Mọi task đều phải giữ các ràng buộc sau. Giá trị copy nguyên văn từ spec.

- **Không sửa rules engine đã có test.** Chỉ thêm method/event mới; không đổi chữ ký hoặc hành vi method đã test. (spec §2)
- Editor version pin: `6000.3.23f1` — cả nhóm 1 patch duy nhất.
- Đường dẫn editor: `/home/duydt/Unity/Hub/Editor/6000.3.23f1/Editor/Unity`
- Product name: `Thể Chất KMA` · Android application id: `com.kma.thechat` (PLAN §11 mục 2)
- Company name: `KMA` — **suy ra, không có trong spec.** Spec và PLAN.md chỉ chốt product name và package id; giá trị hiện tại `DefaultCompany` rõ ràng là chưa đặt. Muốn tên khác thì sửa cả `ProjectConfigurator.CompanyName` và `ProjectSettingsTests.ProductIdentityMatchesSpec`.
- Orientation: **landscape only** — Portrait và PortraitUpsideDown = `false`, LandscapeLeft và LandscapeRight = `true`
- Android: minSdk `24`, targetSdk `Auto`, arch `ARM64`, backend `IL2CPP`, stripping `Medium`, api compat `.NET Standard 2.1`
- Graphics API (Android), đúng thứ tự: `Vulkan`, `OpenGLES3`
- Audio: DSP buffer = Best latency (`m_RequestedDSPBufferSize: 256`)
- Render pipeline: **URP 2D**, tắt realtime light và post-process ngay từ đầu (spec S1 quyết định 4)
- Test baseline phải giữ xanh sau **mỗi** task: `unity test --mode EditMode` và `--mode PlayMode`
- Commit thường xuyên. Scene bị Editor ghi lại phải commit **riêng**, không trộn với thay đổi config.

---

## File Structure

| File | Trách nhiệm |
|---|---|
| `ProjectSettings/ProjectVersion.txt` | Pin editor version. Sửa. |
| `README.md` | Ghi version pin. Sửa. |
| `Packages/manifest.json` | Khai báo package + version. Sửa. |
| `Packages/packages-lock.json` | Unity tự ghi khi resolve. Commit kết quả. |
| `Assets/Editor/KMA.EditorTools.asmdef` | Asmdef Editor-only cho tooling. Tạo. |
| `Assets/Editor/ProjectConfigurator.cs` | Áp ProjectSettings qua `PlayerSettings` API. Tạo. |
| `Assets/Editor/UrpBootstrap.cs` | Gán URP asset vào Graphics/Quality Settings. Tạo. |
| `Assets/Editor/BuildScript.cs` | Build APK Android headless. Tạo. |
| `Assets/Tests/EditMode/Config/KMA.Config.EditMode.Tests.asmdef` | Asmdef test config. Tạo. |
| `Assets/Tests/EditMode/Config/ProjectSettingsTests.cs` | Assert ProjectSettings. Tạo. |
| `Assets/Tests/EditMode/Config/PackageManifestTests.cs` | Assert package đã thêm/xoá. Tạo. |
| `Assets/Tests/EditMode/Config/RenderPipelineTests.cs` | Assert URP 2D đã gán. Tạo. |
| `Assets/Tests/EditMode/Config/ProjectLayoutTests.cs` | Assert cây thư mục + audio DSP. Tạo. |
| `Assets/_Project/Settings/URP/URP-2D.asset` | URP pipeline asset. Tạo (qua Editor menu). |
| `Assets/_Project/Settings/URP/URP-2D_Renderer2D.asset` | Renderer 2D data. Tạo (qua Editor menu). |
| `ProjectSettings/AudioManager.asset` | DSP buffer. Sửa. |
| `ProjectSettings/GraphicsSettings.asset` | Unity tự ghi khi gán URP. Commit kết quả. |
| `ProjectSettings/QualitySettings.asset` | Unity tự ghi khi gán URP. Commit kết quả. |

`ProjectConfigurator`, `UrpBootstrap`, `BuildScript` tách 3 file vì 3 trách nhiệm khác nhau và `UrpBootstrap` tham chiếu type của URP — type đó chưa tồn tại cho tới sau Task 3, nên không được nằm cùng file với code phải compile từ Task 2.

---

## Task 1: Pin editor version và lập test baseline

Đây là task duy nhất mở project bằng Editor lần đầu. Import đầu tiên có thể mất 5–20 phút và **có thể ghi lại 6 file scene viết tay** (chúng thiếu `RenderSettings`/`LightmapSettings`/`NavMeshSettings`). Đó là kỳ vọng, không phải lỗi.

**Files:**
- Modify: `ProjectSettings/ProjectVersion.txt`
- Modify: `README.md:8` (dòng `- Unity \`6000.3.22f1\``)
- Modify (do Unity ghi): `Assets/_Project/Scenes/*.unity`

**Interfaces:**
- Consumes: không có.
- Produces: số test EditMode và PlayMode thật, dùng làm baseline cho mọi task sau. Ghi con số vào commit message.

- [ ] **Step 1: Xác nhận editor đã cài**

```bash
~/.local/bin/unity editors -i
```

Expected: một dòng chứa `6000.3.23f1` và cột Platforms có `Android`, `Android SDK & NDK Tools`, `OpenJDK`.
Nếu không thấy: `~/.local/bin/unity install 6000.3.23f1 -m android android-sdk-ndk-tools --cm --accept-eula -y` rồi chạy lại.

- [ ] **Step 2: Repin version**

Revision hash của `6000.3.23f1` là `09d2ecc7fb28` — đã trích từ binary editor đã cài. Hash `1c726e1fb402` đang có trong file là của `22f1`.

```bash
cd /home/duydt/project/the-chat-KMA
cat > ProjectSettings/ProjectVersion.txt <<'EOF'
m_EditorVersion: 6000.3.23f1
m_EditorVersionWithRevision: 6000.3.23f1 (09d2ecc7fb28)
EOF
cat ProjectSettings/ProjectVersion.txt
```

Nếu editor được cài lại bản khác, lấy hash tương ứng bằng:

```bash
grep -rhoE "6000\.3\.[0-9]+f1 \([0-9a-f]{12}\)" \
  /home/duydt/Unity/Hub/Editor/6000.3.23f1/Editor/Unity | sort -u
```

- [ ] **Step 3: Cập nhật README**

```bash
sed -i 's|^- Unity `6000\.3\.22f1`|- Unity `6000.3.23f1`|' README.md
sed -i 's|Install Unity `6000\.3\.22f1`|Install Unity `6000.3.23f1`|' README.md
grep -n '6000\.3' README.md
```

Expected: không còn dòng nào chứa `22f1`.

- [ ] **Step 4: Chạy EditMode test — lần import đầu tiên**

```bash
cd /home/duydt/project/the-chat-KMA
~/.local/bin/unity test --mode EditMode --output /tmp/kma-editmode.xml --timeout 2400
```

Expected: PASS. Lần chạy này sinh `Library/` và có thể mất 5–20 phút.
Nếu lỗi compile: đọc log mà CLI in ra, sửa, chạy lại. Không đi tiếp khi còn lỗi compile.

- [ ] **Step 5: Đọc số test thật**

```bash
python3 - <<'PY'
import xml.etree.ElementTree as ET
r = ET.parse('/tmp/kma-editmode.xml').getroot()
print('EditMode total =', r.get('total'), 'passed =', r.get('passed'), 'failed =', r.get('failed'))
PY
```

Expected: `failed = 0`. Ghi lại `total` — README nói `121`, con số thật là con số đúng.

- [ ] **Step 6: Chạy PlayMode test**

```bash
~/.local/bin/unity test --mode PlayMode --output /tmp/kma-playmode.xml --timeout 2400
python3 - <<'PY'
import xml.etree.ElementTree as ET
r = ET.parse('/tmp/kma-playmode.xml').getroot()
print('PlayMode total =', r.get('total'), 'passed =', r.get('passed'), 'failed =', r.get('failed'))
PY
```

Expected: `failed = 0`. README nói `37`.

- [ ] **Step 7: Commit thay đổi version (chỉ version)**

```bash
cd /home/duydt/project/the-chat-KMA
git add ProjectSettings/ProjectVersion.txt README.md
python3 - <<'PYX' > /tmp/kma-commit-msg.txt
import xml.etree.ElementTree as ET
edit = ET.parse('/tmp/kma-editmode.xml').getroot()
play = ET.parse('/tmp/kma-playmode.xml').getroot()
print("chore: pin editor 6000.3.23f1")
print()
print(f"Baseline đã xác minh: EditMode {edit.get('passed')} pass, "
      f"PlayMode {play.get('passed')} pass.")
PYX
cat /tmp/kma-commit-msg.txt
git commit -F /tmp/kma-commit-msg.txt
```

Số test đọc trực tiếp từ file kết quả, không nhập tay.

- [ ] **Step 8: Commit riêng phần scene Unity ghi lại**

```bash
git status --short
```

Nếu có file `Assets/_Project/Scenes/*.unity` hoặc `ProjectSettings/*.asset` bị đổi:

```bash
git add Assets/_Project/Scenes ProjectSettings
git commit -m "chore: normalize scene YAML via Editor

6 scene được viết tay bằng YAML, thiếu RenderSettings/LightmapSettings/
NavMeshSettings. Editor ghi lại ở lần import đầu tiên. Không có thay đổi
logic nào trong commit này."
```

Nếu `git status --short` không có gì: bỏ qua step này, Unity không cần ghi lại.

- [ ] **Step 9: Chạy lại cả 2 suite để chắc scene normalize không phá gì**

```bash
~/.local/bin/unity test --mode EditMode --output /tmp/kma-editmode2.xml --timeout 2400
~/.local/bin/unity test --mode PlayMode --output /tmp/kma-playmode2.xml --timeout 2400
```

Expected: cùng số total, `failed = 0` cả hai.

---

## Task 2: ProjectSettings qua PlayerSettings API

**Files:**
- Create: `Assets/Tests/EditMode/Config/KMA.Config.EditMode.Tests.asmdef`
- Create: `Assets/Tests/EditMode/Config/ProjectSettingsTests.cs`
- Create: `Assets/Editor/KMA.EditorTools.asmdef`
- Create: `Assets/Editor/ProjectConfigurator.cs`
- Modify (do Unity ghi): `ProjectSettings/ProjectSettings.asset`

**Interfaces:**
- Consumes: baseline test xanh từ Task 1.
- Produces: `KMA.EditorTools.ProjectConfigurator.ApplyAll()` — `public static void`, gọi được qua `-executeMethod`. Task 6 dùng cùng asmdef `KMA.EditorTools`.

Test **chỉ assert trạng thái**, không gọi configurator. Nếu test tự gọi configurator rồi assert thì nó luôn xanh và không kiểm được gì.

- [ ] **Step 1: Tạo asmdef cho test config**

```bash
mkdir -p Assets/Tests/EditMode/Config
cat > Assets/Tests/EditMode/Config/KMA.Config.EditMode.Tests.asmdef <<'EOF'
{
    "name": "KMA.Config.EditMode.Tests",
    "rootNamespace": "KMA.Tests.Config",
    "references": [
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner"
    ],
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": true,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "versionDefines": [],
    "noEngineReferences": false
}
EOF
```

- [ ] **Step 2: Viết test thất bại**

```bash
cat > Assets/Tests/EditMode/Config/ProjectSettingsTests.cs <<'EOF'
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine.Rendering;

namespace KMA.Tests.Config
{
    public sealed class ProjectSettingsTests
    {
        [Test]
        public void ProductIdentityMatchesSpec()
        {
            Assert.That(PlayerSettings.companyName, Is.EqualTo("KMA"));
            Assert.That(PlayerSettings.productName, Is.EqualTo("Thể Chất KMA"));
            Assert.That(PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android),
                Is.EqualTo("com.kma.thechat"));
        }

        [Test]
        public void OrientationIsLandscapeOnly()
        {
            Assert.That(PlayerSettings.defaultInterfaceOrientation,
                Is.EqualTo(UIOrientation.AutoRotation));
            Assert.That(PlayerSettings.allowedAutorotateToPortrait, Is.False);
            Assert.That(PlayerSettings.allowedAutorotateToPortraitUpsideDown, Is.False);
            Assert.That(PlayerSettings.allowedAutorotateToLandscapeLeft, Is.True);
            Assert.That(PlayerSettings.allowedAutorotateToLandscapeRight, Is.True);
        }

        [Test]
        public void AndroidBuildConfigMatchesSpec()
        {
            Assert.That(PlayerSettings.Android.minSdkVersion,
                Is.EqualTo(AndroidSdkVersions.AndroidApiLevel24));
            Assert.That(PlayerSettings.Android.targetSdkVersion,
                Is.EqualTo(AndroidSdkVersions.AndroidApiLevelAuto));
            Assert.That(PlayerSettings.Android.targetArchitectures,
                Is.EqualTo(AndroidArchitecture.ARM64));
            Assert.That(PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android),
                Is.EqualTo(ScriptingImplementation.IL2CPP));
            Assert.That(PlayerSettings.GetManagedStrippingLevel(NamedBuildTarget.Android),
                Is.EqualTo(ManagedStrippingLevel.Medium));
            Assert.That(PlayerSettings.GetApiCompatibilityLevel(NamedBuildTarget.Android),
                Is.EqualTo(ApiCompatibilityLevel.NET_Standard));
        }

        [Test]
        public void AndroidGraphicsApisArePinnedInOrder()
        {
            Assert.That(PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.Android), Is.False);
            Assert.That(PlayerSettings.GetGraphicsAPIs(BuildTarget.Android), Is.EqualTo(new[]
            {
                GraphicsDeviceType.Vulkan,
                GraphicsDeviceType.OpenGLES3
            }));
        }
    }
}
EOF
```

- [ ] **Step 3: Chạy test để xác nhận nó fail**

```bash
~/.local/bin/unity test --mode EditMode --filter ProjectSettingsTests \
  --output /tmp/kma-cfg.xml --timeout 1200
```

Expected: FAIL. `ProductIdentityMatchesSpec` fail vì `companyName` đang là `DefaultCompany`; `OrientationIsLandscapeOnly` fail vì portrait đang bật; `AndroidBuildConfigMatchesSpec` fail vì minSdk đang là `25`; `AndroidGraphicsApisArePinnedInOrder` fail vì `GetUseDefaultGraphicsAPIs` đang `true`.

Nếu thay vào đó gặp **lỗi compile** về tên API (ví dụ `ApiCompatibilityLevel.NET_Standard` không tồn tại), dump tên đúng rồi sửa test:

```bash
/home/duydt/Unity/Hub/Editor/6000.3.23f1/Editor/Unity -batchmode -nographics -quit \
  -projectPath . -logFile /tmp/enum.log \
  -executeMethod KMA.EditorTools.ProjectConfigurator.DumpEnumNames
```

(method `DumpEnumNames` được viết ở Step 4.)

- [ ] **Step 4: Tạo asmdef + configurator**

```bash
mkdir -p Assets/Editor
cat > Assets/Editor/KMA.EditorTools.asmdef <<'EOF'
{
    "name": "KMA.EditorTools",
    "rootNamespace": "KMA.EditorTools",
    "references": [],
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
EOF

cat > Assets/Editor/ProjectConfigurator.cs <<'EOF'
using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;

namespace KMA.EditorTools
{
    /// <summary>
    /// Áp ProjectSettings theo spec S1. Idempotent — chạy lại bao nhiêu lần cũng ra
    /// cùng kết quả. Canh bởi KMA.Config.EditMode.Tests/ProjectSettingsTests.
    /// </summary>
    public static class ProjectConfigurator
    {
        const string CompanyName = "KMA";
        const string ProductName = "Thể Chất KMA";
        const string AndroidApplicationId = "com.kma.thechat";

        [MenuItem("KMA/Apply Project Settings")]
        public static void ApplyAll()
        {
            ApplyProductIdentity();
            ApplyOrientation();
            ApplyAndroidBuildConfig();
            ApplyAndroidGraphicsApis();
            AssetDatabase.SaveAssets();
            Debug.Log("[KMA] Project settings applied.");
        }

        static void ApplyProductIdentity()
        {
            PlayerSettings.companyName = CompanyName;
            PlayerSettings.productName = ProductName;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, AndroidApplicationId);
        }

        static void ApplyOrientation()
        {
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
        }

        static void ApplyAndroidBuildConfig()
        {
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android,
                ScriptingImplementation.IL2CPP);
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android,
                ManagedStrippingLevel.Medium);
            PlayerSettings.SetApiCompatibilityLevel(NamedBuildTarget.Android,
                ApiCompatibilityLevel.NET_Standard);
        }

        static void ApplyAndroidGraphicsApis()
        {
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[]
            {
                GraphicsDeviceType.Vulkan,
                GraphicsDeviceType.OpenGLES3
            });
        }

        /// <summary>
        /// Cứu hộ khi tên enum khác giữa các bản Unity: in ra tên thật rồi thoát.
        /// </summary>
        public static void DumpEnumNames()
        {
            foreach (var type in new[]
            {
                typeof(ScriptingImplementation), typeof(ManagedStrippingLevel),
                typeof(ApiCompatibilityLevel), typeof(AndroidArchitecture),
                typeof(AndroidSdkVersions), typeof(UIOrientation), typeof(GraphicsDeviceType)
            })
            {
                Debug.Log($"[KMA] {type.Name}: {string.Join(", ", Enum.GetNames(type))}");
            }
        }
    }
}
EOF
```

- [ ] **Step 5: Chạy configurator headless**

```bash
/home/duydt/Unity/Hub/Editor/6000.3.23f1/Editor/Unity -batchmode -nographics -quit \
  -projectPath /home/duydt/project/the-chat-KMA \
  -executeMethod KMA.EditorTools.ProjectConfigurator.ApplyAll \
  -logFile /tmp/kma-config.log
echo "exit=$?"
grep -n "\[KMA\]" /tmp/kma-config.log
```

Expected: `exit=0` và log có `[KMA] Project settings applied.`

- [ ] **Step 6: Chạy test để xác nhận nó pass**

```bash
~/.local/bin/unity test --mode EditMode --filter ProjectSettingsTests \
  --output /tmp/kma-cfg.xml --timeout 1200
python3 -c "
import xml.etree.ElementTree as ET
r=ET.parse('/tmp/kma-cfg.xml').getroot()
print('total',r.get('total'),'passed',r.get('passed'),'failed',r.get('failed'))"
```

Expected: `failed 0`, `total 4`.

- [ ] **Step 7: Chạy full suite để chắc không phá baseline**

```bash
~/.local/bin/unity test --mode EditMode --output /tmp/kma-editmode.xml --timeout 2400
~/.local/bin/unity test --mode PlayMode --output /tmp/kma-playmode.xml --timeout 2400
```

Expected: `failed = 0` cả hai; EditMode total = baseline + 4.

- [ ] **Step 8: Commit**

```bash
git add Assets/Editor Assets/Tests/EditMode/Config ProjectSettings/ProjectSettings.asset
git commit -m "chore: áp ProjectSettings theo spec S1

Sửa bằng PlayerSettings API qua -executeMethod thay vì vá YAML, vì giá
trị enum trong ProjectSettings.asset không đọc được nghĩa từ file.
4 EditMode test canh từng giá trị."
```

Ghi chú: `.meta` của file mới do Unity sinh ở lần import kế tiếp. Nếu `git status` còn `.meta` chưa add sau khi chạy test, add và amend.

---

## Task 3: Package — thêm URP, ugui, 2d.sprite; xoá multiplayer.center

**Files:**
- Create: `Assets/Tests/EditMode/Config/PackageManifestTests.cs`
- Modify: `Packages/manifest.json`
- Modify (do Unity ghi): `Packages/packages-lock.json`

**Interfaces:**
- Consumes: asmdef `KMA.Config.EditMode.Tests` từ Task 2.
- Produces: type `UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset` và `TMPro.TextMeshProUGUI` resolvable trong project. Task 4 phụ thuộc cái đầu; S2 phụ thuộc cái sau.

Test tra type bằng cách quét assembly đã load, **không** qua asmdef reference — nhờ vậy asmdef test không phải phụ thuộc URP hay TMP.

- [ ] **Step 1: Viết test thất bại**

```bash
cat > Assets/Tests/EditMode/Config/PackageManifestTests.cs <<'EOF'
using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace KMA.Tests.Config
{
    public sealed class PackageManifestTests
    {
        static string Manifest => File.ReadAllText("Packages/manifest.json");

        static Type FindLoadedType(string fullName) => AppDomain.CurrentDomain
            .GetAssemblies()
            .Select(assembly => assembly.GetType(fullName, false))
            .FirstOrDefault(type => type != null);

        [Test]
        public void UniversalRenderPipelineIsInstalled()
        {
            Assert.That(Manifest, Does.Contain("\"com.unity.render-pipelines.universal\""));
            Assert.That(FindLoadedType("UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset"),
                Is.Not.Null, "URP đã khai báo trong manifest nhưng type không resolve được.");
        }

        [Test]
        public void UguiAndTextMeshProAreInstalled()
        {
            Assert.That(Manifest, Does.Contain("\"com.unity.ugui\""));
            Assert.That(FindLoadedType("TMPro.TextMeshProUGUI"), Is.Not.Null,
                "TextMeshPro nằm trong com.unity.ugui ở Unity 6 — thiếu package này thì S2 không dựng được UI.");
        }

        [Test]
        public void SpriteEditorPackageIsInstalled()
        {
            Assert.That(Manifest, Does.Contain("\"com.unity.2d.sprite\""));
        }

        [Test]
        public void MultiplayerCenterIsRemoved()
        {
            Assert.That(Manifest, Does.Not.Contain("com.unity.multiplayer.center"),
                "Package rác, project offline hoàn toàn không dùng multiplayer.");
        }
    }
}
EOF
```

- [ ] **Step 2: Chạy test để xác nhận nó fail**

```bash
~/.local/bin/unity test --mode EditMode --filter PackageManifestTests \
  --output /tmp/kma-pkg.xml --timeout 1200
```

Expected: FAIL cả 4 test — URP/ugui/2d.sprite chưa có trong manifest, và `com.unity.multiplayer.center` đang có.

- [ ] **Step 3: Sửa manifest**

```bash
cd /home/duydt/project/the-chat-KMA
python3 - <<'PY'
import json, collections
p = 'Packages/manifest.json'
m = json.load(open(p), object_pairs_hook=collections.OrderedDict)
deps = m['dependencies']
deps.pop('com.unity.multiplayer.center', None)
deps['com.unity.render-pipelines.universal'] = '17.3.0'
deps['com.unity.ugui'] = '2.0.0'
deps['com.unity.2d.sprite'] = '1.0.0'
m['dependencies'] = collections.OrderedDict(sorted(deps.items()))
json.dump(m, open(p, 'w'), indent=2)
open(p, 'a').write('\n')
print('deps =', len(m['dependencies']))
PY
git diff --stat Packages/manifest.json
```

Version lấy từ manifest của chính editor này (`Editor/Data/Resources/PackageManager/Editor/manifest.json`): URP `17.3.0`, ugui `2.0.0`, 2d.sprite `1.0.0`. Pin version thay vì để resolver tự chọn, để diff review được và tái lập được trên máy khác.

- [ ] **Step 4: Cho Unity resolve package**

```bash
~/.local/bin/unity test --mode EditMode --filter PackageManifestTests \
  --output /tmp/kma-pkg.xml --timeout 2400
python3 -c "
import xml.etree.ElementTree as ET
r=ET.parse('/tmp/kma-pkg.xml').getroot()
print('total',r.get('total'),'passed',r.get('passed'),'failed',r.get('failed'))"
```

Expected: `failed 0`, `total 4`. Lần chạy này Unity tải URP + dependency của nó (`com.unity.render-pipelines.core` `17.3.0`) nên chậm.

Nếu resolve lỗi vì version không tồn tại: đọc version editor gợi ý rồi thay vào Step 3.

```bash
python3 -c "
import json
m=json.load(open('/home/duydt/Unity/Hub/Editor/6000.3.23f1/Editor/Data/Resources/PackageManager/Editor/manifest.json'))
for k in ['com.unity.render-pipelines.universal','com.unity.ugui','com.unity.2d.sprite']:
    print(k, m['packages'][k])"
```

- [ ] **Step 5: Chạy full suite**

```bash
~/.local/bin/unity test --mode EditMode --output /tmp/kma-editmode.xml --timeout 2400
~/.local/bin/unity test --mode PlayMode --output /tmp/kma-playmode.xml --timeout 2400
```

Expected: `failed = 0` cả hai. EditMode total = baseline + 8.

- [ ] **Step 6: Commit**

```bash
git add Packages/manifest.json Packages/packages-lock.json Assets/Tests/EditMode/Config
git commit -m "chore: thêm URP 17.3.0, ugui 2.0.0, 2d.sprite 1.0.0

Xoá com.unity.multiplayer.center — project offline, không dùng.
Version pin theo manifest của editor 6000.3.23f1 để tái lập được.
TextMeshPro nằm trong com.unity.ugui ở Unity 6, S2 cần nó cho UI."
```

---

## Task 4: URP 2D asset, gán vào Graphics và Quality Settings

**Files:**
- Create: `Assets/_Project/Settings/URP/URP-2D.asset` (qua Editor menu)
- Create: `Assets/_Project/Settings/URP/URP-2D_Renderer2D.asset` (qua Editor menu)
- Create: `Assets/Editor/UrpBootstrap.cs`
- Create: `Assets/Tests/EditMode/Config/RenderPipelineTests.cs`
- Modify (do Unity ghi): `ProjectSettings/GraphicsSettings.asset`, `ProjectSettings/QualitySettings.asset`

**Interfaces:**
- Consumes: type `UniversalRenderPipelineAsset` từ Task 3.
- Produces: `GraphicsSettings.defaultRenderPipeline` là một `UniversalRenderPipelineAsset` dùng `Renderer2DData`. S2 dựa vào đây khi thêm `UniversalAdditionalCameraData` cho `GameCamera.prefab`.

Asset URP tạo bằng **Editor menu**, không bằng script: tạo `Renderer2DData` từ script phải chọc vào `m_RendererDataList` qua `SerializedObject` với tên field private không có bảo đảm ổn định. Asset là file commit được, nên tạo tay một lần là đủ; phần tự động hoá đáng giá là **test canh nó**, không phải script tạo nó.

- [ ] **Step 1: Viết test thất bại**

```bash
cat > Assets/Tests/EditMode/Config/RenderPipelineTests.cs <<'EOF'
using NUnit.Framework;
using UnityEditor;
using UnityEngine.Rendering;

namespace KMA.Tests.Config
{
    public sealed class RenderPipelineTests
    {
        [Test]
        public void DefaultPipelineIsUniversal()
        {
            var pipeline = GraphicsSettings.defaultRenderPipeline;
            Assert.That(pipeline, Is.Not.Null,
                "Chưa gán URP asset ở Project Settings > Graphics.");
            Assert.That(pipeline.GetType().FullName,
                Is.EqualTo("UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset"));
        }

        [Test]
        public void UniversalPipelineUses2DRenderer()
        {
            var pipeline = GraphicsSettings.defaultRenderPipeline;
            Assert.That(pipeline, Is.Not.Null);

            var serialized = new SerializedObject(pipeline);
            var rendererList = serialized.FindProperty("m_RendererDataList");
            Assert.That(rendererList, Is.Not.Null,
                "Không tìm được m_RendererDataList — tên field URP đã đổi, xem YAML của asset.");
            Assert.That(rendererList.arraySize, Is.GreaterThan(0));

            var rendererData = rendererList.GetArrayElementAtIndex(0).objectReferenceValue;
            Assert.That(rendererData, Is.Not.Null);
            Assert.That(rendererData.GetType().Name, Is.EqualTo("Renderer2DData"),
                "Spec S1 chốt URP **2D** Renderer, không phải Universal Renderer.");
        }

        [Test]
        public void PipelineAssetLivesInProjectSettingsFolder()
        {
            var pipeline = GraphicsSettings.defaultRenderPipeline;
            Assert.That(pipeline, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(pipeline),
                Does.StartWith("Assets/_Project/Settings/URP/"));
        }
    }
}
EOF

~/.local/bin/unity test --mode EditMode --filter RenderPipelineTests \
  --output /tmp/kma-urp.xml --timeout 1200
```

Expected: FAIL cả 3 — `GraphicsSettings.defaultRenderPipeline` đang `null`.

- [ ] **Step 2: Tạo URP 2D asset bằng Editor menu**

Mở Editor:

```bash
~/.local/bin/unity open /home/duydt/project/the-chat-KMA
```

Trong Editor, làm đúng thứ tự:
1. Project window → tạo thư mục `Assets/_Project/Settings/URP` nếu chưa có.
2. Chuột phải vào thư mục đó → `Create > Rendering > URP Asset (with 2D Renderer)`.
3. Đặt tên `URP-2D`. Unity sinh kèm `URP-2D_Renderer2D`.
4. Chọn `URP-2D`, ở Inspector **tắt** `HDR` và `Post-processing` (spec S1: không post-process).
5. `Edit > Project Settings > Graphics` → gán `URP-2D` vào `Default Render Pipeline`.
6. `Edit > Project Settings > Quality` → với **mọi** quality level, để `Render Pipeline Asset` trống (thừa hưởng default) hoặc gán `URP-2D`.
7. `File > Save Project`. Đóng Editor.

- [ ] **Step 3: Chạy test để xác nhận nó pass**

```bash
~/.local/bin/unity test --mode EditMode --filter RenderPipelineTests \
  --output /tmp/kma-urp.xml --timeout 1200
python3 -c "
import xml.etree.ElementTree as ET
r=ET.parse('/tmp/kma-urp.xml').getroot()
print('total',r.get('total'),'passed',r.get('passed'),'failed',r.get('failed'))"
```

Expected: `failed 0`, `total 3`.
Nếu `UniversalPipelineUses2DRenderer` fail vì tên field: xem YAML thật rồi sửa tên trong test.

```bash
grep -nE "m_Renderer|Renderer2D" Assets/_Project/Settings/URP/URP-2D.asset | head
```

- [ ] **Step 4: Viết `UrpBootstrap` để tái lập được trên máy khác**

```bash
cat > Assets/Editor/UrpBootstrap.cs <<'EOF'
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace KMA.EditorTools
{
    /// <summary>
    /// Gán lại URP asset đã commit vào Graphics Settings. Dùng khi clone mới hoặc
    /// khi Graphics Settings bị mất tham chiếu. Không tạo asset — asset đã ở trong git.
    /// </summary>
    public static class UrpBootstrap
    {
        const string PipelineAssetPath = "Assets/_Project/Settings/URP/URP-2D.asset";

        [MenuItem("KMA/Reassign URP Pipeline")]
        public static void Reassign()
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(PipelineAssetPath);
            if (pipeline == null)
            {
                Debug.LogError($"[KMA] Không tìm thấy {PipelineAssetPath}.");
                EditorApplication.Exit(1);
                return;
            }

            GraphicsSettings.defaultRenderPipeline = pipeline;
            AssetDatabase.SaveAssets();
            Debug.Log($"[KMA] URP pipeline gán từ {PipelineAssetPath}.");
        }
    }
}
EOF

~/.local/bin/unity test --mode EditMode --filter RenderPipelineTests \
  --output /tmp/kma-urp.xml --timeout 1200
```

Expected: vẫn `failed 0` — thêm script không đổi trạng thái.

- [ ] **Step 5: Chạy full suite**

```bash
~/.local/bin/unity test --mode EditMode --output /tmp/kma-editmode.xml --timeout 2400
~/.local/bin/unity test --mode PlayMode --output /tmp/kma-playmode.xml --timeout 2400
```

Expected: `failed = 0` cả hai. EditMode total = baseline + 11.

Cảnh báo cần để ý trong log: scene hiện có dùng shader Built-in sẽ hiện hồng khi chạy URP. Ở S1 chưa có SpriteRenderer nào nên không ảnh hưởng; S2 dựng art bằng shader URP từ đầu.

- [ ] **Step 6: Commit**

```bash
git add Assets/_Project/Settings Assets/Editor/UrpBootstrap.cs \
  Assets/Tests/EditMode/Config/RenderPipelineTests.cs \
  ProjectSettings/GraphicsSettings.asset ProjectSettings/QualitySettings.asset
git commit -m "chore: dựng URP 2D pipeline, tắt HDR và post-process

Asset tạo bằng Editor menu và commit vào git; test canh việc gán và
canh đúng Renderer2D. UrpBootstrap để gán lại khi clone mới."
```

---

## Task 5: Cây thư mục PLAN §2.6 và độ trễ audio

**Files:**
- Create: `Assets/Tests/EditMode/Config/ProjectLayoutTests.cs`
- Create: 14 thư mục dưới `Assets/_Project/` (kèm `.gitkeep`)
- Modify: `ProjectSettings/AudioManager.asset:11`

**Interfaces:**
- Consumes: asmdef test từ Task 2.
- Produces: cây thư mục mà S2 (`Scripts/UI`, `Prefabs/UI`, `Fonts`), S3 (`Settings/Input`), S4 (`ScriptableObjects/Subjects`) ghi file vào.

- [ ] **Step 1: Viết test thất bại**

```bash
cat > Assets/Tests/EditMode/Config/ProjectLayoutTests.cs <<'EOF'
using System.IO;
using NUnit.Framework;

namespace KMA.Tests.Config
{
    public sealed class ProjectLayoutTests
    {
        static readonly string[] RequiredFolders =
        {
            "Assets/_Project/Art/Characters",
            "Assets/_Project/Art/Environments",
            "Assets/_Project/Art/UI",
            "Assets/_Project/Art/FX",
            "Assets/_Project/Audio/Music",
            "Assets/_Project/Audio/SFX",
            "Assets/_Project/Fonts",
            "Assets/_Project/Prefabs/UI",
            "Assets/_Project/Prefabs/Gameplay",
            "Assets/_Project/Settings/URP",
            "Assets/_Project/Settings/Input",
            "Assets/_Project/Settings/AudioMixer",
            "Assets/_Project/ScriptableObjects/Subjects",
            "Assets/_Project/ScriptableObjects/Rhythm",
            "Assets/_Project/ScriptableObjects/Difficulty",
            "Assets/_Project/ScriptableObjects/Quotes"
        };

        [Test]
        public void PlanFolderTreeExists()
        {
            foreach (var folder in RequiredFolders)
            {
                Assert.That(Directory.Exists(folder), Is.True, $"Thiếu thư mục {folder} (PLAN §2.6).");
            }
        }

        [Test]
        public void AudioDspBufferIsBestLatency()
        {
            var yaml = File.ReadAllText("ProjectSettings/AudioManager.asset");
            Assert.That(yaml, Does.Match(@"m_RequestedDSPBufferSize:\s*256"),
                "DSP buffer phải là Best latency (256) — chạy bền và boss tính nhịp theo dspTime.");
        }
    }
}
EOF

~/.local/bin/unity test --mode EditMode --filter ProjectLayoutTests \
  --output /tmp/kma-layout.xml --timeout 1200
```

Expected: FAIL cả 2 — thư mục chưa tồn tại, `m_RequestedDSPBufferSize` đang là `0`.

- [ ] **Step 2: Tạo thư mục**

```bash
cd /home/duydt/project/the-chat-KMA
for d in Art/Characters Art/Environments Art/UI Art/FX \
         Audio/Music Audio/SFX Fonts Prefabs/UI Prefabs/Gameplay \
         Settings/URP Settings/Input Settings/AudioMixer \
         ScriptableObjects/Subjects ScriptableObjects/Rhythm \
         ScriptableObjects/Difficulty ScriptableObjects/Quotes; do
  mkdir -p "Assets/_Project/$d"
  touch "Assets/_Project/$d/.gitkeep"
done
find Assets/_Project -name .gitkeep | wc -l
```

Expected: `16`.

- [ ] **Step 3: Đặt DSP buffer = Best latency**

```bash
sed -i 's/^  m_RequestedDSPBufferSize: 0$/  m_RequestedDSPBufferSize: 256/' \
  ProjectSettings/AudioManager.asset
grep -n "m_RequestedDSPBufferSize\|m_DSPBufferSize" ProjectSettings/AudioManager.asset
```

Expected: `m_DSPBufferSize: 1024` (giá trị hiện hành Unity tính ra) và `m_RequestedDSPBufferSize: 256`.
`m_RequestedDSPBufferSize` là field ứng với dropdown "DSP Buffer Size": Default `0`, Best latency `256`, Good latency `512`, Best performance `1024`.

- [ ] **Step 4: Chạy test để xác nhận nó pass**

```bash
~/.local/bin/unity test --mode EditMode --filter ProjectLayoutTests \
  --output /tmp/kma-layout.xml --timeout 1200
python3 -c "
import xml.etree.ElementTree as ET
r=ET.parse('/tmp/kma-layout.xml').getroot()
print('total',r.get('total'),'passed',r.get('passed'),'failed',r.get('failed'))"
```

Expected: `failed 0`, `total 2`.

- [ ] **Step 5: Chạy full suite**

```bash
~/.local/bin/unity test --mode EditMode --output /tmp/kma-editmode.xml --timeout 2400
~/.local/bin/unity test --mode PlayMode --output /tmp/kma-playmode.xml --timeout 2400
```

Expected: `failed = 0` cả hai. EditMode total = baseline + 13.

- [ ] **Step 6: Commit**

```bash
git add Assets/_Project ProjectSettings/AudioManager.asset \
  Assets/Tests/EditMode/Config/ProjectLayoutTests.cs
git commit -m "chore: dựng cây thư mục PLAN §2.6, DSP buffer Best latency

DSP 256 là bắt buộc cho chạy bền và boss — cả hai tính nhịp theo
AudioSettings.dspTime, latency mặc định làm lệch judge."
```

---

## Task 6: Build APK Android và chạy trên máy thật

**Files:**
- Create: `Assets/Editor/BuildScript.cs`
- Create: `Builds/` (đã có trong `.gitignore` — không commit)

**Interfaces:**
- Consumes: asmdef `KMA.EditorTools` (Task 2), ProjectSettings đã áp (Task 2), URP đã gán (Task 4).
- Produces: `KMA.EditorTools.BuildScript.BuildAndroid()` — `public static void`, đọc `-buildOutput`, `EditorApplication.Exit(1)` khi build fail. S16 dùng lại cho build release.

- [ ] **Step 1: Viết build script**

```bash
cat > Assets/Editor/BuildScript.cs <<'EOF'
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace KMA.EditorTools
{
    public static class BuildScript
    {
        const string DefaultOutputPath = "Builds/Android/kma.apk";

        public static void BuildAndroid()
        {
            var outputPath = ReadArgument("-buildOutput") ?? DefaultOutputPath;
            var directory = Path.GetDirectoryName(Path.GetFullPath(outputPath));
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                Debug.LogError("[KMA] EditorBuildSettings không có scene nào bật.");
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log($"[KMA] Build {scenes.Length} scene → {outputPath}");

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None
            };

            var summary = BuildPipeline.BuildPlayer(options).summary;
            Debug.Log($"[KMA] Build {summary.result}, {summary.totalSize} bytes, " +
                $"{summary.totalErrors} error, {summary.totalWarnings} warning.");

            if (summary.result != BuildResult.Succeeded)
            {
                EditorApplication.Exit(1);
                return;
            }

            EditorApplication.Exit(0);
        }

        static string ReadArgument(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (var index = 0; index < args.Length - 1; index++)
            {
                if (string.Equals(args[index], name, StringComparison.Ordinal))
                {
                    return args[index + 1];
                }
            }

            return null;
        }
    }
}
EOF
```

- [ ] **Step 2: Compile-check bằng cách chạy full suite**

```bash
~/.local/bin/unity test --mode EditMode --output /tmp/kma-editmode.xml --timeout 2400
```

Expected: `failed = 0`, total không đổi so với Task 5 (script mới không thêm test). Nếu lỗi compile về `BuildTargetGroup` hay `BuildPlayerOptions`, sửa rồi chạy lại.

- [ ] **Step 3: Commit script trước khi build**

`unity build` mặc định chặn build khi working tree bẩn, nên commit trước.

```bash
git add Assets/Editor/BuildScript.cs
git commit -m "chore: thêm build script Android headless

Đọc -buildOutput, exit 1 khi build fail — dùng được trong CI."
```

- [ ] **Step 4: Build APK**

```bash
cd /home/duydt/project/the-chat-KMA
~/.local/bin/unity build --target Android \
  --execute-method KMA.EditorTools.BuildScript.BuildAndroid \
  -o Builds/Android/kma.apk \
  --android-export-type apk \
  -l /tmp/kma-build.log
echo "exit=$?"
ls -lh Builds/Android/kma.apk
```

Expected: `exit=0` và file APK tồn tại. Lần build IL2CPP đầu tiên rất lâu (15–40 phút).
Nếu fail vì thiếu NDK/SDK: `~/.local/bin/unity install-modules -e 6000.3.23f1 -m android-sdk-ndk-tools --cm --accept-eula -y`.

- [ ] **Step 5: Kiểm nội dung APK khớp spec**

```bash
grep -nE "\[KMA\] Build" /tmp/kma-build.log
python3 - <<'PY'
import zipfile
z = zipfile.ZipFile('Builds/Android/kma.apk')
names = z.namelist()
print('libs =', sorted(n for n in names if n.endswith('.so') and 'lib/' in n)[:6])
print('có arm64-v8a  :', any('arm64-v8a' in n for n in names))
print('có armeabi-v7a:', any('armeabi-v7a' in n for n in names))
PY
```

Expected: `có arm64-v8a: True`, `có armeabi-v7a: False` (spec chốt ARM64).

- [ ] **Step 6: Cài lên máy Android thật**

```bash
adb devices
adb install -r Builds/Android/kma.apk
adb shell monkey -p com.kma.thechat -c android.intent.category.LAUNCHER 1
adb logcat -d -s Unity | tail -40
```

Expected: cài thành công, app mở, **khoá landscape**, không crash. Màn hình sẽ trống — S1 chưa có UI nào, đó là đúng.
Kiểm tay trên máy: tên app hiện `Thể Chất KMA`; quay máy sang portrait, app **không** quay theo.

- [ ] **Step 7: Ghi lại kết quả gate vào README**

```bash
cd /home/duydt/project/the-chat-KMA
python3 - <<'PY'
import io, xml.etree.ElementTree as ET
edit = ET.parse('/tmp/kma-editmode.xml').getroot()
play = ET.parse('/tmp/kma-playmode.xml').getroot()
p = 'README.md'
s = io.open(p, encoding='utf-8').read()
old = 'The latest verified suite passed `121` EditMode tests and `37` PlayMode tests.'
new = (f"The latest verified suite passed `{edit.get('passed')}` EditMode tests and "
       f"`{play.get('passed')}` PlayMode tests on Unity `6000.3.23f1`.")
assert old in s, 'câu cũ trong README đã đổi — cập nhật tay'
io.open(p, 'w', encoding='utf-8').write(s.replace(old, new, 1))
print(new)
PY
```

- [ ] **Step 8: Commit**

```bash
git add README.md
git commit -m "docs: cập nhật số test và version đã xác minh

Gate S1 xong: compile sạch, test xanh, APK ARM64 cài và chạy trên máy
Android thật, khoá landscape đúng."
```

---

## Gate S1 — điều kiện coi là xong

- [ ] `unity test --mode EditMode` → `failed = 0`, total = baseline + 13
- [ ] `unity test --mode PlayMode` → `failed = 0`, total = baseline
- [ ] `ProjectVersion.txt` pin `6000.3.23f1`, README khớp
- [ ] `Packages/manifest.json`: có URP `17.3.0`, ugui `2.0.0`, 2d.sprite `1.0.0`; **không** có `multiplayer.center`
- [ ] `GraphicsSettings.defaultRenderPipeline` là URP asset dùng `Renderer2DData`, HDR và post-process tắt
- [ ] 16 thư mục PLAN §2.6 tồn tại
- [ ] `m_RequestedDSPBufferSize: 256`
- [ ] APK build được, chỉ chứa `arm64-v8a`
- [ ] APK cài và mở được trên máy Android thật, tên `Thể Chất KMA`, khoá landscape
- [ ] Scene bị Editor ghi lại đã commit **riêng**, tách khỏi commit config

## Ngoài phạm vi S1 — đừng làm ở đây

- Camera prefab, HUD, `UITheme`, font tiếng Việt → **S2**
- Detector input, gộp `KMA.inputactions` → **S3**
- `SaveSystem`, `AudioManager`, `HapticsService`, và `Application.targetFrameRate = 60` + vSync off (spec đặt trong `GameManager`) → **S4**
- App icon, tắt splash Unity → **S16** (đó là hạng mục release, không phải toolchain)
