using Google.Cloud.PubSub.V1;
using Microsoft.TeamFoundation.Build.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;

namespace slackClientTesting
{
    public class GoogleCloudPubSub
    {
        private const string tfsserverKey = "https://tfs.slb.com/tfs";
        private const string teamProjectName = @"AllWG";

        private const string psibuildName = "PSI.Main-CI";
        private const string psvbuildName = "PSV.Main";
        private const string ocbuildName = "OC.Main";
        private BuildDefinition psi = null; 
        private BuildDefinition psv = null;
        private BuildDefinition oc = null;
        private TFSBuildService _psimonitor;
        Dictionary<string, string> _buildIds;
        private Timer buildTimerLatestBuild;
        private Timer buildTimerBuildFromLast30Days;
        public GoogleCloudPubSub()
        {
            _psimonitor =  new TFSBuildService(tfsserverKey);
            _buildIds= new Dictionary<string, string>(); ;
        }
        public void InitializeProjects()
        {           
            List<BuildDefinition> buildsDefn = _psimonitor.GetAllbuildsOnServer(teamProjectName);
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
            if(buildTimerLatestBuild == null)
            {
                buildTimerLatestBuild = new System.Timers.Timer(30000);
                buildTimerLatestBuild.Elapsed += BuildTimer_Elapsed;
                buildTimerLatestBuild.Enabled = true;
                postMessagetopubsubLatest();
                postMessagetopubsubMaxCheckIn();
            }
            if(buildTimerBuildFromLast30Days==null)
            {
                buildTimerBuildFromLast30Days = new Timer(120000);
                buildTimerBuildFromLast30Days.Elapsed += BuildTimerBuildFromLast30Days_Elapsed;
                buildTimerBuildFromLast30Days.Enabled = true;
                postMessagetopubsubLast30Days();
            }
        }

        private void BuildTimerBuildFromLast30Days_Elapsed(object sender, ElapsedEventArgs e)
        {
            postMessagetopubsubLast30Days();
        }
        public string GetTFSURLToOpenBuildResults(Uri buildURi)
        {

            //string tfsUrlToReturn = tfsserverKey + "//WG//" + teamProjectName+ "//_build/xaml?buildId=";
            //return tfsUrlToReturn;
            return _psimonitor.GetUrlForBuildResulst(buildURi).ToString();
        }
        private void postMessagetopubsubLatest()
        {
            string latestbuildMessage = GetLatestBuildStatusForGoogleCloud();          
            string projectId = $"psipubsubproject";
            string subscriptionId1 = "MyLatestPSISubscription";
            string latesttopicID = "MyLatestPSIStatus";          
            postbuildMessageUsingPublisherSubscriberOfGoogle(latestbuildMessage,
                projectId, latesttopicID, subscriptionId1);
        }
        private void postMessagetopubsubLast30Days()
        {            
            string message = GetUnsuccessfulBuildStatusForGoogleCloud();
            string projectId = $"psipubsubproject";
            string topicId = "MyPSIBuildStatus";
            string subscriptionId = "MyPSISubscription";
           
            postbuildMessageUsingPublisherSubscriberOfGoogle(message,
                projectId, topicId, subscriptionId);
            
        }
        private void postMessagetopubsubMaxCheckIn()
        {
            Dictionary<string, int> maxcheckinPSI = _psimonitor.GetMaxCheckIn("/Prestack/Main/PSI/src/");
            string psimessage = _psimonitor.ConvertDictionaryToString(maxcheckinPSI);
            string projectId = $"psipubsubproject";
            string topicId = "MaxCheckinPSI";
            string subscriptionId = "MaxCheckinPSISubscription";

            postbuildMessageUsingPublisherSubscriberOfGoogle(psimessage,
                projectId, topicId, subscriptionId);
            Dictionary<string, int> maxcheckinPSV = _psimonitor.GetMaxCheckIn("/Prestack/Main/PSV/src/");
            string psvmessage = _psimonitor.ConvertDictionaryToString(maxcheckinPSV);
            
            string topicIdpsv = "MaxCheckinPSV";
            string subscriptionIdpsv = "MyPSVSubscription";

            postbuildMessageUsingPublisherSubscriberOfGoogle(psvmessage,
                projectId, topicIdpsv, subscriptionIdpsv);

            Dictionary<string, int> maxcheckinOC = _psimonitor.GetMaxCheckIn("/OC/Main/src/");
            string ocmessage = _psimonitor.ConvertDictionaryToString(maxcheckinOC);

            string topicIdoc = "MaxCheckinOC";
            string subscriptionIdOC = "MyOCSubscription";

            postbuildMessageUsingPublisherSubscriberOfGoogle(ocmessage,
                projectId, topicIdoc, subscriptionIdOC);

        }
        private void BuildTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            postMessagetopubsubLatest();
            postMessagetopubsubMaxCheckIn();
        }

