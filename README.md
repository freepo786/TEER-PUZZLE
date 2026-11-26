# Teer Puzzle — Unity Project

**Version: 1.4 — CI scene-build step + banner-hide (testing only)**

> Ye version sirf testing ke liye hai. Google Play Store par upload abhi NAHI karna.
> Aage ke versions (1.5, 2.1, ...) isi project par banenge — dekhiye `CHANGELOG.md`.

## Ye kya hai

Teer Puzzle aik 2D arrow puzzle game hai (Arrow Puzzle jaisi, lekin sab kuch original):
grid par teer hain — tap karo to teer apni simt mein slide kar ke bahar nikle (agar raasta
khaali ho); blocked tap par bump aur 1 dil kam. Saare teer nikalo to level mukammal.

## Version 1.4 mein kya hai (naya)

- **CI scene-build step:** GitHub Actions workflow ab build se pehle SceneBuilder chalata hai — `MainScene.unity` CI mein banti hai aur Build Settings mein register hoti hai (pehle scene ke baghair build hoti)
- **Banner-hide:** Remove Ads khareedne par pehle se dikh raha banner foran hide ho jata hai
- Final QA pass — pagination, Daily Challenge, interstitial cadence, HUD sound save sab dobara check
- Version 1.4.0, Android version code 4 (levels 2000 hi, ads/IAP 1.2 se waisi hi — test mode)

## Version 1.3 mein kya hai

- Level map pagination: 100 levels per page, 20 pages; default page wohi jis mein highest unlocked level ho; stars/locks waisi hi
- Daily Challenge rotation fix: ab days-since-epoch se index banta hai — saare 2000 levels rotation mein aate hain (pehle DayOfYear sirf pehle ~366 levels tak mehdood tha)
- Version 1.3.0, Android version code 3 (ads/IAP 1.2 se waisi hi — test mode)

## Version 1.2 mein kya hai (naya)

- 2000 solver-verified launch levels — Easy 600, Medium 600, Hard 500, Expert 300 (100 se barha kar)
- AppLovin MAX ads — **test mode** (test ad units, koi asli paisa nahi):
  interstitial har 3-4 levels ke baad, rewarded ad hint/extra life ke liye,
  banner sirf level map par — game board par kabhi nahi
- Remove Ads one-time IAP — **test product** (asli product baad mein)
- Game Over par "Ad dekh kar 1 dil" — rewarded ad se 1 dil wapas
- Settings screen: sound, vibration, language (English/Urdu — Urdu UI V2 mein),
  privacy policy link, Remove Ads
- Splash screen: logo + title, 1.5 second
- **Undo button hata diya gaya** (Rehan ka faisla) — HUD mein ab sirf Hint hai

> **Test mode ka matlab:** ads aur IAP abhi test IDs/products se chalte hain.
> Asli ads dikhane se pehle Rehan ka ad account, asli ad unit IDs aur Play
> Console mein IAP product chahiye (neeche "Rehan se kya chahiye" dekhen).

## Version 1.1 mein kya hai

- Core gameplay: grid + path check, cube arrows, 3 lives, hint, undo (3 per level),
  move counter, stars (1–3), win screen ("Level X mukammal" + stats + sirf "Agla Level"
  button), lose screen ("Dobara Khelo"), local save (JSON)
- 100 solver-verified levels — Easy 30, Medium 30, Hard 25, Expert 15 (JSON)
- 6 color themes — Classic, Black & White, Neon, Sunset, Ocean, Candy (dropdown se)
- 11 original sound effects (.wav), background music nahi
- Original art + app icon — Easybrain ki koi asset nahi
- **Nahi hai:** ads, analytics, Play Store signing/publishing, backend/server

## Project structure

```
teer-puzzle-unity/
├── Assets/
│   ├── Scripts/          # C# gameplay code (namespace TeerPuzzle)
│   ├── Levels/           # Level JSON (source) + generate_levels.py
│   ├── Art/              # Original sprites + icon (source)
│   ├── Audio/            # 11 SFX .wav (source)
│   ├── Resources/
│   │   ├── Levels/       # Level JSON ki copies (runtime load)
│   │   ├── Art/          # Sprites ki copies (runtime load)
│   │   └── Audio/        # SFX ki copies (runtime load)
│   ├── Scenes/           # MainScene.unity (SceneBuilder se banti hai)
│   └── Editor/           # ProjectSetup.cs, SceneBuilder.cs
├── ProjectSettings/      # PlayerSettings (bundle id placeholder)
├── README.md
└── CHANGELOG.md
```

