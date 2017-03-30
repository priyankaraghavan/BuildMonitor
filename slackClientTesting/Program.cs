using Microsoft.TeamFoundation.Build.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace slackClientTesting
{
    class Program
    {
        private const string incomingwebhookurl = "https://hooks.slack.com/services/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";
        private const string tfsserverKey = "https://tfs.slb.com/tfs";
        private const string teamProjectName = @"AllWG";
       
        private const string psibuildName = "PSI.Main-CI";
        private const string psvbuildName = "PSV.Main";
        private const string ocbuildName = "OC.Main";
        static void Main(string[] args)
        {

            Task.WaitAll(postaMessagetoSlack());
        }

        private static async Task postaMessagetoSlack()
        {
            TFSBuildService bs = new TFSBuildService(tfsserverKey);
            List<BuildDefinition> buildsDefn;
            BuildDefinition psi = null; ;
            BuildDefinition psv=null;
            BuildDefinition oc = null;
            buildsDefn = bs.GetAllbuildsOnServer(teamProjectName);            
            foreach (var buildDefinition in buildsDefn)
            {
                if (buildDefinition.DefinitionName.Equals(psibuildName))

                {
                    psi = buildDefinition;
                }
                if (buildDefinition.DefinitionName.Equals(psvbuildName))
                {
                    psv = buildDefinition;
                }
                if (buildDefinition.DefinitionName.Equals(ocbuildName))
                {
                    oc = buildDefinition;
                }

            }
            BuildStatus psibuildStatus = BuildStatus.None;
            BuildStatus psvbuildStatus = BuildStatus.None;
            BuildStatus ocbuildStatus = BuildStatus.None;
            if (psi != null)
            {
                IBuildDetail[] bd = bs.GetLatestBuild(new BuildDefinition(psi.TeamProject, psi.DefinitionName));
                if (bd != null && bd.Length > 0)
                {
                    psibuildStatus= bs.GetLatestBuildStatusForProject(bd[0]);
                }
            }
            if (psv != null)
            {
                IBuildDetail[] bd = bs.GetLatestBuild(new BuildDefinition(psv.TeamProject, psv.DefinitionName));
                if (bd != null && bd.Length>0)                {
                    psvbuildStatus = bs.GetLatestBuildStatusForProject(bd[0]);
                }
            }
            if (oc != null)
            {
                IBuildDetail[] bd = bs.GetLatestBuild(new BuildDefinition(oc.TeamProject, oc.DefinitionName));
                if (bd != null && bd.Length >0)
                {
                    ocbuildStatus = bs.GetLatestBuildStatusForProject(bd[0]);
                }
            }

            StringBuilder sb1 = new StringBuilder();
            sb1.Append("Time:" + DateTime.Now);
            sb1.Append("\n");
            sb1.Append("PSI status:");
            sb1.Append(psibuildStatus.ToString());
            sb1.Append("\n");
            sb1.Append("PSV status:");
            sb1.Append(psvbuildStatus.ToString());
            sb1.Append("\n");
            sb1.Append("OC status:");
            sb1.Append(ocbuildStatus.ToString());
            SlackClient client = new SlackClient(incomingwebhookurl);
           
           var response = await client.PostMessage(username: "praghavan",text:sb1.ToString(), channel: "#localtfs");
           var isValid = response.IsSuccessStatusCode ? "valid" : "invalid";
           Console.WriteLine($"Received {isValid} response.");
           
        }
    }
}
