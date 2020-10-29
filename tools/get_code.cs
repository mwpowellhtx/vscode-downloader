using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

[assembly: AssemblyVersion("0.1.0.0")]

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
        internal static IEnumerable<T> Range<T>(params T[] values)
        {
            foreach (var value in values)
            {
                yield return value;
            }
        }

        private static bool TryPresentHelp(string summary, params (string[] flags, string description, string[] values)[] opts)
        {
            //const int colWidth = 4;
            const string flagDelim = ",";
            const string flagPrefix = "  ";

            var maxFlagWidth = opts.Max(x => x.flags.Sum(y => y.Length + flagPrefix.Length) + (x.flags.Length - 1) * flagDelim.Length);
            //var flagsWidth = ((int)(maxFlagWidth / colWidth) + 1) * colWidth;

            Console.WriteLine();

            foreach (var line in summary.SplitMultiline())
            {
                Console.WriteLine(line);
            }

            Console.WriteLine();

            foreach (var (flags, description, values) in opts)
            {
                Console.WriteLine(string.Join(flagDelim, flags.Select(x => flagPrefix + x)));

                foreach (var line in description.SplitMultiline(maxFlagWidth))
                {
                    Console.WriteLine(line.PadLeft(maxFlagWidth + line.Length));
                }

                // TODO: TBD: may need to do a similar thing here concerning fitting the values into the line width.
                if (values.Any())
                {
                    var renderedValues = $"{nameof(values).ToUpper()}: {string.Join(", ", values)}";
                    Console.WriteLine(renderedValues.PadLeft(maxFlagWidth + renderedValues.Length));
                }
            }

            return false;
        }

        private static bool TryShowVersion()
        {
            Console.WriteLine($"{programFileName} {programAssy.GetName().Version}");
            return false;
        }

        private static string RenderStringOrNull(string s)
        {
            const string @null = nameof(@null);
            return s == null ? @null : $"'{s}'";
        }

        private static string RenderStringOrNull<T>(T value, Func<T, string> onRender) =>
            RenderStringOrNull(onRender.Invoke(value));

        private static string OnRenderVersion(Version version) => version == null ? null : $"{version}";

        /// <summary>
        /// Tries to Invoke the <see cref="Assets.Wget"/> asset given the <paramref name="uri"/>,
        /// <paramref name="path"/> and additional <paramref name="args"/>.
        /// </summary>
        /// <param name="path">The output directory where wget should place the download.</param>
        /// <param name="uri">The Uri that wget should use when getting the download.</param>
        /// <param name="args">Additional command line arguments.</param>
        /// <returns></returns>
        private static bool TryInvokeWget(string path, string uri, params string[] args)
        {
            var wgetPath = Assets.wgetPath;

            // -P for --directory-prefix, in this form.
            args = args.Concat(Range("-P", path, uri)).ToArray();

            if (this.dry)
            {
                Console.WriteLine($"{nameof(this.dry)}: {wgetPath} {string.Join(" ", args)}");
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

        /// <summary>
        ///
        /// </summary>
        /// <param name="version">A rendered version string.</param>
        /// <param name="t">A target.</param>
        /// <param name="b">An optional build.</param>
        /// <param name="a">An optional architecture.</param>
        private static void ProcessSingle(OptionsParser op, string version, string t, string b = null, string a = null)
        {
            if (op.dry)
            {
                Console.WriteLine($"{nameof(op.dry)}: {nameof(ProcessSingle)}({nameof(version)}: '{version}', {nameof(t)}: '{t}', {nameof(b)}: {RenderStringOrNull(b)}, {nameof(a)}: {RenderStringOrNull(a)})");
            }

            b = b ?? string.Empty;
            a = a ?? string.Empty;

            // Render both the updateCodeUri and version bits...
            var baseUriVersion = this.insider
                ? $"{Assets.updateCodeUri}{version}-{nameof(this.insider)}/"
                : $"{Assets.updateCodeUri}{version}/";
            version = version == nameof(Versions.latest) ? $"{Versions.latest}" : version;

            void MakeDirectory(string path)
            {
                if (op.dry)
                {
                    Console.WriteLine($"{nameof(op.dry)}: Making directory: {path}");
                    return;
                }

                Directory.CreateDirectory(path);
            }

            bool TryProcessAny(string path, string versionUriPhrase)
            {
                MakeDirectory(path);
                return TryInvokeWget(path, $"{baseUriVersion}{versionUriPhrase}{slashStableOrInsider}");
            }

            string RenderAssertOrAssetInsiderPhrase(params string[] parts) => string.Join(
                "-", parts.Concat(this.InsiderParts).Where(x => !string.IsNullOrEmpty(x))
            );

            // TODO: TBD: can probable refactor the general case given a path, uri, and directory...
            bool TryProcessWin32()
            {
                var build = b == Build.system ? null : b;
                var arch = a == Architecture.x86 ? null : a;
                return TryProcessAny(Path.Combine(version, t, a), RenderAssertOrAssetInsiderPhrase(t, arch, build));
            }

            bool TryProcessLinux()
            {
                var build = b == Build.archive ? null : b;

                var arch = b == Build.snap ? Architecture.x64
                    : (a == Architecture.arm ? Architecture.armhf : a);

                var phrase = RenderAssertOrAssetInsiderPhrase(t, build, arch);

                return b == Build.snap
                    ? TryProcessAny(Path.Combine(version, t, b), phrase)
                    : TryProcessAny(Path.Combine(version, t, a), phrase);
            }

            bool TryProcessDarwin() => TryProcessAny(Path.Combine(version, Directories.macOS, $"{Versions.MacOS}+"), RenderAssertOrAssetInsiderPhrase(t));

            switch (t)
            {
                case Target.win32:
                    TryProcessWin32();
                    break;

                case Target.linux:
                    TryProcessLinux();
                    break;

                case Target.darwin:
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
        private static void ProcessConfiguration(OptionsParser op, Version v)
        {
            var version = v == Versions.latest ? nameof(Versions.latest) : $"{v}";

            const string @null = nameof(@null);

            if (op.dry)
            {
                Console.WriteLine($"{nameof(op.dry)}: {nameof(ProcessConfiguration)}({nameof(v)}: {RenderStringOrNull(v, OnRenderVersion)}), {nameof(version)}: {RenderStringOrNull(version)}");
            }

            void ProcessMacOS(string t)
            {
                if (t == Targets.darwin)
                {
                    ProcessSingle(version, t);
                }
            }

            void ProcessWin32(string t, string b = null, string a = null)
            {
                var builds = (string.IsNullOrEmpty(b) ? Build.win32 : Range(b)).ToArray();
                var arches = (string.IsNullOrEmpty(a) ? Architecture.win32 : Range(a)).ToArray();

                if (t == Targets.win32)
                {
                    foreach (var arch in arches)
                    {
                        foreach (var build in builds)
                        {
                            ProcessSingle(version, t, build, arch);
                        }
                    }
                }
            }

            void ProcessLinux(string t, string b = null, string a = null)
            {
                var builds = (string.IsNullOrEmpty(b) ? Build.linux : Range(b)).ToArray();
                var arches = (string.IsNullOrEmpty(a) ? Architecture.linux : Range(a)).ToArray();

                if (t == Targets.linux)
                {
                    foreach (var arch in arches)
                    {
                        foreach (var build in builds)
                        {
                            ProcessSingle(version, t, build, arch);
                        }
                    }

                    ProcessSingle(version, t, Build.snap);
                }
            }

            void VetDownloadFlags()
            {
                /* We support arm64 arch downloads for win32 targets.
                * Downloads says "ARM" but it is really arm64 behind the link. */
                if (target == Targets.win32 && arch == Architecture.arm)
                {
                    arch = Architecture.arm64;
                }

                // Assumes Debian, RPM, or Snap builds are Linux targets.
                if (Range(Build.deb, Build.rpm, Build.snap).Contains(build))
                {
                    target = Targets.linux;
                }

                // Assumes x64 when linux and one of its builds specified.
                if (target == Targets.linux
                    && Range(Build.deb, Build.rpm, Build.archive).Contains(build)
                    && !Range(Architecture.x64, Architecture.arm, Architecture.arm64).Contains(arch))
                {
                    arch = Architecture.x64;
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

        private static Assets CurrentAssets { get; } = new Assets();

        private static Versions CurrentVersions { get; } = new Versions();

        public static void Main(string[] args)
        {
            var op = new OptionsParser();

            if (op.TryParseArguments(CurrentAssets, CurrentVersions, args);))
            {
                return;
            }

            void ReportNameValuePair<T>(string name, T value) => Console.WriteLine($"{name}: {value}".ToLower());

            if (op.dry)
            {
                ReportNameValuePair(nameof(op.dry), op.dry);
                ReportNameValuePair(nameof(op.all), op.all);
                ReportNameValuePair(nameof(op.NoPause), op.NoPause);
                ReportNameValuePair(nameof(target), target ?? string.Empty);
                ReportNameValuePair(nameof(arch), arch ?? string.Empty);
                ReportNameValuePair(nameof(build), build ?? string.Empty);
                ReportNameValuePair(nameof(op.insider), op.insider);
                ReportNameValuePair(nameof(op.stable), op.stable);
            }

            ProcessConfiguration(Versions.version);
        }

        private const int maxConsoleWindowWidth = 100;

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
            var widthOrConsoleWindowWidth = Math.Min(width ?? Console.WindowWidth, maxConsoleWindowWidth);

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

    private static class Directories
    {
        internal const string Windows = nameof(Windows);
        internal const string x64 = nameof(x64);
        internal const string x86 = nameof(x86);
        internal const string arm = nameof(arm);
        internal const string arm64 = nameof(arm64);
    }

    public class Versions
    {
        /// <summary>
        /// Gets the Latest Version for internal use. When we know that the
        /// <see cref="version"/> request is for the Latest, then we can
        /// simply use the word, &quot;latest&quot;.
        /// </summary>
        public static Version latest { get; } = Version.Parse("1.50.1");

        private Version _version;

        public Version version
        {
            get => _version ?? this.latest;
            set => _version = value;
        }

        public static Version macOS { get; } = Version.Parse("10.10");
    }

    internal class Assets
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
        public string WsusOfflineUri { get; } = "https://download.wsusoffline.net";

        /// <summary>
        /// &quot;https://update.code.visualstudio.com/&quot;
        /// </summary>
        public string UpdateCodeUri { get; } = "https://update.code.visualstudio.com/";

        /// <summary>
        /// &quot;https://code.visualstudio.com/Download&quot;
        /// </summary>
        public string CodeDownloadUri { get; } = "https://code.visualstudio.com/Download";

        /// <summary>
        /// &quot;https://github.com/microsoft/vscode/issues/109329&quot;
        /// </summary>
        /// <remarks>Automating the downloads with help from repeatable links</remarks>
        public string CodeGithubIssueUri { get; } = "https://github.com/microsoft/vscode/issues/109329";

        private static bool TryDiscoverAssets(out string path)
        {
            path = null;

            using (var process = new Process())
            {
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.FileName = Assets.where;
                process.StartInfo.Arguments = Assets.wget;
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
        /// Gets whether the <see cref="wgetPath"/> Assets Are Discovered.
        /// </summary>
        public bool AreDiscovered
        {
            get
            {
                Console.WriteLine("Discovering assets...");

                bool TryDiscoveryFailed()
                {
                    Console.WriteLine($"Unable to locate {wget} in your path.");
                    Console.WriteLine($"Redirecting to download {wsusOfflineUri} package.");
                    Process.Start(wsusOfflineUri);
                    return false;
                }

                return TryDiscoverAssets(out _wgetPath) || TryDiscoveryFailed();
            }
        }
    }

    public class OptionsParser
    {
        private bool help { get; set; }

        private bool ShowVersion { get; set; }

        internal bool NoPause { get; private set; }

        internal bool dry { get; private set; }

        internal bool all { get; private set; }

        internal bool insider { get; private set; }

        private IEnumerable<string> InsiderParts
        {
            get
            {
                if (this.insider)
                {
                    yield return nameof(this.insider).ToLower();
                }
            }
        }

        internal bool stable => !this.insider;

        private string SlashStableOrInsider => $"/{(this.stable ? nameof(this.stable) : nameof(this.insider))}";

        private Target CurrentTarget { get; set; } = default;

        private Architecture? CurrentArch { get; set; } = default;

        private Build? CurrentBuild { get; set; } = default;

        private static Assembly ProgramAssy { get; } = typeof(Program).Assembly;

        private static string ProgramFileName { get; } = Path.GetFileName(ProgramAssy.Location);

        private string HelpSummary { get; } = GetHelpSummary(ProgramFileName);

        private string RenderHelpSummary(string fileName)
        {
            static string RenderFlagValues<K, V>(K key, params V[] values)
            {
                const string pipe = "|";
                return $"--{key} {string.Join(pipe, values.Select(x => $"{x}"))}";
            }

            var target = nameof(Target).ToLower();
            var build = nameof(Build).ToLower();
            var arch = nameof(Architecture).Substring(0, 4).ToLower();

            const Target darwin = Target.darwin;
            const Target linux = Target.linux;
            const Target win32 = Target.win32;

            const Architecture x64 = Architecture.x64;
            const Architecture x86 = Architecture.x86;
            const Architecture arm = Architecture.arm;
            const Architecture arm64 = Architecture.arm64;

            const Build user = Build.user;
            const Build system = Build.system;
            const Build archive = Build.archive;
            const Build deb = Build.deb;
            const Build rpm = Build.rpm;
            const Build snap = Build.snap;

            return $@"Provides a command line programmatic view into the Code download web page matrix. The options describe the values for each option, but not all combinations are valid. The following combinations work for each area of the matrix.

  {fileName} {RenderFlagValues(target, darwin)}
  {fileName} {RenderFlagValues(target, win32)} {RenderFlagValues(build, user, system, archive)} {RenderFlagValues(arch, x64, x86, arm64)}
  {fileName} {RenderFlagValues(target, linux)} {RenderFlagValues(build, deb, rpm, archive)} {RenderFlagValues(arch, x64, x86, arm, arm64)}
  {fileName} {RenderFlagValues(target, linux)} {RenderFlagValues(build, snap)}

There is only one download for {nameof(Versions.macOS)} {darwin} {Versions.macOS}+, --{arch} --{build} are both ignored.
{RenderFlagValues(arch, arm)} is assumed to be {RenderFlagValues(arch, arm64)} when {RenderFlagValues(target, win32)} is specified.
--{arch} is ignored when {RenderFlagValues(target, linux)} {RenderFlagValues(build, smap)} is specified.
{RenderFlagValues(arch, x86)} is assumed to be {RenderFlagValues(arch, x64)} when {RenderFlagValues(target, linux)} is specified.

Based on the {CodeDownloadUri} web page and informed by the {CodeGithubIssueUri} code github issue.";
        }

        public bool TryParseArguments(Assets currentAssets, Versions currentVersions, params string[] args)
        {
            string OnRenderValue<T>(T value) => $"{value}";

            string GetArgument(int index) => args.ElementAt(index).ToLower();

            var noPauseOpts = Range("--no-pause").ToArray();
            var helpOpts = Range($"--{nameof(this.help)}", $"--{nameof(this.help).First()}").ToArray();
            var targetOpts = Range($"--{nameof(Target).ToLower()}", $"-{nameof(Target).ToLower().First()}").ToArray();
            var archOpts = Range($"--{nameof(Architecture).ToLower().Substring(0, 4)}", $"-{nameof(Architecture).ToLower().First()}").ToArray();
            var buildOpts = Range($"--{nameof(Build).ToLower()}", $"-{nameof(Build).ToLower().First()}").ToArray();
            var allOpts = Range($"--{nameof(this.all)}").ToArray();
            var dryOpts = Range($"--{nameof(this.dry)}").ToArray();
            var codeVersionOpts = Range($"--code-{nameof(Version).ToLower()}", $"-c{nameof(Version).ToLower().First()}").ToArray();
            var insiderOpts = Range($"--{nameof(this.insider)}", $"-{nameof(this.insider).First()}").ToArray();
            var versionOpts = Range($"--{nameof(Version).ToLower()}", $"-{nameof(Version).ToLower().First()}").ToArray();

            var targetValues = Range(Target.darwin, Target.linux, Target.win32).Select(OnRenderValue).ToArray();
            var archValues = Range(Architecture.x64, Architecture.x86, Architecture.ios, Architecture.arm, Architecture.arm64).Select(OnRenderValue).ToArray();
            var buildValues = Range(Build.user, Build.system, Build.archive, Build.deb, Build.rpm, Build.snap).Select(OnRenderValue).ToArray();
            var defaultValues = Range<string>().ToArray();

            // Reset the optional values to nominal defaults.
            this.NoPause = false;
            this.all = false;
            this.dry = false;
            this.ShowVersion = false;
            this.CurrentTarget = default;
            this.CurrentArch = default;
            this.CurrentBuild = default;

            bool TryPresentHelpOnReturn() => TryPresentHelp(
                this.HelpSummary
                , (helpOpts, "--x, what you are reading now.", defaultValues)
                , (targetOpts, "--x VALUE, the targets.", targetValues)
                , (archOpts, "--x VALUE, the architectures.", archValues)
                , (buildOpts, "--x VALUE, the builds.", buildValues)
                , (allOpts, "--x, get all targets, architectures, and builds", defaultValues)
                , (dryOpts, "--x, performs the features in dry run scenarios", defaultValues)
                , (codeVersionOpts, "--x VALUE, specify the Code version, or latest", Range(nameof(Versions.latest), nameof(Versions.version).ToUpper()).ToArray())
                , (insiderOpts, $"--x, whether to get the {nameof(this.insider)} or {nameof(this.stable)}", defaultValues)
                , (versionOpts, "--x, whether to show the downloader version", defaultValues)
            );

            int i;

            for (i = 0; i < args.Length; i++)
            {
                var arg = GetArgument(i);

                if (helpOpts.Contains(arg))
                {
                    this.help = true;
                    TryShowVersion();
                    return TryPresentHelpOnReturn();
                }

                if (versionOpts.Contains(arg))
                {
                    this.ShowVersion = true;
                    return TryShowVersion();
                }

                if (noPauseOpts.Contains(arg))
                {
                    this.NoPause = true;
                    continue;
                }

                // In generally the order in which you would review the downloads page.
                // https://code.visualstudio.com/Download
                if (targetOpts.Contains(arg))
                {
                    arg = GetArgument(++i);
                    if (targetValues.Contains(arg))
                    {
                        this.CurrentTarget = arg;
                    }
                    continue;
                }

                if (archOpts.Contains(arg))
                {
                    arg = GetArgument(++i);
                    if (archValues.Contains(arg))
                    {
                        this.CurrentArch = arg;
                    }
                    continue;
                }

                if (buildOpts.Contains(arg))
                {
                    arg = GetArgument(++i);
                    if (buildValues.Contains(arg))
                    {
                        this.CurrentBuild = arg;
                    }
                    continue;
                }

                if (insiderOpts.Contains(arg))
                {
                    this.insider = true;
                    continue;
                }

                if (allOpts.Contains(arg))
                {
                    this.all = true;
                    continue;
                }

                if (dryOpts.Contains(arg))
                {
                    this.dry = true;
                    continue;
                }

                if (codeVersionOpts.Contains(arg))
                {
                    currentVersions.version = Version.Parse(arg);
                    continue;
                }
            }

            if (!currentAssets.AreDiscovered.Invoke())
            {
                return false;
            }

            if (this.dry)
            {
                Console.WriteLine($"{nameof(dry)}: {nameof(args)}.{nameof(args.Length)}: {args.Length}, {nameof(i)}: {i}");
            }

            return i == args.Length;
        }
    }

    // win32+system+x86+version => VSCodeSetup-ia32-1.49.2.exe
    // win32+user+x86+version => VSCodeUserSetup-ia32-1.49.2.exe
    // win32+archive+x86+version => VSCode-win32-ia32-1.49.2.zip

    // win32+system+x64+version => VSCodeSetup-x86-1.49.2.exe
    // win32+user+x64+version => VSCodeUserSetup-x86-1.49.2.exe
    // win32+archive+x64+version => VSCode-win32-x86-1.49.2.zip

    // win32+system+x64+version => VSCodeSetup-x86-1.49.2.exe
    // win32+user+x64+version => VSCodeUserSetup-x86-1.49.2.exe
    // win32+archive+x64+version => VSCode-win32-x86-1.49.2.zip

    // win32+system+arm64+version => VSCodeSetup-arm64-1.49.2.exe
    // win32+user+arm64+version => VSCodeUserSetup-arm64-1.49.2.exe
    // win32+archive+arm64+version => VSCode-win32-arm64-1.49.2.zip

    // linux+deb+x64+version => code_1.49.2-amd64.deb
    // linux+rpm+x64+version => code_1.49.2-amd64.rpm
    // linux+archive+x64+version => code_1.49.2-amd64.tar.gz

    // darwin+version+stable => VSCode-darwin-1.49.2-stable.zip

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

    public enum Architectures
    {
        x64,
        x86,
        ios,
        arm,
        armhf,
        arm64
    }

    private enum Build
    {
        user,
        system,
        deb,
        rpm,
        archive,
        snap
    }

    // darwin
    // linux-arm64
    // linux-armhf
    // linux-deb-arm64
    // linux-deb-armhf
    // linux-deb-x64
    // linux-rpm-arm64
    // linux-rpm-armhf
    // linux-rpm-x64
    // linux-snap-x64
    // linux-x64
    // win32
    // win32-archive
    // win32-arm64
    // win32-arm64-archive
    // win32-arm64-user
    // win32-user
    // win32-x64
    // win32-x64-archive
    // win32-x64-user

    public class DownloadDescriptor
    {
        private static IEnumerable<Architecture> GetArchitectures(Target target)
        {
            if (target == Target.win32)
            {
                yield return Architecture.x64;
                yield return Architecture.x86;
                yield return Architecture.arm64;
            }
            else if (target == Target.linux)
            {
                yield return Architecture.x64;
                yield return Architecture.arm;
                yield return Architecture.arm64;
            }
        }

        private static IEnumerable<Build> GetBuilds(Target target)
        {
            if (target == Target.win32)
            {
                yield return Build.user;
                yield return Build.system;
                yield return Build.archive;
            }
            else if (target == Target.linux)
            {
                yield return Build.deb;
                yield return Build.rpm;
                yield return Build.archive;
            }
        }

        private static IDictionary<Target, IEnumerable<Architecture>> _targetArchitectures;

        private static IDictionary<Target, IEnumerable<Build>> _targetBuilds;

        public static IDictionary<Target, IEnumerable<Architecture>> TargetArchitectures => _targetArchitectures ?? (
            _targetArchitectures = Range(Target.win32, Target.linux, Target.darwin).ToDictionary(x => x, GetArchitectures)
        );

        public static IDictionary<Target, IEnumerable<Build>> TargetBuilds => _targetBuilds ?? (
            _targetBuilds = Range(Target.win32, Target.linux, Target.darwin).ToDictionary(x => x, GetBuilds)
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

            if (this.Target == Target.darwin)
            {
                this.Elements = (this.Target, null, null);
            }
        }
    }
}
