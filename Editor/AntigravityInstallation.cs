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
			var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
			var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

			// Standard installation paths
			candidates.Add(IOPath.Combine(localAppData, "Programs", "Antigravity", "Antigravity.exe"));
			candidates.Add(IOPath.Combine(localAppData, "Antigravity", "Antigravity.exe"));
			candidates.Add(IOPath.Combine(programFiles, "Antigravity", "Antigravity.exe"));
			candidates.Add(IOPath.Combine(programFilesX86, "Antigravity", "Antigravity.exe"));
#elif UNITY_EDITOR_OSX
			var appPath = IOPath.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
			candidates.AddRange(Directory.EnumerateDirectories(appPath, "Antigravity*.app"));
			candidates.AddRange(Directory.EnumerateDirectories(Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "/Applications", "Antigravity*.app"));
#elif UNITY_EDITOR_LINUX
			// Well known locations
			candidates.Add("/usr/bin/antigravity");
			candidates.Add("/bin/antigravity");
			candidates.Add("/usr/local/bin/antigravity");
			candidates.Add("/snap/bin/antigravity");
#endif

			foreach (var candidate in candidates.Distinct())
			{
				if (TryDiscoverInstallation(candidate, out var installation))
					yield return installation;
			}
		}

		public override void CreateExtraFiles(string projectDirectory)
		{
			// Create .vscode/settings.json to hide .meta files and configure workspace
			// We use .vscode because most Electron-based editors (like Cursor) support it
			var vscodeDirectory = IOPath.Combine(projectDirectory, ".vscode");

			if (!Directory.Exists(vscodeDirectory))
				Directory.CreateDirectory(vscodeDirectory);

			var settingsFile = IOPath.Combine(vscodeDirectory, "settings.json");
			
			// Only create if it doesn't exist to avoid overwriting user settings
			if (!File.Exists(settingsFile))
			{
				const string content = @"{
    ""files.exclude"": {
        ""**/*.meta"": true,
        ""**/*.unity"": true,
        ""**/*.prefab"": true,
        ""**/*.asset"": true,
        ""Library/"": true,
        ""ProjectSettings/"": true,
        ""Temp/"": true
    },
    ""files.associations"": {
        ""*.asset"": ""yaml"",
        ""*.meta"": ""yaml"",
        ""*.prefab"": ""yaml"",
        ""*.unity"": ""yaml""
    }
}";
				File.WriteAllText(settingsFile, content);
			}
		}

		public override bool Open(string path, int line, int column, string solution)
		{
			var application = Path;
			var directory = IOPath.GetDirectoryName(solution);
			var workspace = directory; // Use the solution directory as workspace

			line = Math.Max(1, line);
			column = Math.Max(0, column);

#if UNITY_EDITOR_WIN
			// Open workspace (directory) AND specific file
			// Format: "executable" "workspace_path" -g "file_path":line:column
			// This is the standard VS Code / Cursor format
			
			string arguments;
			if (string.IsNullOrEmpty(path))
			{
				arguments = $"\"{workspace}\"";
			}
			else
			{
				arguments = $"\"{workspace}\" -g \"{path}\":{line}:{column}";
			}

			var startInfo = ProcessRunner.ProcessStartInfoFor(application, arguments, redirect: false);
#elif UNITY_EDITOR_OSX
			string arguments;
			if (string.IsNullOrEmpty(path))
			{
				arguments = $"-n \"{application}\" --args \"{workspace}\"";
			}
			else
			{
				arguments = $"-n \"{application}\" --args \"{workspace}\" -g \"{path}\":{line}:{column}";
			}
			
			var startInfo = ProcessRunner.ProcessStartInfoFor("open", arguments, redirect: false, shell: true);
#else
			string arguments;
			if (string.IsNullOrEmpty(path))
			{
				arguments = $"\"{workspace}\"";
			}
			else
			{
				arguments = $"\"{workspace}\" -g \"{path}\":{line}:{column}";
			}

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
