using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

// https://fake.build/dotnet-assemblyinfo.html
// https://csharppedia.com/en/tutorial/4264/assemblyinfo-cs-examples
[assembly: AssemblyProduct("VSCode")]
[assembly: AssemblyTitle("VSCode Downloader")]
[assembly: AssemblyCompany("Ellumination Technologies")]
[assembly: AssemblyCopyright("Copyright (c) 2020-2022")]
[assembly: AssemblyVersion("1.3.0.0")]
[assembly: AssemblyFileVersion("1.3.0.0")]
[assembly: AssemblyInformationalVersion("1.3.0.0")]
[assembly: Guid("f100c6cc-5c31-49e8-a913-0bda0cfefacc")]

// Windows / User Installer / x64 / VSCodeUserSetup-x64-1.68.1.exe
// Windows / User Installer / x32 / VSCodeUserSetup-ia32-1.68.1.exe
// Windows / User Installer / arm / VSCodeUserSetup-arm64-1.68.1.exe

// Windows / System Installer / x64 / VSCodeSetup-x64-1.68.1.exe
// Windows / System Installer / x32 / VSCodeSetup-ia32-1.68.1.exe
// Windows / System Installer / arm / VSCodeSetup-arm64-1.68.1.exe

// Windows / .zip / x64 / VSCode-win32-x64-1.68.1.zip
// Windows / .zip / x32 / VSCode-win32-ia32-1.68.1.zip
// Windows / .zip / arm / VSCode-win32-arm64-1.68.1.zip

// Linux / .deb / x64 / code_1.68.1-1617808414_amd64.deb
// Linux / .deb / arm / code_1.68.1-1617807713_armhf.deb
// Linux / .deb / arm64 / code_1.68.1-1617807524_arm64.deb

// Linux / .rpm / x64 / code-1.68.1-1617808495.el8.x86_64.rpm
// Linux / .rpm / arm / code-1.68.1-1617807780.el8.armv7hl.rpm
// Linux / .rpm / arm64 / code-1.68.1-1617807586.el8.aarch64.rpm

// Linux / .tar.gz / x64 / code-stable-x64-1617808689.tar.gz
// Linux / .tar.gz / arm / code-stable-armhf-1617807959.tar.gz
// Linux / .tar.gz / arm64 / code-stable-arm64-1617807804.tar.gz

// Linux / snap

// Mac / macOS 10.11+ / Universal / darwin-universal
// Mac / macOS 10.11+ / Intel Chip / darwin
// Mac / macOS 10.11+ / Apple Silicon / darwin-arm64

// TODO: TBD: just about done with this one...
// TODO: TBD: I may also commit it to github after all, we'll see...
// TODO: TBD: just about all the plumbing is in and verified...
// TODO: TBD: also have the assets verification pretty much working...
// TODO: TBD: what remains is to verify that the wget process itself actually works as expected...
// TODO: TBD: might consider giving the Code repo a gander and establishing this repo as a seamless overlay there...
// TODO: TBD: with the thought being that potentially could merge the two repos, potentially...
namespace Code.Downloader
{
    using static Architecture;
    using static Build;
    using static CodeVersion;
    using static Help;
    using static Insider;
    using static NoPause;
    using static Target;
    using static AssetManager;
    using static Chars;
    using static DownloadStrategy;
    using static Program;
    using static StringExtensions;
    using static Versions;

    /// <summary>
    /// Defines many commonly used delimiters and such for use throughout.
    /// </summary>
    internal static class Chars
    {
        /// <summary>
        /// &apos;:'&apos;
        /// </summary>
        public const char colon = ':';

        /// <summary>
        /// &apos;'&apos;
        /// </summary>
        public const char tick = '\'';

        /// <summary>
        /// &apos;;&apos;
        /// </summary>
        public const char comma = ',';

        /// <summary>
        /// &apos;-&apos;
        /// </summary>
        public const char hyp = '-';

        /// <summary>
        /// &apos;.&apos;
        /// </summary>
        public const char dot = '.';

        /// <summary>
        /// &apos;/&apos;
        /// </summary>
        public const char slash = '/';

        /// <summary>
        /// &apos;_&apos;
        /// </summary>
        public const char underscore = '_';

        /// <summary>
        /// &apos;|&apos;
        /// </summary>
        public const char pipe = '|';

        /// <summary>
        /// &quot;()&quot;
        /// </summary>
        public const string parens = "()";

        /// <summary>
        /// &quot;[]&quot;
        /// </summary>
        public const string squareBrackets = "[]";

        /// <summary>
        /// &quot;<>&quot;
        /// </summary>
        public const string angleBrackets = "<>";

        /// <summary>
        /// &quot;{}&quot;
        /// </summary>
        public const string curlyBraces = "{}";

        /// <summary>
        /// &quot;http&quot;
        /// </summary>
        public const string http = nameof(http);

        /// <summary>
        /// &quot;https&quot;
        /// </summary>
        public const string https = nameof(https);
    }

    public enum CodeVersion
    {
        /// <summary>
        /// For use with code.
        /// </summary>
        code,

        /// <summary>
        /// For use with version.
        /// </summary>
        version,

        /// <summary>
        /// For use with version.
        /// </summary>
        latest,
    }

    /// <summary>
    /// Dry
    /// </summary>
    public enum Dry
    {
        /// <summary>
        /// For use with all.
        /// </summary>
        dry,
    }

    /// <summary>
    /// All
    /// </summary>
    public enum All
    {
        /// <summary>
        /// For use with all.
        /// </summary>
        all,
    }

    /// <summary>
    /// Help
    /// </summary>
    public enum Help
    {
        /// <summary>
        /// For use with help.
        /// </summary>
        show,
    }

    /// <summary>
    /// Target
    /// </summary>
    public enum Target
    {
        /// <summary>
        /// For use with macOS.
        /// </summary>
        darwin,

        /// <summary>
        /// For all Linux flavors.
        /// </summary>
        linux,

        /// <summary>
        /// For use with all Windows flavors.
        /// </summary>
        win32,
    }

    /// <summary>
    /// Architecture
    /// </summary>
    public enum Architecture
    {
        /// <summary>
        /// 
        /// </summary>
        x64,

        /// <summary>
        /// 
        /// </summary>
        x86,

        /// <summary>
        /// 
        /// </summary>
        arm,

        /// <summary>
        /// 
        /// </summary>
        armhf,

        /// <summary>
        /// 
        /// </summary>
        arm64,

        /// <summary>
        ///
        /// </summary>
        universal,
    }

    public enum Build
    {
        /// <summary>
        /// 
        /// </summary>
        user,

        /// <summary>
        /// 
        /// </summary>
        system,

        /// <summary>
        /// 
        /// </summary>
        deb,

        /// <summary>
        /// 
        /// </summary>
        rpm,

        /// <summary>
        /// 
        /// </summary>
        archive,

        /// <summary>
        /// 
        /// </summary>
        snap,
    }

    public enum Insider
    {
        /// <summary>
        /// For use with stable.
        /// </summary>
        stable,

        /// <summary>
        /// For use with insider.
        /// </summary>
        insider,
    }

    public enum NoPause
    {
        /// <summary>
        /// For use with noPause.
        /// </summary>
        pause,
    }

    // win32+system+x86+version => VSCodeSetup-ia32-major.minor.patch.exe
    // win32+user+x86+version => VSCodeUserSetup-ia32-major.minor.patch.exe
    // win32+archive+x86+version => VSCode-win32-ia32-major.minor.patch.zip
    //
    // win32+system+x64+version => VSCodeSetup-x64-major.minor.patch.exe
    // win32+user+x64+version => VSCodeUserSetup-x64-major.minor.patch.exe
    // win32+archive+x64+version => VSCode-win32-x64-major.minor.patch.zip
    //
    // win32+system+arm+version => VSCodeSetup-arm64-major.minor.patch.exe
    // win32+user+arm+version => VSCodeUserSetup-arm64-major.minor.patch.exe
    // win32+archive+arm+version => VSCode-win32-arm64-major.minor.patch.zip
    //
    // win32+system+arm64+version => VSCodeSetup-arm64-major.minor.patch.exe
    // win32+user+arm64+version => VSCodeUserSetup-arm64-major.minor.patch.exe
    // win32+archive+arm64+version => VSCode-win32-arm64-major.minor.patch.zip
    //
    // win32+system+arm64+version => VSCodeSetup-arm64-major.minor.patch.exe
    // win32+user+arm64+version => VSCodeUserSetup-arm64-major.minor.patch.exe
    // win32+archive+arm64+version => VSCode-win32-arm64-major.minor.patch.zip
    //
    // linux+deb+x64+version => code_major.minor.patch-amd64.deb
    // linux+rpm+x64+version => code_major.minor.patch-amd64.rpm
    // linux+archive+x64+version => code_major.minor.patch-amd64.tar.gz
    //
    // darwin+universal+version+stable => VSCode-darwin-universal-major.minor.patch-stable.zip
    // darwin+version+stable => VSCode-darwin-major.minor.patch-stable.zip
    // darwin+arm64+version+stable => VSCode-darwin-arm64-major.minor.patch-stable.zip
    //
    // TODO: TBD: add additional APPLE options... universal, intel, apple, arm64
    // Universal: https://code.visualstudio.com/sha/download?build=stable&os=darwin-universal => VSCode-darwin-universal.zip
    // Intel chip: https://code.visualstudio.com/sha/download?build=stable&os=darwin => VSCode-darwin.zip
    // Apple Silicon: https://code.visualstudio.com/sha/download?build=stable&os=darwin-arm64 => VSCode-darwin-arm64.zip
    //
    // Windows User Installer:
    // x64: VSCodeUserSetup-x64-1.50.1.exe
    // x86: VSCodeUserSetup-ia32-1.50.1.exe
    // ARM: VSCodeUserSetup-arm64-1.50.1.exe
    //
    // Windows System Installer:
    // x64: VSCodeSetup-x64-1.50.1.exe
    // x86: VSCodeSetup-ia32-1.50.1.exe
    // ARM: VSCodeSetup-arm64-1.50.1.exe
    //
    // Windows .zip archive:
    // x64: VSCode-win32-x64-1.50.1.zip
    // x86: VSCode-win32-ia32-1.50.1.zip
    // ARM: VSCode-win32-arm64-1.50.1.zip
    //
    // Linux .deb:
    // x64: code_1.50.1-1602600906_amd64.deb
    // ARM: code_1.50.1-1602600660_armhf.deb
    // ARM64: code_1.50.1-1602600638_arm64.deb
    //
    // Linux .rpm:
    // x64: code-1.50.1-1602601064.el7.x86_64.rpm
    // ARM: code-1.50.1-1602600721.el7.armv7hl.rpm
    // ARM64: code-1.50.1-1602600714.el7.aarch64.rpm
    //
    // Linux .tar.gz tarball archives:
    // x64: code-stable-x64-1602601238.tar.gz
    // ARM: code-stable-armhf-1602600874.tar.gz
    // ARM64: code-stable-arm64-1602601132.tar.gz
    //
    // Linux snap:
    // snap: linux-snap-x64

