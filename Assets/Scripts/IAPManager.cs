using System;
using UnityEngine;
using UnityEngine.Purchasing;

namespace TeerPuzzle
{
    /// <summary>
    /// In-app purchases (Unity IAP v4): sirf "Remove Ads" (one-time,
    /// non-consumable). Test mode ON hai by default: BuyRemoveAds()
    /// foran simulate karta hai — koi real store call nahi, koi crash nahi.
    ///
    /// TODO (Rehan): real purchase ke liye
    ///   - Google Play Console mein product banao, ID: teer_puzzle_remove_ads
    ///   - testMode = false karo
    /// Tab tak testMode = true hi rakho.
    /// </summary>
    public class IAPManager : MonoBehaviour, IStoreListener
    {
        /// <summary>Remove Ads product ID (Play Console mein yehi ID banao).</summary>
        public const string RemoveAdsProductId = "teer_puzzle_remove_ads";

        /// <summary>Test mode ON = khareedari simulate, koi real store call nahi.</summary>
        public bool testMode = true;

        private const string RemoveAdsPlayerPrefsKey = "teer_remove_ads";

        /// <summary>True jab Remove Ads khareeda gaya ho (test mode ya real purchase).</summary>
        public bool RemoveAdsOwned { get; private set; }

        /// <summary>True jab store se connect ho kar products load ho chuke hon.</summary>
        public bool IsInitialized { get; private set; }

        /// <summary>Remove Ads ki haalat badle to fire hota hai.</summary>
        public event Action OnRemoveAdsChanged;

        private IStoreController storeController;
        private IExtensionProvider extensionProvider;
        private bool initializing;

        private void Awake()
        {
            RemoveAdsOwned = PlayerPrefs.GetInt(RemoveAdsPlayerPrefsKey, 0) == 1;
        }

        private void Start()
        {
            if (!testMode)
            {
                InitializePurchasing();
            }
            else
            {
                Debug.Log("[IAPManager] Test mode — koi real store call nahi. " +
                          "Play Console mein product banao aur testMode = false karo.");
            }
        }

        // ------------------------------------------------------------------
        // Public API
        // ------------------------------------------------------------------

        /// <summary>Remove Ads khareedne ki koshish karta hai.</summary>
        public void BuyRemoveAds()
        {
            if (testMode)
            {
                Debug.Log("[IAPManager] TEST MODE: Remove Ads khareedari simulate ho rahi hai.");
                SetRemoveAdsOwned(true);
                return;
            }

            if (!IsInitialized)
            {
                Debug.LogWarning("[IAPManager] Store abhi initialize nahi hua — dobara koshish karo.");
                InitializePurchasing();
                return;
            }

            try
            {
                Product product = storeController.GetProductById(RemoveAdsProductId);
                if (product != null && product.availableToPurchase)
                {
                    storeController.InitiatePurchase(product);
                }
                else
                {
                    Debug.LogWarning("[IAPManager] Remove Ads product nahi mila ya khareedne ke liye tayyar nahi. " +
                                     "Play Console mein product ID check karo: " + RemoveAdsProductId);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[IAPManager] BuyRemoveAds failed: " + e.Message);
            }
        }

        /// <summary>
        /// Pehle ki khareedariyan wapas lata hai (iOS). Android par
        /// khareedariyan initialize par khud hi mil jati hain (receipt check).
        /// </summary>
        public void RestorePurchases()
        {
            if (testMode)
            {
                Debug.Log("[IAPManager] TEST MODE: restore simulate — pehle se khareeda hua: " + RemoveAdsOwned);
                return;
            }

            try
            {
                if (extensionProvider != null)
                {
                    IAppleExtensions apple = extensionProvider.GetExtension<IAppleExtensions>();
                    apple.RestoreTransactions((success, error) =>
                    {
                        Debug.Log("[IAPManager] RestoreTransactions: success = " + success +
                                  (string.IsNullOrEmpty(error) ? "" : ", error = " + error));
                    });
                }
                else
                {
                    Debug.LogWarning("[IAPManager] Store initialize nahi hua — restore nahi ho sakta.");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[IAPManager] RestorePurchases failed: " + e.Message);
            }
        }

        // ------------------------------------------------------------------
        // Unity IAP setup (real mode)
        // ------------------------------------------------------------------

        private void InitializePurchasing()
        {
            if (IsInitialized || initializing)
            {
                return;
            }

            initializing = true;

            try
            {
                ConfigurationBuilder builder = ConfigurationBuilder.Instance(typeof(StandardPurchasingModule));
                builder.AddProduct(RemoveAdsProductId, ProductType.NonConsumable);
                UnityPurchasing.Initialize(this, builder);
            }
            catch (Exception e)
            {
                initializing = false;
                Debug.LogWarning("[IAPManager] InitializePurchasing failed: " + e.Message);
            }
        }

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            storeController = controller;
            extensionProvider = extensions;
            IsInitialized = true;
            initializing = false;

            Debug.Log("[IAPManager] Store initialize ho gaya.");

            try
            {
                // Pehle se khareeda hua ho (doosri device / dobara install) to flag set karo.
                Product product = storeController.GetProductById(RemoveAdsProductId);
                if (product != null && product.hasReceipt)
                {
                    SetRemoveAdsOwned(true);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[IAPManager] Receipt check failed: " + e.Message);
            }
        }

        // Unity IAP v4 signature (error + message).
        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
            initializing = false;
            IsInitialized = false;
            Debug.LogWarning("[IAPManager] Store initialize nahi hua: " + error +
                             (string.IsNullOrEmpty(message) ? "" : " — " + message));
        }

        // Unity IAP v3 signature — purani version ke saath bhi chale.
        public void OnInitializeFailed(InitializationFailureReason error)
        {
            initializing = false;
            IsInitialized = false;
            Debug.LogWarning("[IAPManager] Store initialize nahi hua: " + error);
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            try
            {
                if (args != null && args.purchasedProduct != null &&
                    args.purchasedProduct.definition.id == RemoveAdsProductId)
                {
                    Debug.Log("[IAPManager] Remove Ads khareed liya gaya.");
                    SetRemoveAdsOwned(true);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[IAPManager] ProcessPurchase failed: " + e.Message);
            }

            return PurchaseProcessingResult.Complete;
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
        {
            string id = product != null ? product.definition.id : "unknown";
            Debug.LogWarning("[IAPManager] Khareedari nakaam: " + id + " — " + failureReason);
        }

        // ------------------------------------------------------------------
        // Internals
        // ------------------------------------------------------------------

        private void SetRemoveAdsOwned(bool owned)
        {
            if (RemoveAdsOwned == owned)
            {
                return;
            }

            RemoveAdsOwned = owned;
            PlayerPrefs.SetInt(RemoveAdsPlayerPrefsKey, owned ? 1 : 0);
            PlayerPrefs.Save();

            if (OnRemoveAdsChanged != null)
            {
                OnRemoveAdsChanged();
            }
        }
    }
}
