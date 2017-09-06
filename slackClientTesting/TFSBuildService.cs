using System;
using System.Collections.Generic;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.TestManagement.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using System.Text;

namespace slackClientTesting
{
    public class BuildDefinitionSpec : IBuildDefinitionSpec
    {
       public BuildDefinitionSpec(string tp,string defntName)
        {
            teamProject = tp;
            name = defntName;
        }
        private ContinuousIntegrationType ci;
        public ContinuousIntegrationType ContinuousIntegrationType
        {
            get
            {
                return ci;
            }

            set
            {
                ci = value;
            }
        }
        private string path = null;
        public string FullPath
        {
            get
            {
                return path;
            }
        }
        private string name = null;
        public string Name
        {
            get
            {
                return name;
            }

            set
            {
                name=value;
            }
        }
        private QueryOptions options;
        public QueryOptions Options
        {
            get
            {
                return options;
            }

            set
            {
                options = value;
            }
        }
        private List<string> propertyNameFilters;
        public List<string> PropertyNameFilters
        {
            get
            {
                return propertyNameFilters;
            }

            set
            {
                propertyNameFilters = value;
            }
        }
        private string teamProject;
        public string TeamProject
        {
            get
            {
                return teamProject;
            }
        }
        private DefinitionTriggerType triggerType;
        public DefinitionTriggerType TriggerType
        {
            get
            {
                return triggerType;
            }

            set
            {
                triggerType=value;
            }
        }
    }
    public class BuildDefinition
    {
        public BuildDefinition(string tp, string name)
        {
            this.TeamProject = tp;
            DefinitionName = name;
        }

        public string TeamProject { get; set; }

        public string DefinitionName { get; set; }
    }
    /// <summary>
    /// This class is used to get all the build details for builds stored in the TFS version control system
    /// </summary>
    public class TFSBuildService
    {
        private readonly IBuildServer _buildServer;
        private readonly VersionControlServer _versionControlServer;
        private readonly ITestManagementService _testManagementService;
        private readonly TswaClientHyperlinkService _httpServiceTfs = null;
        private ITestManagementTeamProject _testTeamProject = null;
        private string pathofTeamProject = null;
        /// <summary>
        /// The constructor sets up build service based on server key
        /// </summary>
        /// <param name="tfsserverKey"></param>
        public TFSBuildService(string tfsserverKey)
        {
            var tfs = new TfsTeamProjectCollection(TfsTeamProjectCollection.GetFullyQualifiedUriForName(tfsserverKey));
            this._buildServer = tfs.GetService<IBuildServer>();
            this._versionControlServer = tfs.GetService<VersionControlServer>();
            this._testManagementService = tfs.GetService<ITestManagementService>();
            _httpServiceTfs = tfs.GetService<TswaClientHyperlinkService>();
            var tp = _versionControlServer.GetTeamProject(@"AllWG");
            pathofTeamProject = tp.ServerItem;
           
        }
        /// <summary>
        /// This returns all the builds definitions related to team project name
        /// </summary>
        /// <param name="teamProjectName"></param>
        /// <returns></returns>
        public List<BuildDefinition> GetAllbuildsOnServer(string teamProjectName)
        {
            var buildList = new List<BuildDefinition>();
            try
            {
                var teamProjects = this._versionControlServer.GetAllTeamProjects(true);
                IBuildDefinition[] projectBuilds = this._buildServer.QueryBuildDefinitions(teamProjectName);
                foreach (var definition in projectBuilds)
                {
                    buildList.Add(new BuildDefinition(definition.TeamProject, definition.Name));
                }

            }
            catch (Exception)
            {
                return null;
            }
            return buildList;
        }
        
