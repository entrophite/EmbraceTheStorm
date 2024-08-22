# EmbraceTheStorm

These are my note for statically injected code to alter the gameplay of *Against the Storm*, in purpose for reinjecting these codes after future version updates.

<b><span style="color:#60ff60">VERSION: </span></b> 1.3@steam

## 1. Environment/Glades/Settlement

### 1.1. Hostility

<b><span style="color:#4040ff">SYNOPSIS: </span></b> Modifying the number of hostility points per level (the hostility progression bar "size" of each level) while still keep tracking the total hostility points gained. In the example below the bar size is set to the sum of points required to reach the maximum level in the original game. The bar size hence scales with difficulty, e.g. 3100 per level in Viceroy+ (since max level is 31) and 600 in Settler (max level is 6).

```c#
// Eremite.Services.HostilityService.GetPointsForNextLevel
public int GetPointsForNextLevel()
{
	int num = 0;
	for (int i = 0; i < this.Difficulty.levels.Length; i++)
	{
		num += this.Difficulty.levels[i].pointsToLeave;
	}
	return num;
}

// Eremite.Services.HostilityService.GetPointsForPrevLevel
public int GetPointsForPrevLevel()
{
	bool assertions = DebugModes.Assertions;
	int num = 0;
	for (int i = 0; i < this.Difficulty.levels.Length; i++)
	{
		num += this.Difficulty.levels[i].pointsToLeave;
	}
	return num;
}
```

### 1.2. Impatience rate

<b><span style="color:#4040ff">SYNOPSIS: </span></b> Modifying the amount of impatience increase, affects both one-time penalty from events and effects, and temporal cumulation. Does not affect impatience decrease. 0.1x (90% off) in the example below.

```c#
// Eremite.Services.ReputationService.AddReputationPenalty
public void AddReputationPenalty(float amount, ReputationChangeSource type, bool force, string reason = null)
{
	if (Mathf.Approximately(amount, 0f))
	{
		this.CheckForLoose();
		return;
	}
	if (!force && this.IsGameFinished())
	{
		return;
	}
	amount = amount > 0 ? amount * 0.1f : amount;
	this.State.reputationPenalty = Mathf.Clamp(this.State.reputationPenalty + amount, 0f, (float)this.GetReputationPenaltyToLoose());
	this.ReputationPenalty.Value = this.State.reputationPenalty;
	this.reputationPenaltyChangedSubject.OnNext(new ReputationChange(amount, reason, type));
	this.CheckForLoose();
}
```

### 1.3. Blightrot corruption rate

<b><span style="color:#4040ff">SYNOPSIS: </span></b> Modifying the rate of corruption, 0.1x (90% slower) in the example below.

```c#
// Eremite.Services.BlightService.CalculateFrameCorruption
public float CalculateFrameCorruption()
{
	return Serviceable.BlightService.GetGlobalCorruptionPerSec() * Time.deltaTime * 0.1f;
}
```

### 1.3.1. Building movability lite

<b><span style="color:#4040ff">SYNOPSIS: </span></b> Enabling building movement as if the map modifier "Levitating Monument" is always on.

```c#
// Eremite.Services.EffectsService.IsMovingAllBuildingsEnabled
public bool IsMovingAllBuildingsEnabled()
{
	return this.Effects.movingAllBuildingsEnablers.Count > 0 || true;
}
```

### 1.3.2. Building movability enhanced

<b><span style="color:#4040ff">SYNOPSIS: </span></b> Enabling building movement more aggressively, allowing moving more *buildings* than the "Levitating Monument" map modifier, including hearths, ruins, caches, etc., even glade events. Resource nodes are still not movable.

