# Code Editor Package for Antigravity

## About Antigravity Editor

The Antigravity Editor package provides the Unity Editor with support for using Antigravity AI-powered code editor. This includes support for generating C# project files for IntelliSense, auto-discovery of Antigravity installations, and seamless file opening integration.

## Installation

Install this package through the Unity Package Manager:

1. Open Unity and go to **Window > Package Manager**
2. Click the **+** button and select **Add package from git URL**
3. Enter: `https://github.com/kazkytw/com.kazkytw.ide.antigravity.git`
4. Click **Add**

## Requirements

This version of the Antigravity Editor package is compatible with the following versions of the Unity Editor:

* Unity 2021.3 and later

To use this package, you must have Antigravity installed on your system:

* **On Windows**: Antigravity installed at `%LOCALAPPDATA%\Programs\Antigravity\Antigravity.exe`
* **On macOS**: Antigravity.app in `/Applications`
* **On Linux**: Antigravity in `/usr/bin/antigravity` or `/usr/local/bin/antigravity`

## Usage

After installing the package:

1. Go to **Edit > Preferences > External Tools**
2. Select **Antigravity Editor** from the **External Script Editor** dropdown
3. Unity will now open C# files in Antigravity

The package will automatically detect your Antigravity installation and configure Unity to use it as the default code editor.
