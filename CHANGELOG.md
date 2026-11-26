# Changelog — Teer Puzzle

## [1.4.0] — CI scene-build step + banner-hide (testing only) — 2026-11-26
- **CI scene-build step (sab se zaroori fix):** `Assets/Scenes/` khaali thi — `MainScene.unity` repo mein nahi thi aur workflow mein SceneBuilder chalane ka koi step nahi tha, is liye CI build ke paas build karne ko scene hi nahi hoti. Ab build step se pehle aik step `TeerPuzzle.Editor.SceneBuilder.BuildMainScene` ko `-executeMethod` se chalata hai (builder wali image `unityci/editor:ubuntu-6000.3.21f1-android-3`, wohi license secrets) — scene banti hai aur Build Settings mein register hoti hai, phir build step enabled scenes banata hai.
- **Banner-hide follow-up:** Remove Ads khareedne par pehle se dikh raha banner foran hide hota hai — AdManager ab `IAPManager.OnRemoveAdsChanged` par subscribe karta hai aur `HideBanner()` call karta hai (test mode mein banner hota hi nahi).
- Final QA pass: pagination 100/page (20 pages, 2000 levels), Daily Challenge rotation (days-since-epoch % TotalLevels), interstitial cadence save, HUD sound save — sab dobara check, theek.
- Version 1.4.0, Android version code 4. Levels 2000 hi (generator nahi chalaya).

## [1.3.0] — Level map pagination + Daily Challenge fix (testing only) — 2026-11-26
- Level map pagination: 100 levels per page, 20 pages; default page wohi jis mein highest unlocked level ho; stars/locks waisi hi.
- Daily Challenge rotation fix: ab days-since-epoch (1 Jan 1970 se din) se index banta hai, is liye saare 2000 levels rotation mein aate hain — pehle DayOfYear (1..366) sirf pehle ~366 levels tak mehdood tha.
- QA pass (1.2 flows dobara check — static trace): interstitial sirf level mukammal hone ke baad har 3rd level, kabhi mid-level nahi; rewarded hint/extra life, banner sirf map par, Remove Ads flag save/restore, Settings toggles persist, Splash 1.5s ke baad map — sab theek.
- Interstitial cadence ab save hoti hai (SaveData.completedLevels) — har app launch par dobara shuru nahi hoti.
- HUD sound button ab sound setting save karta hai — restart par wapas nahi aata (pehle sirf Settings wala toggle save karta tha).
- Version 1.3.0, Android version code 3.

## [1.2.0] — Ads + Settings (testing only) — 2026-11-26
- 2000 solver-verified launch levels — Easy 600, Medium 600, Hard 500, Expert 300 (100 se barha kar — doosra worker).
- AppLovin MAX ads — test mode: interstitial har 3-4 levels ke baad, rewarded ad hint/extra life ke liye, banner sirf level map par (game board par nahi).
- Remove Ads one-time IAP — test product (asli product ID aur Play Console setup Rehan se baqi hai).
- Game Over par "Ad dekh kar 1 dil" — rewarded ad se 1 dil wapas, level jaari.
- Settings screen: sound, vibration, language (English/Urdu — Urdu UI poori tarah V2 mein), privacy policy link, Remove Ads.
- Splash screen: logo + "Teer Puzzle" + version, 1.5 second ke baad level map.
- Undo button hata diya gaya (Rehan ka faisla).
- Version 1.2.0, Android version code 2.

## [1.1.0] — Core (testing only) — 2026-11-26
- Core gameplay: grid, cube arrows (tap → slide-out, blocked → bump + 1 life), 3 lives, hint, undo (3 per level), move counter, stars (1–3), win/lose screens, local JSON save.
- 100 solver-verified levels (Easy 30, Medium 30, Hard 25, Expert 15), JSON format.
- 6 color themes (Classic, Black & White, Neon, Sunset, Ocean, Candy) — dropdown se.
- 11 original sound effects (WebAudio nahi — .wav files), background music nahi.
- Original art + app icon (koi Easybrain asset nahi).
- Koi ads, koi analytics, koi Play Store upload nahi — sirf testing ke liye Unity project.
- GitHub Actions CI (`.github/workflows/`) — free cloud APK build (game-ci), bina Unity/VPS ke.

## [Unreleased]
### 2.1 — (TBD)
