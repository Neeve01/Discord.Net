﻿using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Reflection;
using System.Diagnostics;
using System.Net;
using System.IO;
using System.Globalization;

namespace Discord.Helpers
{
	internal static class Http
	{
#if DEBUG
		private const bool _isDebug = true;
#else
		private const bool _isDebug = false;
#endif
		private static readonly HttpClient _client;
		private static readonly HttpMethod _patch = new HttpMethod("PATCH"); //Not sure why this isn't a default...

		static Http()
		{
			_client = new HttpClient(new HttpClientHandler
			{
				AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip,
				UseCookies = false,
				PreAuthenticate = false //We do auth ourselves
			});
			_client.DefaultRequestHeaders.Add("accept", "*/*");
			_client.DefaultRequestHeaders.Add("accept-encoding", "gzip, deflate");

			string version = typeof(Http).GetTypeInfo().Assembly.GetName().Version.ToString(2);
			_client.DefaultRequestHeaders.Add("user-agent", $"Discord.Net/{version} (https://github.com/RogueException/Discord.Net)");
		}

		private static string _token;
		public static string Token
		{
			get { return _token; }
			set
			{
				_token = value;
				_client.DefaultRequestHeaders.Remove("authorization");
				if (_token != null)
					_client.DefaultRequestHeaders.Add("authorization", _token);
			}
		}

		internal static Task<ResponseT> Get<ResponseT>(string path)
			where ResponseT : class
			=> Send<ResponseT>(HttpMethod.Get, path, null);
		internal static Task<string> Get(string path)
			=> Send(HttpMethod.Get, path, null);
		
		internal static Task<ResponseT> Post<ResponseT>(string path, object data)
			where ResponseT : class
			=> Send<ResponseT>(HttpMethod.Post, path, AsJson(data));
		internal static Task<string> Post(string path, object data)
			=> Send(HttpMethod.Post, path, AsJson(data));
		internal static Task<ResponseT> Post<ResponseT>(string path)
			where ResponseT : class
			=> Send<ResponseT>(HttpMethod.Post, path, null);
		internal static Task<string> Post(string path)
			=> Send(HttpMethod.Post, path, null);
		
		internal static Task<ResponseT> Put<ResponseT>(string path, object data)
			where ResponseT : class
			=> Send<ResponseT>(HttpMethod.Put, path, AsJson(data));
		internal static Task<string> Put(string path, object data)
			=> Send(HttpMethod.Put, path, AsJson(data));
		internal static Task<ResponseT> Put<ResponseT>(string path)
			where ResponseT : class
			=> Send<ResponseT>(HttpMethod.Put, path, null);
		internal static Task<string> Put(string path)
			=> Send(HttpMethod.Put, path, null);

		internal static Task<ResponseT> Patch<ResponseT>(string path, object data)
			where ResponseT : class
			=> Send<ResponseT>(_patch, path, AsJson(data));
		internal static Task<string> Patch(string path, object data)
			=> Send(_patch, path, AsJson(data));
		internal static Task<ResponseT> Patch<ResponseT>(string path)
			where ResponseT : class
			=> Send<ResponseT>(_patch, path, null);
		internal static Task<string> Patch(string path)
			=> Send(_patch, path, null);

		internal static Task<ResponseT> Delete<ResponseT>(string path, object data)
			where ResponseT : class
			=> Send<ResponseT>(HttpMethod.Delete, path, AsJson(data));
		internal static Task<string> Delete(string path, object data)
			=> Send(HttpMethod.Delete, path, AsJson(data));
		internal static Task<ResponseT> Delete<ResponseT>(string path)
			where ResponseT : class
			=> Send<ResponseT>(HttpMethod.Delete, path, null);
		internal static Task<string> Delete(string path)
			=> Send(HttpMethod.Delete, path, null);

		internal static Task<ResponseT> File<ResponseT>(string path, Stream stream, string filename = null)
			where ResponseT : class
			=> Send<ResponseT>(HttpMethod.Post, path, AsFormData(stream, filename));
		internal static Task<string> File(string path, Stream stream, string filename = null)
			=> Send(HttpMethod.Post, path, AsFormData(stream, filename));

		private static async Task<ResponseT> Send<ResponseT>(HttpMethod method, string path, HttpContent content)
			where ResponseT : class
		{
			string responseJson = await SendRequest(method, path, content, true);
			var response = JsonConvert.DeserializeObject<ResponseT>(responseJson);
#if DEBUG
			CheckResponse(responseJson, response);
#endif
			return response;
		}
		private static async Task<string> Send(HttpMethod method, string path, HttpContent content)
		{
			string responseJson = await SendRequest(method, path, content, _isDebug);
#if DEBUG
			CheckEmptyResponse(responseJson);
#endif
			return responseJson;
		}

		private static async Task<string> SendRequest(HttpMethod method, string path, HttpContent content, bool hasResponse)
		{
#if DEBUG
			Stopwatch stopwatch = Stopwatch.StartNew();
#endif			
			HttpRequestMessage msg = new HttpRequestMessage(method, path);
			if (content != null)
				msg.Content = content;

			string result;
			HttpResponseMessage response;
            if (hasResponse)
			{
				response = await _client.SendAsync(msg, HttpCompletionOption.ResponseContentRead);
				if (!response.IsSuccessStatusCode)
					throw new HttpException(response.StatusCode);
				result = await response.Content.ReadAsStringAsync();
			}
			else
			{
				response = await _client.SendAsync(msg, HttpCompletionOption.ResponseHeadersRead);
				if (!response.IsSuccessStatusCode)
					throw new HttpException(response.StatusCode);
				result = null;
			}

#if DEBUG
			stopwatch.Stop();
			Debug.WriteLine($"{method} {path}: {Math.Round(stopwatch.ElapsedTicks / (double)TimeSpan.TicksPerMillisecond, 2)}ms");
#endif
            return result;
		}

#if DEBUG
		private static void CheckResponse<T>(string json, T obj)
		{
			/*JToken token = JToken.Parse(json);
			JToken token2 = JToken.FromObject(obj);
			if (!JToken.DeepEquals(token, token2))
				throw new Exception("API check failed: Objects do not match.");*/
		}

		private static void CheckEmptyResponse(string json)
		{
			if (!string.IsNullOrEmpty(json))
				throw new Exception("API check failed: Response is not empty.");
		}
#endif

		private static StringContent AsJson(object obj)
		{
			return new StringContent(JsonConvert.SerializeObject(obj), Encoding.UTF8, "application/json");
		}
		private static MultipartFormDataContent AsFormData(Stream stream, string filename)
		{
			var content = new MultipartFormDataContent("Upload----" + DateTime.Now.ToString(CultureInfo.InvariantCulture));
			content.Add(new StreamContent(stream), "file", filename);
			return content;
		}
	}
}