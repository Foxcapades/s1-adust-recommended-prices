using HarmonyLib;
using MelonLoader;
#if IL2CPP
using Il2CppInterop.Runtime;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.Product;
using Il2CppSystem;
using Il2CppSystem.Collections.Generic;
#elif MONO
using ScheduleOne.DevUtilities;
using ScheduleOne.Product;
using System;
using System.Collections.Generic;
#endif

[assembly: MelonInfo(typeof(RecommendedPrice.Mod), "Recommended Price", "1.0.0-b1", "Foxcapades")]
[assembly: MelonGame("TVGS", "Schedule I")]

#nullable enable
namespace RecommendedPrice {
  [HarmonyPatch(typeof(ProductManager))]
  public class Mod: MelonMod {
    private static MelonPreferences_Category? preferences;
    private static MelonPreferences_Entry<float>? weedModifier;
    private static MelonPreferences_Entry<float>? cokeModifier;
    private static MelonPreferences_Entry<float>? methModifier;
    private static MelonPreferences_Entry<float>? shrmModifier;

    private static Action<ProductDefinition>? onProductAction;

    // ReSharper disable once ArrangeObjectCreationWhenTypeEvident
    private static readonly Dictionary<string, float> originalProductPrices = new Dictionary<string, float>(12);

    private static bool inMainScene;

    public override void OnInitializeMelon() {
      preferences = MelonPreferences.CreateCategory("Recommended Price", "Recommended Price");
      weedModifier = preferences.CreateEntry("weedModifier", 1f, "Weed Price Multiplier");
      cokeModifier = preferences.CreateEntry("cocaineModifier", 1f, "Cocaine Price Multiplier");
      methModifier = preferences.CreateEntry("methModifier", 1f, "Meth Price Multiplier");
      shrmModifier = preferences.CreateEntry("shoomModifier", 1f, "Shroom Price Multiplier");
    }

    public override void OnPreferencesLoaded() {
      applyInMainOnly();
    }

    public override void OnPreferencesSaved() {
      applyInMainOnly();
    }

    public override void OnSceneWasLoaded(int _, string sceneName) {
      if (sceneName == "Main")
        inMainScene = true;
    }

    public override void OnSceneWasUnloaded(int _, string sceneName) {
      if (sceneName == "Main")
        inMainScene = false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(ProductManager.OnStartServer))]
    static void PreStartServer(ProductManager __instance) {
      // applyModifiers(__instance);
#if IL2CPP
      onProductAction = DelegateSupport.ConvertDelegate<Action<ProductDefinition>>(onProduct);
#elif MONO
      onProductAction = onProduct;
#endif

      __instance.onProductDiscovered += onProductAction;
      __instance.onNewProductCreated += onProductAction;
    }

    [HarmonyPostfix]
    [HarmonyPatch("Clean")]
    static void PostClean(ProductManager __instance) {
      unapplyModifiers(__instance);

      if (onProductAction == null)
        return;

      __instance.onProductDiscovered -= onProductAction;
      __instance.onNewProductCreated -= onProductAction;
      onProductAction = null;
    }

    private static void applyInMainOnly() {
      if (!inMainScene)
        return;

      var logger = Melon<Mod>.Instance.LoggerInstance;
      var manager = NetworkSingleton<ProductManager>.Instance;

      logger.Msg("attempting to apply recommended price modifiers");

      var prices = getProductPrices(manager);

      if (prices == null) {
        logger.Error("failed to get product prices, cannot modify price values");
        return;
      }

      foreach (var product in manager.AllProducts) {
        var mktValue = product.MarketValue;

        originalProductPrices.TryAdd(product.ID, mktValue);

        product.MarketValue = originalProductPrices[product.ID] * multiplier(product.DrugType);

        if (prices.TryGetValue(product, out var price)) {
          // ReSharper disable once CompareOfFloatsByEqualityOperator
          if (price != mktValue)
            continue;
        }

        prices[product] = product.MarketValue;
      }
    }

    private static void onProduct(ProductDefinition product) {
      originalProductPrices.TryAdd(product.ID, product.MarketValue);
      product.MarketValue = originalProductPrices[product.ID] * multiplier(product.DrugType);
    }

    private static void unapplyModifiers(ProductManager manager) {
      var prices = getProductPrices(manager);
      var logger = Melon<Mod>.Instance.LoggerInstance;

      logger.Msg("attempting to revert recommended price modifiers");

      if (prices == null) {
        logger.Error("failed to get product prices, cannot revert price values");
        return;
      }

      foreach (var product in manager.AllProducts) {
        if (!originalProductPrices.ContainsKey(product.ID)) {
          logger.Warning("unrecognized product id {0}", product.ID);
          continue;
        }

        if (!prices.ContainsKey(product)) {
          logger.Warning("product id {0} not found in product price index", product.ID);
          continue;
        }

        prices[product] = originalProductPrices[product.ID];
      }
    }

    private static float multiplier(EDrugType type) {
      return type switch {
        EDrugType.Marijuana => safeMultiplier(weedModifier!),
        EDrugType.Methamphetamine => safeMultiplier(methModifier!),
        EDrugType.Cocaine => safeMultiplier(cokeModifier!),
        EDrugType.Shrooms => safeMultiplier(shrmModifier!),
        _ => 1
      };
    }

    private static float safeMultiplier(MelonPreferences_Entry<float> pref) {
      return pref.Value < 0.01f ? 0.01f : pref.Value;
    }

    private static Dictionary<ProductDefinition, float>? getProductPrices(ProductManager manager) {
      var prop = AccessTools.DeclaredProperty(typeof(ProductManager), "ProductPrices");

      if (prop == null)
        return null;

      return (Dictionary<ProductDefinition, float>?) prop.GetValue(manager);
    }
  }
}