    /// <summary>
    /// Enumerated elements contributing to the file naming conventions. These
    /// should be mapped or at least strategically arranged accordingly.
    /// </summary>
    public enum Element
    {
        Windows,
        Linux,
        macOS,
        code,
        VSCode,
        User,
        Setup,
        darwin,
        linux,
        win32,
        user,
        archive,

        /// <summary>
        /// Not meaning <see cref="Insider.insider"/>, but rather a placeholder for either
        /// of the <see cref="Insider"/> bits.
        /// </summary>
        insider,

        /// <summary>
        /// Same as <see cref="insider"/> except we accept <see cref="Insider.insider"/> only,
        /// or <c>null</c> when it was not the case.
        /// </summary>
        insiderOrNull,
        snap,
        universal,
        ia32,
        x86,
        x64,
        arm,
        amd64, // For Linux DEB x64
        arm64, // For Linux ARM64 DEB and archive, macOS darwin
        el7, // For Linux RPM...
        x86_64, // For Linux RPM (x86)/x64
        armhf, // For Linux ARM DEB and archive
        armv7hl, // For Linux RPM ARM
        aarch64, // For Linux RPM ARM64
        exe,
        zip,
        deb,
        rpm,
        tar,
        gz,

        /// <summary>
        /// Rendered &quot;version&quot; or &quot;latest&quot;, sometimes also
        /// &quot;insider&quot;, depending on the context, whether in terms of
        /// <em>Url</em> or <em>file name</em> rendering.
        /// </summary>
        version,

        /// <summary>
        /// Represents a different version, the actual <see cref="Versions.macOS"/>.
        /// </summary>
        versionMacOS,
        update,
        visualstudio,
        com
    }

    internal class AssetManager
    {
        /// <summary>
        /// Executable extension constant definition.
        /// </summary>
        private const string exe = "." + nameof(exe);

        /// <summary>
        /// Assumes that WGET is in your path somewhere. Refer to the WSUS URL
        /// for downloads of a package containing the WGET resource.
        /// </summary>
        /// <see cref="exe"/>
        /// <see cref="wsusOfflineUri"/>
        internal const string wget = nameof(wget) + exe;

        /// <summary>
        ///
        /// </summary>
        /// <see cref="exe"/>
        internal const string where = nameof(where) + exe;

        private string _wgetPath;

        public virtual string wgetPath => _wgetPath;

        /// <summary>
        /// Uri to the WSUS Offline downloads, &quot;https://download.wsusoffline.net&quot;.
        /// </summary>
        /// <see cref="!:https://download.wsusoffline.net">WSUS Offline Download</see>
        public const string wsusOfflineUri = "https://download.wsusoffline.net";

        /// <summary>
        /// &quot;https://update.code.visualstudio.com/&quot;
        /// </summary>
        internal const string updateCodeUri = "https://update.code.visualstudio.com/";

        /// <summary>
        /// &quot;https://code.visualstudio.com/Download&quot;
        /// </summary>
        internal const string codeDownloadUri = "https://code.visualstudio.com/Download";

        /// <summary>
        /// &quot;https://snapcraft.io/code&quot;
        /// </summary>
        internal const string snapCraftDotIoCode = "https://snapcraft.io/code";

        /// <summary>
        /// The following are the valid combinations. <see cref="darwin"/> supports only
        /// <see cref="archive"/> <see cref="Architecture"/> builds. <see cref="linux"/> without
        /// the build is considered a
        /// <see cref="!:https://en.wikipedia.org/wiki/Tar_(computing)#Suffixes_for_compressed_files">tarball</see>
        /// archive, with the file extension <c>.tar.gz</c>. <see cref="linux"/>
        /// <see cref="snap"/> <see cref="Build"/> does not have a corresponding
        /// <see cref="Architecture"/>. <see cref="win32"/> targets without a target
        /// <see cref="Architecture"/> or <see cref="Build"/> are considered <see cref="x86"/>
        /// or <see cref="system"/>, respectively. Lastly, versions are always specified in terms
        /// of <c>major.minor.patch</c>.
        /// <br/>
        /// <br/><see cref="snap"/> is a unique <see cref="linux"/> corner case. We consider it
        /// one of the <see cref="Build"/> options and it occurs only for the <see cref="x64"/>
        /// <see cref="Architecture"/>, but we consider that <c>null</c>, as well, for internal
        /// purposes. The <em>Download</em> link redirects to the
        /// <see cref="!:https://snapcraft.io/code"/> store front, however, we are aware of the
        /// actual download Uri, so we utilize that reference directly.
        /// <br/>
        /// <br/>darwin-universal
        /// <br/>darwin
        /// <br/>darwin-arm64
        /// <br/>linux-arm64
        /// <br/>linux-armhf
        /// <br/>linux-deb-arm64
        /// <br/>linux-deb-armhf
        /// <br/>linux-deb-x64
        /// <br/>linux-rpm-arm64
        /// <br/>linux-rpm-armhf
        /// <br/>linux-rpm-x64
        /// <br/>linux-snap-x64
        /// <br/>linux-x64
        /// <br/>win32
        /// <br/>win32-archive
        /// <br/>win32-arm64
        /// <br/>win32-arm64-archive
        /// <br/>win32-arm64-user
        /// <br/>win32-user
        /// <br/>win32-x64
        /// <br/>win32-x64-archive
        /// <br/>win32-x64-user
        /// <br/>
        /// <br/>And concerning naming conventions:
        /// <br/>
        /// <br/>win32+system+x86+version => VSCodeSetup-ia32-major.minor.patch.exe
        /// <br/>win32+user+x86+version => VSCodeUserSetup-ia32-major.minor.patch.exe
        /// <br/>win32+archive+x86+version => VSCode-win32-ia32-major.minor.patch.zip
        /// <br/>
        /// <br/>win32+system+x64+version => VSCodeSetup-x86-major.minor.patch.exe
        /// <br/>win32+user+x64+version => VSCodeUserSetup-x86-major.minor.patch.exe
        /// <br/>win32+archive+x64+version => VSCode-win32-x86-major.minor.patch.zip
        /// <br/>
        /// <br/>win32+system+x64+version => VSCodeSetup-x86-major.minor.patch.exe
        /// <br/>win32+user+x64+version => VSCodeUserSetup-x86-major.minor.patch.exe
        /// <br/>win32+archive+x64+version => VSCode-win32-x86-major.minor.patch.zip
        /// <br/>
        /// <br/>win32+system+arm64+version => VSCodeSetup-arm64-major.minor.patch.exe
        /// <br/>win32+user+arm64+version => VSCodeUserSetup-arm64-major.minor.patch.exe
        /// <br/>win32+archive+arm64+version => VSCode-win32-arm64-major.minor.patch.zip
        /// <br/>
        /// <br/>linux+deb+x64+version => code_major.minor.patch-amd64.deb
        /// <br/>linux+rpm+x64+version => code_major.minor.patch-amd64.rpm
        /// <br/>linux+archive+x64+version => code_major.minor.patch-amd64.tar.gz
        /// <br/>
        /// <br/>linux+snap => code-stable-major.minor.patch.snap
        /// <br/>linux+archive+snap => code-stable-major.minor.patch.snap
        /// <br/>
        /// <br/>darwin+universal+version+stable => VSCode-darwin-universal-major.minor.patch-stable.zip
        /// <br/>darwin+version+stable => VSCode-darwin-major.minor.patch-stable.zip
        /// <br/>darwin+arm64+version+stable => VSCode-darwin-arm64-major.minor.patch-stable.zip
        /// <br/>
        /// <br/>And a few notes concerning combinations and file naming conventions:
        /// <see cref="win32"/> and <see cref="darwin"/> <see cref="archive"/> extensions are
        /// <c>.zip</c>. <see cref="win32"/> filename <see cref="Architecture"/> is <c>ia32</c>.
        /// <see cref="linux"/> <see cref="x64"/> <see cref="Architecture"/> is <c>amd64</c>.
        /// </summary>
        /// <see cref="!:https://github.com/microsoft/vscode/issues/109329">Automating the downloads with help from repeatable links</see>
        /// <see cref="!:https://update.code.visualstudio.com/latest/win32-x64-user/stable"/>
        /// <see cref="!:https://update.code.visualstudio.com/latest/win32-x64-user/insider"/>
        /// <see cref="!:https://update.code.visualstudio.com/major.minor.patch/win32-x64-user/stable"/>
        /// <see cref="!:https://update.code.visualstudio.com/major.minor.patch-insider/win32-x64-user/insider"/>
        /// <see cref="!:https://update.code.visualstudio.com/major.minor.patch/linux-snap-x64/stable"/>
        /// <see cref="snapCraftDotIoCode"/>
        internal const string codeGithubIssueUri = "https://github.com/microsoft/vscode/issues/109329";

        private static bool TryDiscoverAssets(out string path)
        {
            path = null;

            var startInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                FileName = AssetManager.where,
                Arguments = AssetManager.wget,
                CreateNoWindow = true,
                RedirectStandardOutput = true
            };

            using (var process = Process.Start(startInfo))
            {
                // TODO: TBD: as far as we know, we do not own the StandardOutput instance in order to dispose of it...
                var process_StandardOutput = process.StandardOutput;

                var pathCand = process_StandardOutput.ReadLine();

                if (pathCand.FileExists())
                {
                    path = pathCand;
                }

                process.WaitForExit();
            }

            // Probably was safe before, but do the null check anyway.
            return path.FileExists();
        }

