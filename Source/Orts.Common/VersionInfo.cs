// COPYRIGHT 2013 by the Open Rails project.
// 
// This file is part of Open Rails.
// 
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Open Rails is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Open Rails.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace ORTS.Common
{
    /// <summary>
    /// Static class which provides version and build information about the whole game.
    /// </summary>
    public static class VersionInfo
    {
        static readonly string ApplicationPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
        // GetRevision() must come before GetVersion()
        /// <summary>Revision number, e.g. Release: "1648",       experimental: "1649",   local: ""</summary>
        public static readonly string Revision = GetRevision("Revision.txt");
        /// <summary>Full version number, e.g. Release: "0.9.0.1648", experimental: "X.1649", local: ""</summary>
        public static readonly string Version = GetVersion("Version.txt");
        /// <summary>Full build number, e.g. "0.0.5223.24629 (2014-04-20 13:40:58Z)"</summary>
        public static readonly string Build = GetBuild("ORTS.Common.dll", "OpenRails.exe", "Menu.exe", "RunActivity.exe");
        /// <summary>Version, but if "", returns Build</summary>
        public static readonly string VersionOrBuild = GetVersionOrBuild();

        static string GetRevision(string fileName)
        {
            try
            {
                using (var f = new StreamReader(Path.Combine(ApplicationPath, fileName)))
                {
                    var revision = f.ReadLine().Trim();
                    if (revision.StartsWith("$Revision:") && revision.EndsWith("$"))
                    {
                        if (!revision.Contains(" 000 "))
                            return revision.Substring(10, revision.Length - 11).Trim();
                    }
                    else
                    {
                        return revision;
                    }
                }
            }
            catch
            {
            }
            return "";
        }

        static string GetVersion(string fileName)
        {
            try
            {
                using (var f = new StreamReader(Path.Combine(ApplicationPath, fileName)))
                {
                    var version = f.ReadLine().Trim();
                    if (!String.IsNullOrEmpty(Revision))
                        return version + "-" + Revision;
                }
            }
            catch
            {
            }
            return "";
        }

        static string GetBuild(params string[] fileNames)
        {
            var builds = new Dictionary<TimeSpan, string>();
            foreach (var fileName in fileNames)
            {
                try
                {
                    var version = FileVersionInfo.GetVersionInfo(Path.Combine(ApplicationPath, fileName));
                    builds.Add(new TimeSpan(version.ProductBuildPart, 0, 0, version.ProductPrivatePart * 2), version.ProductVersion);
                }
                catch
                {
                }
            }
            if (builds.Count > 0)
            {
                var datetime = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                var timespan = builds.Keys.OrderBy(ts => ts).Last();
                return String.Format("{0} ({1:u})", builds[timespan], datetime + timespan);
            }
            return "";
        }

        static string GetVersionOrBuild()
        {
            return Version.Length > 0 ? Version : Build;
        }

        /// <summary>
        /// Find whether a requested version and build are valid for this build 
        /// </summary>
        /// <param name="version">version to test again</param>
        /// <param name="build">build to test again</param>
        /// <param name="youngestFailedToResume">youngest build that failed to resume</param>
        /// <returns>true or false when able to determine validity, null otherwise</returns>
        public static bool? GetValidity(string version, string build, int youngestFailedToResume)
        {
            int revision = GetRevisionFromVersion(version);
            int programRevision = 0;
            try  // as Convert.ToInt32() can fail and version may be ""
            {
                programRevision = Convert.ToInt32(VersionInfo.Revision);
            }
            catch { } // ignore errors
            //MessageBox.Show(String.Format("VersionInfo.Build = {0}, build = {1}, version = {2}, youngestFailedToResume = {3}", VersionInfo.Build, build, Version, youngestFailedToResume));
            if (revision != 0)  // compiled remotely by Open Rails
            {
                if (revision == programRevision)
                {
                    return true;
                }
                else
                {
                    if (revision > youngestFailedToResume        // 1. Normal situation
                    || programRevision < youngestFailedToResume) // 2. If an old version of OR is used, then attempt to load Saves
                                                                 //    which would be blocked by the current version of OR
                    {
                        return null;
                    }
                }
            }
            else  // compiled locally
            {
                if (build.EndsWith(VersionInfo.Build))
                {
                    return true;
                }
                else
                {
                    return null;
                }
            }
            return false; // default validity
        }


        /// <summary>
        /// Converts many possible Open Rails versions into a standard Version struct
        /// </summary>
        /// <param name="version">text version to parse</param>
        public static Version ParseVersion(string version)
        {
            // Version numbers which we do parse:
            //  - 0.9.0.1648            --> 0.9
            //  - 1.3.1.4328            --> 1.3.1
            //  - T1.3.1-241-g6ff150c21 --> 1.3.1.241
            //  - X1.3.1-370-g7df5318c2 --> 1.3.1.370
            //  - 1.4                   --> 1.4
            //  - 1.4-rc1               --> 1.4
            //  - T1.4-2-g7db094316     --> 1.4.0.2
            // Version numbers which we do NOT parse:
            //  - U2019.07.25-2200
            //  - U2021.06.25-0406
            //  - X.1648
            //  - X1648

            if (version.StartsWith("T") || version.StartsWith("X")) version = version.Substring(1);

            var versionParts = version.Split('-');
            if (!System.Version.TryParse(versionParts[0], out var parsedVersion)) return new Version();

            var commits = 0;
            if (versionParts.Length > 1) int.TryParse(versionParts[1], out commits);
            // parsedVersion.Build will be -1 if the version only has major and minor, but we need the build number >= 0 here
            return new Version(parsedVersion.Major, parsedVersion.Minor, Math.Max(0, parsedVersion.Build), commits);
        }

        /// <summary>
        /// Find the revision number (e.g. 1648) from the full version (e.g. 0.9.0.1648 or X.1648 or X1648)
        /// </summary>
        /// <param name="version">full version</param>
        public static int GetRevisionFromVersion(string fullVersion)
        {
            var versionParts = fullVersion.Split('.');
            var revision = 0;
            try
            {
                var version = versionParts[versionParts.Length - 1];
                if (version.StartsWith("X"))
                    version = version.Substring(1);
                // Might throw an error if it isn't a number like we expect.
                revision = Convert.ToInt32(version);
            }
            catch { }
            return revision;
        }

        public static long GetVersionLong(Version version)
        {
            long number = 0;
            if (version.Major > 0) number += (long)(version.Major & 0xFFFF) << 48;
            if (version.Minor > 0) number += (long)(version.Minor & 0xFFFF) << 32;
            if (version.Build > 0) number += (version.Build & 0xFFFF) << 16;
            if (version.Revision > 0) number += (version.Revision & 0xFFFF);
            return number;
        }
    }
}
