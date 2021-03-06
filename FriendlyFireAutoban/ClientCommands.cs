﻿using Smod2;
using Smod2.EventHandlers;
using Smod2.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace FriendlyFireAutoban
{
	class CallCommandHandler : IEventHandlerCallCommand
	{
		private FriendlyFireAutobanPlugin plugin;

		public CallCommandHandler(Plugin plugin)
		{
			this.plugin = (FriendlyFireAutobanPlugin)plugin;
		}

		public void OnCallCommand(PlayerCallCommandEvent ev)
		{
			string command = ev.Command.Split(' ')[0];
			string[] quotedArgs = Regex.Matches(ev.Command, "[^\\s\"\']+|\"([^\"]*)\"|\'([^\']*)\'")
				.Cast<Match>()
				.Select(m => {
					return Regex.Replace(Regex.Replace(m.Value, "^\'([^\']*)\'$", "$1"), "^\"([^\"]*)\"$", "$1");
				})
				.ToArray()
				.Skip(1)
				.ToArray();

			if (this.plugin.outall)
			{
				this.plugin.Info("Quoted Args for command: " + string.Join(" | ", quotedArgs));
			}

			if (command.Equals(this.plugin.GetTranslation("forgive_command")))
			{
				if (this.plugin.enable)
				{
					if (this.plugin.TeamkillVictims.ContainsKey(ev.Player.UserId) &&
						this.plugin.TeamkillVictims[ev.Player.UserId] != null)
					{
						Teamkill teamkill = this.plugin.TeamkillVictims[ev.Player.UserId];
						if (this.plugin.Teamkillers.ContainsKey(teamkill.KillerUserId))
						{
							int removedBans = this.plugin.Teamkillers[teamkill.KillerUserId].Teamkills.RemoveAll(x => x.Equals(teamkill));
							if (removedBans > 0)
							{
								// No need for broadcast with return message
								//ev.Player.PersonalBroadcast(5, "You forgave this player.", false);
								// TODO: Send a broadcast to the killer
								ev.ReturnMessage = string.Format(this.plugin.GetTranslation("forgive_success"), teamkill.KillerName, teamkill.GetRoleDisplay());
							}
							else
							{
								ev.ReturnMessage = string.Format(this.plugin.GetTranslation("forgive_duplicate"), teamkill.KillerName, teamkill.GetRoleDisplay());
							}
						}
						else
						{
							ev.ReturnMessage = this.plugin.GetTranslation("forgive_disconnect");
						}

						// No matter what, remove this teamkill cached in the array
						this.plugin.TeamkillVictims.Remove(ev.Player.UserId);
					}
					else
					{
						ev.ReturnMessage = this.plugin.GetTranslation("forgive_invalid");
					}
				}
				else
				{
					ev.ReturnMessage = this.plugin.GetTranslation("ffa_disabled");
				}
			}
			else if (command.Equals(this.plugin.GetTranslation("tks_command")))
			{
				if (this.plugin.enable)
				{
					if (quotedArgs.Length == 1)
					{
						List<Teamkiller> teamkillers = new List<Teamkiller>();
						try
						{
							if (Regex.Match(quotedArgs[0], "^[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]$").Success)
							{
								// https://stackoverflow.com/questions/55436309/how-do-i-use-linq-to-select-from-a-list-inside-a-map
								teamkillers = this.plugin.Teamkillers.Values.Where(
									x => x.UserId.Equals(quotedArgs[0])
								).ToList();
							}
							else
							{
								// https://stackoverflow.com/questions/55436309/how-do-i-use-linq-to-select-from-a-list-inside-a-map
								teamkillers = this.plugin.Teamkillers.Values.Where(
									x => x.Name.Contains(quotedArgs[0])
								).ToList();
							}
						}
						catch (Exception e)
						{
							if (this.plugin.outall)
							{
								this.plugin.Error(e.Message);
								this.plugin.Error(e.StackTrace);
							}
						}

						if (teamkillers.Count == 1)
						{
							string retval = "Player " + teamkillers[0].Name + " has a K/D ratio of " + teamkillers[0].Kills + ":" + teamkillers[0].Deaths + " or " + teamkillers[0].GetKDR() + ".\n";
							foreach (Teamkill tk in teamkillers[0].Teamkills)
							{
								retval +=
									string.Format(
										this.plugin.GetTranslation("tks_teamkill_entry"),
										(tk.Duration / 60) + ":" + (tk.Duration % 60),
										tk.KillerName,
										tk.VictimName,
										tk.GetRoleDisplay()
									) + "\n";
							}
							ev.ReturnMessage = retval;
						}
						else
						{
							ev.ReturnMessage = this.plugin.GetTranslation("tks_no_teamkills");
						}
					}
					else
					{
						ev.ReturnMessage = this.plugin.GetTranslation("tks_not_found");
					}
				}
				else
				{
					ev.ReturnMessage = this.plugin.GetTranslation("ffa_disabled");
				}
			}
		}
	}
}
