using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using System.Diagnostics;

namespace Server {
	public class Server {
		public const string NAME = "Server3000";
		public const string DEFAULT_IP = "127.0.0.1";
		public const int DEFAULT_PORT = 80;
		
		
		
		public static void Main(string[] args) {
			IPAddress ip;
			int port;
			
			if(args.Length == 2) {
				ip = IPAddress.Parse(args[0]);
				port = Int32.Parse(args[1]);
			} else {
				ip = IPAddress.Parse(DEFAULT_IP);
				port = DEFAULT_PORT;
			}
			
			TcpListener server = new TcpListener(ip, port);
			server.Start();
			
			while(true) {
				TcpClient client = server.AcceptTcpClient();
				Connection conn = new Connection(client);
				new Thread(new ThreadStart(conn.Run)).Start();
			}
			
			//server.Stop();
		}
	}
	
	public class Connection {
		public const string PROTOCOL = "HTTP/1.1";
		public const string PUBLIC_DIR = "public_html";
		public const string DEFAULT_PAGE = "index.html";
		
		private TcpClient client;
		private NetworkStream stream;
		
		public Connection(TcpClient client) {
			this.client = client;
			this.stream = client.GetStream();
		}
		
		// the method that handles an http request
		public void Run() {
			Request req = Request.Parse(stream);
			
			if(req == null || (req.type != "GET" && req.type != "POST" && req.type != "HEAD")) {
				Respond(400, null, "", true);
				client.Close();
				return;
			}
			
			Headers headers = Headers.Parse(stream);
			
			string post = "";
			if(req.type == "POST" && headers.Get("content-length") != null && headers.Get("content-length") != "0") {
				post = ParsePostData(Int32.Parse(headers.Get("content-length")));
			}
			
			if(req.path.IndexOf("..") != -1) {
				Respond(403, null, "", true);
				client.Close();
				return;
			}

			if(req.path == "/") {
				req.path = "/" + DEFAULT_PAGE;
			}
			
			string returnText = "";
			string fullPath = GetFullPath(req.path);

			if(File.Exists(fullPath)) {
				if(IsPHP(fullPath))
					returnText = PHPHandler.Run(fullPath, req, headers, post);
				else
					returnText = File.ReadAllText(fullPath);
			}
			
			if(returnText != null)
				Respond(200, null, req.type == "HEAD" ? "" : returnText, !IsPHP(fullPath));
			else
				Respond(404, null, "", !IsPHP(fullPath));
			
			client.Close();
		}
		
		// checks the extension to see if a file is a php script
		private bool IsPHP(string path) {
			return Path.GetExtension(path) == ".php";
		}
		
		// sends a response back
		private void Respond(int status, Headers headers, string body, bool writeHeaders) {
			byte[] returnRequest = Encoding.ASCII.GetBytes(String.Format("{0} {1} {2}\n", PROTOCOL, status, GetErrorMsg(status)));
			byte[] returnBody = Encoding.ASCII.GetBytes(body);
			
			if(headers == null)
				headers = new Headers();
			
			if(headers.Get("Content-length") == "")
				headers.Set("Content-length", returnBody.Length.ToString());
			
			byte[] returnHeaders = Encoding.ASCII.GetBytes(headers.ToString());
			
			stream.Write(returnRequest, 0, returnRequest.Length);
			if(writeHeaders)
				stream.Write(returnHeaders, 0, returnHeaders.Length);
			stream.Write(returnBody, 0, returnBody.Length);
		}
		
		// get text associated with an error code
		private string GetErrorMsg(int code) {
			switch(code) {
				case 200:
					return "OK";
				case 403:
					return "Forbidden";
				case 404:
					return "Not found";
				default:
					return "";
			}
		}
		
		// returns the full path to the given path
		private string GetFullPath(string path) {
			if(path[0] == '/')
				path = path.Substring(1);
			
			return Path.GetFullPath(Path.Combine(PUBLIC_DIR, path));
		}
		
		// returns the full path of the public directory
		public static string GetRootPath() {
			return Path.GetFullPath(PUBLIC_DIR);
		}
		
		// parses the postdata
		private string ParsePostData(int length) {
			StringBuilder builder = new StringBuilder();
			
			int i = 0;
			while(i++ < length) {
				builder.Append((char)stream.ReadByte());
			}
			
			return builder.ToString();
		}
	}
	
	// parses the first line of the request
	public class Request {
		public string type {get; set;}
		public string path {get; set;}
		public string query {get; set;}
		public string protocol {get; set;}
		
		public static Request Parse(NetworkStream stream) {
			Request req = new Request();
			
			string firstLine = stream.ReadLine();
			string[] words = firstLine.Split(' ');
			
			if(words.Length != 3) return null;
			
			string[] pathQuery = words[1].Split('?');
			
			req.type = words[0];
			req.path = pathQuery[0];
			
			if(pathQuery.Length == 2)
				req.query = pathQuery[1];
			
			req.protocol = words[2];
			
			return req;
		}
	}
	
	// parses the header data
	public class Headers {
		public Dictionary<string, string> list;
	
		public Headers() {
			list = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		}
		
		public static Headers Parse(NetworkStream stream) {
			Headers instance = new Headers();
			
			string line;
			while((line = stream.ReadLine()) != "\n") {
				line = line.Trim();
				int index = line.IndexOf(':');
				if(index < 1)
					break;
				
				string key = line.Substring(0, index);
				string value = line.Substring(index + 2);
				
				instance.list.Add(key, value);
			}
			
			return instance;
		}
		
		public new string ToString() {
			StringBuilder builder = new StringBuilder();
			
			foreach(KeyValuePair<string, string> header in list) {
				builder.Append(header.Key + ": " + header.Value + "\n");
			}
			
			builder.Append("\n");
			
			return builder.ToString();
		}
		
		public string Get(string key) {
			string ret;
			if(list.TryGetValue(key, out ret))
				return ret;
			else
				return "";
		}
		
		public void Set(string key, string val) {
			list[key] = val;
		}
	}
	
	public static class Extentions {
		public static string ReadLine(this NetworkStream stream) {
			byte[] buffer = new byte[2048];
			int i = 0;
			
			while(i < buffer.Length) {
				buffer[i] = (byte) stream.ReadByte();
				
				if(buffer[i++] == '\n') {
					return System.Text.Encoding.ASCII.GetString(buffer, 0, i - 1);
				}
			}
			
			return "";
		}
	}
}