        /// <summary>
        /// Returns whether the <see cref="wgetPath"/> Assets Are Discovered.
        /// </summary>
        /// <param name="writer"></param>
        /// <returns></returns>
        public bool AreDiscovered(TextWriter writer)
        {
            writer.WriteLine("Discovering assets...");

            bool TryDiscoveryFailed()
            {
                writer.WriteLine($"Unable to locate {wget} in your path.");
                writer.WriteLine($"Redirecting to download {wsusOfflineUri} package.");
                Process.Start(wsusOfflineUri);
                return false;
            }

            return TryDiscoverAssets(out _wgetPath) || TryDiscoveryFailed();
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class Versions
    {
        /// <summary>
        /// Gets or Sets whether to Show the downloader Version.
        /// </summary>
        internal bool show { get; set; }

        private System.Version _version;
        private CodeVersion _selector = latest;

        /// <summary>
        /// Gets or Sets the version.
        /// </summary>
        /// <see cref="selector"/>
        public System.Version version
        {
            get => this.selector == latest ? latestVersion : (this._version ?? latestVersion);
            set
            {
                this._version = value;
                this.selector = this._version == null ? latest : CodeVersion.version;
            }
        }

        /// <summary>
        /// Gets or Sets the selector.
        /// </summary>
        /// <see cref="version"/>
        public CodeVersion selector
        {
            get => this._selector;
            set
            {
                this._selector = value;
                this._version = this._selector == latest ? null : this._version;
            }
        }

        /// <summary>
        /// Replaces the <see cref="Version"/> with the parsed <paramref name="s"/> value.
        /// When this fails due to <see cref="Version.Parse"/> exception, replaces with the
        /// <paramref name="previous"/>.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="previous"></param>
        /// <param name="onReplace"></param>
        private static void ReplaceVersion(string s, Version previous, SetVersionCallback onReplace)
        {
            try
            {
                var parsed = Version.Parse(s);
                onReplace.Invoke(parsed);
            }
            catch
            {
                // Replace with the previous value when
                //Console.Out.WriteLine($"An exception occurred replacing {previous}");
                onReplace.Invoke(previous);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        private delegate void SetVersionCallback(Version value);

        /// <summary>
        /// Replaces the <see cref="macOS"/> with the parsed <paramref name="s"/>.
        /// </summary>
        /// <param name="s"></param>
        /// <see cref="_macOS"/>
        internal static void ReplaceMacOS(string s) => ReplaceVersion(s, macOS, x => _macOS = x);

        /// <summary>
        /// Gets the Default <see cref="macOS"/>. Defaults to &quot;10.11+&quot;.
        /// </summary>
        internal static System.Version defaultMacOS { get; } = Version.Parse("10.11");

        private static System.Version _macOS;

        /// <summary>
        /// Gets the current <see cref="macOS"/>. Defaults to <see cref="defaultMacOS"/>.
        /// </summary>
        internal static System.Version macOS => _macOS ?? (_macOS = defaultMacOS);

        // TODO: TBD: version informs the directory path...
        // TODO: TBD: the macOS version informs darwin directory path...
        // TODO: TBD: version may inform the download Uri...
        // TODO: TBD: version may also inform the download Uri...
        // TODO: TBD: or may request "latest", in which case "latest" informs the download Uri...

        /// <summary>
        /// Gets or Sets the Rendered <see cref="string"/> <see cref="macOS"/>.
        /// </summary>
        /// <remarks>Mainly for use in the <see cref="Target.darwin"/> Directory Path.</remarks>
        /// <see cref="macOS"/>
        internal static string renderedMacOS => $"{macOS}";

        /// <summary>
        /// Replaces the <see cref="latestVersion"/> with the parsed <paramref name="s"/>.
        /// </summary>
        /// <param name="s"></param>
        /// <see cref="_latestVersion"/>
        internal static void ReplaceLatestVersion(string s) => ReplaceVersion(s, latestVersion, x => _latestVersion = x);

        /// <summary>
        /// Gets the Default <see cref="latestVersion"/>. Defaults to &quot;1.68.1&quot; at the time
        /// of this commit.
        /// </summary>
        internal static System.Version defaultLatestVersion { get; } = Version.Parse("1.68.1");

        private static System.Version _latestVersion;

        /// <summary>
        /// Gets the <see cref="latestVersion"/>. Defaults to <see cref="defaultLatestVersion"/>.
        /// </summary>
        internal static System.Version latestVersion => _latestVersion ?? (_latestVersion = defaultLatestVersion);

        /// <summary>
        /// Gets or Sets the Rendered <see cref="string"/> <see cref="version"/>.
        /// </summary>
        /// <remarks>Mainly for use in the Directory Path.</remarks>
        /// <see cref="version"/>
        internal string renderedVersion => $"{version}";

        /// <summary>
        /// Gets or Sets whether to Show the downloader Version.
        /// </summary>
        /// <remarks>For use when Rendering each FileName.</remarks>
        /// <see cref="CodeVersion"/>
        /// <see cref="CodeVersion.latest"/>
        /// <see cref="version"/>
        /// <see cref="selector"/>
        internal string renderedVersionOrLatest => this.selector == latest ? nameof(latest) : this.renderedVersion;

        /// <summary>
        /// Resets the Versions to default state.
        /// </summary>
        internal void Reset()
        {
            this.selector = latest;
            this.version = null;
        }

        internal Versions()
        {
            this.Reset();
        }
    }

    public abstract class CanWrite
    {
        /// <summary>
        /// Gets the WriterSelector for internal use.
        /// </summary>
        private Func<TextWriter> WriterSelector { get; }

        /// <summary>
        /// Gets the selected Writer for the object.
        /// </summary>
        /// <see cref="WriterSelector"/>
        protected TextWriter Writer => this.WriterSelector.Invoke();

        /// <summary>
        /// Returns the <see cref="Console.Out"> for private use.
        /// </summary>
        /// <returns></returns>
        /// <see cref="Console.Out"/>
        private static TextWriter DefaultWriterSelector() => Console.Out;

        protected CanWrite(Func<TextWriter> writerSelector)
        {
            this.WriterSelector = writerSelector ?? DefaultWriterSelector;
        }
    }

    public class OptionsParser : CanWrite
    {
        private AssetManager CurrentAssets { get; }

        private Help? help { get; set; }

        internal bool ShowHelp => this.help == show;

        internal NoPause? pause { get; private set; } = NoPause.pause;

        internal bool ShouldPause => this.pause == NoPause.pause;

        internal Dry? dry { get; private set; }

        internal bool IsDry => this.dry == Dry.dry;

        // TODO: TBD: so insider bits should be refactored...
        // TODO: TBD: in fact I'm not sure that's also not part of a key...
        internal Insider insider { get; private set; } = default;

        internal All? all { get; private set; }

        internal bool DownloadAll => this.all == All.all;

        private Target? target { get; set; }

        private Architecture? arch { get; set; }

        private Build? build { get; set; }

        internal (Target? t, Build? b, Architecture? a) Filter => (t: this.target, b: this.build, a: this.arch);

        internal bool HasTarget => this.target != null;

        internal bool HasArch => this.arch != null;

        internal bool HasBuild => this.build != null;

        private static Assembly programAssy { get; } = typeof(Program).Assembly;

        private static string programFileName { get; } = Path.GetFileName(programAssy.Location);

        private string _helpSum;

        private string HelpSum => this._helpSum ?? (this._helpSum = RenderHelpSummary(programFileName));

        private string[] HelpOpts { get; }
        private string[] TargetOpts { get; }
        private string[] ArchOpts { get; }
        private string[] BuildOpts { get; }
        private string[] AllOpts { get; }
        private string[] DryOpts { get; }
        private string[] InsiderOpts { get; }
        private string[] VersionOpts { get; }
        private string[] NoPauseOpts { get; }
        private string[] CodeVersionOpts { get; }
        private string[] MacOsOpts { get; }
        private string[] LatestVersionOpts { get; }

        private string[] TargetVals { get; }
        private string[] ArchVals { get; }
        private string[] BuildVals { get; }
        private string[] CodeVersionVals { get; }
        private string[] DefaultVals { get; }

        internal Versions Versions { get; } = new Versions();

        ///// <summary>
        ///// Gets the valid combinations of <see cref="Target"/> and corresponding
        ///// <see cref="Build"/>.
        ///// </summary>
        //private IDictionary<Target, Build[]> BuildsForTarget { get; }

        ///// <summary>
        ///// Gets the valid combinations of <see cref="Architecture"/> corresponding
        ///// with the <see cref="Target"/> and <see cref="Build"/> pairs.
        ///// </summary>
        //private IDictionary<(Target, Build), Architecture?[]> ArchesForTargetBuild { get; }

        internal OptionsParser(AssetManager assets)
            : this(assets, null)
        {
        }

        internal OptionsParser(AssetManager assets, Func<TextWriter> writerSelector)
            : base(writerSelector)
        {
            this.CurrentAssets = assets;

            string OnRenderValue<T>(T value) => $"{value}";

            this.TargetVals = Range(darwin, linux, win32).Select(OnRenderValue).ToArray();
            this.ArchVals = Range(x64, x86, arm, arm64, universal).Select(OnRenderValue).ToArray();
            this.BuildVals = Range(user, system, archive, deb, rpm, snap).Select(OnRenderValue).ToArray();
            this.CodeVersionVals = Range(nameof(CodeVersion.latest), string.Join(nameof(CodeVersion.version).ToUpper(), angleBrackets.ToArray())).ToArray();
            this.DefaultVals = Range<string>().ToArray();

            static IEnumerable<string> GetTypeBasedOptions<T>(params int?[] lengths)
            {
                var type = typeof(T);
                var type_Name = type.Name;
                return lengths.Select(length => length ?? type_Name.Length)
                    .Select(length => type_Name.Substring(0, length));
            }

            static string DressOptionPunctuation(string opt)
            {
                IEnumerable<char> OnSelectManyOpt(char ch)
                {
                    if (char.IsUpper(ch))
                    {
                        yield return hyp;
                    }
                    yield return ch;
                }

                opt = opt.SelectMany(OnSelectManyOpt).Aggregate(string.Empty, (g, ch) => g + ch).ToLower();

                // Including the Hyp.
                return opt.Length == 2 ? opt : $"{hyp}{opt}";
            }

            int? selectAll = null;

            this.HelpOpts = GetTypeBasedOptions<Help>(selectAll).Select(DressOptionPunctuation).ToArray();
            this.TargetOpts = GetTypeBasedOptions<Target>(selectAll, 1).Select(DressOptionPunctuation).ToArray();
            this.ArchOpts = GetTypeBasedOptions<Architecture>(4, 1).Select(DressOptionPunctuation).ToArray();
            this.BuildOpts = GetTypeBasedOptions<Build>(selectAll, 1).Select(DressOptionPunctuation).ToArray();
            this.AllOpts = GetTypeBasedOptions<All>(selectAll).Select(DressOptionPunctuation).ToArray();
            this.DryOpts = GetTypeBasedOptions<Dry>(selectAll).Select(DressOptionPunctuation).ToArray();
            this.InsiderOpts = GetTypeBasedOptions<Insider>(selectAll, 1).Select(DressOptionPunctuation).ToArray();
            this.VersionOpts = GetTypeBasedOptions<Version>(selectAll, 1).Select(DressOptionPunctuation).ToArray();
            this.NoPauseOpts = GetTypeBasedOptions<NoPause>(selectAll).Select(DressOptionPunctuation).ToArray();

            this.MacOsOpts = Range("--macos").ToArray();
            this.LatestVersionOpts = Range("-L", "--latest-version").ToArray();

            // TODO: TBD: there's probably a pattern here we can factor to a method, at least...
            this.CodeVersionOpts = Range($"{hyp}{hyp}{CodeVersion.code}{hyp}{CodeVersion.version}"
                , $"{hyp}{CodeVersion.code.ToString().First()}{CodeVersion.version.ToString().First()}").ToArray();
        }

        private string RenderHelpSummary(string fileName)
        {
            static string RenderFlagValues<K, V>(K key, params V[] values)
                => $"--{key} {string.Join($"{pipe}", values.Select(x => $"{x}"))}";

            const string target = nameof(this.target);
            const string build = nameof(this.build);
            const string arch = nameof(this.arch);

            return $@"Provides a command line programmatic view into the Code download web page matrix. The options describe the values for each option, but not all combinations are valid. The following combinations work for each area of the matrix.

  {fileName} {RenderFlagValues(target, win32)} {RenderFlagValues(build, user, system, archive)} {RenderFlagValues(arch, x64, x86, arm64)}
  {fileName} {RenderFlagValues(target, linux)} {RenderFlagValues(build, deb, rpm, archive)} {RenderFlagValues(arch, x64, x86, arm, arm64)}
  {fileName} {RenderFlagValues(target, linux)} {RenderFlagValues(build, snap)}
  {fileName} {RenderFlagValues(target, darwin)} [{RenderFlagValues(arch, universal, arm64)}]

There is only one download for {nameof(macOS)} {darwin} {macOS}+, --{arch} --{build} are both ignored.
{RenderFlagValues(arch, arm)} is assumed to be {RenderFlagValues(arch, arm64)} when {RenderFlagValues(target, win32)} is specified.
--{arch} is ignored when {RenderFlagValues(target, linux)} {RenderFlagValues(build, snap)} is specified.
{RenderFlagValues(arch, x86)} is assumed to be {RenderFlagValues(arch, x64)} when {RenderFlagValues(target, linux)} is specified.

Based on the {codeDownloadUri} web page and informed by the {codeGithubIssueUri} code github issue.";
        }

        private bool TryShowVersion()
        {
            this.Writer.WriteLine($"{programFileName} {programAssy.GetName().Version}");
            return false;
        }

        private bool TryPresentHelp(string summary, params (string[] flags, string description, string[] values)[] opts)
        {
            this.Writer.WriteLine();

            void PresentVersion(string label, Version value, Version @default)
            {
                this.Writer.Write($"{label} {value}");

                if (value == @default)
                {
                    this.Writer.WriteLine();
                }
                else
                {
                    this.Writer.Write(" ");
                    this.Writer.WriteLine(string.Join(string.Join($"{colon} ", nameof(@default), $"{@default}"), parens.ToArray()));
                }
            }

            PresentVersion("Latest version", Versions.latestVersion, Versions.defaultLatestVersion);
            PresentVersion("Macintosh OS X version", Versions.macOS, Versions.defaultMacOS);

            //const int colWidth = 4;
            const string flagPrefix = "  ";

            var maxFlagWidth = opts.Max(x => x.flags.Sum(y => y.Length + flagPrefix.Length) + (x.flags.Length - 1));
            //var flagsWidth = ((int)(maxFlagWidth / colWidth) + 1) * colWidth;

            this.Writer.WriteLine();

            foreach (var line in summary.SplitMultiline())
            {
                this.Writer.WriteLine(line);
            }

            this.Writer.WriteLine();

            foreach (var (flags, description, values) in opts)
            {
                this.Writer.WriteLine(string.Join($"{comma}", flags.Select(x => flagPrefix + x)));

                foreach (var line in description.SplitMultiline(maxFlagWidth))
                {
                    this.Writer.WriteLine(line.PadLeft(maxFlagWidth + line.Length));
                }

                // TODO: TBD: may need to do a similar thing here concerning fitting the values into the line width.
                if (values.Any())
                {
                    var renderedValues = $"{nameof(values).ToUpper()}: {string.Join(", ", values)}";
                    this.Writer.WriteLine(renderedValues.PadLeft(maxFlagWidth + renderedValues.Length));
                }
            }

            return false;
        }

        private bool TryPresentHelpOnReturn(Versions currentVersions) => TryPresentHelp(
            this.HelpSum
            , (this.HelpOpts, "--x, what you are reading now.", this.DefaultVals)
            , (this.TargetOpts, "--x VALUE, the targets.", this.TargetVals)
            , (this.ArchOpts, "--x VALUE, the architectures.", this.ArchVals)
            , (this.BuildOpts, "--x VALUE, the builds.", this.BuildVals)
            , (this.AllOpts, "--x, get all targets, architectures, and builds", this.DefaultVals)
            , (this.DryOpts, "--x, performs the features in dry run scenarios", this.DefaultVals)
            , (this.CodeVersionOpts, "--x VALUE, specify the Code version, or latest", this.CodeVersionVals)
            , (this.InsiderOpts, $"--x, whether to get the {nameof(insider)}. Defaults to {nameof(stable)} absent.", this.DefaultVals)
            , (this.VersionOpts, "--x, whether to show the downloader version", this.DefaultVals)
            , (this.MacOsOpts, "--x VALUE, permits users to train downloader with the macOS version", this.DefaultVals)
            , (this.LatestVersionOpts, "--x VALUE, permits users to train downloader with the latest version", this.DefaultVals)
        );

        private void ReportDryRun(int i, params string[] args)
        {
            if (!this.IsDry)
            {
                return;
            }

            this.Writer.WriteLine($"{nameof(Dry)}: {nameof(args)}: {A(args)}, {nameof(i)}: {i}");

            void ReportNameValuePair(string name, object value) => this.Writer.WriteLine(
                $"{RenderNameObjectPairs((name, value)).Single()}"
            );

            ReportNameValuePair(nameof(this.dry), this.dry);
            ReportNameValuePair(nameof(this.all), this.all);
            ReportNameValuePair(nameof(this.pause), this.pause);
            ReportNameValuePair(nameof(this.target), this.target);
            ReportNameValuePair(nameof(this.arch), this.arch);
            ReportNameValuePair(nameof(this.build), this.build);
            ReportNameValuePair(nameof(this.insider), this.insider);

            ReportNameValuePair(nameof(this.HelpOpts), A(this.HelpOpts));
            ReportNameValuePair(nameof(this.TargetOpts), A(this.TargetOpts));
            ReportNameValuePair(nameof(this.ArchOpts), A(this.ArchOpts));
            ReportNameValuePair(nameof(this.BuildOpts), A(this.BuildOpts));
            ReportNameValuePair(nameof(this.AllOpts), A(this.AllOpts));
            ReportNameValuePair(nameof(this.DryOpts), A(this.DryOpts));
            ReportNameValuePair(nameof(this.CodeVersionOpts), A(this.CodeVersionOpts));
            ReportNameValuePair(nameof(this.InsiderOpts), A(this.InsiderOpts));
            ReportNameValuePair(nameof(this.VersionOpts), A(this.VersionOpts));

            ReportNameValuePair(nameof(this.TargetVals), A(this.TargetVals));
            ReportNameValuePair(nameof(this.ArchVals), A(this.ArchVals));
            ReportNameValuePair(nameof(this.BuildVals), A(this.BuildVals));
            ReportNameValuePair(nameof(this.CodeVersionVals), A(this.CodeVersionVals));

            ReportNameValuePair($"{nameof(this.Versions)}.{nameof(Versions.show)}", $"{this.Versions.show}".ToLower());
            ReportNameValuePair($"{nameof(this.Versions)}.{nameof(Versions.version)}", this.Versions.version);
            ReportNameValuePair($"{nameof(Versions)}.{nameof(Versions.macOS)}", Versions.macOS);
            ReportNameValuePair($"{nameof(Versions)}.{nameof(Versions.latestVersion)}", Versions.latestVersion);
        }

        ///// <summary>
        ///// We will start from the valid set of download specifications. We will select subsets
        ///// of these depending on the command line arguments that we are given.
        ///// </summary>
        //private IEnumerable<(Target t, Build b, Architecture? a)> AllDownloadSpecs { get; }

        internal void OnShowHelp() => OnShowHelp(show);

        private void OnShowHelp(Help value)
        {
            this.help = value;
            TryShowVersion();
            TryPresentHelpOnReturn(this.Versions);
        }

        public bool TryParseArguments(params string[] args)
        {
            string GetArgument(int index) => index >= args.Length ? string.Empty : args.ElementAt(index);

            // Reset the optional values to nominal defaults.
            this.pause = NoPause.pause;
            this.help = default;
            this.all = default;
            this.dry = default;
            this.target = default;
            this.arch = default;
            this.build = default;
            this.insider = default;
            this.Versions.Reset();

            int i;

            for (i = 0; i < args.Length; i++)
            {
                var arg = GetArgument(i);

                if (this.HelpOpts.Contains(arg))
                {
                    this.help = show;
                    continue;
                }

                if (this.NoPauseOpts.Contains(arg))
                {
                    this.pause = null;
                    continue;
                }

                // TODO: TBD: we could possibly discover unary and binary (or more) command line parameters here...

                // In generally the order in which you would review the downloads page.
                // https://code.visualstudio.com/Download
                if (this.TargetOpts.Contains(arg))
                {
                    this.target = GetArgument(++i).ParseEnum<Target>();
                    continue;
                }

                if (this.ArchOpts.Contains(arg))
                {
                    this.arch = GetArgument(++i).ParseEnum<Architecture>();
                    continue;
                }

                if (this.BuildOpts.Contains(arg))
                {
                    this.build = GetArgument(++i).ParseEnum<Build>();
                    continue;
                }

                if (this.InsiderOpts.Contains(arg))
                {
                    this.insider = Insider.insider;
                    continue;
                }

                if (this.AllOpts.Contains(arg))
                {
                    this.all = All.all;
                    continue;
                }

                if (this.DryOpts.Contains(arg))
                {
                    this.dry = Dry.dry;
                    continue;
                }

                if (this.MacOsOpts.Contains(arg))
                {
                    Versions.ReplaceMacOS(GetArgument(++i));
                    continue;
                }

                if (this.LatestVersionOpts.Contains(arg))
                {
                    Versions.ReplaceLatestVersion(GetArgument(++i));
                    continue;
                }

                if (this.VersionOpts.Contains(arg))
                {
                    this.Versions.show = true;
                    return TryShowVersion();
                }

                if (this.CodeVersionOpts.Contains(arg))
                {
                    arg = GetArgument(++i);
                    if (Version.TryParse(arg, out var v))
                    {
                        this.Versions.version = v;
                    }
                    else if (arg.ParseEnum<CodeVersion>().HasValue)
                    {
                        // We make the assumption, the only other CodeVersion argument we accept is the latest.
                        this.Versions.selector = latest;
                    }
                    continue;
                }
            }

            ReportDryRun(i, args);

            /* Arguments are considered parsed successfully when:
             * 1. Whether to show help after (arguments have parsed).
             * 2. Arguments processed successfully
             * 3. Assets properly discovered
             */

            // Requesting help short circuits, should not evaluate whether assets are discovered.
            if (this.help == show)
            {
                return false;
            }

            return i == args.Length
                && this.CurrentAssets.AreDiscovered(this.Writer);
        }
    }

    public class DownloadProcessor : CanWrite
    {
        private AssetManager CurrentAssets { get; }

        private OptionsParser CurrentOptions { get; }

        internal Versions CurrentVersions { get; } = new Versions();

        // TODO: TBD: probably do not need a string Version property...
        // TODO: TBD: we will relay the CurrentVersions instance to the strategy instead...
        private string Version => this.CurrentVersions.selector == latest ? $"{latest}" : $"{version}";

        /// <br/>win32+system+x86+version => VSCodeUserSetup-ia32-major.minor.patch.exe
        /// <br/>win32+user+x86+version => VSCodeSetup-ia32-major.minor.patch.exe
        /// <br/>win32+archive+x86+version => VSCode-win32-ia32-major.minor.patch.zip
        /// <br/>
        /// <br/>win32+system+x64+version => VSCodeSetup-x64-major.minor.patch.exe
        /// <br/>win32+user+x64+version => VSCodeUserSetup-x64-major.minor.patch.exe
        /// <br/>win32+archive+x64+version => VSCode-win32-x64-major.minor.patch.zip
        /// <br/>
        /// <br/>win32+system+arm64+version => VSCodeSetup-arm64-major.minor.patch.exe
        /// <br/>win32+user+arm64+version => VSCodeUserSetup-arm64-major.minor.patch.exe
        /// <br/>win32+archive+arm64+version => VSCode-win32-arm64-major.minor.patch.zip
        /// <br/>
        /// <br/>linux+deb+x64+version => code_major.minor.version-stable_amd64.deb
        /// <br/>linux+rpm+x64+version => code-major.minor.version-stable.el7.x86_64.rpm
        /// <br/>linux+archive+x64+version => code-major.minor.version-x64-stable.tar.gz
        /// <br/>
        /// <br/>linux+deb+arm+version => code_major.minor.version-stable_armhf.deb
        /// <br/>linux+rpm+arm+version => code-major.minor.version-stable.el7.armv7hl.rpm
        /// <br/>linux+archive+arm+version => code-major.minor.version-armhf-stable.tar.gz
        /// <br/>
        /// <br/>linux+deb+arm64+version => code_major.minor.version-stable_arm64.deb
        /// <br/>linux+rpm+arm64+version => code-major.minor.version-stable.el7.aarch64.rpm
        /// <br/>linux+archive+arm64+version => code-major.minor.version-arm64-stable.tar.gz
        /// <br/>
        /// <br/>darwin+version+stable => VSCode-darwin-major.minor.patch-stable.zip

        // TODO: TBD: so far with support for "stable" convention...
        // TODO: TBD: add capability for different conventions, stable, insider, etc...
        private static IEnumerable<DownloadStrategy> GetStrategies(OptionsParser op)
        {
            // win32+system+x86+version => VSCodeUserSetup-ia32-major.minor.patch.exe
            // win32+user+x86+version => VSCodeSetup-ia32-major.minor.patch.exe
            // win32+archive+x86+version => VSCode-win32-ia32-major.minor.patch.zip
            yield return Strategy(op, 2, (win32, system, x86))
                .Directories(Element.Windows, Element.x86).Extensions(Element.exe)
                // TODO: TBD: reduce the number of dictionaries, collections...
                // TODO: TBD: and refocus in algo terms...
                .Convention(Element.VSCode, Element.Setup, Element.ia32, Element.insider, Element.version)
                // https://update.code.visualstudio.com/major.minor.patch/win32/stable
                .Url(Range(Element.win32))
                ;

            yield return Strategy(op, 3, (win32, user, x86))
                .Directories(Element.Windows, Element.x86).Extensions(Element.exe)
                .Convention(Element.VSCode, Element.User, Element.Setup, Element.ia32, Element.insider, Element.version)
                // https://update.code.visualstudio.com/major.minor.patch/win32-user/stable
                .Url(Range(Element.win32, Element.user))
                ;

            yield return Strategy(op, (win32, archive, x86))
                .Directories(Element.Windows, Element.x86).Extensions(Element.zip)
                .Convention(Element.VSCode, Element.win32, Element.ia32, Element.insider, Element.version)
                // https://update.code.visualstudio.com/major.minor.patch/win32-archive/stable
                .Url(Range(Element.win32, Element.archive))
                ;

            // win32+system+x64+version => VSCodeSetup-x64-major.minor.patch.exe
            // win32+user+x64+version => VSCodeUserSetup-x64-major.minor.patch.exe
            // win32+archive+x64+version => VSCode-win32-x64-major.minor.patch.zip
            yield return Strategy(op, 2, (win32, system, x64))
                .Directories(Element.Windows, Element.x64).Extensions(Element.exe)
                .Convention(Element.VSCode, Element.Setup, Element.x64, Element.insider, Element.version)
                // https://update.code.visualstudio.com/major.minor.patch/win32-x64/stable
                .Url(Range(Element.win32, Element.x64))
                ;

            yield return Strategy(op, 3, (win32, user, x64))
                .Directories(Element.Windows, Element.x64).Extensions(Element.exe)
                .Convention(Element.VSCode, Element.User, Element.Setup, Element.x64, Element.insider, Element.version)
                // https://update.code.visualstudio.com/major.minor.patch/win32-x64-user/stable
                .Url(Range(Element.win32, Element.x64, Element.user))
                ;

            yield return Strategy(op, (win32, archive, x64))
                .Directories(Element.Windows, Element.x64).Extensions(Element.zip)
                .Convention(Element.VSCode, Element.win32, Element.x64, Element.insider, Element.version)
                // https://update.code.visualstudio.com/major.minor.patch/win32-x64-archive/stable
                .Url(Range(Element.win32, Element.x64, Element.archive))
                ;

            // win32+system+arm64+version => VSCodeSetup-arm64-major.minor.patch.exe
            // win32+user+arm64+version => VSCodeUserSetup-arm64-major.minor.patch.exe
            // win32+archive+arm64+version => VSCode-win32-arm64-major.minor.patch.zip
            yield return Strategy(op, 2, (win32, system, arm64))
                .Directories(Element.Windows, Element.arm64).Extensions(Element.exe)
                .Convention(Element.VSCode, Element.Setup, Element.arm64, Element.insider, Element.version)
                // https://update.code.visualstudio.com/major.minor.patch/win32-arm64/stable
                .Url(Range(Element.win32, Element.arm64))
                ;

            yield return Strategy(op, 3, (win32, user, arm64))
                .Directories(Element.Windows, Element.arm64).Extensions(Element.exe)
                .Convention(Element.VSCode, Element.User, Element.Setup, Element.arm64, Element.insider, Element.version)
                // https://update.code.visualstudio.com/major.minor.patch/win32-arm64-user/stable
                .Url(Range(Element.win32, Element.arm64, Element.user))
                ;

            yield return Strategy(op, (win32, archive, arm64))
                .Directories(Element.Windows, Element.arm64).Extensions(Element.zip)
                .Convention(Element.VSCode, Element.win32, Element.arm64, Element.insider, Element.version)
                // https://update.code.visualstudio.com/major.minor.patch/win32-arm64-archive/stable
                .Url(Range(Element.win32, Element.arm64, Element.archive))
                ;

            // linux+deb+x64+version => code_major.minor.version-stable_amd64.deb
            // linux+rpm+x64+version => code-major.minor.version-stable.el7.x86_64.rpm
            // linux+archive+x64+version => code-major.minor.version-x64-stable.tar.gz
            yield return Strategy(op, (linux, deb, x64))
                .Directories(Element.Linux, Element.x64)
                .Extensions(Element.amd64, Element.deb)
                .Convention(underscore, Element.code, Element.insider, Element.version)
                // https://update.code.visualstudio.com/major.minor.patch/linux-deb-x64/stable
                .Url(Range(Element.linux, Element.deb, Element.x64))
                ;

            yield return Strategy(op, (linux, rpm, x64))
                .Directories(Element.Linux, Element.x64)
                .Extensions(Element.el7, Element.x86_64, Element.rpm)
                .Convention(underscore, Element.code, Element.insider, Element.version)
                // https://update.code.visualstudio.com/major.minor.patch/linux-rpm-x64/stable
                .Url(Range(Element.linux, Element.rpm, Element.x64))
                ;

            yield return Strategy(op, (linux, archive, x64))
                .Directories(Element.Linux, Element.x64)
                .Extensions(Element.tar, Element.gz)
                .Convention(underscore, Element.code, Element.insiderOrNull, Element.x64, Element.version)
                // https://update.code.visualstudio.com/major.minor.patch/linux-rpm-x64/stable
                .Url(Range(Element.linux, Element.x64))
                ;

            // linux+deb+arm+version => code_major.minor.version-stable_armhf.deb
            // linux+rpm+arm+version => code-major.minor.version-stable.el7.armv7hl.rpm
            // linux+archive+arm+version => code-major.minor.version-armhf-stable.tar.gz
            yield return Strategy(op, (linux, deb, arm))
                .Directories(Element.Linux, Element.arm)
                .Extensions(Element.el7, Element.armhf, Element.deb)
                .Convention(underscore, Element.code, Element.insider, Element.version)
                // https://update.code.visualstudio.com/major.minor.patch/linux-deb-armhf/stable
                .Url(Range(Element.linux, Element.deb, Element.armhf))
                ;

            yield return Strategy(op, (linux, rpm, arm))
                .Directories(Element.Linux, Element.arm)
                .Extensions(Element.el7, Element.armv7hl, Element.rpm)
                .Convention(underscore, Element.code, Element.insider, Element.version)
                // https://update.code.visualstudio.com/major.minor.patch/linux-rpm-armhf/stable
                .Url(Range(Element.linux, Element.rpm, Element.armhf))
                ;

            yield return Strategy(op, (linux, archive, arm))
                .Directories(Element.Linux, Element.arm)
                .Extensions(Element.armhf, Element.tar, Element.gz)
                .Convention(underscore, Element.code, Element.insider, Element.version)
                // https://update.code.visualstudio.com/major.minor.patch/linux-armhf/stable
                .Url(Range(Element.linux, Element.armhf))
                ;

            // linux+deb+arm64+version => code_major.minor.version-stable_arm64.deb
            // linux+rpm+arm64+version => code-major.minor.version-stable.el7.aarch64.rpm
            // linux+archive+arm64+version => code-major.minor.version-arm64-stable.tar.gz
            yield return Strategy(op, (linux, deb, arm64))
                .Directories(Element.Linux, Element.arm64)
                .Extensions(Element.deb)
                .Convention(underscore, Element.code, Element.insider, Element.version, Element.arm64)
                // code_insider_major.minor.patch_arm64.deb
                .Url(Range(Element.linux, Element.deb, Element.arm64))
                ;

            yield return Strategy(op, (linux, rpm, arm64))
                .Directories(Element.Linux, Element.arm64)
                .Extensions(Element.el7, Element.aarch64, Element.rpm)
                .Convention(underscore, Element.code, Element.insider, Element.version)
                // code_insider_major.minor.patch.el7.aarch64.rpm
                .Url(Range(Element.linux, Element.rpm, Element.arm64))
                ;

            yield return Strategy(op, (linux, archive, arm64))
                .Directories(Element.Linux, Element.arm64)
                .Extensions(Element.tar, Element.gz)
                .Convention(underscore, Element.code, Element.insider, Element.version, Element.arm64)
                // code_insider_major.minor.patch_arm64.tar.gz
                .Url(Range(Element.linux, Element.arm64))
                ;

            // linux+snap => code-stable-major.minor.patch.snap
            // linux+archive+snap => code-stable-major.minor.patch.snap
            yield return Strategy(op, (linux, snap, null))
                .Directories(Element.Linux, Element.snap).Extensions(Element.snap)
                .Convention(Element.code, Element.insider, Element.version)
                // code_insider_major.minor.patch.snap
                .Url(Range(Element.linux, Element.snap, Element.x64))
                ;

            // darwin+universal+version+stable => VSCode-darwin-universal-major.minor.patch-stable.zip
            yield return Strategy(op, (darwin, archive, universal))
                .Directories(Element.macOS, Element.universal, Element.versionMacOS).Extensions(Element.zip)
                .Convention(Element.VSCode, Element.darwin, Element.universal, Element.insider, Element.version)
                // VSCode-darwin-universal-insider-major.minor.patch.zip
                .Url(Range(Element.darwin, Element.universal))
                ;

            // darwin+version+stable => VSCode-darwin-major.minor.patch-stable.zip
            yield return Strategy(op, (darwin, archive, null))
            // Considered whether to change the path here, reviewed; reconsidered, i.e. will leave it alone
                .Directories(Element.macOS, Element.versionMacOS).Extensions(Element.zip)
                .Convention(Element.VSCode, Element.darwin, Element.insider, Element.version)
                // VSCode-darwin-insider-major.minor.patch.zip
                .Url(Range(Element.darwin))
                ;

            // darwin+arm64+version+stable => VSCode-darwin-arm64-major.minor.patch-stable.zip
            yield return Strategy(op, (darwin, archive, arm64))
                .Directories(Element.macOS, Element.arm64, Element.versionMacOS).Extensions(Element.zip)
                .Convention(Element.VSCode, Element.darwin, Element.arm64, Element.insider, Element.version)
                // VSCode-darwin-arm64-insider-major.minor.patch.zip
                .Url(Range(Element.darwin, Element.arm64))
                ;
        }

        private IDictionary<(Target t, Build b, Architecture? a), DownloadStrategy> _strategies;

        // TODO: TBD: the strategies dictionary being what it is, we might consider these keys as being the "specs" themselves, actually...
        // TODO: TBD: will save that factoring for a subsequent commitment...
        /// <summary>
        /// Gets the Strategies for use during Download processing. It is critical that we
        /// postpone mending the Strategies until after command line argument parsing has
		/// finished. This is because mission critical details like Code and MacOS versions
		/// may be trained during argument parsing.
        /// </summary>
        private IDictionary<(Target t, Build b, Architecture? a), DownloadStrategy> Strategies => this._strategies ?? (
            this._strategies = GetStrategies(this.CurrentOptions).ToDictionary(x => x.Spec)
        );

        private IEnumerable<(Target t, Build b, Architecture? a)> GetSelectedSpecifications(OptionsParser op) =>
            this.GetSelectedSpecifications(op.Filter);

        private IEnumerable<(Target t, Build b, Architecture? a)> GetSelectedSpecifications((Target? t, Build? b, Architecture? a) filter)
        {
            // We work with all three de-con fields, TARGET, BUILD, ARCH
            var (t, b, a) = filter;

            var op = this.CurrentOptions;

            if (op.IsDry)
            {
                IEnumerable<(string name, string value)> GetRenderedFilterTuple()
                {
                    yield return (nameof(t), RenderObjectOrNull(t));
                    yield return (nameof(b), RenderObjectOrNull(b));
                    yield return (nameof(a), RenderObjectOrNull(a));
                }

                this.Writer.WriteLine($"{nameof(Dry)}: {nameof(GetSelectedSpecifications)}"
                    + $"{T(parens, (nameof(filter), T(GetRenderedFilterTuple().ToArray())))}");
            }

            if (t == win32 && a == arm)
            {
                a = arm64;
            }

            // Do a little screening of the command line arguments ensuring optimum alignment.
            if (t == linux && (a == x86 || (a == null && b == snap)))
            {
                // TODO: TBD: for the moment it includes snap... but we do not think it should...
                a = x64;
            }

            if (t == darwin && b != archive && a != null)
            {
                b = archive;
                a = null;
            }

            bool OnSelectSpecification((Target t, Build b, Architecture? a) x) =>
                (t == null || t == x.t)
                    && (b == null || b == x.b)
                    && (a == null || a == x.a)
                ;

            return this.Strategies.Keys.Where(OnSelectSpecification).ToArray();
        }

        internal DownloadProcessor(AssetManager assets, OptionsParser options)
            : this(assets, options, null)
        {
        }

        internal DownloadProcessor(AssetManager assets, OptionsParser options, Func<TextWriter> writerSelector)
            : base(writerSelector)
        {
            this.CurrentAssets = assets;
            this.CurrentOptions = options;
        }

        private IEnumerable<(Target t, Build b, Architecture? a)> _selectedSpecifications;

        /// <summary>
        /// Gets the DownloadSpecs for use throughout the tool. This is calculated just once
        /// per session, so be careful of the timing during which it is called. Ensure that
        /// the command line arguments have all been properly parsed by that moment.
        /// </summary>
        internal IEnumerable<(Target t, Build b, Architecture? a)> SelectedSpecifications => this._selectedSpecifications ?? (
            this._selectedSpecifications = this.GetSelectedSpecifications(this.CurrentOptions.Filter)
        );

        /// <summary>
        /// Returns whether there are Any <see cref="SelectedSpecifications"/> items.
        /// </summary>
        /// <returns></returns>
        internal bool TryFilterSpecifications() => this.SelectedSpecifications.Any();

        private string _version;

        /// <summary>
        ///
        /// </summary>
        private string CurrentVersion => this._version ?? (
            this._version = this.CurrentVersions.selector == latest
                ? $"{latest}"
                : $"{this.CurrentVersions.version}"
        );

        private void MakeDirectory(string path)
        {
            var op = this.CurrentOptions;

            if (op.IsDry)
            {
                this.Writer.WriteLine($"{nameof(Dry)}: Making directory '{path}'");
            }
            else
            {
                if (!path.DirectoryExists())
                {
                    this.Writer.WriteLine($"Creating directory '{path}'.");
                }

                Directory.CreateDirectory(path);
            }
        }

        /// <summary>
        /// Tries to Invoke the <see cref="Assets.Wget"/> asset given the <em>uri</em>,
        /// <em>path</em> and <em>fileName</em> <paramref name="scenario"/>, and additional
        /// <paramref name="args"/>.
        /// </summary>
        /// <param name="scenario">The <em>uri</em>, <em>path</em>, <em>fileName</em> Scenario.</param>
        /// <param name="args">Any additional command line arguments.</param>
        /// <returns></returns>
        private bool TryInvokeWget((string uri, string path, string fileName) scenario, params string[] args)
        {
            var op = this.CurrentOptions;

            (string uri, string path, string fileName) = scenario;

            if (op.IsDry)
            {
                this.Writer.WriteLine($"{nameof(Dry)}: {nameof(this.TryInvokeWget)}"
                    + $"{T(parens, (nameof(scenario), T((nameof(uri), Q(uri)), (nameof(path), Q(path)), (nameof(fileName), Q(fileName)))))}");
            }

            var wgetPath = this.CurrentAssets.wgetPath;

            // -O, --output-document, -P, --directory-prefix, in this form.
            args = args.Concat(Range("-O", fileName, "-P", path, uri)).ToArray();

            if (op.IsDry)
            {
                this.Writer.WriteLine($"{nameof(Dry)}: {wgetPath} {string.Join(" ", args)}");
            }
            else
            {
                // TODO: TBD: this is pretty much what a get/move Task should do in this clause...

                /* We think that wget will make the directory, however, files are not landing
                 * in that directory like we might expect. For instance, assuming:
                 *
                 *      X:> cls && get_code.exe --target win32 --arch x64 --dry
                 *
                 * And we expect:
                 *      wget.exe -O VSCode-win32-x64-1.50.1.zip -P 1.50.1\Windows\x64
                 *          https://update.code.visualstudio.com/latest/win32-x64-archive/stable
                 *
                 * Or:
                 *
                 *      wget.exe -O VSCodeUserSetup-x64-1.50.1.exe -P 1.50.1\Windows\x64
                 *          https://update.code.visualstudio.com/latest/win32-x64-user/stable
                 *
                 * Or:
                 *
                 *      wget.exe -O VSCodeSetup-x64-1.50.1.exe -P 1.50.1\Windows\x64
                 *          https://update.code.visualstudio.com/latest/win32-x64/stable
                 *
                 * The directory '1.50.1\Windows\x64' gets created. But the files land in
                 * the working directory. */

                this.MakeDirectory(path);

                this.Writer.WriteLine($"Running: {Path.GetFileName(wgetPath)} {string.Join(" ", args)}");

                // TODO: TBD: so... we are doing these one at a time...
                // TODO: TBD: we might look into increasing the parallelism here...
                var startInfo = new ProcessStartInfo(wgetPath)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Arguments = string.Join(" ", args)
                };

                // TODO: TBD: use concurrent dictionary?
                // TODO: TBD: remove processes from the dictionary as they exit...
                // TODO: TBD: add new scenarios while we still have them...
                // TODO: TBD: and do so until there are no more scenarios...
                // https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.process.exited
                // https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.process.-ctor
                // https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.processstartinfo.arguments
                using (var process = Process.Start(startInfo))
                {
                    process.Exited += delegate { /* TODO: TBD: handle any async responses here... */ };
                    process.EnableRaisingEvents = true;
                    process.WaitForExit();
                }

                try
                {
                    if (fileName.FileExists() && path.DirectoryExists())
                    {
                        this.Writer.WriteLine($"Moving '{fileName}' to '{path}'.");
                        fileName.MoveFile(path.CombinePath(fileName));
                    }
                    else
                    {
                        this.Writer.WriteLine($"Unable to move '{fileName}' to '{path}'.");
                    }
                }
                catch (Exception ex)
                {
                    this.Writer.WriteLine($"Failed to move '{fileName}' to '{path}'.");
                    this.Writer.WriteLine($"{ex.Message}");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Processes the <em>uri</em>, <em>path</em>, <em>fileName</em>
        /// <paramref name="scenario"/>.
        /// </summary>
        /// <param name="scenario">The <em>uri</em>, <em>path</em>, <em>fileName</em> Scenario.</param>
        private void OnProcessScenario((string uri, string path, string fileName) scenario)
        {
            var op = this.CurrentOptions;

            var (uri, path, fileName) = scenario;

            if (op.IsDry)
            {
                this.Writer.WriteLine($"{nameof(Dry)}: {nameof(this.OnProcessScenario)}"
                    + $"{T(parens, (nameof(scenario), T((nameof(uri), Q(uri)), (nameof(path), Q(path)), (nameof(fileName), Q(fileName)))))}");
            }

            TryInvokeWget(scenario);
        }

        private void OnProcessSpecification((Target t, Build b, Architecture? a) spec)
        {
            var op = this.CurrentOptions;

            // Working with all three de-con fields, TARGET, BUILD, ARCH
            var (t, b, a) = spec;

            if (op.IsDry)
            {
                this.Writer.WriteLine($"{nameof(this.OnProcessSpecification)}"
                    + $"{T(parens, (nameof(t), (object)t), (nameof(b), (object)b), (nameof(a), (object)a))}");
            }

            this.OnProcessScenario(this.Strategies[spec].Render(op.Versions));
        }

        public void ProcessDownloadSpecs()
        {
            var op = this.CurrentOptions;

            if (op.IsDry)
            {
                this.Writer.WriteLine($"{nameof(Dry)}: {nameof(ProcessDownloadSpecs)}{parens}, {nameof(op)}.{nameof(this.SelectedSpecifications)}.{nameof(IList.Count)}: {this.SelectedSpecifications.Count()}");
            }

            this.SelectedSpecifications.ToList().ForEach(this.OnProcessSpecification);
        }
    }

    public class DownloadStrategy
    {
        private OptionsParser CurrentOptions { get; }

        /// <summary>
        /// Gets the PrefixCount, that is, how many <see cref="Element"/> bits
        /// contribute to a non-delimited prefix.
        /// </summary>
        internal int PrefixCount { get; } = 1;

        private readonly ICollection<Element> _path = Range<Element>().ToList();

        private readonly ICollection<Element> _extensions = Range<Element>().ToList();

        /// <summary>
        /// Gets or Sets the <see name="_conventions"/> delimiter, default <see name="hyp"/>.
        /// </summary>
        /// <see name="hyp"/>
        /// <see name="underscore"/>
        private char Delim { get; set; } = hyp;

        /// <summary>
        /// The base <see cref="_urls"/> elements.
        /// </summary>
        /// <see cref="Element.update"/>
        /// <see cref="Element.code"/>
        /// <see cref="Element.visualstudio"/>
        /// <see cref="Element.com"/>
        private static readonly IEnumerable<Element> _urlBase = Range(
            Element.update, Element.code, Element.visualstudio, Element.com).ToArray();
 
        /// <summary>
        /// Urls, meaning the <see cref="slash"/> delimited segments of the download routing
        /// path. The base address is always the same so we do not specify that outside of
        /// the algorithm itself. As far as we can determine, each phrase <see cref="Element"/>
        /// is delimited by <see cref="hyp"/>, whereas the root phrase is always delimited by
        /// <see cref="dot"/>.
        /// </summary>
        /// <remarks>Algo wise and by convention, we know the default values to be true. Which
        /// allows the strategy specifications to focus on the targets, builds, and architectures
        /// only.</remarks>
        private readonly IList<Element[]> _urls = Range(
            Range(Element.version, Element.insiderOrNull)
            , Range(Element.insider)).Select(x => x.ToArray()).ToList();

        /// <summary>
        /// Conventions, as in file naming conventions.
        /// </summary>
        private readonly ICollection<Element> _conventions = Range<Element>().ToList();

        private DownloadStrategy AddElements(ICollection<Element> collection, params Element[] values)
        {
            values.ToList().ForEach(collection.Add);
            return this;
        }

        /// <summary>
        /// Sets the Directories in a fluent manner.
        /// </summary>
        /// <param name="elements"></param>
        /// <returns></returns>
        internal DownloadStrategy Directories(params Element[] elements) => this.AddElements(this._path, elements);

        /// <summary>
        /// Sets the Extensions in a fluent manner.
        /// </summary>
        /// <param name="elements"></param>
        /// <returns></returns>
        internal DownloadStrategy Extensions(params Element[] elements) => this.AddElements(this._extensions, elements);

        /// <summary>
        /// Establishes a specified Url pattern given the <paramref name="segments"/>,
        /// potentially replacing the conventional segments.
        /// </summary>
        /// <param name="specify">Meaning to override the convention.</param>
        /// <param name="segments">The <see cref="IEnumerable{T}"/> <see cref="Element"/>
        /// Segments.</param>
        /// <returns></returns>
        internal DownloadStrategy Url(bool specify, params IEnumerable<Element>[] segments)
        {
            if (specify)
            {
                // Specify replaces the Strategy convention with the Elements.
                this._urls.Clear();
                segments.Select(x => x.ToArray()).ToList().ForEach(this._urls.Add);
                return this;
            }

            return Url(segments);
        }

        /// <summary>
        /// Establishes a conventional Url pattern given the <paramref name="segments"/>,
        /// allowing for default opening <em>Version</em> and <em>Insider</em> segments.
        /// </summary>
        /// <param name="segments">The <see cref="IEnumerable{T}"/> <see cref="Element"/>
        /// <returns></returns>
        internal DownloadStrategy Url(params IEnumerable<Element>[] segments)
        {
            void Insert(Element[] item) => this._urls.Insert(this._urls.Count - 1, item);
            segments.Select(x => x.ToArray()).ToList().ForEach(Insert);
            return this;
        }

        /// <summary>
        /// Sets the Convention in a fluent manner, default <see cref="Delim"/>
        /// <see cref="hyp"/>.
        /// </summary>
        /// <param name="elements"></param>
        /// <returns></returns>
        internal DownloadStrategy Convention(params Element[] elements) => this.Convention(hyp, elements);

        /// <summary>
        /// Sets the Convention in a fluent manner.
        /// </summary>
        /// <param name="delim"></param>
        /// <param name="elements"></param>
        /// <returns></returns>
        internal DownloadStrategy Convention(char delim, params Element[] elements)
        {
            this.Delim = delim;
            return this.AddElements(this._conventions, elements);
        }

        /// <summary>
        /// Gets the Descriptor specification.
        /// </summary>
        internal (Target t, Build b, Architecture? a) Spec { get; }

        /// <summary>
        /// Creates a new <see cref="DownloadStrategy"/> instance.
        /// </summary>
        /// <param cref="op"></param>
        /// <param cref="spec"></param>
        /// <returns></returns>
        internal static DownloadStrategy Strategy(OptionsParser op, (Target t, Build b, Architecture? a) spec) => new DownloadStrategy(op, spec);

        /// <summary>
        /// Creates a new <see cref="DownloadStrategy"/> instance.
        /// </summary>
        /// <param cref="op"></param>
        /// <param cref="prefixCount"></param>
        /// <param cref="spec"></param>
        /// <returns></returns>
        internal static DownloadStrategy Strategy(OptionsParser op, int prefixCount, (Target t, Build b, Architecture? a) spec) => new DownloadStrategy(op, prefixCount, spec);

        private DownloadStrategy(OptionsParser op, (Target t, Build b, Architecture? a) spec) : this(op, 1, spec) { }

        private DownloadStrategy(OptionsParser op, int prefixCount, (Target t, Build b, Architecture? a) spec)
        {
            this.CurrentOptions = op;
            this.PrefixCount = prefixCount;
            this.Spec = spec;
        }

        // TODO: TBD: is this used? rather we ran with the strategy approach?
        [Obsolete("Unused")]
        private Element TargetElement
        {
            get
            {
                // We just need the de-con TARGET field
                var (t, _, __) = this.Spec;

                switch (t) {
                    case darwin:    return Element.darwin;
                    case linux:     return Element.Linux;
                    default:        return Element.Windows;
                }
            }
        }

        // TODO: TBD: is this used? rather we ran with the strategy approach?
        [Obsolete("Unused")]
        private IEnumerable<Element> ArchitectureElements
        {
            get
            {
                // We need both the de-con TARGET and ARCH fields
                var (t, _, a) = this.Spec;

                // // // TODO: TBD: ths issue is more of a sidebar at this point, we like the switch syntax much better...
                // // // TODO: TBD: return enumerated Elements instead...
                // // TODO: TBD: Update CSharp syntax highlighting / https://github.com/microsoft/vscode/issues/121124
                // (Element?, Element) GetArchitectureElement(Element element) => (null, element);

                // Identify the TARGET when ARCH is what it is
                switch (t) {

                    case win32 when (  a == x86                     ):  return Range(Element.ia32);
                    case win32 when (              a == x64         ):  return Range(Element.x64);
                    case win32 when (!(a == x86 || a == x64)        ):  return Range(Element.arm64);

                    case linux when (  a == x64                     ):  return Range(Element.x86_64);
                    case linux when (              a == arm         ):  return Range(Element.arm);
                    case linux when (!(a == x64 || a == arm)        ):  return Range(Element.arm64);

                    case darwin when ( a == universal               ):  return Range(Element.versionMacOS, Element.universal);
                    case darwin when (                   a == arm64 ):  return Range(Element.versionMacOS, Element.arm64);

                    // Default, we will run with the macOS darwin in the default arch
                    default:                                            return Range(Element.versionMacOS);
                }
            }
        }

        internal (string url, string path, string fileName) Render(Versions versions)
        {
            var op = this.CurrentOptions;

            string OnRenderUrlElement(Element element)
            {
                // TODO: TBD: fill in the blanks with any other bits...
                switch (element)
                {
                    case Element.insider: return $"{op.insider}";

                    case Element.insiderOrNull:
                        /* Insider should appear when: --insider && --code-version
                         * Insider should NOT appear when: --latest (under ANY circumstances) */
                        return (!(op.Versions.selector == latest || op.insider == Insider.insider)
                            || op.Versions.selector == latest) ? (string)null : $"{op.insider}";

                    case Element.version: return op.Versions.renderedVersionOrLatest;

                    default: return $"{element}";
                }
            }

            string OnRenderDelimitedDownloadUrlPhrase(char delim, params Element[] elements) =>
                string.Join($"{delim}", elements.Select(OnRenderUrlElement).Where(IsNotNullOrEmpty));

            string OnRenderDownloadUrlPhrase(params Element[] elements) =>
                OnRenderDelimitedDownloadUrlPhrase(hyp, elements);

            string RenderDownloadUrl() =>
                string.Join($"{colon}{slash}{slash}", https
                    , string.Join($"{slash}", Range(OnRenderDelimitedDownloadUrlPhrase(dot, _urlBase.ToArray()))
                        .Concat(this._urls.Select(OnRenderDownloadUrlPhrase))));

            // TODO: TBD: url Uri parts...
            var url = RenderDownloadUrl();

            string RenderPathElement(Element element)
            {
                // TODO: TBD: which if this is the case, then we can refactor this to a simple ternary operator... (x ? y : z)
                // This one is kind of a special use case.
                if (element == Element.versionMacOS)
                {
                    return $"{Versions.macOS}+";
                }

                // We should be able to render every other element.
                return $"{element}";
            }

            var path = Range(versions.renderedVersion).Concat(this._path.ToList().Select(RenderPathElement)).CombinePath();

            string RenderFileName(int prefixCount, char delim)
            {
                string RenderConventionOrExtension(Element element)
                {
                    switch (element)
                    {
                        case Element.insider: return $"{op.insider}";
                        case Element.insiderOrNull: return op.insider == Insider.stable ? (string)null : $"{op.insider}";
                        // TODO: TBD: ditto likewise the path elements...
                        case Element.version: return $"{versions.version}";
                        default: return $"{element}";
                    }
                }

                var extensions = this._extensions.ToList().Select(RenderConventionOrExtension).Where(IsNotNullOrEmpty).ToArray();

                var renderedConvention = this._conventions.Select(RenderConventionOrExtension).Where(IsNotNullOrEmpty).ToArray();

                var fileName = prefixCount < 2
                    ? string.Join($"{delim}", renderedConvention)
                    : string.Join($"{delim}", Range(renderedConvention.Take(prefixCount)
                        .Aggregate(string.Empty, (g, x) => g + x)).Concat(renderedConvention.Skip(prefixCount)));

                return string.Join($"{dot}", Range(fileName).Concat(extensions));
            }

            var fileName = RenderFileName(this.PrefixCount, this.Delim);

            // TODO: TBD: format it here...
            return (url, path, fileName);
        }
    }

    /// <summary>
    /// It is a bit crude I will admit, but the intention here is to run very light. Literally,
    /// no dependencies, no other files. Literally, the only thing we should need to do here is to
    /// &quot;csc filename&quot; and that&apos;s it. Maybe a handful of csc arguments as well, but
    /// that is all. Almost a lite CSharp script of sorts, short of adopting a <em>PowerShell</em>
    /// approach. Which so far we are able to accomplish with a nominal set of System level using
    /// statements.
    /// </summary>
    /// <see cref="!:https://github.com/Microsoft/vscode"/>
    /// <see cref="!:https://docs.microsoft.com/en-us/dotnet/framework/app-domains/build-single-file-assembly"/>
    /// <see cref="!:https://github.com/microsoft/vscode/issues/121119">Identify macOS download links</see>
    /// <see cref="!:https://github.com/microsoft/vscode/issues/109329">Automating the downloads with help from repeatable links</see>
    public static class Program
    {
        /// <summary>
        /// Returns the range of <paramref name="values"/> as a true <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="values"></param>
        /// <returns></returns>
        internal static IEnumerable<T> Range<T>(params T[] values)
        {
            foreach (var value in values)
            {
                yield return value;
            }
        }

        internal static string RenderObjectOrNull(object value)
        {
            // TODO: TBD: evaluate use cases for string specific tick delimited...
            return value == null ? "null" : $"{value}";
        }

        internal static IEnumerable<string> RenderNameObjectPairs(params (string name, object value)[] pairs)
        {
            foreach (var (name, value) in pairs)
            {
                yield return string.Join($"{colon} ", name, RenderObjectOrNull(value));
            }
        }

        private static AssetManager CurrentAssets { get; } = new AssetManager();

        private static DownloadProcessor CurrentProcessor { get; }

        private static OptionsParser CurrentOptions { get; }

        /// <summary>
        /// Program static constructor.
        /// </summary>
        static Program()
        {
            // This is poor man's "DI" right here...
            CurrentOptions = new OptionsParser(CurrentAssets);
            CurrentProcessor = new DownloadProcessor(CurrentAssets, CurrentOptions);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            var op = CurrentOptions;
            var cp = CurrentProcessor;

            /* Helps make it clear that we must parse arguments before dealing at all
             * with processing strategies. */

            if (!op.TryParseArguments(args))
            {
                op.OnShowHelp();
                return;
            }
            else if (!cp.TryFilterSpecifications())
            {
                op.OnShowHelp();
                return;
            }

            cp.ProcessDownloadSpecs();
        }

        /// <summary>
        /// Gets the Maximum allowable <see cref="Console.WindowWidth"/>, defaults to <c>100</c>.
        /// </summary>
        internal static int MaxConsoleWindowWidth { get; } = 100;

        /// <summary>
        /// Returns the <paramref name="s"/> <see cref="string"/> enclosed by <see cref="tick"/>
        /// marks.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string Q(string s) => Q(s, tick);

        /// <summary>
        /// Returns the <paramref name="s"/> <see cref="string"/> enclosed by
        /// <paramref name="delim"/> marks.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string Q(string s, char delim) => string.Join(s, delim, delim);

        /// <summary>
        /// Returns the <paramref name="values"/> enumerated as an Array enclosed by
        /// <see cref="squareBrackets"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="values"></param>
        /// <returns></returns>
        public static string A<T>(params T[] values) => A(squareBrackets, values);

        /// <summary>
        /// Returns the <paramref name="values"/> enumerated as an Array enclosed by the
        /// <paramref name="enclosure"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enclosure"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public static string A<T>(string enclosure, params T[] values)
        {
            string S(T value) => $"{value}";

            return string.Join(string.Join($"{comma} ", values.Select(S)), enclosure.ToArray());
        }

        /// <summary>
        /// Returns the <paramref name="values"/> enumerated as a <see cref="string"/> Array
        /// quoted by <see cref="tick"/> marks and enclosed by <see cref="squareBrackets"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="values"></param>
        /// <returns></returns>
        public static string AQ<T>(params T[] values) => AQ(tick, squareBrackets, values);

        /// <summary>
        /// Returns the <paramref name="values"/> enumerated as a <see cref="string"/> Array
        /// quoted by <paramref name="delim"/> marks and enclosed by <see cref="squareBrackets"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="delim"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public static string AQ<T>(char delim, params T[] values) => AQ(delim, squareBrackets, values);

        /// <summary>
        /// Returns the <paramref name="values"/> enumerated as a <see cref="string"/> Array
        /// quoted by <see cref="tick"/> marks and enclosed by <paramref name="enclosure"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enclosure"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public static string AQ<T>(string enclosure, params T[] values) => AQ(tick, enclosure, values);

        /// <summary>
        /// Returns the <paramref name="values"/> enumerated as a <see cref="string"/> Array
        /// quoted by <paramref name="delim"/> marks and enclosed by <paramref name="enclosure"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="delim"></param>
        /// <param name="enclosure"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        /// <see cref="Q(string, char)"/>
        /// <see cref="A{T}(string, T[])"/>
        public static string AQ<T>(char delim, string enclosure, params T[] values)
        {
            string _Q(T x) => Q($"{x}", delim);
            return A(enclosure, values.Select(_Q).ToArray());
        }

        /// <summary>
        /// Returns the tuple <see cref="pairs"/> name value associations enclosed by
        /// <see cref="curlyBraces"/>.
        /// </summary>
        /// <param name="pairs"></param>
        /// <returns></returns>
        /// <see cref="T(string, (string name, object value)[])"/>
        public static string T(params (string name, object value)[] pairs) => T(curlyBraces, pairs);

        /// <summary>
        /// Returns the <see cref="pairs"/> name value associations enclosed by
        /// <paramref name="enclosure"/>.
        /// </summary>
        /// <param name="enclosure"></param>
        /// <param name="pairs"></param>
        /// <returns></returns>
        /// <see cref="T(string, (string name, string value)[])"/>
        public static string T(string enclosure, params (string name, object value)[] pairs) =>
            T(enclosure, pairs.Select(x => (x.name, RenderObjectOrNull(x.value))).ToArray());

        /// <summary>
        /// Returns the <paramref name="pairs"/> name value asociations enclosed by
        /// <see cref="curlyBraces"/>.
        /// </summary>
        /// <param name="pairs"></param>
        /// <returns></returns>
        /// <see cref="T(string, (string name, string value)[])"/>
        public static string T(params (string name, string value)[] pairs) => T(curlyBraces, pairs);

        /// <summary>
        /// Returns the <paramref name="pairs"/> name value associations enclosed by
        /// <paramref name="enclosure"/>.
        /// </summary>
        /// <param name="enclosure"></param>
        /// <param name="pairs"></param>
        /// <returns></returns>
        public static string T(string enclosure, params (string name, string value)[] pairs)
        {
            var _pairs = pairs.Select(x => (x.name, (object)x.value)).ToArray();
            return string.Join(string.Join($"{comma} ", RenderNameObjectPairs(_pairs)), enclosure.Substring(0, 2).ToArray());
        }
    }
}

namespace System
{
    using static Code.Downloader.Program;

    internal static class EnumExtensions
    {
        /// <summary>
        /// Returns the <see cref="Enum.GetValues"/> associated with the <typeparamref name="T"/>
        /// type. The parameter <parmaref name="_"/> is here as a placeholder driving the generic
        /// type. That is all.
        /// <summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="_"></param>
        public static IEnumerable<T> GetEnumValues<T>(this T _)
            where T : struct
        {
            var type = typeof(T);

            if (!type.IsEnum)
            {
                throw new InvalidOperationException($"Parse type '{type.FullName}' is not an enum");
            }

            return Enum.GetValues(typeof(T)).OfType<T>();
        }

        /// <summary>
        /// Parses the <see cref="string"/> <paramref name="s"/> as the <see cref="Enum"/>
        /// <typeparamref name="T"/> type.
        /// </summary>
        public static T? ParseEnum<T>(this string s)
            where T : struct
        {
            var values = default(T).GetEnumValues().ToArray();

            foreach (T value in values)
            {
                if ($"{value}".ToLower() == (s ?? string.Empty).ToLower())
                {
                    return value;
                }
            }

            return default(T?);
        }
    }

    internal static class StringExtensions
    {
        // TODO: TBD: I'm not sure how an extension method cannot be seen as a "normal" static method...
        /// <summary>
        /// Returns whether <paramref name="s"/> <see cref="string.IsNullOrEmpty"/>.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static bool IsNotNullOrEmpty(string s) => !string.IsNullOrEmpty(s);

        public static string RenderStringOrNull(this string s)
        {
            const string @null = nameof(@null);
            return s == null ? @null : $"'{s}'";
        }

        // TODO: TBD: Multi-line descriptions are untested at this point...
        /// <summary>
        /// Parcels the <paramref name="s"/> first by new line separators, then according
        /// to its fit within the known <see cref="Console.WindowWidth"/>.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="margin"></param>
        /// <param name="width"></param>
        /// <returns>The multiple lines of <paramref name="s"/> split according to
        /// new lines and its fit within the <paramref name="width"/>.</returns>
        public static IEnumerable<string> SplitMultiline(this string s, int margin = 0, int? width = null)
        {
            var widthOrConsoleWindowWidth = Math.Min(width ?? Console.WindowWidth, MaxConsoleWindowWidth);

            bool IsWhiteSpace(char ch) => char.IsWhiteSpace(ch);

            var lines = s.Replace("\r", "").Split(Range('\n').ToArray());

            foreach (var multi in lines)
            {
                var t = multi;

                if (string.IsNullOrEmpty(t))
                {
                    yield return t ?? string.Empty;
                }
                else
                {
                    while (t.Any())
                    {
                        var line = t.Substring(0, Math.Min(t.Length, widthOrConsoleWindowWidth - margin));
                        t = t.Substring(line.Length);

                        // Seems to be sufficiently to the edge cases.
                        if (line.Length == widthOrConsoleWindowWidth && t.Any()
                            && !IsWhiteSpace(line.Last()) && !IsWhiteSpace(t.First()))
                        {
                            for (var last = line.Last(); !IsWhiteSpace(last); last = line.Last())
                            {
                                // While we "can" do this in the increment for phrase, it is clearer to do them here.
                                line = line.Substring(0, line.Length - 1);
                                t = last + t;
                            }
                        }

                        yield return line.Trim();
                    }
                }
            }
        }
    }
}

namespace System.Collections.ObjectModel
{
    using System;

    internal static class ObjectExtensions
    {
        public static string RenderStringOrNull<T>(this T value, Func<T, string> onRender) =>
            onRender.Invoke(value).RenderStringOrNull();
    }
}

namespace System.IO
{
    using static Code.Downloader.Program;

    internal static class FileExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sourceFileName"></param>
        /// <param name="destFileName"></param>
        public static void MoveFile(this string sourceFileName, string destFileName)
        {
            // TODO: TBD: for whatever reason, wget inconsistently downloads the files to the prefix directory.
            if (destFileName.FileExists())
            {
                File.Delete(destFileName);
            }

            File.Move(sourceFileName, destFileName);
        }

        /// <summary>
        /// Returns whether the <paramref name="path"/> <see cref="File.Exists"/>.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool FileExists(this string path) =>
            path != null && File.Exists(path);

        /// <summary>
        /// Returns whether the <paramref name="path"/> <see cref="Directory.Exists"/>.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool DirectoryExists(this string path) =>
            path != null && Directory.Exists(path);

        /// <summary>
        /// Returns the <see cref="Path.Combine(string[])"/> for the <paramref name="parts"/>.
        /// </summary>
        /// <param name="parts"></param>
        /// <returns></returns>
        public static string CombinePath(this IEnumerable<string> parts) =>
            Path.Combine(parts.ToArray());

        /// <summary>
        /// 
        /// </summary>
        /// <param name="root"></param>
        /// <param name="parts"></param>
        /// <returns></returns>
        public static string CombinePath(this string root, params string[] parts) =>
            Path.Combine((!parts.Any() ? Range(root) : Range(root).Concat(parts)).ToArray());
    }
}
