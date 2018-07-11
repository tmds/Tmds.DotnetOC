using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Tmds.DotnetOC
{
    enum VersionCheckResult
    {
        NewAvailable,
        AlreadyUpToDate,
        UnknownVersions
    }

    class InstallOperations
    {
        private readonly IOpenShift _openshift;
        private readonly IS2iRepo _s2iRepo;
        private string[] _installed;
        private string _installedNamespace;
        private string[] _latestCommunity;
        private string[] _latestRH;
        private JObject _s2iImageStreamsCommunity;
        private JObject _s2iImageStreamsRH;

        public InstallOperations(IOpenShift openshift, IS2iRepo s2iRepo)
        {
            _openshift = openshift;
            _s2iRepo = s2iRepo;
        }

        public string[] GetInstalledVersions(string ocNamespace)
        {
            if (_installed != null && _installedNamespace == ocNamespace)
            {
                return _installed;
            }
            ImageStreamTag[] namespaceStreamTags = _openshift.GetImageTagVersions("dotnet", ocNamespace: ocNamespace);
            string[] namespaceVersions = namespaceStreamTags.Select(t => t.Version).ToArray();
            VersionStringSorter.Sort(namespaceVersions);
            _installed = namespaceVersions;
            _installedNamespace = ocNamespace;
            return namespaceVersions;
        }

        public string[] GetLatestVersions(bool community)
        {
            if (community)
            {
                if (_latestCommunity != null)
                {
                    return _latestCommunity;
                }
            }
            else
            {
                if (_latestRH != null)
                {
                    return _latestRH;
                }
            }
            JObject s2iImageStreams = _s2iRepo.GetImageStreams(community);
            string[] s2iVersions = ImageStreamListParser.GetTags(s2iImageStreams, "dotnet");
            VersionStringSorter.Sort(s2iVersions);
            if (community)
            {
                _latestCommunity = s2iVersions;
                _s2iImageStreamsCommunity = s2iImageStreams;
            }
            else
            {
                _latestRH = s2iVersions;
                _s2iImageStreamsRH = s2iImageStreams;
            }
            return s2iVersions;
        }

        public VersionCheckResult CompareVersions(bool community, string ocNamespace)
        {
            string[] installed = GetInstalledVersions(ocNamespace);
            string[] latest = GetLatestVersions(community);
            IEnumerable<string> newVersions = latest.Except(installed);
            IEnumerable<string> removedVersions = installed.Except(latest);
            if (removedVersions.Any())
            {
                return VersionCheckResult.UnknownVersions;
            }
            if (!newVersions.Any())
            {
                return VersionCheckResult.AlreadyUpToDate;
            }
            return VersionCheckResult.NewAvailable;
        }

        public void UpdateToLatest(bool community, string ocNamespace)
        {
            GetLatestVersions(community);
            JObject imageStreams = community ? _s2iImageStreamsCommunity : _s2iImageStreamsRH;
            if (_installed.Length != 0)
            {
                _openshift.Replace(imageStreams, ocNamespace);
            }
            else
            {
                _openshift.Create(imageStreams, ocNamespace);
            }
        }
    }
}