## Zaroorat (requirements)

- **Unity 6000.3 (Unity 6.3)** — Unity Hub se install karein
- **Android Build Support** module (Unity Hub → Installs → Add Modules):
  Android SDK + NDK Tools + OpenJDK (Unity ke saath aata hai)
- **JDK** — Unity ke built-in OpenJDK kaafi hai (alga install nahi chahiye)
- Windows ya Mac par Unity Editor; Linux par bhi chal sakta hai (headless build)
- Is VM par Unity nahi hai aur RAM kam hai (~700 MB free) — is liye APK yahan
  compile nahi hogi. Build Rehan ke PC ya VPS (8 GB+ RAM) par hogi, **ya**
  GitHub Actions se free cloud build (neeche section dekhen).

## Unity mein kholne ke steps

1. Unity Hub kholen → **Add** → ye folder (`teer-puzzle-unity`) select karein.
2. Unity version **6000.3** select karein, project kholne dein (pehli dafa import
   mein kuch minute lag sakte hain — Library folder banega).
3. Menu: **Teer Puzzle → Apply Project Settings (v1.4)** — naam, package id
   (`com.rehan.teerpuzzle` — placeholder, Rehan baad mein final karega), IL2CPP,
   minSdk 26 / targetSdk 36, version 1.4.0 / version code 4 lag jayenge.
4. Menu: **Teer Puzzle → Build Main Scene** — MainScene.unity ban jayegi
   (Canvas, HUD, board, overlays — scripts se).
5. **Play** dabayen — pehla level khel kar dekhen. Console mein koi error nahi
   honi chahiye.

## Android build (testing ke liye)

1. **File → Build Settings** → Platform **Android** → **Switch Platform**.
2. **Player Settings** mein check karein: Package Name `com.rehan.teerpuzzle`
   (placeholder), Version 1.4.0, Version Code 4, Scripting Backend **IL2CPP**,
   Target Architectures **ARM64**.
3. **Build** → APK banayen (testing ke liye APK kaafi hai; AAB baad mein,
   publishing phase mein).
4. APK phone par install kar ke khelen.

> Signing: testing ke liye Unity ki debug key kaafi hai. Play Store ke liye
> release keystore baad mein banega (publishing phase — abhi nahi).

## GitHub Actions se free APK build (bina Unity ke)

Agar aap ke PC par Unity install nahi hai (jagah kam hai ya VPS nahi hai), to
GitHub ke free cloud runners par APK ban sakti hai — game-ci (Unity Builder)
ke zariye. Koi Unity install nahi chahiye, koi VPS nahi chahiye.

> **Free limits:** public repo par GitHub Actions minutes **unlimited** hain;
> private repo par **2,000 min/month** free hain. Pehli build 20–40 min le
> sakti hai (Library cache pehli dafa banta hai); us ke baad cache se build
> tez hoti hai.

### Steps (aik dafa setup)

1. **GitHub par naya repo banayen** (public ya private) aur is project ko
   push karen:
   ```bash
   git init
   git add .
   git commit -m "Teer Puzzle v1.4 scene-build step + banner-hide"
   git branch -M main
   git remote add origin https://github.com/<aapka-username>/teer-puzzle.git
   git push -u origin main
   ```
   > ⚠️ `.ulf` license file ya koi password **kabhi** repo mein push na karen —
   > wo sirf GitHub Secrets mein dalni hai (neeche step 4).
2. **Activation file banayen:** GitHub repo → **Actions** tab → workflow
   **"Unity License Activation"** → **Run workflow**. Thori der mein
   **Artifacts** mein `Unity_v6000.3.21f1.alf` file milegi — download karen.
3. **License (.ulf) banayen:** https://license.unity3d.com/manual par jayen,
   wahan `.alf` file upload karen — Unity aap ko `.ulf` license file dega.
   (Unity ID se sign in karna hoga; Personal license free hai.)
4. **3 Secrets add karen:** repo → **Settings → Secrets and variables →
   Actions → New repository secret**:

   | Secret | Kya dalna hai |
   |---|---|
   | `UNITY_LICENSE` | `.ulf` file ka **poora text** (file khol kar copy-paste karen) |
   | `UNITY_EMAIL` | Aap ki Unity ID ka email |
   | `UNITY_PASSWORD` | Aap ki Unity ID ka password |

