using System;
using UnityEngine;

namespace TeerPuzzle
{
    /// <summary>
    /// AppLovin MAX ads (interstitial, rewarded, banner).
    ///
    /// Version 1.2 — test mode is ON by default: no real SDK call is
    /// made, rewarded ads are simulated instantly so the game flow can
    /// be tested, and interstitials are only logged. Nothing crashes
    /// when the SDK / ad units are not configured yet.
    ///
    /// TODO (Rehan): real IDs milne par neeche diye gaye constants
    /// badlo aur testMode = false karo:
    ///   - AppLovin dashboard se SDK key
    ///   - Interstitial / Rewarded / Banner ad unit IDs
    /// Ads tab bhi AppLovin dashboard par banane honge. Tab tak
    /// testMode = true hi rakho.
    /// </summary>
    public class AdManager : MonoBehaviour
    {
        /// <summary>Test mode ON = koi real SDK call nahi, sab simulate.</summary>
        public bool testMode = true;

        /// <summary>Rewarded ad se kya milega (undo nahi hai — Rehan ka faisla).</summary>
        public enum RewardType
        {
            Hint,
            ExtraLife
        }

        // TODO: real AppLovin SDK key yahan daalo (Rehan se chahiye).
        public const string SdkKey = "YOUR_APPLOVIN_SDK_KEY";

        // TODO: real ad unit IDs yahan daalo (Rehan se chahiye).
        public const string InterstitialAdUnitId = "YOUR_INTERSTITIAL_AD_UNIT_ID";
        public const string RewardedAdUnitId = "YOUR_REWARDED_AD_UNIT_ID";
        public const string BannerAdUnitId = "YOUR_BANNER_AD_UNIT_ID";

        // Assigned in the scene; missing reference is found automatically.
        public IAPManager iapManager;

        // Har call = aik level mukammal. Har 3rd level par interstitial.
        private int completedLevels;
        private Action pendingReward;
        private bool sdkInitialized;
        private bool bannerCreated;
        private bool removeAdsSubscribed;

        /// <summary>True jab Remove Ads khareeda gaya ho (IAPManager se poochha jata hai).</summary>
        public bool AdsRemoved
        {
            get
            {
                if (iapManager == null)
                {
                    iapManager = FindFirstObjectByType<IAPManager>();
                }
                return iapManager != null && iapManager.RemoveAdsOwned;
            }
        }

        /// <summary>True sirf jab test mode OFF ho AUR real SDK key lagi ho.</summary>
        private bool UseRealSdk
        {
            get
            {
                return !testMode &&
                       !string.IsNullOrEmpty(SdkKey) &&
                       SdkKey != "YOUR_APPLOVIN_SDK_KEY";
            }
        }

        private void Awake()
        {
            if (iapManager == null)
            {
                iapManager = FindFirstObjectByType<IAPManager>();
            }

            // v1.4 — Remove Ads khareedne par pehle se dikh raha banner
            // foran hide ho (ShowBanner pehle se AdsRemoved check karta hai,
            // lekin khareedari ke waqt dikh raha banner khud nahi chhupata).
            SubscribeToRemoveAds();

            try
            {
                MaxSdkCallbacks.OnSdkInitializedEvent += OnSdkInitialized;
                MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += OnInterstitialLoaded;
                MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += OnInterstitialLoadFailed;
                MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += OnInterstitialHidden;
                MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += OnRewardedLoaded;
                MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += OnRewardedLoadFailed;
                MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += OnRewardedReceived;
                MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += OnRewardedHidden;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[AdManager] Could not subscribe to MAX callbacks: " + e.Message);
            }
        }

        private void Start()
        {
            // IAPManager AdManager ke baad bana ho to yahan subscribe karo
            // (Awake mein sirf us waqt ka iapManager milta hai).
            SubscribeToRemoveAds();

            if (!UseRealSdk)
            {
                Debug.Log("[AdManager] Test mode — no real MAX SDK calls. " +
                          "Set SdkKey + ad unit IDs and testMode = false for real ads.");
                return;
            }

            try
            {
                MaxSdk.SetSdkKey(SdkKey);
                MaxSdk.InitializeSdk();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[AdManager] MAX InitializeSdk failed: " + e.Message);
            }
        }

