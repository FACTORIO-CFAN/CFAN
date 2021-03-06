using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Linq;
using CKAN.Factorio;
using CKAN.Factorio.Version;

namespace CKAN
{
    public sealed class GUIMod
    {
        private CfanModule Mod { get; set; }

        public string Name
        {
            get { return Mod.title.Trim(); }
        }

        public bool IsInstalled { get; private set; }
        public bool HasUpdate { get; private set; }
        public bool IsIncompatible { get; private set; }
        public bool IsAutodetected { get; private set; }
        public string Authors { get; private set; }
        public string InstalledVersion { get; private set; }
        public string LatestVersion { get; private set; }
        public string DownloadSize { get; private set; }
        public bool IsCached { get; private set; }

        // These indicate the maximum KSP version that the maximum available
        // version of this mod can handle. The "Long" version also indicates
        // to the user if a mod upgrade would be required. (#1270)
        public string KSPCompatibility { get; private set; }
        public string KSPCompatibilityLong { get; private set; }

        public string KSPversion { get; private set; }
        public string Abstract { get; private set; }
        public string Homepage { get; private set; }
        public string Identifier { get; private set; }
        public bool IsInstallChecked { get; set; }
        public bool IsUpgradeChecked { get; private set; }
        public bool IsNew { get; set; }
        public bool IsCKAN { get; private set; }
        public string Abbrevation { get; private set; }

        public string Version
        {
            get { return IsInstalled ? InstalledVersion : LatestVersion; }
        }

        public GUIMod(CfanModule mod, IRegistryQuerier registry, FactorioVersion current_ksp_version)
        {
            IsCKAN = mod is CfanModule;
            //Currently anything which could alter these causes a full reload of the modlist
            // If this is ever changed these could be moved into the properties
            Mod = mod;
            IsInstalled = registry.IsInstalled(mod.identifier, false);
            IsInstallChecked = IsInstalled;
            HasUpdate = registry.HasUpdate(mod.identifier, current_ksp_version);
            IsIncompatible = !mod.IsCompatibleKSP(current_ksp_version);
            IsAutodetected = registry.IsAutodetected(mod.identifier);
            Authors = mod.authors == null ? "N/A" : String.Join(",", mod.authors);

            var installed_version = registry.InstalledVersion(mod.identifier);
            AbstractVersion latest_version = null;
            var ksp_version = mod.getMinFactorioVersion();

            CfanModule latest_available = null;

            try
            {
                latest_available = registry.LatestAvailable(mod.identifier, current_ksp_version);
                if (latest_available != null)
                    latest_version = latest_available.modVersion;
            }
            catch (ModuleNotFoundKraken)
            {
                latest_version = installed_version;
            }

            InstalledVersion = installed_version != null ? installed_version.ToString() : "-";

            // Let's try to find the compatibility for this mod. If it's not in the registry at
            // all (because it's a DarkKAN mod) then this might fail.

            CfanModule latest_available_for_any_ksp = null;

            try
            {
                latest_available_for_any_ksp = registry.LatestAvailable(mod.identifier, null);
            }
            catch
            {
                // If we can't find the mod in the CKAN, but we've a CkanModule installed, then
                // use that.
                if (IsCKAN)
                    latest_available_for_any_ksp = (CfanModule) mod;
                
            }

            var showInfoFrom = latest_available ?? latest_available_for_any_ksp;

            // If there's known information for this mod in any form, calculate the highest compatible
            // KSP.
            if (showInfoFrom != null)
            {
                string minVersion = showInfoFrom.getMinFactorioVersion()?.ToString();
                string maxVersion = showInfoFrom.HighestCompatibleKSP()?.ToString();
                if (maxVersion != null && ModVersion.isMaxWithTheSameMinor(new ModVersion(maxVersion)))
                {
                    maxVersion = maxVersion.Replace(int.MaxValue.ToString(), "x");
                }

                if (minVersion != null && maxVersion != null)
                {
                    KSPCompatibility = minVersion.ToString() + " - " + maxVersion.ToString();
                }
                else if (minVersion != null)
                {
                    KSPCompatibility = " >= " + minVersion.ToString();
                }
                else if (maxVersion != null)
                {
                    KSPCompatibility = " <= " + maxVersion.ToString();
                }
                else
                {
                    KSPCompatibility = "any";
                }
                KSPCompatibilityLong = KSPCompatibility;

                // If the mod we have installed is *not* the mod we have installed, or we don't know
                // what we have installed, indicate that an upgrade would be needed.
                if (installed_version == null || !showInfoFrom.modVersion.Equals(installed_version))
                {
                    KSPCompatibilityLong = string.Format("{0} (using mod version {1})",
                        KSPCompatibility, showInfoFrom.modVersion);
                }
            }
            else
            {
                // No idea what this mod is, sorry!
                KSPCompatibility = KSPCompatibilityLong = "unknown";
            }

            if (latest_version != null)
            {
                LatestVersion = latest_version.ToString();
            }
            else if (latest_available_for_any_ksp != null)
            {
                LatestVersion = latest_available_for_any_ksp.modVersion.ToString();
            }
            else
            {
                LatestVersion = "-";
            }

            KSPversion = ksp_version != null ? ksp_version.ToString() : "-";

            Abstract = mod.@abstract;
            
            // If we have homepage provided use that, otherwise use the spacedock page or the github repo so that users have somewhere to get more info than just the abstract.

            Homepage = "N/A";
            if (!string.IsNullOrEmpty(mod.homepage))
                Homepage = mod.homepage;

            Identifier = mod.identifier;

            if (mod.download_size == 0)
                DownloadSize = "N/A";
            else if (mod.download_size / 1024.0 < 1)
                DownloadSize = "1<KB";
            else
                DownloadSize = mod.download_size / 1024+"";
            
            Abbrevation = new string(mod.title.Split(' ').
                Where(s => s.Length > 0).Select(s => s[0]).ToArray());

            if (Main.Instance != null)
                IsCached = Main.Instance.CurrentInstance.Cache.IsMaybeCachedZip(mod.download);
        }