```c#
// Eremite.Services.ConstructionService.CanBeMoved
// making buildings movable unconditionally, including hearths, glade events, caches, ghosts, etc. (yet to test the Seal);
// no need to change the overload version of CanBeMoved(Building building); since it always calls CanBeMoved(BuildingModel model) internally and will always return true if the latter always returns true.
public bool CanBeMoved(BuildingModel model)
{
	return (Serviceable.IsDebugMode && DebugModes.Construction) || (!Serviceable.EffectsService.IsMovingBuildingsBlocked() && ((Serviceable.EffectsService.IsMovingAllBuildingsEnabled() && model.movableWithEffects) || model.movable)) || true;
}

// Eremite.Buildings.Hearth.IsTooCloseToOtherHearth
// this solves self-collision when moving hearths
private bool IsTooCloseToOtherHearth()
{
	if (this.state.placed)
	{
		return false;
	}
	this.RecalculateArea();
	foreach (HearthState hearthState in GameMB.StateService.Buildings.hearths)
	{
		Hearth hearth = GameMB.BuildingsService.GetBuildingOrConstruction(hearthState.id) as Hearth;
		if (!hearthState.lifted)
		{
			foreach (Vector2Int field in this.area)
			{
				if (hearth.IsInRange(field))
				{
					return true;
				}
			}
		}
	}
	return false;
}
```

### 1.4. Glade event working time

<b><span style="color:#4040ff">SYNOPSIS: </span></b> Modifying working time required for glade events, 0.2x (equivalent to 500% working speed) in the example below.

```c#
// Eremite.Services.EffectsService.GetRelicWorkingTime
public float GetRelicWorkingTime(float baseTime, Relic relic)
{
	return baseTime * (1f / Mathf.Clamp(this.GetRelicsWorkingRate(relic), this.PerksConfig.minRelicsWorkingTimeRate, this.PerksConfig.maxRelicsWorkingTimeRate)) * 0.2f;
}
```

### 1.5. House capacity

<b><span style="color:#4040ff">SYNOPSIS: </span></b> Modifying capacity for each house, 2x in the example below. This capacity modifier scales other in-game bonuses as well, e.g. +1 bonus in game will have an final effect of +2 capacity under this modifier.

```c#
// Eremite.Services.EffectsService.GetHouseCapacity
// this function overload works at per-house basis, e.g. taking care of haunted houses
public int GetHouseCapacity(House house)
{
	return Mathf.Clamp(house.model.housingPlaces + house.state.bonusCapacity + this.Effects.globalHousesBonusCapacity + this.Effects.housesBonusCapacity.GetSafe(house.ModelName), this.PerksConfig.minHousesSlots, this.PerksConfig.maxHousesSlots) * 2;
}

// Eremite.Services.EffectsService.GetHouseCapacity
// this function overload works at house prototype basis, e.g. taking care of preview and encycropedia
public int GetHouseCapacity(HouseModel house)
{
	return Mathf.Clamp(house.housingPlaces + this.Effects.globalHousesBonusCapacity + this.Effects.housesBonusCapacity.GetSafe(house.Name), this.PerksConfig.minHousesSlots, this.PerksConfig.maxHousesSlots) * 2;
}
```

### 1.6. Goods' selling price to trader

<b><span style="color:#4040ff">SYNOPSIS: </span></b> Modifying sell price of goods to traders to greatly enhance trading experience, 10x worth of value in the example below.

```c#
// Eremite.Services.TradeService.GetValueInCurrency
public float GetValueInCurrency(string name, int amount)
{
	return this.RoundTradeValue(Serviceable.EffectsService.GetTraderSellPriceFor(name) / this.GetTradeCurrencyValue() * 10f, amount);
}
```

### 1.7. Trade route profit

<b><span style="color:#4040ff">SYNOPSIS: </span></b> Modifying sell price of goods to traders to greatly enhances trading experience, 10x worth of value in the example below.

```c#
// Eremite.Services.TradeService.GetValueInCurrency
private int GetPriceFor(float goodTradeValue, TradeTownState town)
{
	float num = goodTradeValue / Serviceable.TradeService.GetTradeCurrencyValue();
	num *= 1f + (float)town.standingLevel * this.Config.basePricePerStandingFactor;
	return Mathf.Max(1, Mathf.RoundToInt(num * this.Config.basePriceFactor * this.factorsCache[this.PriceFactorIndex])) * 10;
}
```

### 1.8. Construction speed

