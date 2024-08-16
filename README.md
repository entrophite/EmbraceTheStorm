# EmbraceTheStorm

These are my note for statically injected code to alter the gameplay of *Against the Storm*, in purpose for reinjecting these codes after future version updates.

<b><span style="color:#60ff60">VERSION: </span></b> 1.3@steam

## 1. Environment/Glades/Settlement

### 1.1. Hostility

<b><span style="color:#4040ff">SYNOPSIS: </span></b> Modifying the hostility bar size while keeping tracking the total gained hostility points. The example belows sets the hostility points per level to the total number of points from original level 0 through the level max (e.g. 3100 per level instead of 100, as the max level under Prestige 20 is 31). This value can change per difficulty, since different max hostility levels are used by the game.

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

### 1.3. Building always movable

<b><span style="color:#4040ff">SYNOPSIS: </span></b> Enabling building movement as if the map modifier "Levitating Monument" is always on.

```c#
// Eremite.Services.EffectsService.IsMovingAllBuildingsEnabled
public bool IsMovingAllBuildingsEnabled()
{
	return this.Effects.movingAllBuildingsEnablers.Count > 0 || true;
}

// Eremite.Services.ConstructionService.CanBeMoved
// the function below is not changed; I haven't tested if setting this to true can be fun (e.g. enabling moving resource nodes and glade events); nor know if this causes bugs.
public bool CanBeMoved(Building building)
{
	return (Serviceable.IsDebugMode && DebugModes.Construction) || (!building.IsMovingBlocked() && (this.CanMoveConstructionSite(building) || this.CanBeMoved(building.BuildingModel)));
}
public bool CanBeMoved(BuildingModel model)
{
	return (Serviceable.IsDebugMode && DebugModes.Construction) || (!Serviceable.EffectsService.IsMovingBuildingsBlocked() && ((Serviceable.EffectsService.IsMovingAllBuildingsEnabled() && model.movableWithEffects) || model.movable));
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

<b><span style="color:#4040ff">SYNOPSIS: </span></b> Modifying capacity for each house, 2x in the example below. This capacity modifier scales after other in-game bonuses (e.g. acquiring an effect of +1 bonus will have an effect +2 capacity after scaling).

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

<b><span style="color:#4040ff">SYNOPSIS: </span></b> Modifying sell price of goods to traders to greatly enhances trading experience, 10x worth of value in the example below.

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

### 2.5. Default global product limit

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

### 2.6. Reduced global product limit for low-star recipes

<b><span style="color:#4040ff">SYNOPSIS: </span></b> Separating the production limit for low-star recipes when using global production limit values, 20% for 0-star recipes and 60% for 1-star recipes in the example below. Unlimited global limits, manually set limits and 2/3-star recipes are not affected. In addition, when the global limit is not unlimited, the reduced limit for low-star recipes cannot be less than 1 (since the game use the value 0 for unlimited production).

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

<b><span style="color:#4040ff">SYNOPSIS: </span></b> Unlocking all blueprints proposed in each draft. Only children make choices, adults want them all!

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

<b><span style="color:#ffff40">IN-DEVELOPMENT: </span></b> Allowing multiple picks of cornerstones among proposed choices per draft. Since some cornerstones has side effects and is not guaranteed to grant advantage in every playthrough, it is not ideal to unlock them unconditionally like blueprints. The goal is to allow multiple picks, however this is significantly more difficult to implement and is still in development.


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

### 6.1. Always-unlocked blueprints

<b><span style="color:#ffff40">IN-DEVELOPMENT: </span></b> Making some blueprints always unlocked for every game without being selected during embarkation preparation. The implementation is still in progress.

### 6.2. More embarkation points

<b><span style="color:#4040ff">SYNOPSIS: </span></b> Modifying base embarkation points for every caravan, 200 in the below example. Note the diminishing returns with high values, as the total count of embarkation bonus to apply for a caravan is limited.

```c#
// Eremite.View.Menu.Pick.BuildingsPickScreen.GetBasePreparationPoints
private int GetBasePreparationPoints()
{
	return Mathf.Max(0, MB.MetaPerksService.GetBasePreparationPoints() + WorldMB.WorldMapService.GetMinDifficultyFor(this.field).preparationPointsPenalty) + 200;
}
```