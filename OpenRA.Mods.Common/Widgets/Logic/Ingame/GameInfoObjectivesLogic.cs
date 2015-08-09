#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

#define DEBUG

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

            System.IO.StreamReader input = new System.IO.StreamReader("difficulty.txt");
            String line = null;

            do
            {
                line = input.ReadLine();

                if (line == null)
                {
                    break;
                }
                if (line == String.Empty)
                {
                    break;
                }
                difficulty = line;
            } while (true);

            widget.Get<LabelWidget>("MISSION_DIFFICULTY").Text = difficulty;

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