        private void OnDestroy()
        {
            if (removeAdsSubscribed && iapManager != null)
            {
                iapManager.OnRemoveAdsChanged -= HandleRemoveAdsChanged;
                removeAdsSubscribed = false;
            }

            try
            {
                MaxSdkCallbacks.OnSdkInitializedEvent -= OnSdkInitialized;
                MaxSdkCallbacks.Interstitial.OnAdLoadedEvent -= OnInterstitialLoaded;
                MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent -= OnInterstitialLoadFailed;
                MaxSdkCallbacks.Interstitial.OnAdHiddenEvent -= OnInterstitialHidden;
                MaxSdkCallbacks.Rewarded.OnAdLoadedEvent -= OnRewardedLoaded;
                MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent -= OnRewardedLoadFailed;
                MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent -= OnRewardedReceived;
                MaxSdkCallbacks.Rewarded.OnAdHiddenEvent -= OnRewardedHidden;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[AdManager] Could not unsubscribe MAX callbacks: " + e.Message);
            }
        }

        // ------------------------------------------------------------------
        // Interstitial — har 3rd mukammal level ke baad, level ke beech mein kabhi nahi
        // ------------------------------------------------------------------

        /// <summary>
        /// Har call = aik level mukammal hua. Har 3rd level par interstitial
        /// dikhata hai (sirf jab ready ho). Ads removed hon to kabhi nahi.
        /// </summary>
        public void ShowInterstitialIfDue()
        {
            // v1.3 — cadence sessions ke paar consistent rahe: pehle
            // save se sync karo (SaveManager na ho to skip — null-safe).
            SaveManager saveManager = FindFirstObjectByType<SaveManager>();
            if (saveManager != null && saveManager.Data != null)
            {
                completedLevels = saveManager.Data.completedLevels;
            }

            if (AdsRemoved)
            {
                return;
            }

            completedLevels += 1;

            // v1.3 — increment foran save karo (chahe is dafa ad na dikhe).
            if (saveManager != null && saveManager.Data != null)
            {
                saveManager.Data.completedLevels = completedLevels;
                saveManager.Save();
            }

            if (completedLevels % 3 != 0)
            {
                return;
            }

            if (!UseRealSdk)
            {
                Debug.Log("[AdManager] TEST MODE: interstitial hota (level " +
                          completedLevels + " mukammal). Koi real ad nahi dikhaya.");
                return;
            }

            try
            {
                if (MaxSdk.IsInterstitialReady(InterstitialAdUnitId))
                {
                    MaxSdk.ShowInterstitial(InterstitialAdUnitId);
                }
                else
                {
                    Debug.Log("[AdManager] Interstitial not ready — loading again.");
                    MaxSdk.LoadInterstitial(InterstitialAdUnitId);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[AdManager] ShowInterstitial failed: " + e.Message);
            }
        }

        // ------------------------------------------------------------------
        // Rewarded — hint / extra dil (undo nahi hai)
        // ------------------------------------------------------------------

        /// <summary>
        /// Rewarded ad dikhata hai; reward milne par onReward() call hota hai.
        /// Test mode mein foran simulate kar ke onReward() call karta hai.
        /// Ad fail / na dikhe to onReward call nahi hota aur game normal chalta hai.
        /// </summary>
        public void ShowRewarded(RewardType type, Action onReward)
        {
            if (onReward == null)
            {
                return;
            }

            if (!UseRealSdk)
            {
                Debug.Log("[AdManager] TEST MODE: rewarded ad simulate ho raha hai (" +
                          type + ") — reward foran mil raha hai.");
                onReward();
                return;
            }

            try
            {
                if (MaxSdk.IsRewardedAdReady(RewardedAdUnitId))
                {
                    pendingReward = onReward;
                    MaxSdk.ShowRewardedAd(RewardedAdUnitId);
                }
                else
                {
                    Debug.LogWarning("[AdManager] Rewarded ad not ready (" + type + ") — loading again.");
                    MaxSdk.LoadRewardedAd(RewardedAdUnitId);
                }
            }
            catch (Exception e)
            {
                pendingReward = null;
                Debug.LogWarning("[AdManager] ShowRewarded failed: " + e.Message);
            }
        }

        // ------------------------------------------------------------------
        // Banner — sirf menu / level map par (game screen par nahi)
        // ------------------------------------------------------------------

        /// <summary>Banner dikhata hai (sirf menu/map se call karo).</summary>
        public void ShowBanner()
        {
            if (AdsRemoved)
            {
                return;
            }

            if (!UseRealSdk)
            {
                Debug.Log("[AdManager] TEST MODE: banner nahi dikhaya (sirf log).");
                return;
            }

            try
            {
                if (!bannerCreated)
                {
                    MaxSdk.CreateBanner(BannerAdUnitId, MaxSdkBase.BannerPosition.BottomCenter);
                    bannerCreated = true;
                }
                MaxSdk.ShowBanner(BannerAdUnitId);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[AdManager] ShowBanner failed: " + e.Message);
            }
        }

        /// <summary>Banner chhupata hai (game screen par jate waqt call karo).</summary>
        public void HideBanner()
        {
            if (!UseRealSdk)
            {
                return;
            }

            try
            {
                if (bannerCreated)
                {
                    MaxSdk.HideBanner(BannerAdUnitId);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[AdManager] HideBanner failed: " + e.Message);
            }
        }

        // ------------------------------------------------------------------
        // Remove Ads — khareedari par banner foran hide karo (v1.4)
        // ------------------------------------------------------------------

        /// <summary>
        /// IAPManager.OnRemoveAdsChanged par subscribe karta hai (agar pehle
        /// se subscribe na ho). Awake aur Start dono se call hota hai,
        /// taake IAPManager kab bana ho, is se farq na pare.
        /// </summary>
        private void SubscribeToRemoveAds()
        {
            if (removeAdsSubscribed)
            {
                return;
            }

            if (iapManager == null)
            {
                iapManager = FindFirstObjectByType<IAPManager>();
            }

            if (iapManager != null)
            {
                iapManager.OnRemoveAdsChanged += HandleRemoveAdsChanged;
                removeAdsSubscribed = true;
            }
        }

        /// <summary>
        /// Remove Ads khareeda gaya (ya haalat badli) — pehle se dikh raha
        /// banner foran chhupao. Test mode mein banner hota hi nahi,
        /// is liye HideBanner wahan kuch nahi karta.
        /// </summary>
        private void HandleRemoveAdsChanged()
        {
            HideBanner();
        }

        // ------------------------------------------------------------------
        // MAX callbacks (saari real calls try/catch mein — ad fail ho to game normal chale)
        // ------------------------------------------------------------------

        private void OnSdkInitialized(MaxSdkBase.SdkConfiguration configuration)
        {
            sdkInitialized = true;
            Debug.Log("[AdManager] MAX SDK initialized.");

            try
            {
                MaxSdk.LoadInterstitial(InterstitialAdUnitId);
                MaxSdk.LoadRewardedAd(RewardedAdUnitId);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[AdManager] Initial ad load failed: " + e.Message);
            }
        }

        private void OnInterstitialLoaded(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            Debug.Log("[AdManager] Interstitial loaded.");
        }

        private void OnInterstitialLoadFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            Debug.LogWarning("[AdManager] Interstitial load failed: " + errorInfo);
        }

        private void OnInterstitialHidden(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            // Agle interstitial ke liye dobara load karo.
            try
            {
                MaxSdk.LoadInterstitial(InterstitialAdUnitId);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[AdManager] Interstitial reload failed: " + e.Message);
            }
        }

        private void OnRewardedLoaded(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            Debug.Log("[AdManager] Rewarded ad loaded.");
        }

        private void OnRewardedLoadFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            Debug.LogWarning("[AdManager] Rewarded ad load failed: " + errorInfo);
        }

        private void OnRewardedReceived(string adUnitId, MaxSdkBase.Reward reward)
        {
            Action rewardCallback = pendingReward;
            pendingReward = null;

            if (rewardCallback != null)
            {
                Debug.Log("[AdManager] Rewarded ad reward mila — reward de raha hoon.");
                rewardCallback();
            }
        }

        private void OnRewardedHidden(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            // Reward mile baghair ad band hua to pending reward khatam.
            pendingReward = null;

            try
            {
                MaxSdk.LoadRewardedAd(RewardedAdUnitId);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[AdManager] Rewarded ad reload failed: " + e.Message);
            }
        }
    }
}