5. **Build chalayen:** `main` branch par koi bhi push karte hi **"Build Android
   APK (v1.4)"** workflow khud chal jayega — ya Actions tab se **Run workflow**
   se manual bhi chala sakte hain.
6. **APK download karen:** workflow khatam ho to **Actions → us run →
   Artifacts → `TeerPuzzle-Android-1.4.0`** — is mein `TeerPuzzle.apk` hogi.
   Phone par install kar ke test karen.

### Notes

- Ye **debug/test APK** hai — Play Store ke liye release signing/keystore
  baad mein (publishing phase) aayega.
- Version `1.4.0` workflow mein likhi hai (`.github/workflows/build-android.yml`
  mein `version: 1.4.0`, `androidVersionCode: 4`) — naye version par wahan update karen.
- Agar build fail ho to pehle **Actions run ka log** dekhen — aam wajuhaat:
  license secret ghalat/na-mukammal ho, ya `ProjectVersion.txt` wali Unity
  version ka game-ci image na mila ho.

## Level format

Har level aik JSON file hai (`Assets/Levels/level_001.json`):

```json
{
  "id": 1, "pack": "easy", "width": 5, "height": 5, "lives": 3,
  "arrows": [
    {"color": 0, "nodalPoints": [{"column": 1, "row": 4}, {"column": 1, "row": 3}]}
  ]
}
```

- `path` = tail se head tak cells; head = aakhri cell; exit direction =
  second-last cell se last cell ki taraf.
- Naye levels: `python3 Assets/Levels/generate_levels.py` — generator har level
  ko solver se check karta hai (hal mumkin hai ya nahi), phir save karta hai.

## Rehan se kya chahiye (abhi baqi)

- Final app naam aur package name (placeholder `com.rehan.teerpuzzle` hai)
- V1 checklist ke faisle (structure doc wali checklist — kya V1 mein rakhen)
- Play Console account — **baad mein**, publishing phase mein (abhi nahi)
- **1.2 ke liye (asli ads/IAP se pehle):** AppLovin MAX ad account + asli ad unit
  IDs (interstitial, rewarded, banner), Play Console mein Remove Ads one-time
  IAP product, aur privacy policy ka asal link (abhi
  `https://example.com/teer-puzzle-privacy` placeholder hai — UIManager.cs mein
  `PrivacyPolicyUrl` badalna hoga)

## Assembly notes (v1.2)

- **SceneBuilder** (`Assets/Editor/SceneBuilder.cs`): menu **Teer Puzzle → Build Main Scene (v1.2)**
  se MainScene.unity banti hai — 10 managers, HUD (Level/Moves/dil/hint/sound/theme
  dropdown — undo nahi), level map (scrollable, stars + locks, Settings button),
  win overlay ("Level X mukammal", aik aik karke pop hone wale stars, sirf
  "Agla Level" button), lose overlay ("Dobara Khelo" + "Ad dekh kar 1 dil"),
  theme dropdown, splash screen (logo + title, 1.5s), settings screen (sound,
  vibration, language, privacy policy, Remove Ads, band karo). Board world-space
  sprites hai, is liye Canvas ke beech mein koi UI raycast target nahi — taps
  seedha teeron tak jate hain.
- **BoardCameraFit** (`Assets/Scripts/BoardCameraFit.cs`): har level load par camera
  ko board ke size par fit karta hai (4x4 se 10x10 tak, portrait phones par).
- **UIManager patch:** runtime mein banne wale level/theme buttons par `LayoutElement`
  (minHeight 96) lagaya gaya hai, taake VerticalLayoutGroup mein saaf dikhen.
- **ThemeManager.boardImage** jaan-boojh kar khaali hai — board world-space hai, UI
  Image nahi. Theme camera background + teeron + top bar (panelImage) par lagta hai.
- **Compile check:** is VM par C# compiler nahi hai — Unity Editor mein khol kar
  pehli dafa compile errors check karna lazmi hai. Braces aur structure haath se
  verify kiye gaye hain, lekin Editor compile hi final tasdeeq hai.

## Notes

- Poora game 40 MB se kam ka target hai (art/audio chhota rakha gaya hai).
- Koi teesri party ka SDK V1 core mein nahi tha — ads (AppLovin MAX, test mode)
  aur Remove Ads IAP (test product) 1.2 mein aaye; asli ad unit IDs aur IAP
  product Rehan se baqi hain.
- Sab art, sounds aur levels original hain — Easybrain ka kuch copy nahi kiya.
