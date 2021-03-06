#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Lint
{
	public class CheckUpgrades : ILintPass
	{
		public void Run(Action<string> emitError, Action<string> emitWarning, Map map)
		{
			CheckUpgradesValidity(emitError, map);
			CheckUpgradesUsage(emitError, emitWarning, map);
		}

		private static void CheckUpgradesValidity(Action<string> emitError, Map map)
		{
			var upgradesGranted = GetAllGrantedUpgrades(emitError, map).ToHashSet();

			foreach (var actorInfo in map.Rules.Actors)
			{
				foreach (var trait in actorInfo.Value.Traits)
				{
					var fields = trait.GetType().GetFields();
					foreach (var field in fields.Where(x => x.HasAttribute<UpgradeUsedReferenceAttribute>()))
					{
						var values = LintExts.GetFieldValues(trait, field, emitError);
						foreach (var value in values.Where(x => !upgradesGranted.Contains(x)))
							emitError("Actor type `{0}` uses upgrade `{1}` that is not granted by anything!".F(actorInfo.Key, value));
					}
				}
			}
		}

		private static void CheckUpgradesUsage(Action<string> emitError, Action<string> emitWarning, Map map)
		{
			var upgradesUsed = GetAllUsedUpgrades(emitError, map).ToHashSet();

			// Check all upgrades granted by traits.
			foreach (var actorInfo in map.Rules.Actors)
			{
				foreach (var trait in actorInfo.Value.Traits)
				{
					var fields = trait.GetType().GetFields();
					foreach (var field in fields.Where(x => x.HasAttribute<UpgradeGrantedReferenceAttribute>()))
					{
						var values = LintExts.GetFieldValues(trait, field, emitError);
						foreach (var value in values.Where(x => !upgradesUsed.Contains(x)))
							emitWarning("Actor type `{0}` grants upgrade `{1}` that is not used by anything!".F(actorInfo.Key, value));
					}
				}
			}

			// Check all upgrades granted by warheads.
			foreach (var weapon in map.Rules.Weapons)
			{
				foreach (var warhead in weapon.Value.Warheads)
				{
					var fields = warhead.GetType().GetFields();
					foreach (var field in fields.Where(x => x.HasAttribute<UpgradeGrantedReferenceAttribute>()))
					{
						var values = LintExts.GetFieldValues(warhead, field, emitError);
						foreach (var value in values.Where(x => !upgradesUsed.Contains(x)))
							emitWarning("Weapon type `{0}` grants upgrade `{1}` that is not used by anything!".F(weapon.Key, value));
					}
				}
			}
		}

		static IEnumerable<string> GetAllGrantedUpgrades(Action<string> emitError, Map map)
		{
			// Get all upgrades granted by traits.
			foreach (var actorInfo in map.Rules.Actors)
			{
				foreach (var trait in actorInfo.Value.Traits)
				{
					var fields = trait.GetType().GetFields();
					foreach (var field in fields.Where(x => x.HasAttribute<UpgradeGrantedReferenceAttribute>()))
					{
						var values = LintExts.GetFieldValues(trait, field, emitError);
						foreach (var value in values)
							yield return value;
					}
				}
			}

			// Get all upgrades granted by warheads.
			foreach (var weapon in map.Rules.Weapons)
			{
				foreach (var warhead in weapon.Value.Warheads)
				{
					var fields = warhead.GetType().GetFields();
					foreach (var field in fields.Where(x => x.HasAttribute<UpgradeGrantedReferenceAttribute>()))
					{
						var values = LintExts.GetFieldValues(warhead, field, emitError);
						foreach (var value in values)
							yield return value;
					}
				}
			}

			// TODO: HACK because GainsExperience grants upgrades differently to most other sources.
			var gainsExperience = map.Rules.Actors.SelectMany(x => x.Value.Traits.WithInterface<GainsExperienceInfo>()
				.SelectMany(y => y.Upgrades.SelectMany(z => z.Value)));

			foreach (var upgrade in gainsExperience)
				yield return upgrade;

			// TODO: HACK because Pluggable grants upgrades differently to most other sources.
			var pluggable = map.Rules.Actors.SelectMany(x => x.Value.Traits.WithInterface<PluggableInfo>()
				.SelectMany(y => y.Upgrades.SelectMany(z => z.Value)));

			foreach (var upgrade in pluggable)
				yield return upgrade;
		}

		static IEnumerable<string> GetAllUsedUpgrades(Action<string> emitError, Map map)
		{
			foreach (var actorInfo in map.Rules.Actors)
			{
				foreach (var trait in actorInfo.Value.Traits)
				{
					var fields = trait.GetType().GetFields();
					foreach (var field in fields.Where(x => x.HasAttribute<UpgradeUsedReferenceAttribute>()))
					{
						var values = LintExts.GetFieldValues(trait, field, emitError);
						foreach (var value in values)
							yield return value;
					}
				}
			}

			// TODO: HACK because GainsExperience and GainsStatUpgrades do not play by the rules...
			// We assume everything GainsExperience grants is used by GainsStatUpgrade
			var gainsExperience = map.Rules.Actors.SelectMany(x => x.Value.Traits.WithInterface<GainsExperienceInfo>()
				.SelectMany(y => y.Upgrades.SelectMany(z => z.Value)));

			foreach (var upgrade in gainsExperience)
				yield return upgrade;
		}
	}
}