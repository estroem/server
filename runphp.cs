using System;
using System.IO;
using System.Diagnostics;

namespace Server {
	public class PHPHandler {
		public const string exePath = @"php\php-cgi.exe";
	
		public static string Run(string fullPath, Request req, Headers headers, string post) {
			if(!File.Exists(fullPath)) {
				return null;
			}
			
			string filename = Path.GetFileName(fullPath);
			
			ProcessStartInfo info = new ProcessStartInfo();
			
			info.FileName = exePath;
			info.WorkingDirectory = Connection.GetRootPath();
			info.Arguments = "";
			info.UseShellExecute = false;
			info.RedirectStandardInput = true;
			info.RedirectStandardOutput = true;
			info.CreateNoWindow = true;
			
			info.EnvironmentVariables["CONTENT_LENGTH"] = post != null ? post.Length.ToString() : "0";
			info.EnvironmentVariables["CONTENT_TYPE"] = "application/x-www-form-urlencoded";
			info.EnvironmentVariables["DOCUMENT_ROOT"] = Connection.GetRootPath();
			info.EnvironmentVariables["GATEWAY_INTERFACE"] = "CGI/1.1";
			info.EnvironmentVariables["HTTP_ACCEPT"] = headers.Get("accept");
			info.EnvironmentVariables["HTTP_ACCEPT_CHARSET"] = headers.Get("accept-charset");
			info.EnvironmentVariables["HTTP_ACCEPT_ENCODING"] = headers.Get("accept-encoding");
			info.EnvironmentVariables["HTTP_ACCEPT_LANGUAGE"] = headers.Get("accept-language");
			info.EnvironmentVariables["HTTP_CONNECTION"] = headers.Get("connection");
			info.EnvironmentVariables["HTTP_COOKIE"] = headers.Get("cookie");
			info.EnvironmentVariables["HTTP_HOST"] = headers.Get("host");
			info.EnvironmentVariables["HTTP_REFERER"] = headers.Get("referer");
			info.EnvironmentVariables["HTTP_USER_AGENT"] = headers.Get("user-agent");
			info.EnvironmentVariables["PHP_SELF"] = req.path;
			info.EnvironmentVariables["QUERY_STRING"] = req.query;
			info.EnvironmentVariables["REDIRECT_STATUS"] = "true";
			info.EnvironmentVariables["REMOTE_ADDR"] = "127.0.0.1";
			info.EnvironmentVariables["REQUEST_METHOD"] = post != null && post != "" ? "POST" : "GET";
			info.EnvironmentVariables["REQUEST_TIME"] = DateTime.UtcNow.ToString();
			info.EnvironmentVariables["REQUEST_URI"] = "/" + filename;
			info.EnvironmentVariables["SCRIPT_FILENAME"] = fullPath;
			info.EnvironmentVariables["SERVER_ADDR"] = "127.0.0.1";
			info.EnvironmentVariables["SERVER_NAME"] = "localhost";
			info.EnvironmentVariables["SERVER_PROTOCOL"] = Connection.PROTOCOL;
			info.EnvironmentVariables["SERVER_SOFTWARE"] = Server.NAME;
			info.EnvironmentVariables["SERVER_PORT"] = "80";
			
			var proc = Process.Start(info);
			
			proc.StandardInput.Write(post);
			
			string ret = proc.StandardOutput.ReadToEnd();
			
			proc.Close();
			
			return ret;		
		}
	}
}