<b><span style="color:#4040ff">SYNOPSIS: </span></b> Modifying construction speed of buildings, 2x in the example below.

```c#
// Eremite.Services.EffectsService.GetConstructionRate
public float GetConstructionRate()
{
	return Mathf.Clamp(this.Effects.constructionSpeed + this.MetaPerks.constructionSpeedBonusRate, this.PerksConfig.minConstructionSpeedRate, this.PerksConfig.maxConstructionSpeedRate) * 2f;
}
```

### 1.9. Exposed glade information

<b><span style="color:#4040ff">SYNOPSIS: </span></b> Always showing dangerous glade icons, ignoring related effects and map modifiers. In addition, always allowing glade tooltip showing its content (similar to the cornerstone "Mists Piercers" without the negative effect).

```c#
// Eremite.Services.EffectsService.BlockDangerousGladesInfo
public void BlockDangerousGladesInfo(string owner)
{
	this.Effects.dangerousGladeInfoBlocksOwners.Add(owner);
	this.DangerousGladeInfo.Value = (this.Effects.dangerousGladeInfoBlocksOwners.Count == 0 || true);
}

// Eremite.Services.EffectsService.RemoveDangerousGladeInfoBlock
public void RemoveDangerousGladeInfoBlock(string owner)
{
	this.Effects.dangerousGladeInfoBlocksOwners.Remove(owner);
	this.DangerousGladeInfo.Value = (this.Effects.dangerousGladeInfoBlocksOwners.Count == 0 || true);
}

// Eremite.Services.EffectsService.GrantGladeInfo
public void GrantGladeInfo(string owner)
{
	this.Effects.gladeInfoOwners.Add(owner);
	this.GladeInfo.Value = (this.Effects.gladeInfoOwners.Count > 0 || true);
}

// Eremite.Services.EffectsService.Remove
public void RemoveGladeInfo(string owner)
{
	this.Effects.gladeInfoOwners.Remove(owner);
	this.GladeInfo.Value = (this.Effects.gladeInfoOwners.Count > 0 || true);
}

// Eremite.Services.EffectsService.PrepareInitialValues
private void PrepareInitialValues()
{
	this.GladeInfo.Value = (this.Effects.gladeInfoOwners.Count > 0 || true);  // mod this line
	this.DangerousGladeInfo.Value = (this.Effects.dangerousGladeInfoBlocksOwners.Count == 0 || true);  // mod this line
	this.IsHearthSacrificeBlocked.Value = (this.Effects.hearthSacrificeBlockOwners.Count > 0);
	this.IsVillagerDeathEffectBlocked.Value = (this.Effects.villagerDeathEffectBlockOwners.Count > 0);
}
```

### 1.10. Deprioritized path construction

<b><span style="color:#4040ff">SYNOPSIS: </span></b> Construction of Path, Paved Road and Reinforced Road has a default construction priority of -2.

```c#
// Eremite.Buildings.Building.SetUpState
private void SetUpState()
{
	this.BuildingState.placed = true;
	this.BuildingState.placedTime = GameMB.GameTimeService.Time;
	this.BuildingState.unscaledPlacedTime = GameMB.GameTimeService.UnscaledTime;
	if (this.BuildingModel.category == MB.Settings.GetBuilding("Path").category)
	{
		this.BuildingState.constructionPriority = Math.Clamp(this.BuildingState.constructionPriority - 2, -5, 5);
	}
	Building.lastRotation = this.BuildingState.rotation;
}
```

## 2. Production

### 2.1. Global production multiplier

<b><span style="color:#4040ff">SYNOPSIS: </span></b> Modifying the amount of goods produced per production cast, 10x in the example below. This affects all recipe-based production, including camps, pumps, mines, farms, rain collectors, production buildings, etc., virually all production in the game.

```c#
// Eremite.Services.EffectsService.GetProduction
public Good GetProduction(Building building, Good baseProduction, RecipeModel recipe, bool isExtra = false)
{
	return this.productionCalculator.GetResult(building, baseProduction, recipe, isExtra) * 10;
}
```

