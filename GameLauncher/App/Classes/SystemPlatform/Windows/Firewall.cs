﻿using GameLauncher.App.Classes.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFirewallHelper;
using WindowsFirewallHelper.FirewallAPIv2;
using WindowsFirewallHelper.FirewallAPIv2.Rules;

namespace GameLauncher.App.Classes.SystemPlatform.Windows
{
    class FirewallHelper
    {
        public static void DoesRulesExist(bool removeFirewallRule, bool firstTimeRun, string nameOfApp, string localOfApp, string groupKey, string description, FirewallProtocol protocol)
        {
            CheckIfRuleExists(removeFirewallRule, firstTimeRun, nameOfApp, localOfApp, groupKey, description, FirewallDirection.Inbound, protocol, FirewallDirection.Inbound.ToString());
            CheckIfRuleExists(removeFirewallRule, firstTimeRun, nameOfApp, localOfApp, groupKey, description, FirewallDirection.Outbound, protocol, FirewallDirection.Outbound.ToString());
        }

        public static void CheckIfRuleExists(bool removeFirewallRule, bool firstTimeRun, string nameOfApp, string localOfApp, string groupKey, string description, FirewallDirection direction, FirewallProtocol protocol, string firewallLogNote)
        {
            //Remove Firewall Rules
            if (removeFirewallRule == true && firstTimeRun == false)
            {
                RemoveRules(nameOfApp, firewallLogNote);
            }
            //Add Firewall Rules
            else if (removeFirewallRule == false && firstTimeRun == true)
            {
                AddApplicationRule(nameOfApp, localOfApp, groupKey, description, direction, protocol, firewallLogNote);
            }
            //Removes a Specific Rule from Firewall (When switching locations)
            else if (removeFirewallRule == true && firstTimeRun == true)
            {
                if (RuleExist(nameOfApp) == true)
                {
                    RemoveRules(nameOfApp, firewallLogNote);
                    Log.Info("WINDOWS FIREWALL: Found " + nameOfApp + " {" + firewallLogNote + "} In Firewall");
                }
                else if (RuleExist(nameOfApp) == false)
                {
                    AddApplicationRule(nameOfApp, localOfApp, groupKey, description, direction, protocol, firewallLogNote);
                }
            }
            else if (removeFirewallRule == false && firstTimeRun == false)
            {
                Log.Info("WINDOWS FIREWALL: Already Exlcuded " + nameOfApp + " {" + firewallLogNote + "}");
            }

            else
            {
                Log.Error("WINDOWS FIREWALL: Firewall Error - Check With Visual Studio for Error Debuging");
            }
        }

        public static void AddApplicationRule(string nameOfApp, string localOfApp, string groupKey, string description, FirewallDirection direction, FirewallProtocol protocol, string firewallLogNote) 
        {
            if (Firewall.Instance.IsSupported)
            {
                Log.Info("WINDOWS FIREWALL: Supported Firewall Found");
                var rule = new StandardRuleWin7(localOfApp, FirewallAction.Allow, direction, FirewallProfiles.Domain | FirewallProfiles.Private | FirewallProfiles.Public)
                {
                    ApplicationName = localOfApp,
                    Name = nameOfApp,
                    Grouping = groupKey,
                    Description = description,
                    InterfaceTypes = FirewallInterfaceTypes.Lan | FirewallInterfaceTypes.RemoteAccess |
                                     FirewallInterfaceTypes.Wireless,
                    Protocol = protocol
                };

                if(direction == FirewallDirection.Inbound) {
                    rule.EdgeTraversalOptions = EdgeTraversalAction.Allow;
                }

                Firewall.Instance.Rules.Add(rule);
                Log.Info("WINDOWS FIREWALL: Finished Adding " + nameOfApp + " to Firewall! {" + firewallLogNote + "}");
            }
            else
            {
                AddDefaultApplicationRule(nameOfApp, localOfApp, direction, protocol, firewallLogNote);
            }

        }

        private static void AddDefaultApplicationRule(string nameOfApp, string localOfApp, FirewallDirection direction, FirewallProtocol protocol, string firewallLogNote)
        {
            Log.Warning("WINDOWS FIREWALL: Falling back to 'LegacyStandard'");
            var defaultRule = FirewallManager.Instance.CreateApplicationRule(
                FirewallProfiles.Domain | FirewallProfiles.Private | FirewallProfiles.Public,
                nameOfApp,
                FirewallAction.Allow,
                localOfApp, protocol);

            defaultRule.Direction = direction;

            FirewallManager.Instance.Rules.Add(defaultRule);
            Log.Warning("WINDOWS FIREWALL: Finished Adding " + nameOfApp + " to Firewall! {" + firewallLogNote + "}");
        }

        public static void RemoveRules(string nameOfApp, string firewallLogNote)
        {
            var myRule = FindRules(nameOfApp).ToArray();
            foreach (var rule in myRule)
                try
                {
                    Log.Warning("WINDOWS FIREWALL: Removed " + nameOfApp + " {" + firewallLogNote + "} From Firewall!");
                    FirewallManager.Instance.Rules.Remove(rule);
                }
                catch (Exception ex)
                {
                    Log.Error("WINDOWS FIREWALL: " + ex.Message);
                }
        }

        public static bool RemoveRule(string nameOfApp, string firewallLogNote)
        {
            var myRule = FindRule(nameOfApp);
            Log.Warning("WINDOWS FIREWALL: Removed " + nameOfApp + " {" + firewallLogNote + "} From Firewall!");
            return FirewallManager.Instance.Rules.Remove(myRule);
        }

        public static bool RuleExist(string nameOfApp)
        {
            return FindRules(nameOfApp).Any();
        }

        public static IEnumerable<IRule> FindRules(string nameOfApp)
        {
            if (Firewall.Instance.IsSupported && StandardRuleWin7.IsSupported)
                return Firewall.Instance.Rules.Where(r => string.Equals(r.Name, nameOfApp,
                    StringComparison.OrdinalIgnoreCase)).ToArray();

            return FirewallManager.Instance.Rules.Where(r => string.Equals(r.Name, nameOfApp,
                StringComparison.OrdinalIgnoreCase)).ToArray();
        }

        public static IRule FindRule(string nameOfApp)
        {
            if (Firewall.Instance.IsSupported && StandardRuleWin7.IsSupported)
                return Firewall.Instance.Rules.FirstOrDefault(r => string.Equals(r.Name, nameOfApp,
                    StringComparison.OrdinalIgnoreCase));

            return FirewallManager.Instance.Rules.FirstOrDefault(r => string.Equals(r.Name, nameOfApp,
                StringComparison.OrdinalIgnoreCase));
        }
    }
}
