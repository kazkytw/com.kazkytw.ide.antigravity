/*-----------------------------------------------------------------------------------------------------------------
 *  Copyright (c) Kazkytw. All rights reserved.
 *  Based on Visual Studio Code integration from Microsoft Corporation.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using SimpleJSON;
using IOPath = System.IO.Path;
using Debug = UnityEngine.Debug;

namespace Microsoft.Unity.VisualStudio.Editor
{
	internal class AntigravityInstallation : VisualStudioInstallation
	{
		private static readonly IGenerator _generator = new SdkStyleProjectGeneration();

		public override bool SupportsAnalyzers
		{
			get
			{
				return true;
			}
		}

		public override Version LatestLanguageVersionSupported
		{
			get
			{
				return new Version(11, 0);
			}
		}

		public override string[] GetAnalyzers()
		{
			return Array.Empty<string>();
		}

		public override IGenerator ProjectGenerator
		{
			get
			{
				return _generator;
			}
		}

		private static bool IsCandidateForDiscovery(string path)
		{
#if UNITY_EDITOR_OSX
			return Directory.Exists(path) && Regex.IsMatch(path, ".*Antigravity.*.app$", RegexOptions.IgnoreCase);
#elif UNITY_EDITOR_WIN
			return File.Exists(path) && Regex.IsMatch(path, ".*Antigravity.*.exe$", RegexOptions.IgnoreCase);
#else
			return File.Exists(path) && path.EndsWith("antigravity", StringComparison.OrdinalIgnoreCase);
#endif
		}

		public static bool TryDiscoverInstallation(string editorPath, out IVisualStudioInstallation installation)
		{
			installation = null;

			if (string.IsNullOrEmpty(editorPath))
				return false;

			if (!IsCandidateForDiscovery(editorPath))
				return false;

			Version version = new Version(1, 0, 0);

			installation = new AntigravityInstallation()
			{
				IsPrerelease = false,
				Name = "Antigravity",
				Path = editorPath,
				Version = version
			};

			return true;
		}

		public static IEnumerable<IVisualStudioInstallation> GetVisualStudioInstallations()
		{
			var candidates = new List<string>();

#if UNITY_EDITOR_WIN
			var localAppPath = IOPath.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs");

			candidates.Add(IOPath.Combine(localAppPath, "Antigravity", "Antigravity.exe"));
#elif UNITY_EDITOR_OSX
			var appPath = IOPath.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
			candidates.AddRange(Directory.EnumerateDirectories(appPath, "Antigravity*.app"));
#elif UNITY_EDITOR_LINUX
			// Well known locations
			candidates.Add("/usr/bin/antigravity");
			candidates.Add("/bin/antigravity");
			candidates.Add("/usr/local/bin/antigravity");
#endif

			foreach (var candidate in candidates.Distinct())
			{
				if (TryDiscoverInstallation(candidate, out var installation))
					yield return installation;
			}
		}

		public override void CreateExtraFiles(string projectDirectory)
		{
			// Antigravity doesn't need special configuration files like VS Code
			// But we could create .antigravity folder here if needed in the future
		}

		public override bool Open(string path, int line, int column, string solution)
		{
			var application = Path;

			line = Math.Max(1, line);
			column = Math.Max(0, column);

			var directory = IOPath.GetDirectoryName(solution);

#if UNITY_EDITOR_WIN
			// On Windows, open file with line and column parameters
			var arguments = string.IsNullOrEmpty(path)
				? $"\"{directory}\""
				: $"\"{path}\" --line {line} --column {column}";

			var startInfo = ProcessRunner.ProcessStartInfoFor(application, arguments, redirect: false);
#elif UNITY_EDITOR_OSX
			// On macOS, wrap with open command
			var arguments = string.IsNullOrEmpty(path)
				? $"-n \"{application}\" --args \"{directory}\""
				: $"-n \"{application}\" --args \"{path}\" --line {line} --column {column}";
			
			var startInfo = ProcessRunner.ProcessStartInfoFor("open", arguments, redirect: false, shell: true);
#else
			// On Linux
			var arguments = string.IsNullOrEmpty(path)
				? $"\"{directory}\""
				: $"\"{path}\" --line {line} --column {column}";

			var startInfo = ProcessRunner.ProcessStartInfoFor(application, arguments, redirect: false);
#endif

			ProcessRunner.Start(startInfo);
			return true;
		}

		public static void Initialize()
		{
		}
	}
}
