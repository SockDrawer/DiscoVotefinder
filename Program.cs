using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DiscoVotefinder
{
	public static class MainClass
	{
		public class Player
		{
			public string Name = "";
			public DateTime LastPost = DateTime.MinValue;
			public Vote CurrentVote;
			public List<Vote> VotesAgainst = new List<Vote>();
			public int Postcount;

			public int Votecount { get { return VotesAgainst.Count((v) => v.UnvotePostNum == 0); } }

			public bool MinusOne { get { return Math.Ceiling((MainClass.Players.Count() + 0.5) / 2.0) - Votecount == 1; } }

			public bool Hammered { get { return Math.Ceiling((MainClass.Players.Count() + 0.5) / 2.0) - Votecount <= 0; } }

			public override string ToString()
			{
				if(CurrentVote != null)
					return string.Format("[Player: {1}({0}) -> {2}]", Votecount, Name, CurrentVote.Target);
				return string.Format("[Player: {1}({0})]", Votecount, Name);
			}
		}

		public class Vote
		{
			public string Actor;
			public string Target;
			public long VotePostNum;
			public long UnvotePostNum;
		}

		public static string Base;

		public static Dictionary<string,Player> Players = new Dictionary<string, Player>();

		public static void Main(string[] args)
		{
			JObject config = JObject.Parse(File.ReadAllText("Game.json"));
			Base = (string) config["auth"]["url"];
			int topic = (int) config["auth"]["thread"];
			int skip = (int) config["state"]["startpost"];
			foreach(string player in config["state"]["players"]) {
				Player p = new Player() { Name = player };
				Players[player.ToLower()] = p;
			}

			foreach(JObject post in GetPosts(topic)) {
				if((int) post["post_number"] < skip)
					continue;
				var actor = ((string) post["username"]).ToLower();
				if(Players.ContainsKey(actor)) {
					Players[actor].LastPost = DateTime.Parse((string) post["created_at"]);
					Players[actor].Postcount++;
					ProcessActions(post, ProcessAction);
				}
			}

			var Quiets = "<table><tr><th>Player</th><th>Posts</th></tr>" + String.Join("",
				             Players.Values.OrderByDescending(p => p.Postcount).Select(p => string.Format("<tr><td>{0}</td><td>{1}</td>",
					             p.Name, p.Postcount))) + "</table>";

			var t = new PostTemplate();
			t.Session = new Dictionary<string, object>();
			t.Session["Day"] = (int) config["state"]["day"];
			t.Session["Motd"] = (string) config["state"]["motd"];
			t.Session["Deadline"] = DateTime.Parse((string) config["state"]["deadline"]);
			t.Session["Players"] = Players.Values;
			t.Session["Topic"] = topic;

			t.Initialize();
			System.Console.Write(t.TransformText());

			System.Diagnostics.Debugger.Break();
		}

		public static Regex QuoteStripper = new Regex("<aside[^>]+>(?>(?!</?aside).|<aside[^>]+>(?<Depth>)|</aside>(?<-Depth>))*(?(Depth)(?!))</aside>");
		public static Regex VoteFinder;

		public static void ProcessActions(JObject postdata, Action<string, string, string, int> callback)
		{
			if(VoteFinder == null) {
				var vfsb = new StringBuilder("(?:<h2>(?:##)?|##) ?(?<action>vote|unvote|kill)(?:</strong>)?(?: (?:<a class=\"mention\" href=\"[^\"]+\">@(?<target>[^<]+)</a>");
				foreach(var p in Players.Values) {
					vfsb.Append("|(?<target>");
					vfsb.Append(p.Name.ToLower());
					vfsb.Append(")");
				}
				vfsb.Append("))?");
				VoteFinder = new Regex(vfsb.ToString());
			}
			string actor = (string) postdata["username"];
			string post = (string) postdata["cooked"];
			int postnum = (int) postdata["post_number"];
			post = QuoteStripper.Replace(post, "").ToLower();
			var actions = VoteFinder.Matches(post);
			foreach(Match actionmatch in actions) {
				string action = actionmatch.Groups["action"].Value;
				string target = null;
				if(actionmatch.Groups["target"].Success)
					target = actionmatch.Groups["target"].Value;
				callback(actor.ToLower(), action, target, postnum);
			}
		}

		public static void ProcessAction(string actor, string action, string target, int postnum)
		{
			if(Players.ContainsKey(actor) && Players[actor].CurrentVote != null &&
			   ("unvote".Equals(action) || ("vote".Equals(action) && !Players[actor].CurrentVote.Target.ToLower().Equals(target)))) {
				Players[actor].CurrentVote.UnvotePostNum = postnum;
				Players[actor].CurrentVote = null;
			}
			if("vote".Equals(action) && target != null && Players.ContainsKey(actor) && Players.ContainsKey(target) &&
				(Players[actor].CurrentVote == null || !Players[actor].CurrentVote.Target.ToLower().Equals(target))) {
				Vote v = new Vote() {
					Actor = Players[actor].Name,
					Target = Players[target].Name,
					VotePostNum = postnum
				};
				Players[actor].CurrentVote = v;
				Players[target].VotesAgainst.Add(v);
			}
		}

		public static IEnumerable<JObject> GetPosts(long topic)
		{
			string url = string.Format("{0}/t/{1}/posts.json?includeraw=1", Base, topic);
			var streamjson = (url + "&post_ids[]=0").ToJson();
			var stream = (JArray) streamjson["post_stream"]["stream"];
			int i = 0;
			while(i < stream.Count) {
				StringBuilder sb = new StringBuilder(url);
				while(i < stream.Count) {
					sb.Append("&post_ids[]=");
					sb.Append((int) stream[i++]);
					if(i % 100 == 0)
						break;
				}
				var hunk = sb.ToString().ToJson();
				foreach(JObject post in hunk["post_stream"]["posts"])
					yield return post;
			}
			yield break;
		}

		public static JObject ToJson(this string url)
		{
			System.Threading.Thread.Sleep(2000);
			return JObject.Load(new JsonTextReader(new StringReader(new WebClient().DownloadString(url))));
		}
	}
}