### 2.2. Global production speed

<b><span style="color:#4040ff">SYNOPSIS: </span></b> Modifying the production speed for, 2x in the example below. This affects all recipe-based production, including camps, pumps, mines, farms, rain collectors, production buildings, etc., virually all production in the game. Note the diminishing returns with high values, as logistics will become the bottleneck.

```c#
// Eremite.Services.EffectsService.GetBuildingProductionRate
public float GetBuildingProductionRate(Building building, RecipeModel recipe)
{
	return Mathf.Clamp(this.GetBuildingRawProductionRate(building.BuildingModel, recipe) + this.GetInternalProductionRate(building), this.PerksConfig.minProductionSpeed, this.PerksConfig.maxProductionSpeed) * 2f;
}
```

### 2.3 Production building capacity (non-rainwater)

<b><span style="color:#4040ff">SYNOPSIS: </span></b> Modifying the product storage of production buildings, 5x in the example below. This affects virually all production buildings in the game, except rain collectors and pumps. Note that setting this number too high can cause other problems, e.g. the amount of products buffered before triggering the delivery, causing starving of downstream production. Although the delivery threshold can be set manually, that's too tideous doing it for every single building.

```c#
// Eremite.Buildings.BuildingStorage.GetFullCapacity
private int GetFullCapacity(int baseCapacity)
{
	return Mathf.Max(1, baseCapacity + GameMB.EffectsService.GetBuildingStorageCapacity(this.building.ModelName)) * 5;
}
```

### 2.4. Rainwater tank capacity

<b><span style="color:#4040ff">SYNOPSIS: </span></b> Modifying the rainwater tank capacity, 10x in the example below. This affects rain collectors and pumps. Note the diminishing returns with high values. In combination with high production multiplier and production speed, one rain collector can be sufficient with just one worker assigned to it.

```c#
// Eremite.Buildings.RainCatcher.SetUp
// the rain collectors behave weirdly; it seems that the member 'RainCatcherModel.baseTankCapacity' is overwritten in prefabs therefore changing its value in the default constructor of RainCatcherModel is meaningless; I haven't found how to modify the prefabs yet... so it is the workaround here; the if statement is to prevent potential multiple applications of the capacity change.
public void SetUp(RainCatcherModel model, RainCatcherState state)
{
	this.model = model;
	this.state = state;
	if (this.model.baseTankCapacity <= 100)
	{
		this.model.baseTankCapacity *= 10;
	}
}

// Eremite.Buildings.Extractor.GetTankCapacity
public int GetTankCapacity()
{
	return Mathf.Max(0, this.model.baseTankCapacity + this.state.bonusTank) * 10;
}
```

### 2.5. Default global production limit

<b><span style="color:#4040ff">SYNOPSIS: </span></b> Modifying the global production limit, 150 in the example below. Manually setting everything is way too tideous.

```c#
// Eremite.Services.WorkshopsService.GetGlobalLimitFor
public int GetGlobalLimitFor(string goodName)
{
	if (!this.Limits.ContainsKey(goodName))
	{
		return 150;
	}
	return this.Limits[goodName];
}
```

### 2.6. Reduced global production limit for low-star recipes

<b><span style="color:#4040ff">SYNOPSIS: </span></b> Separating the production limit for low-star recipes when using global production limit values, 20% for 0-star recipes and 60% for 1-star recipes in the example below. Unlimited global limits, manually set limits and 2/3-star recipes are not affected. In addition, when the global limit of a product is active, the limit for corresponding low-star recipes cannot go below 1 (since the game use the value 0 for unlimited production).

