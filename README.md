# EmbraceTheStorm

These are my note for statically injected code to alter the gameplay of *Against the Storm*, in preparation for future version updates.

<b><span style="color:#60ff60">VERSION: </span></b> 1.3@steam

## 1. Environment/settlement-wise effects

### 1.1. Hostility

<b><span style="color:#4040ff">SYNOPSIS: </span></b> Making the hostility bar longer so it typically cannot ever reach to level higher than 0, while still allowing checking the current level of hostility. Done by changing the points of each hostility level. Code below can still track the total points of hostility (through the max hostility level) in the original gameplay, e.g. a 3100 bar means the original gameplay has hostility level capped at 31.

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
// the function below is not changed; I haven't tested if setting this to true can be fun (e.g. enabling moving resource nodes and glade events); nor know if this can cause bugs.
public bool CanBeMoved(BuildingModel model)
{
	return (Serviceable.IsDebugMode && DebugModes.Construction) || (!Serviceable.EffectsService.IsMovingBuildingsBlocked() && ((Serviceable.EffectsService.IsMovingAllBuildingsEnabled() && model.movableWithEffects) || model.movable));
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

<b><span style="color:#4040ff">SYNOPSIS: </span></b> Unlocks all blueprints proposed in each draft. Only children make choices, adults want them all!

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

## 5. Meta progression

### 5.1. Unlock all proposed non-core citadal upgrades in Queen's Hand Trial mode

<b><span style="color:#4040ff">SYNOPSIS: </span></b> Unlocks all non-core citadel upgrades proposed in each draft. Only children make choices, adults want them all!

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