        public string GetUnsuccessfulBuildStatusForGoogleCloud()
        {
            
            StringBuilder bd = new StringBuilder();
            BuildStatus psibuildStatus = BuildStatus.None;
            BuildStatus psvbuildStatus = BuildStatus.None;
            BuildStatus ocbuildStatus = BuildStatus.None;
            if (psi != null)
            {
                IBuildDetail[] bd1 = _psimonitor.GetLatestBuild(new BuildDefinition(psi.TeamProject, psi.DefinitionName));
                if (bd1 != null && bd1.Length > 0)
                {
                    for (int i = 0; i < bd1.Length; i++)
                    {
                        psibuildStatus = _psimonitor.GetLatestBuildStatusForProject(bd1[i]);
                        if (psibuildStatus == BuildStatus.PartiallySucceeded || psibuildStatus == BuildStatus.Failed)
                        {
                            bd.AppendLine(GetAppendedString(bd1[i], psibuildStatus));
                        }
                    }
                }
               
            }
            if (psv != null)
            {
                IBuildDetail[] bd1 = _psimonitor.GetLatestBuild(new BuildDefinition(psv.TeamProject, psv.DefinitionName));
                if (bd1 != null && bd1.Length > 0)
                {
                    for (int i = 0; i < bd1.Length; i++)
                    {

                        psvbuildStatus = _psimonitor.GetLatestBuildStatusForProject(bd1[i]);
                        if (psvbuildStatus == BuildStatus.PartiallySucceeded || psvbuildStatus == BuildStatus.Failed)
                        {
                            bd.AppendLine(GetAppendedString(bd1[i], psvbuildStatus));
                        }
                    }
                }
                
            }
            if (oc != null)
            {
                IBuildDetail[] bd1 = _psimonitor.GetLatestBuild(new BuildDefinition(oc.TeamProject, oc.DefinitionName));
                if (bd1!= null && bd1.Length > 0)
                {
                    for (int i = 0; i < bd1.Length; i++)
                    {
                        ocbuildStatus = _psimonitor.GetLatestBuildStatusForProject(bd1[i]);
                        if (ocbuildStatus == BuildStatus.PartiallySucceeded || ocbuildStatus == BuildStatus.Failed)
                        {
                            bd.AppendLine(GetAppendedString(bd1[i], ocbuildStatus));
                        }
                    }
                }
            }         
           
            
            return bd.ToString();
        }