```c#
// Eremite.Buildings.Workshop.SetUp
public void SetUp(WorkshopModel model, WorkshopState state)
{
	this.model = model;
	this.state = state;
	foreach (WorkshopRecipeState workshopRecipeState in state.recipes)
	{
		int globalLimit = GameMB.WorkshopsService.GetGlobalLimitFor(workshopRecipeState.productName);
		if (!workshopRecipeState.isLimitLocal && globalLimit > 0)
		{
			int level = this.GetRecipeModel(workshopRecipeState).grade.level;
			if (level == 0)
			{
				workshopRecipeState.limit = Math.Max(1, (int)((float)globalLimit * 0.2f));
			}
			else if (level == 1)
			{
				workshopRecipeState.limit = Math.Max(1, (int)((float)globalLimit * 0.6f));
			}
		}
	}
	this.ProductionStorage.SetUp(this, state.productionStorage, model.maxStorage);
	this.ingredientsStorage.SetUp(this, state.ingredientsStorage, new Func<int, GoodRequest>(this.GetBestGoodToObtain));
	this.blight.SetUp(this, state.blight);
}

// Eremite.Buildings.Workshop.ChangeLimitFor
public void ChangeLimitFor(WorkshopRecipeState recipe, int amount, bool isLocalChange)
{
	if (!isLocalChange && amount > 0)
	{
		int level = this.GetRecipeModel(recipe).grade.level;
		if (level == 0)
		{
			amount = Math.Max(1, (int)((float)amount * 0.2f));
		}
		else if (level == 1)
		{
			amount = Math.Max(1, (int)((float)amount * 0.6f));
		}
	}
	recipe.limit = amount;
	recipe.isLimitLocal = isLocalChange;
}
```



## 3. Logistics

### 3.1. Global carrying capacity multiplier

<b><span style="color:#4040ff">SYNOPSIS: </span></b> Modifying the carrying capacity of workers and carts, 5x in the example below. Note that setting these numbers too high can cause other problems, e.g. taking unnecessarily large amount of each ingredient from the warehouse to the production building upon enabling the recipe in the building.

```c#
// Eremite.Services.EffectsService.GetCartCapacity
public int GetCartCapacity(Cart cart)
{
	return (cart.model.baseCapacity + this.Effects.globalBonusCapacity + this.MetaPerks.globalCapacityBonus + this.GetProfessionsBonusCapacity(cart.GetWorkplaceProfession().Name) + this.GetWorkplaceBonusCapacity(cart)) * 5;
}

// Eremite.Services.EffectsService.GetVillagerCapacity
public int GetVillagerCapacity(Villager villager)
{
	return (villager.professionModel.MaxCapacity + this.Effects.globalBonusCapacity + this.MetaPerks.globalCapacityBonus + this.GetProfessionsBonusCapacity(villager.Profession) + this.GetWorkplaceBonusCapacity(villager)) * 5;
}
```

## 4. Drafting

### 4.1. Unlock all blueprints proposed

<b><span style="color:#4040ff">SYNOPSIS: </span></b> Unlocking all blueprints from each draft poll. Only children make choices, adults want them all!

```c#
// Eremite.Services.ReputationRewardsService.RewardPicked
public void RewardPicked(BuildingModel building)
{
	foreach (ReputationReward reputationReward in this.GetCurrentPicks())
	{
		Serviceable.GameContentService.Unlock(Serviceable.Settings.GetBuilding(reputationReward.building));
	}
	this.SendPickAnalytics(building);
	this.SetRewardAsPicked();
	this.UpdateWaitingRewards();
	if (this.RewardsToCollect.Value > 0)
	{
		this.GenerateNewPick();
	}
}

// Eremite.Services.ReputationRewardsService.SendPickAnalytics
// play prank with devs, sending garbage data to their analytics everytime I draft; they should do data cleaning and outlier removal before analysis anyway. 
private void SendPickAnalytics(BuildingModel building)
{
	if (Serviceable.TutorialService.IsAnyTutorial(Serviceable.Biome))
	{
		return;
	}
	Serviceable.GameAnalyticsService.Buildings.SendReputationRewardPick(this.State.currentPick.options, "only children make choices, adults want them all!");
}
```

### 4.2. Multiple cornerstone pick

<b><span style="color:#4040ff">SYNOPSIS: </span></b> Allowing multiple picks of cornerstones per draft poll. Since some cornerstones has side effects and is not guaranteed to grant advantage in every playthrough, it is not ideal to unlock them unconditionally like blueprints. Clicking the decline button will dismiss the draft and discard all remaining cornerstones.

