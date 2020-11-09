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
