using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

// https://fake.build/dotnet-assemblyinfo.html
// https://csharppedia.com/en/tutorial/4264/assemblyinfo-cs-examples
[assembly: AssemblyVersion("0.2.0.0")]

// TODO: TBD: just about done with this one...
// TODO: TBD: I may also commit it to github after all, we'll see...
// TODO: TBD: just about all the plumbing is in and verified...
// TODO: TBD: also have the assets verification pretty much working...
// TODO: TBD: what remains is to verify that the wget process itself actually works as expected...
// TODO: TBD: might consider giving the Code repo a gander and establishing this repo as a seamless overlay there...
// TODO: TBD: with the thought being that potentially could merge the two repos, potentially...
namespace Code.Downloader
{
    using static Program;
    using static Help;
    using static Target;
    using static Build;
    using static Architecture;
    using static Insider;
    using static Versions;
    using static CodeVersion;
    using static NoPause;
    using static AssetManager;

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

    public enum Dry
    {
        /// <summary>
        /// For use with all.
        /// </summary>
        dry,
    }

    public enum All
    {
        /// <summary>
        /// For use with all.
        /// </summary>
        all,
    }

    public enum Help
    {
        /// <summary>
        /// For use with help.
        /// </summary>
        show,
    }

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
        win32
    }

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
        arm64
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
        snap
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
    // darwin+version+stable => VSCode-darwin-major.minor.patch-stable.zip
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
        win32,
        insider,
        stable,
        ia32,
        x64,
        amd64, // For Linux DEB x64
        arm64, // For Linux ARM64 DEB and archive
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
    }

    /// <summary>
    ///
    /// </summary>
    public static class Bits2
    {
        /// <summary>
        ///
        /// </summary>
        internal const string Windows = nameof(Windows);

        /// <summary>
        ///
        /// </summary>
        internal const string Linux = nameof(Windows);

        /// <summary>
        ///
        /// </summary>
        internal const string macOS = nameof(macOS);

        /// <summary>
        ///
        /// </summary>
        internal const string x64 = nameof(x64);

        /// <summary>
        ///
        /// </summary>
        internal const string x86 = nameof(x86);

        /// <summary>
        ///
        /// </summary>
        internal const string arm = nameof(arm);

        /// <summary>
        ///
        /// </summary>
        internal const string arm64 = nameof(arm64);

        /// <summary>
        ///
        /// </summary>
        internal const string zip = nameof(zip);

        /// <summary>
        ///
        /// </summary>
        internal const string tar = nameof(tar);

        /// <summary>
        ///
        /// </summary>
        internal const string gz = nameof(gz);

        /// <summary>
        ///
        /// </summary>
        /// <see cref="tar"/>
        /// <see cref="gz"/>
        internal static string tarball { get; } = string.Join(".", tar, gz);
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
        /// <br/>darwin
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
        ///
        /// <br/>win32+system+x64+version => VSCodeSetup-x86-major.minor.patch.exe
        /// <br/>win32+user+x64+version => VSCodeUserSetup-x86-major.minor.patch.exe
        /// <br/>win32+archive+x64+version => VSCode-win32-x86-major.minor.patch.zip
        ///
        /// <br/>win32+system+x64+version => VSCodeSetup-x86-major.minor.patch.exe
        /// <br/>win32+user+x64+version => VSCodeUserSetup-x86-major.minor.patch.exe
        /// <br/>win32+archive+x64+version => VSCode-win32-x86-major.minor.patch.zip
        ///
        /// <br/>win32+system+arm64+version => VSCodeSetup-arm64-major.minor.patch.exe
        /// <br/>win32+user+arm64+version => VSCodeUserSetup-arm64-major.minor.patch.exe
        /// <br/>win32+archive+arm64+version => VSCode-win32-arm64-major.minor.patch.zip
        ///
        /// <br/>linux+deb+x64+version => code_major.minor.patch-amd64.deb
        /// <br/>linux+rpm+x64+version => code_major.minor.patch-amd64.rpm
        /// <br/>linux+archive+x64+version => code_major.minor.patch-amd64.tar.gz
        ///
        /// <br/>darwin+version+stable => VSCode-darwin-major.minor.patch-stable.zip
        /// <br/>
        /// <br/>And a few notes concerning combinations and file naming conventions:
        /// <see cref="win32"/> and <see cref="darwin"/> <see cref="archive"/> extensions are
        /// <c>.zip</c>. <see cref="win32"/> filename <see cref="Architecture"/> is <c>ia32</c>.
        /// <see cref="linux"/> <see cref="x64"/> <see cref="Architecture"/> is <c>amd64</c>.
        /// </summary>
        /// <see cref="https://github.com/microsoft/vscode/issues/109329">Automating the downloads with help from repeatable links</see>
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

            using (var process = new Process())
            {
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.FileName = AssetManager.where;
                process.StartInfo.Arguments = AssetManager.wget;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();

                // TODO: TBD: as far as we know, we do not own the StandardOutput instance in order to dispose of it...
                var process_StandardOutput = process.StandardOutput;

                var pathCand = process_StandardOutput.ReadLine();

                if (File.Exists(pathCand))
                {
                    path = pathCand;
                }

                process.WaitForExit();
            }

            return File.Exists(path);
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

        public System.Version version
        {
            get => this.selector == latest ? latestVersion : (_version ?? latestVersion);
            set => _version = value;
        }

        public CodeVersion selector { get; set; } = CodeVersion.latest;

        internal static System.Version macOS { get; } = Version.Parse("10.10");

        internal static System.Version latestVersion { get; } = Version.Parse("1.50.1");

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

        internal Insider insider { get; private set; } = default;

        /// <summary>
        /// Gets the rendered <see cref="Insider"/> Parts for use with the download Uri.
        /// </summary>
        /// <see cref="insider"/>
        /// <see cref="stable"/>
        private IEnumerable<string> InsiderParts
        {
            get
            {
                var s = $"{this.insider}";

                if (this.insider == Insider.insider)
                {
                    yield return s;
                    yield return s;
                }

                if (this.insider == stable)
                {
                    yield return string.Empty;
                    yield return s;
                }
            }
        }

        /// <summary>
        /// Gets the rendered Stable or Insider for use with the download Uri.
        /// </summary>
        /// <see cref="insider"/>
        /// <see cref="stable"/>
        private string SlashStableOrInsider
        {
            get
            {
                string RenderInsider() => this.insider == stable ? nameof(stable) : nameof(insider);
                const char forwardSlash = '/';
                return $"{forwardSlash}{RenderInsider()}";
            }
        }

        internal All? all { get; private set; }

        internal bool DownloadAll => this.all == All.all;

        private Target? target { get; set; }

        private Architecture? arch { get; set; }

        private Build? build { get; set; }

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

        private string[] TargetVals { get; }
        private string[] ArchVals { get; }
        private string[] BuildVals { get; }
        private string[] CodeVersionVals { get; }
        private string[] DefaultVals { get; }

        internal Versions Versions { get; } = new Versions();

        /// <summary>
        /// Gets the valid combinations of <see cref="Target"/> and corresponding
        /// <see cref="Build"/>.
        /// </summary>
        private IDictionary<Target, Build[]> BuildsForTarget { get; }

        /// <summary>
        /// Gets the valid combinations of <see cref="Architecture"/> corresponding
        /// with the <see cref="Target"/> and <see cref="Build"/> pairs.
        /// </summary>
        private IDictionary<(Target, Build), Architecture?[]> ArchesForTargetBuild { get; }

        internal OptionsParser(AssetManager assets)
            : this(assets, null)
        {
        }

        internal OptionsParser(AssetManager assets, Func<TextWriter> writerSelector)
            : base(writerSelector)
        {
            this.CurrentAssets = assets;

            const string angleBrackets = "<>";
            string OnRenderValue<T>(T value) => $"{value}";

            this.TargetVals = Range(darwin, linux, win32).Select(OnRenderValue).ToArray();
            this.ArchVals = Range(x64, x86, arm, arm64).Select(OnRenderValue).ToArray();
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

            const char hyp = '-';

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

            // TODO: TBD: there's probably a pattern here we can factor to a method, at least...
            this.CodeVersionOpts = Range($"{hyp}{hyp}{CodeVersion.code}{hyp}{CodeVersion.version}"
                , $"{hyp}{CodeVersion.code.ToString().First()}{CodeVersion.version.ToString().First()}").ToArray();

            /// <summary>
            /// Returns the valid combinations corresponding to the <see cref="Target"/>
            /// <paramref name="key"/>. This is our way of vetting the valid from invalid
            /// permutations for quality control purposes. As long as the combination is
            /// valid we let it pass. But when it is determined to be invalid, then we may
            /// report usage.
            /// </summary>
            IEnumerable<Build> OnGetBuildsForTarget(Target key)
            {
                if (key == win32)
                {
                    yield return user;
                    yield return system;
                }

                if (key == linux)
                {
                    yield return deb;
                    yield return rpm;
                    yield return snap;
                }

                // This is correct, all targets support archive in one form or another.
                yield return archive;
            }

            /// <summary>
            /// Returns the valid combinations corresponding to the <see cref="Target"/>
            /// <see cref="Build"/> <paramref name="pair"/> combinations. Also ditto further
            /// <see cref=""/> remarks.
            /// </summary>
            IEnumerable<Architecture?> OnGetArchesForTargetBuild((Target t, Build b) pair)
            {
                if (pair.t == win32)
                {
                    yield return x86;
                    yield return x64;
                    // While we allow for ARM for convenience, download maps to ARM64.
                    yield return arm;
                    yield return arm64;
                }

                if (pair.t == linux)
                {
                    if (pair.b == snap)
                    {
                        yield return null;
                        // Null is allowable, we will default that to x64 for path composition purposes.
                        yield return x64;
                    }
                    else
                    {
                        // While we allow for x86 and x64 for convenience, download maps to amd64.
                        yield return x86;
                        yield return x64;
                        yield return arm;
                        yield return arm64;
                    }
                }

                if (pair.t == darwin)
                {
                    yield return null;
                }
            }

            IEnumerable<(Target t, Build b, Architecture? a)> GetAllDownloadSpecs()
            {
                foreach (Target t in win32.GetEnumValues())
                {
                    foreach (Build b in this.BuildsForTarget[t])
                    {
                        foreach (Architecture? a in this.ArchesForTargetBuild[(t, b)])
                        {
                            yield return (t, b, a);
                        }
                    }
                }
            }

            this.BuildsForTarget = Range(win32, linux, darwin)
                .Select(key => (key, Values: OnGetBuildsForTarget(key).ToArray()))
                .ToDictionary(x => x.key, x => x.Values);

            this.ArchesForTargetBuild = this.BuildsForTarget
                .SelectMany(pair => pair.Value.Select(b => (t: pair.Key, b)))
                .ToDictionary(x => x, x => OnGetArchesForTargetBuild(x).ToArray());

            this.AllDownloadSpecs = GetAllDownloadSpecs().ToArray();
        }

        private string RenderHelpSummary(string fileName)
        {
            static string RenderFlagValues<K, V>(K key, params V[] values)
            {
                const string pipe = "|";
                return $"--{key} {string.Join(pipe, values.Select(x => $"{x}"))}";
            }

            const string target = nameof(this.target);
            const string build = nameof(this.build);
            const string arch = nameof(this.arch);

            return $@"Provides a command line programmatic view into the Code download web page matrix. The options describe the values for each option, but not all combinations are valid. The following combinations work for each area of the matrix.

  {fileName} {RenderFlagValues(target, darwin)}
  {fileName} {RenderFlagValues(target, win32)} {RenderFlagValues(build, user, system, archive)} {RenderFlagValues(arch, x64, x86, arm64)}
  {fileName} {RenderFlagValues(target, linux)} {RenderFlagValues(build, deb, rpm, archive)} {RenderFlagValues(arch, x64, x86, arm, arm64)}
  {fileName} {RenderFlagValues(target, linux)} {RenderFlagValues(build, snap)}

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
            //const int colWidth = 4;
            const string flagDelim = ",";
            const string flagPrefix = "  ";

            var maxFlagWidth = opts.Max(x => x.flags.Sum(y => y.Length + flagPrefix.Length) + (x.flags.Length - 1) * flagDelim.Length);
            //var flagsWidth = ((int)(maxFlagWidth / colWidth) + 1) * colWidth;

            this.Writer.WriteLine();

            foreach (var line in summary.SplitMultiline())
            {
                this.Writer.WriteLine(line);
            }

            this.Writer.WriteLine();

            foreach (var (flags, description, values) in opts)
            {
                this.Writer.WriteLine(string.Join(flagDelim, flags.Select(x => flagPrefix + x)));

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
            , (this.InsiderOpts, $"--x, whether to get the {nameof(insider)} or {nameof(stable)}", this.DefaultVals)
            , (this.VersionOpts, "--x, whether to show the downloader version", this.DefaultVals)
        );

        private void ReportDryRun(int i, params string[] args)
        {
            if (!this.IsDry)
            {
                return;
            }

            const string squareBrackets = "[]";
            const char comma = ',';
            this.Writer.WriteLine($"{nameof(Dry)}: {nameof(args)}: {string.Join(string.Join($"{comma} ", args), squareBrackets.ToArray())}, {nameof(i)}: {i}");

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

            ReportNameValuePair(nameof(this.HelpOpts), string.Join(", ", this.HelpOpts));
            ReportNameValuePair(nameof(this.TargetOpts), string.Join(", ", this.TargetOpts));
            ReportNameValuePair(nameof(this.ArchOpts), string.Join(", ", this.ArchOpts));
            ReportNameValuePair(nameof(this.BuildOpts), string.Join(", ", this.BuildOpts));
            ReportNameValuePair(nameof(this.AllOpts), string.Join(", ", this.AllOpts));
            ReportNameValuePair(nameof(this.DryOpts), string.Join(", ", this.DryOpts));
            ReportNameValuePair(nameof(this.CodeVersionOpts), string.Join(", ", this.CodeVersionOpts));
            ReportNameValuePair(nameof(this.InsiderOpts), string.Join(", ", this.InsiderOpts));
            ReportNameValuePair(nameof(this.VersionOpts), string.Join(", ", this.VersionOpts));

            ReportNameValuePair(nameof(this.TargetVals), string.Join(", ", this.TargetVals));
            ReportNameValuePair(nameof(this.ArchVals), string.Join(", ", this.ArchVals));
            ReportNameValuePair(nameof(this.BuildVals), string.Join(", ", this.BuildVals));
            ReportNameValuePair(nameof(this.CodeVersionVals), string.Join(", ", this.CodeVersionVals));
            ReportNameValuePair(nameof(this.TargetVals), string.Join(", ", this.TargetVals));

            ReportNameValuePair($"{nameof(this.Versions)}.{nameof(Versions.show)}", $"{this.Versions.show}".ToLower());
            ReportNameValuePair($"{nameof(this.Versions)}.{nameof(Versions.version)}", this.Versions.version);
            ReportNameValuePair($"{nameof(Versions)}.{nameof(Versions.macOS)}", Versions.macOS);
            ReportNameValuePair($"{nameof(Versions)}.{nameof(Versions.latestVersion)}", Versions.latestVersion);
        }

        /// <summary>
        /// We will start from the valid set of download specifications. We will select subsets
        /// of these depending on the command line arguments that we are given.
        /// </summary>
        private IEnumerable<(Target t, Build b, Architecture? a)> AllDownloadSpecs { get; }

        private IEnumerable<(Target t, Build b, Architecture? a)> GetSelectedDownloadSpecs(Target? t, Build? b, Architecture? a)
        {
            // Do a little screening of the command line arguments ensuring optimum alignment.
            if (t == linux && b == snap && a == null)
            {
                a = x64;
            }

            if (t == darwin && b != archive && a != null)
            {
                b = archive;
                a = null;
            }

            return this.AllDownloadSpecs.Where(x =>
                (t == null || t == x.t)
                && (b == null || b == x.b)
                && (a == null || a == x.a)
            );
        }

        private IEnumerable<(Target t, Build b, Architecture? a)> _selectedDownloadSpecs;

        /// <summary>
        /// Gets the DownloadSpecs for use throughout the tool. This is calculated just once
        /// per session, so be careful of the timing during which it is called. Ensure that
        /// the command line arguments have all been properly parsed by that moment.
        /// </summary>
        internal IEnumerable<(Target t, Build b, Architecture? a)> SelectedDownloadSpecs => this._selectedDownloadSpecs ?? (
            this._selectedDownloadSpecs = GetSelectedDownloadSpecs(this.target, this.build, this.arch)
        );

        public bool TryParseArguments(params string[] args)
        {
            string GetArgument(int index) => index >= args.Length
                ? string.Empty : args.ElementAt(index).ToLower();

            // Reset the optional values to nominal defaults.
            this.pause = NoPause.pause;
            this.all = default;
            this.dry = default;
            this.target = default;
            this.arch = default;
            this.build = default;
            this.insider = default;
            this.Versions.Reset();

            int i;

            void OnShowHelp(Help value)
            {
                this.help = value;
                TryShowVersion();
                TryPresentHelpOnReturn(this.Versions);
            }

            for (i = 0; i < args.Length; i++)
            {
                var arg = GetArgument(i);

                if (this.HelpOpts.Contains(arg))
                {
                    OnShowHelp(show);
                    break;
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

            var downloadSpecs = this.SelectedDownloadSpecs;

            if (!downloadSpecs.Any())
            {
                OnShowHelp(show);
                return false;
            }

            /* Arguments are considered parsed successfully when:
             * 1. Arguments processed successfully
             * 2. Download specifications are properly aligned
             * 3. Assets properly discovered
             */
            return i == args.Length
                && downloadSpecs.Any()
                && this.CurrentAssets.AreDiscovered(this.Writer);
        }
    }

    public class DownloadProcessor : CanWrite
    {
        private AssetManager CurrentAssets { get; }

        private OptionsParser CurrentOptions { get; }

        internal Versions CurrentVersions { get; } = new Versions();

        private string Version => this.CurrentVersions.selector == latest ? $"{latest}" : $"{version}";

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

        /// <summary>
        /// Tries to Invoke the <see cref="Assets.Wget"/> asset given the <paramref name="uri"/>,
        /// <paramref name="path"/> and additional <paramref name="args"/>.
        /// </summary>
        /// <param name="path">The output directory where wget should place the download.</param>
        /// <param name="uri">The Uri that wget should use when getting the download.</param>
        /// <param name="args">Additional command line arguments.</param>
        /// <returns></returns>
        private bool TryInvokeWget(string path, string uri, params string[] args)
        {
            var op = this.CurrentOptions;

            var wgetPath = this.CurrentAssets.wgetPath;

            // -P for --directory-prefix, in this form.
            args = args.Concat(Range("-P", path, uri)).ToArray();

            if (op.IsDry)
            {
                this.Writer.WriteLine($"{nameof(Dry)}: {wgetPath} {string.Join(" ", args)}");
            }
            else
            {
                var startInfo = new ProcessStartInfo(wgetPath)
                {
                    Arguments = string.Join(" ", args)
                };

                using (var process = Process.Start(startInfo))
                {
                    process.WaitForExit();
                }
            }

            return true;
        }

        private string _version;

        /// <summary>
        ///
        /// </summary>
        private string CurrentVersion => this._version ?? (
            this._version = this.CurrentVersions.selector == latest
                ? $"{latest}"
                : $"{this.CurrentVersions.version}"
        );

#if false // Temporarily disabled while working out the front of the process

        /// <summary>
        ///
        /// </summary>
        /// <param name="t">A target.</param>
        /// <param name="b">An optional build.</param>
        /// <param name="a">An optional architecture.</param>
        private void ProcessSingle(string t, string b = null, string a = null)
        {
            var op = this.CurrentOptions;
            //var assets = this.CurrentAssets;

            if (op.IsDry)
            {
                this.Writer.WriteLine($"{nameof(Dry)}: {nameof(ProcessSingle)}({nameof(t)}: '{RenderObjectOrNull(t)}', {nameof(b)}: {RenderObjectOrNull(b)}, {nameof(a)}: {RenderObjectOrNull(a)})");
            }

            b = b ?? string.Empty;
            a = a ?? string.Empty;

            var version = this.CurrentVersion;

            // Render both the UpdateCodeUri and version bits...
            var baseUriVersion = op.insider
                ? $"{updateCodeUri}{version}-{nameof(op.insider)}/"
                : $"{updateCodeUri}{version}/";

            void MakeDirectory(string path)
            {
                if (op.IsDry)
                {
                    this.Writer.WriteLine($"{nameof(Dry)}: Making directory: {path}");
                    return;
                }

                Directory.CreateDirectory(path);
            }

            bool TryProcessAny(string path, string versionUriPhrase)
            {
                MakeDirectory(path);
                return TryInvokeWget(path, $"{baseUriVersion}{versionUriPhrase}{this.slashStableOrInsider}");
            }

            string RenderAssertOrAssetInsiderPhrase(params string[] parts) => string.Join(
                "-", parts.Concat(this.InsiderParts).Where(x => !string.IsNullOrEmpty(x))
            );

            // TODO: TBD: can probable refactor the general case given a path, uri, and directory...
            bool TryProcessWin32()
            {
                var build = b == system ? null : b;
                var arch = a == x86 ? null : a;
                return TryProcessAny(Path.Combine(version, t, a), RenderAssertOrAssetInsiderPhrase(t, arch, build));
            }

            bool TryProcessLinux()
            {
                var build = b == archive ? null : b;

                var arch = b == snap ? x64
                    : (a == arm ? armhf : a);

                var phrase = RenderAssertOrAssetInsiderPhrase(t, build, arch);

                return b == snap
                    ? TryProcessAny(Path.Combine(version, t, b), phrase)
                    : TryProcessAny(Path.Combine(version, t, a), phrase);
            }

            bool TryProcessDarwin() => TryProcessAny(Path.Combine(version, Directories.macOS, $"{Versions.MacOS}+"), RenderAssertOrAssetInsiderPhrase(t));

            switch (t)
            {
                case win32:
                    TryProcessWin32();
                    break;

                case linux:
                    TryProcessLinux();
                    break;

                case darwin:
                    TryProcessDarwin();
                    break;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="v">A version.</param>
        /// <param name="t">A target.</param>
        /// <param name="b">A build.</param>
        /// <param name="a">An architecture.</param>
        public void ProcessConfiguration()
        {
            var op = this.CurrentOptions;

            const string @null = nameof(@null);

            if (op.IsDry)
            {
                this.Writer.WriteLine($"{nameof(Dry)}: {nameof(ProcessConfiguration)}()");
            }

            void ProcessMacOS((Target t, Build? b, Architecture? a) tuple)
            {
                var (t, _, __) = tuple;

                if (t == Targets.darwin)
                {
                    ProcessSingle(t);
                }
            }

            void ProcessWin32((Target t, Build? b, Architecture? a) tuple)
            {
                var (t, b, a) = tuple;
                var builds = (string.IsNullOrEmpty(b) ? system : Range(b)).ToArray();
                var arches = (string.IsNullOrEmpty(a) ? x64 : Range(a)).ToArray();

                if (t == Targets.win32)
                {
                    foreach (var arch in arches)
                    {
                        foreach (var build in builds)
                        {
                            ProcessSingle(t, build, arch);
                        }
                    }
                }
            }

            void ProcessLinux((Target t, Build? b, Architecture? a) tuple)
            {
                var (t, b, a) = tuple;
                var builds = Range(b ?? deb).ToArray();
                var arches = Range(a ?? x64).ToArray();

                if (t == Targets.linux)
                {
                    foreach (var arch in arches)
                    {
                        foreach (var build in builds)
                        {
                            ProcessSingle(t, build, arch);
                        }
                    }

                    ProcessSingle(t, snap);
                }
            }

            void VetDownloadFlags()
            {
                /* We support arm64 arch downloads for win32 targets.
                * Downloads says "ARM" but it is really arm64 behind the link. */
                if (target == Targets.win32 && arch == arm)
                {
                    arch = arm64;
                }

                // Assumes Debian, RPM, or Snap builds are Linux targets.
                if (Range(deb, rpm, snap).Contains(build))
                {
                    target = Targets.linux;
                }

                // Assumes x64 when linux and one of its builds specified.
                if (target == Targets.linux
                    && Range(deb, rpm, archive).Contains(build)
                    && !Range(x64, arm, arm64).Contains(arch))
                {
                    arch = x64;
                }
            }

            VetDownloadFlags();

            if (all)
            {
                ProcessWin32(Targets.win32);
                ProcessLinux(Targets.linux);
                ProcessMacOS(Targets.darwin);
            }
            else
            {
                var t = target;
                var b = build;
                var a = arch;

                ProcessWin32(t ?? string.Empty, b, a);
                ProcessLinux(t ?? string.Empty, b, a);
                ProcessMacOS(t ?? string.Empty);
            }
        }

#endif // Temporarily disabled while working out the front of the process

    }

    public class DownloadDescriptor
    {
        private static IEnumerable<Architecture> GetArchitectures(Target target)
        {
            if (target == win32)
            {
                yield return x64;
                yield return x86;
                yield return arm64;
            }
            else if (target == linux)
            {
                yield return x64;
                yield return arm;
                yield return arm64;
            }
        }

        private static IEnumerable<Build> GetBuilds(Target target)
        {
            if (target == win32)
            {
                yield return user;
                yield return system;
                yield return archive;
            }
            else if (target == linux)
            {
                yield return deb;
                yield return rpm;
                yield return archive;
            }
        }

        private static IDictionary<Target, IEnumerable<Architecture>> _targetArchitectures;

        private static IDictionary<Target, IEnumerable<Build>> _targetBuilds;

        public static IDictionary<Target, IEnumerable<Architecture>> TargetArchitectures => _targetArchitectures ?? (
            _targetArchitectures = Range(win32, linux, darwin).ToDictionary(x => x, GetArchitectures)
        );

        public static IDictionary<Target, IEnumerable<Build>> TargetBuilds => _targetBuilds ?? (
            _targetBuilds = Range(win32, linux, darwin).ToDictionary(x => x, GetBuilds)
        );

        private (Target target, Architecture? arch, Build? build) Elements { get; }

        private Target Target => this.Elements.target;

        private Architecture? Arch => this.Elements.arch;

        private Build? Build => this.Elements.build;

        private Version Version { get; set; }

        public bool IsLatest{ get; set; }

        public string DestinationPath { get; }

        public DownloadDescriptor((Target target, Architecture? arch, Build? build) elements)
        {
            this.Elements = elements;

            if (this.Target == darwin)
            {
                this.Elements = (this.Target, null, null);
            }
        }
    }

    /// <summary>
    /// It is a bit crude I will admit, but the intention here is to run very light. Literally,
    /// no dependencies, no other files. Literally, the only thing we should need to do here is
    /// to &quot;csc filename&quot; and that&apos;s it. Maybe a handful of csc arguments as well,
    /// but that is all. Almost a lite CSharp script of sorts, short of adopting a
    /// <em>PowerShell</em> approach. Which so far we are able to accomplish with
    /// a nominal set of System level using statements.
    /// </summary>
    /// <see cref="!:https://github.com/Microsoft/vscode"/>
    /// <see cref="!:https://docs.microsoft.com/en-us/dotnet/framework/app-domains/build-single-file-assembly"/>
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

        internal static string RenderObjectOrNull(object value) => value == null ? "null" : $"{value}";

        internal static IEnumerable<string> RenderNameObjectPairs(params (string name, object value)[] pairs)
        {
            foreach (var (name, value) in pairs)
            {
                yield return string.Join(": ", name, RenderObjectOrNull(value));
            }
        }

        private static AssetManager CurrentAssets { get; } = new AssetManager();

        private static DownloadProcessor CurrentProcessor { get; }

        private static OptionsParser CurrentOptions { get; }

        static Program()
        {
            // This is poor man's "DI" right here...
            CurrentOptions = new OptionsParser(CurrentAssets);
            CurrentProcessor = new DownloadProcessor(CurrentAssets, CurrentOptions);
        }

        public static void Main(string[] args)
        {
            var op = CurrentOptions;

            if (!op.TryParseArguments(args))
            {
                return;
            }

            // TODO: TBD: commented out while workout out changes to the front of the process.
            //CurrentProcessor.ProcessConfiguration();
        }

        /// <summary>
        /// Gets the Maximum allowable <see cref="Console.WindowWidth"/>, defaults to <c>100</c>.
        /// </summary>
        internal static int MaxConsoleWindowWidth { get; } = 100;
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