        /// <summary>
        /// This method gets all the builds for specified time period
        /// </summary>
        /// <param name="bd"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <returns></returns>
        public IBuildDetail[] GetBuildsForSpecifiedTimePeriod(BuildDefinition bd, DateTime startTime, DateTime endTime)
        {
            try
            {
                //Specify query
                var spec = _buildServer.CreateBuildDetailSpec(bd.TeamProject, bd.DefinitionName);
                spec.InformationTypes = null; // for speed improvement
                spec.MinFinishTime = startTime; //to get only builds of last 3 weeks
                spec.MaxFinishTime = endTime;
                spec.QueryOrder = BuildQueryOrder.FinishTimeDescending; //get the latest build only
                spec.QueryOptions = QueryOptions.All;
                return _buildServer.QueryBuilds(spec).Builds;
            }
            catch (Exception)
            {
                return null;
            }
        }
        /// <summary>
        /// This convenience method added to get BuildDetail for a particular name
        /// </summary>
        /// <param name="bds"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        internal IBuildDetail GetBuildFromName(IBuildDetail[] bds, string name)
        {
            foreach (var definition in bds)
            {
                if (definition.LabelName.Equals(name))
                    return definition;
            }
            return null;
        }
        /// <summary>
        /// Get uri for build
        /// </summary>
        /// <param name="buildURi"></param>
        /// <returns></returns>
        public Uri GetUrlForBuildResulst(Uri buildURi)
        {
            return _httpServiceTfs.GetViewBuildDetailsUrl(buildURi);
        }
        /// <summary>
        /// Get test result for particular build detail and team project
        /// </summary>
        /// <param name="bd"></param>
        /// <param name="teamProjectName"></param>
        /// <returns></returns>
        public IEnumerable<ITestRun> GetTestRuns(IBuildDetail bd, string teamProjectName)
        {
            if (_testTeamProject == null)
                _testTeamProject = this._testManagementService.GetTeamProject(teamProjectName);
            var testRuns = _testTeamProject.TestRuns.ByBuild(bd.Uri);
            return testRuns;
        }
        /// <summary>
        /// This returns number of tests passed, failed and total tests given testRuns
        /// </summary>
        /// <param name="testFailed"></param>
        /// <param name="testPassed"></param>
        /// <param name="totalNumberOfTests"></param>
        /// <param name="testRuns"></param>
        public void GetTestResults(out int testFailed,
            out int testPassed,
            out int totalNumberOfTests, IEnumerable<ITestRun> testRuns)
        {
            testFailed = 0;
            testPassed = 0;
            totalNumberOfTests = 0;
            foreach (var testRun in testRuns)
            {
                testFailed += testRun.Statistics.FailedTests;
                testPassed += testRun.Statistics.PassedTests;
                totalNumberOfTests += testRun.Statistics.TotalTests;
            }

        }
        public IBuildDetail[] GetLatestBuild(BuildDefinition bd)
        {
            try
            {
                //Specify query
                var spec = _buildServer.CreateBuildDetailSpec(bd.TeamProject, bd.DefinitionName);
                spec.InformationTypes = null; // for speed improvement
                spec.MinFinishTime = DateTime.Now.AddDays(-30); //to get only builds of last 2 days
                spec.MaxFinishTime = DateTime.Now;
                spec.QueryOrder = BuildQueryOrder.FinishTimeDescending; //get the latest build only
                spec.QueryOptions = QueryOptions.All;
                return _buildServer.QueryBuilds(spec).Builds;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public IBuildDetail[] GetBuildFromLast2days(BuildDefinition bd)
        {
            try
            {
                //Specify query
                var spec = _buildServer.CreateBuildDetailSpec(bd.TeamProject, bd.DefinitionName);
                spec.InformationTypes = null; // for speed improvement
                spec.MinFinishTime = DateTime.Now.AddDays(-7); //to get only builds of last 2 days
                spec.MaxFinishTime = DateTime.Now;
                spec.QueryOrder = BuildQueryOrder.FinishTimeDescending; //get the latest build only
                spec.QueryOptions = QueryOptions.All;
                return _buildServer.QueryBuilds(spec).Builds;
            }
            catch (Exception)
            {
                return null;
            }
        }
        public BuildStatus GetLatestBuildStatusForProject(IBuildDetail bd)
        {
            BuildStatus status = BuildStatus.None;

            if (bd != null)
            {
                status = bd.Status;
            }

            return status;
        }
        public void GetDetailedStatus(IBuildDetail bd, out string changesetversion,
          out string buildRequestedBy, 
          out string compilationStatus, 
          out string testStatus, out string date)
        {
            changesetversion = null;
            buildRequestedBy = null;
            compilationStatus = null;
            testStatus = null;
            date = null;
            if (bd != null)
            {
                changesetversion = bd.SourceGetVersion;
                buildRequestedBy = bd.RequestedBy;
                compilationStatus = bd.CompilationStatus.ToString();
                testStatus = bd.TestStatus.ToString();
                date = bd.FinishTime.ToString();                
            }
        }

        public Dictionary<string, int> GetMaxCheckIn(string sourceControlPath)
        {
           
            Dictionary<string, int> usersWithMaxCheckIn = 
                new Dictionary<string, int>();            
            var changesets = _versionControlServer
                .QueryHistory(pathofTeamProject 
                + sourceControlPath,RecursionType.Full,50);
            if (changesets != null )
            {
                foreach (var c in changesets)
                {
                    Changeset changeSet = _versionControlServer.GetChangeset(c.ChangesetId);
                    if (!usersWithMaxCheckIn.ContainsKey(changeSet.OwnerDisplayName))
                    {
                        usersWithMaxCheckIn.Add(changeSet.OwnerDisplayName, 1);
                    }
                    else
                    {
                        int currentCountOfCheckins = usersWithMaxCheckIn[changeSet.OwnerDisplayName];
                        usersWithMaxCheckIn[changeSet.OwnerDisplayName] = currentCountOfCheckins + 1;
                    }
                }
            }
            return usersWithMaxCheckIn;
        }

        public string ConvertDictionaryToString(Dictionary<string, int>  dic)
        {
            StringBuilder sb1 = new StringBuilder();
            foreach(string key in dic.Keys)
            {
                sb1.Append(key);
                sb1.Append(":");
                sb1.Append(dic[key].ToString());
                sb1.Append(";");
            }
            return sb1.ToString();

        }

    }
   

    
}