```c#
// Eremite.Services.CornerstonesService.Pick
// return the remaining cornerstones to the front of pick queue, if applicable
public void Pick(EffectModel reward)
{
	bool assertions = DebugModes.Assertions;
	RewardPickState currentPick = this.GetCurrentPick();
	Log.Info(string.Format("[Cor] Set last date pick: {0} {1} {2}", currentPick.date.year, currentPick.date.season, currentPick.date.quarter), null);
	if (reward != null)
	{
		this.SendPickAnalytics(currentPick, reward.Name);
	}
	if (!currentPick.isExtra)
	{
		Serviceable.StateService.Gameplay.lastCornerstonePickDate = currentPick.date;
	}
	this.Picks.Remove(currentPick);
	if (reward != null)
	{
		reward.Apply(EffectContextType.None, null, 0);
		currentPick.options.Remove(reward.Name);
		if (currentPick.options.Count > 0)
		{
			this.Picks.Insert(0, currentPick);
		}
	}
	if (reward == null)
	{
		this.RewardForDecline();
	}
	this.OnRewardsPicked.OnNext(reward);
}

// Eremite.View.HUD.RewardPickPopup.OnRewardPicked
// make cornerstone pick screen auto-repop when the pick queue is not empty
// similar to Eremite.View.HUD.ReputationRewardsPopup.TryShowingNext
private void OnRewardPicked(EffectModel reward)
{
	Log.Info(string.Format("[Cor] Cornerstone {0} picked in {1} {2} {3}", new object[]
	{
		reward.Name,
		GameMB.CalendarService.Year,
		GameMB.CalendarService.Season,
		GameMB.CalendarService.Quarter
	}), null);
	GameMB.CornerstonesService.Pick(reward);
	if (GameMB.CornerstonesService.GetCurrentPick() != null)
	{
		this.SetUpSlots();
		this.SetUpReroll();
		this.SetUpDeclineButton();
		this.SetUpPollPanel();
		this.SetUpDialogue();
		return;
	}
	this.Hide();
}
```


## 5. Meta progression

### 5.1. Unlock all proposed non-core citadal upgrades in Queen's Hand Trial mode

<b><span style="color:#4040ff">SYNOPSIS: </span></b> Unlocks all non-core citadel upgrades proposed in each draft. Why I have to give up *Beaver House* to obtain *Field Kitchen*?. Only children make choices, adults want them all!

```c#
// Eremite.Services.IronmanService.Pick
public void Pick(CapitalUpgradeModel model)
{
	if (this.IsCore(model))
	{
		this.BuyCore(model);
		return;
	}
	this.PayForUpgrade(model);
	foreach (IronmanPickOption ironmanPickOption in this.State.currentPick.options)
	{
		this.Unlock(Serviceable.Settings.GetCapitalUpgrade(ironmanPickOption.model));
	}
	this.SetAsSeen();
	this.MarkAsPicked();
	if (!this.HasReachedMaxPicks())
	{
		this.SetNewPick();
	}
	this.CallEvents(model);
}
```

## 6. Embarkation

### 6.1. Larger embarkation caravan

<b><span style="color:#4040ff">SYNOPSIS: </span></b> Modifying starting up number of villager for each embarkation caravan, 4x in the example below.

```c#
// Eremite.Services.Meta.CaravanGenerator.GetVillagersAmount
private int GetVillagersAmount(System.Random rng)
{
	return Mathf.Max(Serviceable.Settings.gameplayRaces, this.Config.embarkVillagersAmount.Random(rng) + this.cycleEffects.bonusEmbarkVillagers) * 4;
}
```

### 6.3. Embarkation goods

<b><span style="color:#4040ff">SYNOPSIS: </span></b> Modifying picked goods and adding extra goods to the embarking caravan, 10x everything with an extra of 200 Amber in the example below. The very same function can be used to modify effects (e.g. cornerstones).

