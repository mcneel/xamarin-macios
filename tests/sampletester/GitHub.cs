﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Xml;

using NUnit.Framework;

public static class GitHub
{
	public static string [] GetSolutions (string user, string repo)
	{
		var fn = Path.Combine (Configuration.RootDirectory, $"{repo}.filelist");
		if (File.Exists (fn))
			return File.ReadAllLines (fn);
		Directory.CreateDirectory (Path.GetDirectoryName (fn));

		using (var client = new WebClient ()) {
			byte [] data;
			try {
				client.Headers.Add (HttpRequestHeader.UserAgent, "xamarin");
				data = client.DownloadData ($"https://api.github.com/repos/{user}/{repo}/git/trees/master?recursive=1");
			} catch (WebException we) {
				return new string [] { $"Failed to load {user}/{repo}: {we.Message}" };
			}
			var reader = JsonReaderWriterFactory.CreateJsonReader (data, new XmlDictionaryReaderQuotas ());
			var doc = new XmlDocument ();
			doc.Load (reader);
			var rv = new List<string> ();
			foreach (XmlNode node in doc.SelectNodes ("/root/tree/item/path")) {
				var path = node.InnerText;
				if (!path.EndsWith (".sln", StringComparison.OrdinalIgnoreCase))
					continue;
				rv.Add (node.InnerText);
			}

			File.WriteAllLines (fn, rv.ToArray ());
			return rv.ToArray ();
		}
	}

	public static string [] GetDirectories (string user, string repo, bool recursive)
	{
		var fn = Path.Combine (Configuration.RootDirectory, $"{user}-{repo}.filelist");
		if (File.Exists (fn))
			return File.ReadAllLines (fn);
		Directory.CreateDirectory (Path.GetDirectoryName (fn));

		using (var client = new WebClient ()) {
			byte [] data;
			try {
				client.Headers.Add (HttpRequestHeader.UserAgent, "xamarin");
				data = client.DownloadData ($"https://api.github.com/repos/{user}/{repo}/git/trees/master?recursive=0");
			} catch (WebException we) {
				return new string [] { $"Failed to load repo {user}/{repo}: {we.Message}" };
			}
			var reader = JsonReaderWriterFactory.CreateJsonReader (data, new XmlDictionaryReaderQuotas ());
			var doc = new XmlDocument ();
			doc.Load (reader);
			var rv = new List<string> ();
			foreach (XmlNode node in doc.SelectNodes ("/root/tree/item[type = 'tree']/path")) {
				var path = node.InnerText;
				if (!recursive && path.IndexOf ('/') >= 0)
					continue;
				rv.Add (node.InnerText);
			}

			File.WriteAllLines (fn, rv.ToArray ());
			return rv.ToArray ();
		}
	}

	public static string CloneRepository (string user, string repo)
	{
		var repo_dir = Path.Combine (Configuration.RootDirectory, repo);

		Directory.CreateDirectory (Configuration.RootDirectory);

		if (!Directory.Exists (repo_dir)) {
			var exitCode = 0;
			Assert.IsTrue (ProcessHelper.RunProcess ("git", $"clone git@github.com:{user}/{repo}", out exitCode, TimeSpan.FromMinutes (10), Configuration.RootDirectory), "cloned in 10 minutes");
			Assert.AreEqual (0, exitCode, "git clone exit code");
		}

		return repo_dir;
	}
}