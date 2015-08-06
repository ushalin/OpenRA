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
using System.Drawing;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;
using OpenRA.Widgets;
using OpenRA.FileSystem;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	class GameInfoObjectivesLogic
	{
		readonly ContainerWidget template;

        // DELETE THIS LATER POSSIBLY
		// DropDownButtonWidget difficulty;
        //Widget temp_wid;

        String difficulty;

		[ObjectCreator.UseCtor]
		public GameInfoObjectivesLogic(Widget widget, World world)
		{
			var lp = world.LocalPlayer;

			var missionStatus = widget.Get<LabelWidget>("MISSION_STATUS");
			missionStatus.GetText = () => lp.WinState == WinState.Undefined ? "In progress":
				lp.WinState == WinState.Won ? "Accomplished" : "Failed";
			missionStatus.GetColor = () => lp.WinState == WinState.Undefined ? Color.White :
				lp.WinState == WinState.Won ? Color.LimeGreen : Color.Red;

            /*var temp = world.Map.Options.Difficulties.Select(d => new LabelWidget
            {
                GetText = () => difficulty = d
            });*/

            //difficulty = world.Map.Options.Difficulties.FirstOrDefault();

            difficulty = widget.Get<DropDownButtonWidget>("DIFFICULTY_DROPDOWNBUTTON").Text;

            //var missionDifficulty = widget.Get<LabelWidget>("MISSION_DIFFICULTY");
            widget.Get<LabelWidget>("MISSION_DIFFICULTY").Text = difficulty;

            //String s_diff = Map.Difficulty();

			var mo = lp.PlayerActor.TraitOrDefault<MissionObjectives>();
			if (mo == null)
				return;

			var objectivesPanel = widget.Get<ScrollPanelWidget>("OBJECTIVES_PANEL");
			template = objectivesPanel.Get<ContainerWidget>("OBJECTIVE_TEMPLATE");

			PopulateObjectivesList(mo, objectivesPanel, template);

			Action<Player, bool> redrawObjectives = (player, _) =>
			{
				if (player == lp)
					PopulateObjectivesList(mo, objectivesPanel, template);
			};
			mo.ObjectiveAdded += redrawObjectives;
		}

		void PopulateObjectivesList(MissionObjectives mo, ScrollPanelWidget parent, ContainerWidget template)
		{
			parent.RemoveChildren();

			foreach (var o in mo.Objectives.OrderBy(o => o.Type))
			{
				var objective = o; // Work around the loop closure issue in older versions of C#
				var widget = template.Clone();

				var label = widget.Get<LabelWidget>("OBJECTIVE_TYPE");
				label.GetText = () => objective.Type == ObjectiveType.Primary ? "Primary" : "Secondary";

				var checkbox = widget.Get<CheckboxWidget>("OBJECTIVE_STATUS");
				checkbox.IsChecked = () => objective.State != ObjectiveState.Incomplete;
				checkbox.GetCheckType = () => objective.State == ObjectiveState.Completed ? "checked" : "crossed";
				checkbox.GetText = () => objective.Description;

				parent.AddChild(widget);
			}
		}
        class DropDownOption
        {
            public string Title;
            public Func<bool> IsSelected;
            public Action OnClick;
        }
	}
}