```c#
// Eremite.Services.Meta.CaravanGenerator.GenerateGoods
private List<Good> GenerateGoods()
{
	List<Good> goods = (from g in (from g in this.Config.caravanGoodsAlwaysIncluded
	select g.ToGood()).Concat(this.GetMetaEmbarkGoods()).Unify().Select(new Func<Good, Good>(this.MultiplyByEmbarkFactor)).Concat(this.GetCycleEmbarkGoods()).Unify()
	orderby Serviceable.Settings.GetGood(g.name).category.order, Serviceable.Settings.GetGood(g.name).order descending
	select g).ToList<Good>();
	for (int i = 0; i < goods.Count; i++)
	{
		Good g2 = new Good(goods[i].name, goods[i].amount * 10);
		goods[i] = g2;
	}
	goods.Add(new Good("[Valuable] Amber", 200));
	return goods;
}
```

### 6.3. More embarkation points

<b><span style="color:#4040ff">SYNOPSIS: </span></b> Modifying base embarkation points for every caravan, 200 in the below example. Note the diminishing returns with high values, as the total count of embarkation bonus to apply for a caravan is limited.

```c#
// Eremite.View.Menu.Pick.BuildingsPickScreen.GetBasePreparationPoints
private int GetBasePreparationPoints()
{
	return Mathf.Max(0, MB.MetaPerksService.GetBasePreparationPoints() + WorldMB.WorldMapService.GetMinDifficultyFor(this.field).preparationPointsPenalty) + 200;
}
```


### 6.4. Always-unlocked blueprints

<b><span style="color:#4040ff">SYNOPSIS: </span></b> Making some blueprints unconditionally unlocked for every game. Multiple buildings are included in the example below. Name of buildings to add can be found in [buildings_list.txt](buildings_list.txt), note not all buildings from this list can be built in game.

```c#
// Eremite.Services.GameContentService.EnsureBuildings
private void EnsureBuildings()
{
	if (this.State.buildings.Count == 0)
	{
		this.State.essentialBuildings = this.GetEssentialBuildings();
		this.State.buildings = (from b in (from b in Serviceable.Settings.Buildings
		where b.canBePicked || this.IsEssential(b)
		where (this.IsEssential(b) && Serviceable.MetaStateService.Content.buildings.Contains(b.Name)) || Serviceable.MetaStateService.GameConditions.embarkBuildings.Any((string p) => p == b.Name)
		select b).Concat(this.GetOptionalBuildings()).Distinct<BuildingModel>()
		select b.Name).ToList<string>();
	}
	List<string> list = new List<string>();
	list.Add("Forager's Camp");
	list.Add("Herbalist's Camp");
	list.Add("Trapper's Camp");
	list.Add("SmallFarm");
	list.Add("Plantation");
	list.Add("Herb Garden");
	list.Add("Hallowed SmallFarm");
	list.Add("Hallowed Herb Garden");
	list.Add("Hallowed Herb Garden");
	list.Add("Beaver House");
	list.Add("Fox House");
	list.Add("Harpy House");
	list.Add("Human House");
	list.Add("Lizard House");
	list.Add("Purged Beaver House");
	list.Add("Purged Fox House");
	list.Add("Purged Harpy House");
	list.Add("Purged Human House");
	list.Add("Purged Lizard House");
	list.Add("Flawless Brewery");
	list.Add("Flawless Cellar");
	list.Add("Flawless Cooperage");
	list.Add("Flawless Druid");
	list.Add("Flawless Leatherworks");
	list.Add("Flawless Rain Mill");
	list.Add("Flawless Smelter");
	list.Add("Holy Market");
	list.Add("Holy Temple");
	list.Add("Trading Post");
	HashSet<string> buildingSet = new HashSet<string>(this.State.buildings);
	foreach (string building in list)
	{
		BuildingModel replaces = Serviceable.Settings.GetBuilding(building).replaces;
		if (replaces && buildingSet.Contains(replaces.Name))
		{
			buildingSet.Remove(replaces.Name);
		}
		buildingSet.Add(building);
	}
	this.State.buildings = buildingSet.ToList<string>();
}
```