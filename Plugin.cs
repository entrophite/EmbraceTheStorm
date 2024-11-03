using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Assertions;

namespace EmbraceTheStorm;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
	internal static new ManualLogSource Logger;

	private void Awake()
	{
		// Plugin startup logic
		Logger = base.Logger;
		Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

		var harmony = new Harmony("EmbraceTheStorm");
		harmony.PatchAll();
		Logger.LogInfo($"{harmony.GetPatchedMethods().Count()} harmony patches loaded!");
	}

	internal static Eremite.Controller.IGameController GetGameController()
	{
		return Eremite.Controller.GameController.Instance;
	}

	internal static Eremite.Controller.IController GetMainController()
	{
		return Eremite.Controller.MainController.Instance;
	}
	internal static Eremite.Controller.IMetaController GetMetaController()
	{
		return Eremite.Controller.MetaController.Instance;
	}

	internal static object ReflectiveInstanceCall<T>(ref T instance, string method_name, object[] args)
	{
		var minfo = AccessTools.Method(typeof(T), method_name);
		return minfo.Invoke(instance, args);
	}

	// hostility (next level)
	[HarmonyPatch(typeof(Eremite.Services.HostilityService))]
	[HarmonyPatch(nameof(Eremite.Services.HostilityService.GetPointsForNextLevel))]
	internal class HostilityPointsNextLevel
	{
		static void Postfix(ref Eremite.Services.HostilityService __instance, ref int __result)
		{
			__result = 10000;
			return;
		}
	}

	// hostility (prev level)
	[HarmonyPatch(typeof(Eremite.Services.HostilityService))]
	[HarmonyPatch(nameof(Eremite.Services.HostilityService.GetPointsForPrevLevel))]
	internal class HostilityPointsPrevLevel
	{
		static void Postfix(ref int __result)
		{
			__result = 10000;
			return;
		}
	}

	// impatience rate
	[HarmonyPatch(typeof(Eremite.Services.ReputationService))]
	[HarmonyPatch(nameof(Eremite.Services.ReputationService.AddReputationPenalty))]
	internal class ImpatienceRate
	{
		static bool Prefix(ref float amount)
		{
			amount = amount > 0 ? amount * 0.1f : amount;
			return true;
		}
	}

	// blightrot corruption rate
	[HarmonyPatch(typeof(Eremite.Services.BlightService))]
	[HarmonyPatch(nameof(Eremite.Services.BlightService.CalculateFrameCorruption))]
	internal class BlightrotCorruptionRate
	{
		static void Postfix(ref float __result)
		{
			__result *= 0.1f;
			return;
		}
	}

	// building movability
	[HarmonyPatch()]
	internal class BuildingMovability
	{
		static MethodBase TargetMethod()
		{
			return AccessTools.Method(
				typeof(Eremite.Services.ConstructionService),
				"CanBeMoved",
				new System.Type[]
				{
					typeof(Eremite.Buildings.BuildingModel)
				}
			);
		}

		static void Postfix(ref bool __result)
		{
			__result = true;
			return;
		}
	}

	// hearth collision for moving
	[HarmonyPatch(typeof(Eremite.Buildings.Hearth))]
	[HarmonyPatch("IsTooCloseToOtherHearth")]
	internal class HearthCollisionForMoving
	{
		static bool Prefix(ref Eremite.Buildings.Hearth __instance, ref bool __result)
		{
			__result = false;
			if (!__instance.state.placed)
			{
				// call Recalculate (private method)
				ReflectiveInstanceCall(ref __instance, "RecalculateArea", new object[] { });

				var _area = AccessTools.FieldRefAccess<Eremite.Buildings.Hearth, HashSet<Vector2Int>>(__instance, "area");

				foreach (Eremite.Buildings.HearthState hearthState in GetGameController().GameServices.StateService.Buildings.hearths)
				{
					if (!hearthState.lifted)
					{
						Eremite.Buildings.Hearth hearth = GetGameController().GameServices.BuildingsService.GetBuilding(hearthState.id) as Eremite.Buildings.Hearth;
						foreach (Vector2Int field in _area)
						{
							if (hearth.IsInRange(field))
							{
								__result = true;
							}
						}
					}
				}
			}
			return false;  // skip original call
		}
	}

	// glade event work time
	[HarmonyPatch(typeof(Eremite.Services.EffectsService))]
	[HarmonyPatch(nameof(Eremite.Services.EffectsService.GetRelicWorkingTime))]
	internal class GladeEventWorkTime
	{
		static void Postfix(ref float __result)
		{
			__result *= 0.2f;
			return;
		}
	}

	// expedition duration
	[HarmonyPatch(typeof(Eremite.Buildings.Port))]
	[HarmonyPatch(nameof(Eremite.Buildings.Port.CalculateDuration))]
	internal class ExpeditionDuration
	{
		static void Postfix(ref float __result)
		{
			__result *= 0.2f;
			return;
		}
	}


	// house capacity (per instance)
	[HarmonyPatch()]
	internal class HouseCapacity
	{
		static MethodBase TargetMethod()
		{
			return AccessTools.Method(
				typeof(Eremite.Services.EffectsService),
				"GetHouseCapacity",
				new System.Type[]
				{
					typeof(Eremite.Buildings.House)
				}
			);
		}

		static void Postfix(ref int __result)
		{
			__result *= 3;
			return;
		}
	}

	// good's sell price to trader
	[HarmonyPatch()]
	internal class GoodsSellPriceToTrader
	{
		static MethodBase TargetMethod()
		{
			return AccessTools.Method(
				typeof(Eremite.Services.TradeService),
				"GetValueInCurrency",
				new System.Type[]
				{
					typeof(string),
					typeof(int)
				}
			);
		}

		static void Postfix(ref float __result)
		{
			__result *= 10f;
			return;
		}
	}

	// trade route profit
	[HarmonyPatch(typeof(Eremite.Services.TradeRoutesGenerator))]
	[HarmonyPatch("GetPriceFor")]
	internal class TradeRouteProfit
	{
		static void Postfix(ref int __result)
		{
			__result *= 10;
			return;
		}
	}

	// construction speed
	[HarmonyPatch(typeof(Eremite.Services.EffectsService))]
	[HarmonyPatch(nameof(Eremite.Services.EffectsService.GetConstructionRate))]
	internal class ConstructionSpeed
	{
		static void Postfix(ref float __result)
		{
			__result *= 5f;
			return;
		}
	}

	// expose glade info
	[HarmonyPatch()]
	internal class ExposeGladeInfo
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			return new MethodBase[]
			{
				AccessTools.Method(typeof(Eremite.Services.EffectsService), "BlockDangerousGladesInfo"),
				AccessTools.Method(typeof(Eremite.Services.EffectsService), "RemoveDangerousGladeInfoBlock"),
				AccessTools.Method(typeof(Eremite.Services.EffectsService), "GrantGladeInfo"),
				AccessTools.Method(typeof(Eremite.Services.EffectsService), "RemoveGladeInfo"),
				AccessTools.Method(typeof(Eremite.Services.EffectsService), "PrepareInitialValues")
			};
		}

		static void Postfix(ref Eremite.Services.EffectsService __instance)
		{
			__instance.GladeInfo.Value = true;
			__instance.DangerousGladeInfo.Value = true;
			return;
		}
	}

	// deprioritize path construction
	[HarmonyPatch(typeof(Eremite.Buildings.Building))]
	[HarmonyPatch("SetUpState")]
	internal class DeprioritizePathConstruction
	{
		static void Postfix(ref Eremite.Buildings.Building __instance)
		{
			if (__instance.BuildingModel.category == GetMainController().Settings.GetBuilding("Path").category)
			{
				__instance.BuildingState.constructionPriority = Mathf.Clamp(__instance.BuildingState.constructionPriority - 2, -5, 5);
			}
			return;
		}
	}

	// global production multiplier
	[HarmonyPatch()]
	internal class GlobalProductionMultiplier
	{
		static MethodBase TargetMethod()
		{
			return AccessTools.Method(
				typeof(Eremite.Services.EffectsService),
				"GetProduction",
				new System.Type[]
				{
					typeof(Eremite.Buildings.Building),
					typeof(Eremite.Model.Good),
					typeof(Eremite.Buildings.RecipeModel),
					typeof(bool)
				}
			);
		}

		static void Postfix(ref Eremite.Model.Good __result)
		{
			__result *= 10;
			return;
		}
	}

	// global production speed
	[HarmonyPatch(typeof(Eremite.Services.EffectsService))]
	[HarmonyPatch(nameof(Eremite.Services.EffectsService.GetProductionRate))]
	internal class GlobalProductionSpeed
	{
		static void Postfix(ref float __result)
		{
			__result *= 5f;
			return;
		}
	}

	// production building capacity (non-rainwater)
	[HarmonyPatch(typeof(Eremite.Buildings.BuildingStorage))]
	[HarmonyPatch("GetFullCapacity")]
	internal class ProductionBuildingCapacity
	{
		static void Postfix(ref int __result)
		{
			__result *= 10;
			return;
		}
	}

	// rainwater tank capacity (RainCatcher)
	[HarmonyPatch(typeof(Eremite.Buildings.RainCatcher))]
	[HarmonyPatch(nameof(Eremite.Buildings.RainCatcher.SetUp))]
	internal class RainwaterTankCapacity_RainCatcher
	{
		static void Postfix(ref Eremite.Buildings.RainCatcher __instance)
		{
			if (__instance.model.baseTankCapacity <= 100)
			{
				__instance.model.baseTankCapacity *= 10;
			}
			return;
		}
	}

	// rainwater tank capacity (Extractor i.e. Pump)
	[HarmonyPatch(typeof(Eremite.Buildings.Extractor))]
	[HarmonyPatch(nameof(Eremite.Buildings.Extractor.GetTankCapacity))]
	internal class RainwaterTankCapacity_Extractor
	{
		static void Postfix(ref int __result)
		{
			__result *= 10;
			return;
		}
	}

	// global production limit
	[HarmonyPatch()]
	internal class GlobalProductionLimit
	{
		static MethodBase TargetMethod()
		{
			return AccessTools.Method(
				typeof(Eremite.Services.WorkshopsService),
				"GetGlobalLimitFor",
				new System.Type[]
				{
					typeof(string)
				}
			);
		}

		static bool Prefix(ref Eremite.Services.WorkshopsService __instance, ref int __result, ref string goodName)
		{
			var limits = GetGameController().GameServices.StateService.Buildings.workshopsGlobalLimits;
			__result = limits.ContainsKey(goodName) ? limits[goodName] : 500;
			return false;
		}
	}

	// reduced global production limit for low-level recipes (on SetUp)
	[HarmonyPatch(typeof(Eremite.Buildings.Workshop))]
	[HarmonyPatch(nameof(Eremite.Buildings.Workshop.SetUp))]
	internal class ReducedGlobalProductionLimitForLLR_OnSetUp
	{
		static bool Prefix(ref Eremite.Buildings.Workshop __instance, ref Eremite.Buildings.WorkshopState state)
		{
			foreach (Eremite.Buildings.WorkshopRecipeState workshopRecipeState in state.recipes)
			{
				Logger.LogInfo("1");
				int globalLimit = GetGameController().GameServices.WorkshopsService.GetGlobalLimitFor(workshopRecipeState.productName);
				Logger.LogInfo("2");
				if (!workshopRecipeState.isLimitLocal && globalLimit > 0)
				{
					Logger.LogInfo("3");
					int level = __instance.GetRecipeModel(workshopRecipeState).grade.level;
					Logger.LogInfo("4");
					if (level == 0)
					{
						Logger.LogInfo("5");
						workshopRecipeState.limit = Math.Max(1, (int)(globalLimit * 0.2f));
						Logger.LogInfo("6");
					}
					else if (level == 1)
					{
						Logger.LogInfo("7");
						workshopRecipeState.limit = Math.Max(1, (int)(globalLimit * 0.6f));
						Logger.LogInfo("8");
					}
				}
			}
			return true;
		}
	}

	// reduced global production limit for low-level recipes (on ChangeLimitFor)
	[HarmonyPatch(typeof(Eremite.Buildings.Workshop))]
	[HarmonyPatch(nameof(Eremite.Buildings.Workshop.ChangeLimitFor))]
	internal class ReducedGlobalProductionLimitForLLR_OnChangeLimitFor
	{
		static bool Prefix(ref Eremite.Buildings.Workshop __instance, ref Eremite.Buildings.WorkshopRecipeState recipe, ref int amount, bool isLocalChange)
		{
			if (!isLocalChange && amount > 0)
			{
				int level = __instance.GetRecipeModel(recipe).grade.level;
				if (level == 0)
				{
					amount = Math.Max(1, (int)(amount * 0.2f));
				}
				else if (level == 1)
				{
					amount = Math.Max(1, (int)(amount * 0.6f));
				}
			}
			return true;
		}
	}

	// global carrying capacity
	[HarmonyPatch(typeof(Eremite.Services.EffectsService))]
	[HarmonyPatch(nameof(Eremite.Services.EffectsService.GetActorCapacity))]
	internal class GlobalCarryingCapacity
	{
		static void Postfix(ref int __result)
		{
			__result *= 5;
			return;
		}
	}

	// unlock all blueprints proposed
	[HarmonyPatch(typeof(Eremite.Services.ReputationRewardsService))]
	[HarmonyPatch(nameof(Eremite.Services.ReputationRewardsService.RewardPicked))]
	internal class UnlockAllBlueprintsProposed
	{
		static bool Prefix(ref Eremite.Services.ReputationRewardsService __instance)
		{
			foreach (Eremite.Model.State.ReputationReward reputationReward in __instance.GetCurrentPicks())
			{
				GetGameController().GameServices.GameContentService.Unlock(GetMainController().Settings.GetBuilding(reputationReward.building));
			};
			return true;
		}
	}

	// unlock all blueprints proposed (on SendPickAnalytics, prank)
	[HarmonyPatch(typeof(Eremite.Services.ReputationRewardsService))]
	[HarmonyPatch("SendPickAnalytics")]
	internal class UnlockAllBlueprintsProposed_OnSendPickAnalytics
	{
		static bool Prefix()
		{
			if (!GetMetaController().MetaServices.TutorialService.IsAnyTutorial(GetGameController().GameServices.BiomeService.CurrentBiome))
			{
				GetGameController().GameServices.GameAnalyticsService.Buildings.SendReputationRewardPick(GetGameController().GameServices.StateService.ReputationRewards.currentPick.options,
					"only children make choices, adults want them all!");
			}
			return false;
		}
	}

	// multiple cornerstone pick (on Pick)
	[HarmonyPatch(typeof(Eremite.Services.CornerstonesService))]
	[HarmonyPatch(nameof(Eremite.Services.CornerstonesService.Pick))]
	internal class MultipleCornerstonePick_OnPick
	{
		static bool Prefix(ref Eremite.Services.CornerstonesService __instance, ref Eremite.Model.EffectModel reward)
		{
			if (Eremite.DebugModes.Assertions)
			{
				Assert.IsNotNull(__instance.GetCurrentPick());
			}
			Eremite.Model.RewardPickState currentPick = __instance.GetCurrentPick();
			Log.Info(string.Format("[Cor] Set last date pick: {0} {1} {2}", currentPick.date.year, currentPick.date.season, currentPick.date.quarter), null);
			//if (reward != null)
			//{
			//	__instance.SendPickAnalytics(currentPick, reward.Name);
			//}
			ref var picks = ref GetGameController().GameServices.StateService.Gameplay.currentCornerstonePicks;
			if (!currentPick.isExtra)
			{
				GetGameController().GameServices.StateService.Gameplay.lastCornerstonePickDate = currentPick.date;
			}
			picks.Remove(currentPick);
			if (reward != null)
			{
				reward.Apply(Eremite.Model.Effects.EffectContextType.None, null, 0);
				currentPick.options.Remove(reward.Name);
				if (currentPick.options.Count > 0)
				{
					picks.Insert(0, currentPick);
				}
				// insert end
			}
			if (reward == null)
			{
				ReflectiveInstanceCall(ref __instance, "RewardForDecline", new object[] { });
			}
			__instance.OnRewardsPicked.OnNext(reward);
			return false;
		}
	}

	// multiple cornerstone pick (on OnRewardPicked)
	[HarmonyPatch(typeof(Eremite.View.HUD.RewardPickPopup))]
	[HarmonyPatch("OnRewardPicked")]
	internal class MultipleCornerstonePick_OnOnRewardPicked
	{
		static bool Prefix(ref Eremite.View.HUD.RewardPickPopup __instance, ref Eremite.Model.EffectModel reward)
		{
			Log.Info(string.Format("[Cor] Cornerstone {0} picked in {1} {2} {3}", new object[]
			{
				reward.Name,
				GetGameController().GameServices.CalendarService.Year,
				GetGameController().GameServices.CalendarService.Season,
				GetGameController().GameServices.CalendarService.Quarter
			}), null);
			GetGameController().GameServices.CornerstonesService.Pick(reward);
			if (GetGameController().GameServices.CornerstonesService.GetCurrentPick() != null)
			{
				ReflectiveInstanceCall(ref __instance, "SetUpSlots", new object[] { });
				ReflectiveInstanceCall(ref __instance, "SetUpReroll", new object[] { });
				ReflectiveInstanceCall(ref __instance, "SetUpDeclineButton", new object[] { });
				ReflectiveInstanceCall(ref __instance, "SetUpPollPanel", new object[] { });
				ReflectiveInstanceCall(ref __instance, "SetUpDialogue", new object[] { });
			}
			else
			{
				__instance.Hide();
			}
			return false;
		}
	}

	// unlock all queen's hand trial proposed upgrades (on OnRewardPicked)
	[HarmonyPatch(typeof(Eremite.Services.IronmanService))]
	[HarmonyPatch(nameof(Eremite.Services.IronmanService.Pick))]
	internal class UnlockAllQHTProposedUpgrades
	{
		static bool Prefix(ref Eremite.Services.IronmanService __instance, ref Eremite.WorldMap.CapitalUpgradeModel model)
		{
			Assert.IsTrue(__instance.CanAfford(model));
			if (__instance.IsCore(model))
			{
				ReflectiveInstanceCall(ref __instance, "BuyCore", new object[] { model });
			}
			else
			{
				ReflectiveInstanceCall(ref __instance, "PayForUpgrade", new object[] { model });
				var _minfo_Unlock = AccessTools.Method(typeof(Eremite.Services.IronmanService), "Unlock");
				foreach (Eremite.Model.State.IronmanPickOption ironmanPickOption in GetMetaController().MetaServices.MetaStateService.Ironman.currentPick.options)
				{
					_minfo_Unlock.Invoke(__instance, new object[] { ironmanPickOption.model });
				}
				ReflectiveInstanceCall(ref __instance, "SetAsSeen", new object[] { });
				ReflectiveInstanceCall(ref __instance, "MarkAsPicked", new object[] { });
				if (!__instance.HasReachedMaxPicks())
				{
					ReflectiveInstanceCall(ref __instance, "SetNewPick", new object[] { });
				}
				ReflectiveInstanceCall(ref __instance, "CallEvents", new object[] { model });
			}
			return false;
		}
	}

	// larger caravan
	[HarmonyPatch(typeof(Eremite.Services.Meta.CaravanGenerator))]
	[HarmonyPatch("GetVillagersAmount")]
	internal class LargerCaravan
	{
		static void Postfix(ref int __result)
		{
			__result *= 4;
			return;
		}
	}

	// caravan goods
	[HarmonyPatch(typeof(Eremite.Services.Meta.CaravanGenerator))]
	[HarmonyPatch("GenerateGoods")]
	internal class CaravanGoods
	{
		static void Postfix(ref List<Eremite.Model.Good> __result)
		{
			for (int i = 0; i < __result.Count; i++)
			{
				Eremite.Model.Good good = __result[i];
				good.amount *= 10;
				__result[i] = good;
			}
			__result.Add(new Eremite.Model.Good("[Valuable] Amber", 200));
			__result.Add(new Eremite.Model.Good("[Metal] Crystalized Dew", 300));
			return;
		}
	}

	// moah embarkation points
	[HarmonyPatch(typeof(Eremite.View.Menu.Pick.BuildingsPickScreen))]
	[HarmonyPatch("GetBasePreparationPoints")]
	internal class MoahEmbarkationPoints
	{
		static void Postfix(ref int __result)
		{
			__result += 100;
			return;
		}
	}

	// moah embarkation blueprints
	[HarmonyPatch(typeof(Eremite.Services.GameContentService))]
	[HarmonyPatch("EnsureBuildings")]
	internal class MoahEmbarkationBlueprints
	{
		static void Postfix(ref Eremite.Services.GameContentService __instance)
		{
			var to_add = new List<string> {
			// gathering
			"Advanced Rain Catcher",
			"Clay Pit Workshop",
			"Fishing Hut",
			"Forager's Camp",
			"Herbalist's Camp",
			"Trapper's Camp",
			// food prod
			"Flawless Brewery",
			"Flawless Cellar",
			"Flawless Rain Mill",
			"Greenhouse Workshop",
			"Grove",
			"Hallowed Herb Garden",
			"Hallowed SmallFarm",
			"Herb Garden",
			"Homestead",
			"Plantation",
			"SmallFarm",
			// house
			"Beaver House",
			"Fox House",
			"Frog House",
			"Harpy House",
			"Human House",
			"Lizard House",
			"Purged Beaver House",
			"Purged Fox House",
			"Purged Frog House",
			"Purged Harpy House",
			"Purged Human House",
			"Purged Lizard House",
			// industry
			"Finesmith",
			"Flawless Cooperage",
			"Flawless Druid",
			"Flawless Leatherworks",
			"Flawless Smelter",
			"Rainpunk Foundry",
			// city building
			"Altar",
			"Bath House",
			"Clan Hall",
			"Explorers Lodge",
			"Forum",
			"Guild House",
			"Holy Guild House",
			"Holy Market",
			"Holy Temple",
			"Market",
			"Monastery",
			"Port",
			"Tavern",
			"Tea Doctor",
			"Temple",
			};
			var current = new HashSet<string>(GetGameController().GameServices.StateService.Content.buildings);
			foreach (string building in to_add)
			{
				Eremite.Buildings.BuildingModel replaces = GetMainController().Settings.GetBuilding(building).replaces;
				if (replaces && current.Contains(replaces.Name))
				{
					current.Remove(replaces.Name);
				}
				current.Add(building);
			}
			GetGameController().GameServices.StateService.Content.buildings = current.ToList();
			return;
		}
	}

	// moah embarkation cornerstones
	[HarmonyPatch(typeof(Eremite.Services.WorldStateService))]
	[HarmonyPatch(nameof(Eremite.Services.WorldStateService.GenerateNewState))]
	internal class MoahEmbarkationCornerstones
	{
		static void Postfix(ref Eremite.Services.WorldStateService __instance)
		{
			var to_add = new List<string> {
				"Hauling Cart in All Warehouses",
			};
			foreach (string effect in to_add)
			{
				__instance.CycleEffects.bonusEmbarkEffects.Add(effect);
			}
			return;
		}
	}

	// reveal all embarkation races
	[HarmonyPatch(typeof(Eremite.View.Menu.Pick.CaravanPickSlot))]
	[HarmonyPatch("SetUpRaces")]
	internal class RevealAllEmbarkationRaces
	{
		static bool Prefix(ref Eremite.View.Menu.Pick.CaravanPickSlot __instance)
		{
			var state = AccessTools.FieldRefAccess<Eremite.View.Menu.Pick.CaravanPickSlot, Eremite.Model.State.EmbarkCaravanState>(__instance, "state");
			var raceSlots = AccessTools.FieldRefAccess<Eremite.View.Menu.Pick.CaravanPickSlot, List<Eremite.View.Menu.Pick.RaceSlot>>(__instance, "raceSlots");
			var _methinfo_GetOrCreate = AccessTools.Method(typeof(Eremite.BaseMB), "GetOrCreate").MakeGenericMethod(typeof(Eremite.View.Menu.Pick.RaceSlot));
			var _methinfo_HideRest = AccessTools.Method(typeof(Eremite.BaseMB), "HideRest").MakeGenericMethod(typeof(Eremite.View.Menu.Pick.RaceSlot));
			var _methinfo_GetAmountOf = AccessTools.Method(typeof(Eremite.View.Menu.Pick.CaravanPickSlot), "GetAmountOf");
			for (int i = 0; i <state.races.Count; i++)
			{
				Eremite.Model.RaceModel race = GetMainController().Settings.GetRace(state.races[i]);
				var amount = (int)_methinfo_GetAmountOf.Invoke(__instance, new object[] { race });
				((Eremite.View.Menu.Pick.RaceSlot)_methinfo_GetOrCreate.Invoke(__instance, new object[] { raceSlots, i })).SetUp(race, amount);
			}
			for (int i = state.races.Count; i < GetMainController().Settings.gameplayRaces; i++)
			{
				((Eremite.View.Menu.Pick.RaceSlot)_methinfo_GetOrCreate.Invoke(__instance, new object[] { raceSlots, i })).SetUpUnknown();
			}
			_methinfo_HideRest.Invoke(__instance, new object[] { raceSlots, GetMainController().Settings.gameplayRaces });
			return false;
		}
	}
}
