﻿using System;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using log4net;
using Newtonsoft.Json;
using CKAN.Types;
using System.Linq;
using CKAN.Factorio.Version;
using Newtonsoft.Json.Linq;

namespace CKAN
{

    /// <summary>
    /// CKAN client auto-updating routines. This works in conjunction with the
    /// auto-update helper to allow users to upgrade.
    /// </summary>
    public class AutoUpdate
    {

        private readonly ILog log = LogManager.GetLogger(typeof(AutoUpdate));

        private readonly Uri latestCKANReleaseApiUrl = new Uri("https://api.github.com/repos/trakos/CFAN/releases/latest");

        private Tuple<Uri, long> fetchedUpdaterUrl;
        private Tuple<Uri, long> fetchedCkanUrl;

        public CFANVersion LatestVersion { get; private set; }
        public string ReleaseNotes { get; private set; }

        private static AutoUpdate instance;

        public static AutoUpdate Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new AutoUpdate();
                }
                return instance;
            }
            private set { }
        }

        // This is private so we can enforce our class being a singleton.
        private AutoUpdate() { }

        public static void ClearCache()
        {
            instance = new AutoUpdate();
        }

        /// <summary>
        /// Our metadata is considered fetched if we have a latest version, release notes,
        /// and download URLs for the ckan executable and helper.
        /// </summary>
        public bool IsFetched()
        {
            return LatestVersion != null && fetchedCkanUrl != null && ReleaseNotes != null;
        }

        /// <summary>
        /// Fetches all the latest release info, populating our attributes in
        /// the process.
        /// </summary>
        public void FetchLatestReleaseInfo()
        {
            var response = MakeRequest(latestCKANReleaseApiUrl);

            try
            {
                fetchedUpdaterUrl = RetrieveUrl(response, "cfan_updater.exe");
                fetchedCkanUrl = RetrieveUrl(response, "cfan.exe");
            }
            catch (Kraken)
            {
                LatestVersion = new CFANVersion(Meta.Version(), "");
                return;
            }

            string body = (string)response["body"];

            ReleaseNotes = ExtractReleaseNotes(body);
            LatestVersion = new CFANVersion((string)response["tag_name"], (string)response["name"]);
        }

        /// <summary>
        /// Extracts release notes from the body of text provided by the github API.
        /// By default this is everything after the first three dashes on a line by
        /// itself, but as a fallback we'll use the whole body if not found.
        /// </summary>
        /// <returns>The release notes.</returns>
        public string ExtractReleaseNotes(string releaseBody)
        {
            string divider = "\r\n---\r\n";
            string[] notesArray = releaseBody.Split(new string[] { divider }, StringSplitOptions.None);

            if (notesArray.Length > 1)
            {
                // Return everything after the first divider, re-joining if necessary.
                return string.Join(divider, notesArray.Skip(1));
            }

            return notesArray[0];
        }

        /// <summary>
        /// Downloads the new ckan.exe version, as well as the updater helper,
        /// and then launches the helper allowing us to upgrade.
        /// </summary>
        /// <param name="launchCKANAfterUpdate">If set to <c>true</c> launch CKAN after update.</param>
        public void StartUpdateProcess(bool launchCKANAfterUpdate, IUser user = null)
        {
            if (!IsFetched())
            {
                throw new Kraken("We have not fetched the release info yet. Can't update.");
            }

            var pid = Process.GetCurrentProcess().Id;

            // download updater app and new cfan.exe
            string updaterFilename = System.IO.Path.GetTempPath() + Guid.NewGuid().ToString() + ".exe";
            string ckanFilename = System.IO.Path.GetTempPath() + Guid.NewGuid().ToString() + ".exe";
            Net.DownloadWithProgress(new[]{
                new Net.DownloadTarget(fetchedUpdaterUrl.Item1, updaterFilename, fetchedUpdaterUrl.Item2),
                new Net.DownloadTarget(fetchedCkanUrl.Item1, ckanFilename, fetchedCkanUrl.Item2),
            }, user);

            // run updater
            SetExecutable(updaterFilename);
            var path = Assembly.GetEntryAssembly().Location;
            Process.Start(new ProcessStartInfo
            {
                Verb = "runas",
                FileName = updaterFilename,
                Arguments = $@"{pid} ""{path}"" ""{ckanFilename}"" {(launchCKANAfterUpdate ? "launch" : "nolaunch")}",
                UseShellExecute = false
            });

            // exit this ckan instance
            Environment.Exit(0);
        }

        /// <summary>
        /// Extracts the first downloadable asset (either the ckan.exe or its updater)
        /// from the provided github API response
        /// </summary>
        /// <returns>The URL to the downloadable asset.</returns>
        internal Tuple<Uri, long> RetrieveUrl(JObject response, string executableName)
        {
            var asset = response["assets"].Where(a => (string) a["name"] == executableName);
            if (!asset.Any())
            {
                throw new Kraken("The latest release does not contain " + executableName + " (yet?).");
            }
            var firstAsset = asset.First();
            return new Tuple<Uri, long>(new Uri((string)firstAsset["browser_download_url"]), (long)firstAsset["size"]);
        }

        /// <summary>
        /// Fetches the URL provided, and de-serialises the returned JSON
        /// data structure into a dynamic object.
        /// 
        /// May throw an exception (especially a WebExeption) on failure.
        /// </summary>
        /// <returns>A dynamic object representing the JSON we fetched.</returns>
        internal JObject MakeRequest(Uri url)
        {
            var web = new WebClient();
            web.Headers.Add("user-agent", Net.UserAgentString);

            try
            {
                var result = web.DownloadString(url);
                return JObject.Parse(result);
            }
            catch (WebException webEx)
            {
                log.ErrorFormat("WebException while accessing {0}: {1}", url, webEx);
                throw;
            }
        }

        public static void SetExecutable(string fileName)
        {
            // mark as executable if on Linux or Mac
            if (Platform.IsUnix || Platform.IsMac)
            {
                // TODO: It would be really lovely (and safer!) to use the native system
                // call here: http://docs.go-mono.com/index.aspx?link=M:Mono.Unix.Native.Syscall.chmod

                string command = string.Format("+x \"{0}\"", fileName);

                ProcessStartInfo permsinfo = new ProcessStartInfo("chmod", command);
                permsinfo.UseShellExecute = false;
                Process permsprocess = Process.Start(permsinfo);
                permsprocess.WaitForExit();
            }
        }
    }
}
