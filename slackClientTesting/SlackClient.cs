using Newtonsoft.Json;
using System;
using System.Collections.Specialized;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
//https://gist.github.com/jogleasonjr/7121367 source code from
//A simple C# class to post messages to a Slack channel
//Note: This class uses the Newtonsoft Json.NET serializer available via NuGet
public class SlackClient
{
    private readonly Uri _uri;
    private readonly Encoding _encoding = new UTF8Encoding();
    private HttpClient _client;
    public SlackClient(string urlWithAccessToken)
    {
        _uri = new Uri(urlWithAccessToken);
        _client = new HttpClient();
    }    

    //Post a message using a Payload object
    public async Task<HttpResponseMessage> PostMessage(string text, string username = null, string channel = null)
    {
        Payload payload = new Payload()
        {
            Channel = channel,
            Username = username,
            Text = text
        };
        string payloadJson = JsonConvert.SerializeObject(payload);


        NameValueCollection data = new NameValueCollection();
        data["payload"] = payloadJson;

        var response = await _client.PostAsync(_uri, new StringContent(payloadJson, Encoding.UTF8, "application/json"));

        ////The response text is usually "ok"
        //string responseText = _encoding.GetString(response);
        return response;


    }
}

//This class serializes into the Json payload required by Slack Incoming WebHooks
public class Payload
{
    [JsonProperty("channel")]
    public string Channel { get; set; }

    [JsonProperty("username")]
    public string Username { get; set; }

    [JsonProperty("text")]
    public string Text { get; set; }
}