        public string GetLatestBuildStatusForGoogleCloud()
        {

            StringBuilder bd = new StringBuilder();
            BuildStatus psibuildStatus = BuildStatus.None;
            BuildStatus psvbuildStatus = BuildStatus.None;
            BuildStatus ocbuildStatus = BuildStatus.None;
            if (psi != null)
            {
                IBuildDetail[] bd1 = _psimonitor.GetBuildFromLast2days(new BuildDefinition(psi.TeamProject, psi.DefinitionName));
                if (bd1 != null && bd1.Length > 0)
                {
                   
                        psibuildStatus = _psimonitor.GetLatestBuildStatusForProject(bd1[0]);                        
                        bd.AppendLine(GetAppendedString(bd1[0], psibuildStatus));
                        
                    
                }

            }
            if (psv != null)
            {
                IBuildDetail[] bd1 = _psimonitor.GetBuildFromLast2days(new BuildDefinition(psv.TeamProject, psv.DefinitionName));
                if (bd1 != null && bd1.Length > 0)
                {
                  

                        psvbuildStatus = _psimonitor.GetLatestBuildStatusForProject(bd1[0]);
                         bd.AppendLine(GetAppendedString(bd1[0], psvbuildStatus));
                      
                    
                }

            }
            if (oc != null)
            {
                IBuildDetail[] bd1 = _psimonitor.GetBuildFromLast2days(new BuildDefinition(oc.TeamProject, oc.DefinitionName));
                if (bd1 != null && bd1.Length > 0)
                {
                    
                        ocbuildStatus = _psimonitor.GetLatestBuildStatusForProject(bd1[0]);
                        
                        bd.AppendLine(GetAppendedString(bd1[0], ocbuildStatus));
                        
                    
                }
            }


            return bd.ToString();
        }

        private string GetAppendedString(IBuildDetail bd1, BuildStatus buildStatus)
        {

            StringBuilder bd = new StringBuilder();
            if (!_buildIds.ContainsKey(bd1.BuildNumber))
            {
                //build Name
                bd.Append(bd1.BuildDefinition.Name);
                bd.Append(";");
                //build Number
                bd.Append(bd1.BuildNumber);
                bd.Append(";");
                //status
                bd.Append(buildStatus.ToString());
                bd.Append(";");
                string changesetversion = null;
                string buildRequestedBy = null;
                string compilationStatus = null;
                string testStatus = null;
                string time = null;
                _psimonitor.GetDetailedStatus(bd1, out changesetversion, out buildRequestedBy,
                                                out compilationStatus, out testStatus, out time);
                //compile status
                bd.Append(compilationStatus);
                bd.Append(";");
                //test status
                bd.Append(testStatus);
                bd.Append(";");
                //changeset version
                bd.Append(changesetversion);
                bd.Append(";");
                //requested by
                bd.Append(buildRequestedBy);
                bd.Append(";");
                //Date time
                bd.Append(time);
                bd.Append(";");
                Uri url = bd1.Uri;
                string buildURL = GetTFSURLToOpenBuildResults(url); ;
                bd.Append(buildURL);
                bd.Append(";");
                _buildIds.Add(bd1.BuildNumber, bd.ToString());
            }
            else
            {
                bd.Append(_buildIds[bd1.BuildNumber]);
            }
                       

            return bd.ToString();
        }

    

        public void postbuildMessageUsingPublisherSubscriberOfGoogle(string messageToSend,string projectId,
            string topicId, string subscriptionId)
        {
            
            // First create a topic.
            PublisherClient publisher = PublisherClient.Create();
            TopicName topicName = new TopicName(projectId, topicId);
            try
            {
                publisher.CreateTopic(topicName);
            }
            catch (Grpc.Core.RpcException e1)
            {

            }

            // Subscribe to the topic.
            SubscriberClient subscriber = SubscriberClient.Create();
            SubscriptionName subscriptionName = new SubscriptionName(projectId, subscriptionId);
            try
            {
                subscriber.CreateSubscription(subscriptionName, topicName, pushConfig: null, ackDeadlineSeconds: 60);
            }

            catch (Grpc.Core.RpcException e2)
            {

            }
            // Publish a message to the topic.
            PubsubMessage message = new PubsubMessage
            {
                // The data is any arbitrary ByteString. Here, we're using text.
                Data = Google.Protobuf.ByteString.CopyFromUtf8(messageToSend),
                // The attributes provide metadata in a string-to-string dictionary.
                Attributes =
                {
                    { "description", "Simple text message" }
                }
            };
            publisher.Publish(topicName, new[] { message });
        }
    }
}