        public CfanModule ToCkanModule()
        {
            if (!IsCKAN) throw new InvalidCastException("Method can not be called unless IsCKAN");
            var mod = Mod as CfanModule;
            return mod;
        }

        public CfanModule ToModule()
        {
            return Mod;
        }

        public KeyValuePair<GUIMod, GUIModChangeType>? GetRequestedChange()
        {
            if (IsInstalled ^ IsInstallChecked)
            {
                var change_type = IsInstalled ? GUIModChangeType.Remove : GUIModChangeType.Install;
                return new KeyValuePair<GUIMod, GUIModChangeType>(this, change_type);
            }
            if (IsInstalled && (IsInstallChecked && HasUpdate && IsUpgradeChecked))
            {
                return new KeyValuePair<GUIMod, GUIModChangeType>(this, GUIModChangeType.Update);
            }
            return null;
        }

        public static implicit operator CfanModule(GUIMod mod)
        {
            return mod.ToModule();
        }

        public void SetUpgradeChecked(DataGridViewRow row, bool? set_value_to = null)
        {
            //Contract.Requires<ArgumentException>(row.Cells[1] is DataGridViewCheckBoxCell);
            var update_cell = row.Cells[1] as DataGridViewCheckBoxCell;
            var old_value = (bool) update_cell.Value;

            bool value = set_value_to ?? old_value;
            IsUpgradeChecked = value;
            if (old_value != value) update_cell.Value = value;
        }

        public void SetInstallChecked(DataGridViewRow row, bool? set_value_to = null)
        {
            //Contract.Requires<ArgumentException>(row.Cells[0] is DataGridViewCheckBoxCell);
            var install_cell = row.Cells[0] as DataGridViewCheckBoxCell;
            bool changeTo = set_value_to != null ? (bool)set_value_to : (bool)install_cell.Value;
            //Need to do this check here to prevent an infinite loop
            //which is at least happening on Linux
            //TODO: Elimate the cause
            if (changeTo != IsInstallChecked)
            {
                IsInstallChecked = changeTo;
                install_cell.Value = IsInstallChecked;
            }
        }


        private bool Equals(GUIMod other)
        {
            return Equals(Identifier, other.Identifier);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((GUIMod) obj);
        }

        public override int GetHashCode()
        {
            return (Name != null ? Name.GetHashCode() : 0);
        }
    }
}
