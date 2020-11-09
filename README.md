# Visual Studio Code Downloader

Downloader is a single file [CSharp](https://docs.microsoft.com/en-us/dotnet/csharp/) strategy for [Microsoft VSCode](https://code.visualstudio.com/).

## Goals

This is intended to operate more like a script than any intermediate or heavy weight multi-file <em>Visual Studio</em> project. As such we wanted to present a single file for a one line build request.

## Build

Building is simple. From any developer command prompt enter the following:

```
X:\path\to\downloader\> csc get_code.cs
```

Caveats, although any developer command line `csc` invocation should work, we are leveraging current language features. So, in other words, it is doubtful that earlier Visual Studio versions will support. Try at your own risk. Up to you. Good luck!

That is all. Simple.

## Conflicts

I have noticed that [Microsoft Defender](https://docs.microsoft.com/en-us/windows/security/threat-protection/microsoft-defender-antivirus/microsoft-defender-antivirus-in-windows-10) sometimes detects `get_code.exe` as a potentially harmful _Trojan:Win32/Wacatac.B!ml_ virus, which is humorous to me. Verify that _Defender_ is identifying `get_code.exe`, and ignore it.

## Highlights

_Downloader_ serves the purpose of allowing to cross reference the [Visual Studio Code Download](https://code.visualstudio.com/Download) matrix in whatever matrix you require. Some examples...

### Windows only

```
get_code.exe --target win32
```

Downloads all _architectures_ and _builds_ for the `win32` _target_.

### Linux only

```
get_code.exe --target linux
```

Downloads all _architectures_ and _builds_ for the [_Linux_](https://en.wikipedia.org/wiki/Linux) target.

### Macintosh only

```
get_code.exe --target darwin
```

Downloads all _architectures_ and _builds_ for the [_Macintosh_ `darwin`](https://en.wikipedia.org/wiki/Darwin_%28operating_system%29) _target_.

### 64-bit only

```
get_code.exe --arch x64
```

Downloads all _targets_ and _builds_ aligned with a `x64` _64-bit_ _architecture_.

### ARM only

```
get_code.exe --arch arm
```

Downloads all _targets_ and _builds_ aligned with the `arm` _architecture_.

### 64-bit ARM only

```
get_code.exe --arch arm64
```

Downloads all _targets_ and _builds_ aligned with the `arm64` _architecture_.

### Linux snap only

```
get_code.exe --build snap
```

Downloads the [_Linux_](https://en.wikipedia.org/wiki/Linux) [snap](https://en.wikipedia.org/wiki/Snap_(package_manager)) build.

## Noteworthy features

Several other features are noteworthy.

### Latest version

By default the latest version is maintained at the source code level, at the time of this writing known to be `1.51.0`.

However, in the interest of not being in the business of chasing Visual Studio Code versions, we allow the user to train the downloader with the most current latest version.

```
get_code.exe -L 1.51.0
```

### Code version

If you happen to know of a specific [Code] version you want to download.

```
get_code.exe --code-version 1.51.1
```

_Note, `1.51.1` is fictional at the time of this writing, for illustration purposes only._

### Macintosh Operating System version

At the time of this writing, the _macOS_ version is known to be [_OS X Yosemite_](https://en.wikipedia.org/wiki/OS_X_Yosemite), version `10.10`, and technically, that we know of, supports `10.10+`.

```
get_code.exe --macos 20.20
```

_Note, `10.20` is fictional at the time of this writing, for illustration purposes only._

