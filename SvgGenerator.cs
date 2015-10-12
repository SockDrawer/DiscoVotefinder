using System;
using System.Text;

namespace DiscoVotefinder
{
	public static class SvgGenerator
	{
		public static double Width = 100.0;
		public static double Height = 12.0;

		public static string OkFg = "#005600";
		public static string OkBg = "#FFFFFF"; // not used
		public static string M1Fg = "#617500";
		public static string M1Bg = "#B6CF3F";
		public static string NgFg = "#AC1717";
		public static string NgBg = "#D74242"; // not used
		public static string Ovk = "#560000";


		public static string GenerateDataUri(int players, int votes){
			var svg = GenerateSvg(players, votes);
			var base64 = System.Convert.ToBase64String(Encoding.UTF8.GetBytes(svg));
			return "data:image/svg+xml;base64," + base64;
		}

		public static string GenerateSvg(int players, int votes){
			var sb = new StringBuilder(String.Format("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{0:0}\" height=\"{1:0}\">", Width, Height));
			double target = Math.Ceiling((players + 0.5) / 2.0);
			if(votes >= target) { //Hammered
				double overkilllimit = players-target;
				double overkill = votes - target;
				sb.Append(string.Format("<rect width=\"100%\" height=\"100%\" fill=\"{0}\" />", NgFg));
				sb.Append(string.Format("<rect x=\"{1:0.00%}\" width=\"{0:0.00%}\" height=\"100%\" fill=\"{2}\" />", 
					overkill / overkilllimit, 1.0 - overkill / overkilllimit, Ovk));
			}
			else if(votes == target - 1) {
				sb.Append(string.Format("<rect width=\"100%\" height=\"100%\" fill=\"{0}\" />", M1Bg));
				sb.Append(string.Format("<rect x=\"{1:0.00%}\" width=\"{0:0.00%}\" height=\"100%\" fill=\"{2}\" />", 
					votes / target, 1.0 - votes / target, M1Fg));

			}
			else {
				//sb.Append(string.Format("<rect width=\"100%\" height=\"100%\" fill=\"{0}\" />", OkBg));
				sb.Append(string.Format("<rect x=\"{1:0.00%}\" width=\"{0:0.00%}\" height=\"100%\" fill=\"{2}\" />", 
					votes / target, 1.0 - votes / target, OkFg));
			}
			sb.Append("</svg>");
			return sb.ToString();
		}
	}
}

