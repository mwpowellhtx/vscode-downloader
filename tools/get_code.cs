using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

[assembly: AssemblyVersion("0.1.0.0")]

// TODO: TBD: https://docs.microsoft.com/en-us/dotnet/framework/app-domains/build-single-file-assembly
// TODO: TBD: Automating the downloads with help from repeatable links / https://github.com/microsoft/vscode/issues/109329
// TODO: TBD: just about done with this one...
// TODO: TBD: I may also commit it to github after all, we'll see...
// TODO: TBD: just about all the plumbing is in and verified...
// TODO: TBD: also have the assets verification pretty much working...
// TODO: TBD: what remains is to verify that the wget process itself actually works as expected...
// TODO: TBD: might consider giving the Code repo a gander and establishing this repo as a seamless overlay there...
// TODO: TBD: with the thought being that potentially could merge the two repos, potentially...
// TODO: TBD: https://github.com/Microsoft/vscode so if we homed it under .../tools that would be compatible...
namespace Code.Downloader
{
    /// <summary>
    /// It is a bit crude I will admit, but the intention here is to run very light. Literally,
    /// no dependencies, no other files. Literally, the only thing we should need to do here is
    /// to &quot;csc filename&quot; and that&apos;s it. Maybe a handful of csc arguments as well,
    /// but that is all. Almost a lite CSharp script of sorts, short of adopting a
    /// <em>PowerShell</em> approach. Which so far we are able to accomplish with
    /// a nominal set of System level using statements.
    /// </summary>
    public static class Program
    {
        private static class Assets
        {
            /// <summary>
            /// Assumes that WGET is in your path somewhere. Refer to the WSUS URL
            /// for downloads of a package containing the WGET resource.
            /// </summary>
            /// <see cref="wsusOfflineUri"/>
            internal static string wget { get; } = $"{nameof(wget)}.exe";

            internal static string where { get; } = $"{nameof(where)}.exe";

            private static string _wgetPath;

            internal static string wgetPath => _wgetPath;

            /// <summary>
            /// Uri to the WSUS Offline downloads, &quot;https://download.wsusoffline.net&quot;.
            /// </summary>
            /// <see cref="!:https://download.wsusoffline.net">WSUS Offline Download</see>
            private const string wsusOfflineUri = "https://download.wsusoffline.net";

            /// <summary>
            /// &quot;https://update.code.visualstudio.com/&quot;
            /// </summary>
            internal const string updateCodeUri = "https://update.code.visualstudio.com/";

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
            internal static bool areDiscovered
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

        private static class Versions
        {
            /// <summary>
            /// Gets the Latest Version for internal use. When we know that the
            /// <see cref="version"/> request is for the Latest, then we can
            /// simply use the word, &quot;latest&quot;.
            /// </summary>
            internal static Version latest { get; } = Version.Parse("1.50.1");

            private static Version _version;

            internal static Version version
            {
                get => _version ?? latest;
                set => _version = value;
            }

            internal static Version MacOS { get; } = Version.Parse("10.10");
        }

        private static class Directories
        {
            internal const string macOS = nameof(macOS);
            internal const string Windows = nameof(Windows);
            internal const string x64 = nameof(x64);
            internal const string x86 = nameof(x86);
            internal const string arm = nameof(arm);
            internal const string arm64 = nameof(arm64);
        }

        private static class Targets
        {
            /// <summary>
            /// For use with macOS.
            /// </summary>
            internal const string darwin = nameof(darwin);

            /// <summary>
            /// For all Linux flavors.
            /// </summary>
            internal const string linux = nameof(linux);

            /// <summary>
            /// For use with all Windows flavors.
            /// </summary>
            internal const string win32 = nameof(win32);
        }

        private static class Architectures
        {
            internal const string x64 = nameof(x64);
            internal const string x86 = nameof(x86);
            internal const string ios = nameof(ios);
            internal const string arm = nameof(arm);
            internal const string armhf = nameof(armhf);
            internal const string arm64 = nameof(arm64);
            internal static IEnumerable<string> Win32 => Range(x64, x86, arm64);

            /// <summary>
            /// These are the repeatable Linux use cases, whereas <see cref="Builds.snap"/>
            /// is a special use case.
            /// </summary>
            internal static IEnumerable<string> Linux => Range(x64, arm, arm64);
        }

        private static class Builds
        {
            internal const string user = nameof(user);
            internal const string system = nameof(system);
            internal const string deb = nameof(deb);
            internal const string rpm = nameof(rpm);
            internal const string archive = nameof(archive);
            internal const string snap = nameof(snap);
            internal static IEnumerable<string> Win32 => Range(user, system, archive);
            internal static IEnumerable<string> Linux => Range(deb, rpm, archive);
        }

        private static bool help { get; set; }

        private static bool dry { get; set; }

        private static bool all { get; set; }

        private static bool insider { get; set; }

        private static IEnumerable<string> insiderParts
        {
            get
            {
                if (insider)
                {
                    yield return nameof(insider);
                }
            }
        }

        private static bool stable => !insider;

        private static string slashStableOrInsider => $"/{(stable ? nameof(stable) : nameof(insider))}";

        private static bool nopause { get; set; }

        private static string target { get; set; }

        private static string arch { get; set; }

        private static string build { get; set; }

        private static bool showVersion { get; set; }

        private static IEnumerable<T> Range<T>(params T[] values)
        {
            foreach (var value in values)
            {
                yield return value;
            }
        }

        private static bool TryPresentHelp(params (string[] flags, string description, string[] values)[] opts)
        {
            // TODO: TBD: or potentially determine the width of the console...
            var console_WindowWidth = Console.WindowWidth;

            //const int colWidth = 4;
            const string flagDelim = ", ";

            var maxFlagWidth = opts.Max(x => x.flags.Sum(y => y.Length) + (x.flags.Length - 1) * flagDelim.Length);
            //var flagsWidth = ((int)(maxFlagWidth / colWidth) + 1) * colWidth;

            IEnumerable<string> GetDescriptionLines(string s)
            {
                while (s.Any())
                {
                    var line = s.Substring(0, Math.Min(s.Length, console_WindowWidth - maxFlagWidth));
                    s = s.Substring(line.Length, s.Length - line.Length);
                    yield return line;
                }
            }

            foreach (var (flags, description, values) in opts)
            {
                Console.WriteLine(string.Join(flagDelim, flags));

                foreach (var line in GetDescriptionLines(description))
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

        private static Assembly OnShowVersionAssySelector(Type rootType) => rootType.Assembly;

        private static bool TryShowVersion(Type rootType, Func<Type, Assembly> assySelector)
        {
            var assy = assySelector.Invoke(rootType);
            var assyName = assy.GetName();
            var assyFileName = Path.GetFileName(assy.Location);
            Console.WriteLine($"{assyFileName} {assyName.Version}");
            return false;
        }

        private static bool TryShowVersion() => TryShowVersion(typeof(Program), OnShowVersionAssySelector);

        private static bool TryParseArguments(Func<bool> onAreAssetsDiscovered, params string[] args)
        {
            string GetArgument(int index) => args.ElementAt(index).ToLower();

            var helpOpts = Range($"--{nameof(help)}", $"--{nameof(help).First()}").ToArray();
            var targetOpts = Range($"--{nameof(target)}", $"-{nameof(target).First()}").ToArray();
            var archOpts = Range($"--{nameof(arch)}", $"-{nameof(arch).First()}").ToArray();
            var buildOpts = Range($"--{nameof(build)}", $"-{nameof(build).First()}").ToArray();
            var allOpts = Range($"--{nameof(all)}").ToArray();
            var dryOpts = Range($"--{nameof(dry)}").ToArray();
            var codeVersionOpts = Range($"--code-{nameof(Version).ToLower()}", $"-c{nameof(Version).ToLower().First()}").ToArray();
            var insiderOpts = Range($"--{nameof(insider)}", $"-{nameof(insider).First()}").ToArray();
            var versionOpts = Range($"--{nameof(Version).ToLower()}", $"-{nameof(Version).ToLower().First()}").ToArray();

            var targetValues = Range(Targets.darwin, Targets.linux, Targets.win32).ToArray();
            var archValues = Range(Architectures.x64, Architectures.x86, Architectures.ios, Architectures.arm, Architectures.arm64).ToArray();
            var buildValues = Range(Builds.user, Builds.system, Builds.archive, Builds.deb, Builds.rpm, Builds.snap).ToArray();
            var defaultValues = Range<string>().ToArray();

            nopause = false;
            all = false;
            dry = false;
            showVersion = false;
            target = null;
            arch = null;
            build = null;

            bool TryPresentHelpOnReturn() => TryPresentHelp(
                (helpOpts, "--x, what you are reading now.", defaultValues)
                , (targetOpts, "--x VALUE, the targets.", targetValues)
                , (archOpts, "--x VALUE, the architectures.", archValues)
                , (buildOpts, "--x VALUE, the builds.", buildValues)
                , (allOpts, "--x, get all targets, architectures, and builds", defaultValues)
                , (dryOpts, "--x, performs the features in dry run scenarios", defaultValues)
                , (codeVersionOpts, "--x VALUE, specify the Code version, or latest", Range(nameof(Versions.latest), nameof(Versions.version).ToUpper()).ToArray())
                , (insiderOpts, $"--x, whether to get the {nameof(insider)} or {nameof(stable)}", defaultValues)
                , (versionOpts, "--x, whether to show the downloader version", defaultValues)
            );

            int i;

            for (i = 0; i < args.Length; i++)
            {
                var arg = GetArgument(i);

                if (helpOpts.Contains(arg))
                {
                    help = true;
                    TryShowVersion();
                    return TryPresentHelpOnReturn();
                }

                if (versionOpts.Contains(arg))
                {
                    showVersion = true;
                    return TryShowVersion();
                }

                if (arg == "--no-pause")
                {
                    nopause = true;
                    continue;
                }

                // In generally the order in which you would review the downloads page.
                // https://code.visualstudio.com/Download
                if (targetOpts.Contains(arg))
                {
                    arg = GetArgument(++i);
                    if (targetValues.Contains(arg))
                    {
                        target = arg;
                    }
                    continue;
                }

                if (archOpts.Contains(arg))
                {
                    arg = GetArgument(++i);
                    if (archValues.Contains(arg))
                    {
                        arch = arg;
                    }
                    continue;
                }

                if (buildOpts.Contains(arg))
                {
                    arg = GetArgument(++i);
                    if (buildValues.Contains(arg))
                    {
                        build = arg;
                    }
                    continue;
                }

                if (insiderOpts.Contains(arg))
                {
                    insider = true;
                    continue;
                }

                if (allOpts.Contains(arg))
                {
                    all = true;
                    continue;
                }

                if (dryOpts.Contains(arg))
                {
                    dry = true;
                    continue;
                }

                if (codeVersionOpts.Contains(arg))
                {
                    Versions.version = Version.Parse(arg);
                    continue;
                }
            }

            if (!onAreAssetsDiscovered.Invoke())
            {
                return false;
            }

            if (dry)
            {
                Console.WriteLine($"{nameof(dry)}: {nameof(args)}.{nameof(args.Length)}: {args.Length}, {nameof(i)}: {i}");
            }

            return i == args.Length;
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
        ///
        /// </summary>
        /// <param name="version">A rendered version string.</param>
        /// <param name="t">A target.</param>
        /// <param name="b">An optional build.</param>
        /// <param name="a">An optional architecture.</param>
        private static void ProcessSingle(string version, string t, string b = null, string a = null)
        {
            if (dry)
            {
                Console.WriteLine($"{nameof(dry)}: {nameof(ProcessSingle)}({nameof(version)}: '{version}', {nameof(t)}: '{t}', {nameof(b)}: {RenderStringOrNull(b)}, {nameof(a)}: {RenderStringOrNull(a)})");
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

            b = b ?? string.Empty;
            a = a ?? string.Empty;

            // Render both the updateCodeUri and version bits...
            var baseUriVersion = insider
                ? $"{Assets.updateCodeUri}{version}-{nameof(insider)}/"
                : $"{Assets.updateCodeUri}{version}/";
            version = version == nameof(Versions.latest) ? $"{Versions.latest}" : version;

            void MakeDirectory(string path)
            {
                if (dry)
                {
                    Console.WriteLine($"{nameof(dry)}: Making directory: {path}");
                    return;
                }

                Directory.CreateDirectory(path);
            }

            bool TryProcessAny(string path, string versionUriPhrase)
            {
                MakeDirectory(path);

                var uri = $"{baseUriVersion}{versionUriPhrase}{slashStableOrInsider}";

                if (dry)
                {
                    Console.WriteLine($"{nameof(dry)}: {Assets.wget} {uri}");
                    return true;
                }

                // TODO: TBD: otherwise, actually do the get...

                return true;
            }

            string RenderAssertOrAssetInsiderPhrase(params string[] parts) => string.Join(
                "-", parts.Concat(insiderParts).Where(x => !string.IsNullOrEmpty(x))
            );

            // TODO: TBD: can probable refactor the general case given a path, uri, and directory...
            bool TryProcessWin32()
            {
                var build = b == Builds.system ? null : b;
                var arch = a == Architectures.x86 ? null : a;
                return TryProcessAny(Path.Combine(version, t, a), RenderAssertOrAssetInsiderPhrase(t, arch, build));
            }

            bool TryProcessLinux()
            {
                var build = b == Builds.archive ? null : b;

                var arch = b == Builds.snap ? Architectures.x64
                    : (a == Architectures.arm ? Architectures.armhf : a);

                var phrase = RenderAssertOrAssetInsiderPhrase(t, build, arch);

                return b == Builds.snap
                    ? TryProcessAny(Path.Combine(version, t, b), phrase)
                    : TryProcessAny(Path.Combine(version, t, a), phrase);
            }

            bool TryProcessDarwin() => TryProcessAny(Path.Combine(version, Directories.macOS, $"{Versions.MacOS}+"), RenderAssertOrAssetInsiderPhrase(t));

            switch (t)
            {
                case Targets.win32:
                    TryProcessWin32();
                    break;

                case Targets.linux:
                    TryProcessLinux();
                    break;

                case Targets.darwin:
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
        private static void ProcessConfiguration(Version v)
        {
            var version = v == Versions.latest ? nameof(Versions.latest) : $"{v}";

            const string @null = nameof(@null);

            if (dry)
            {
                Console.WriteLine($"{nameof(dry)}: {nameof(ProcessConfiguration)}({nameof(v)}: {RenderStringOrNull(v, OnRenderVersion)}), {nameof(version)}: {RenderStringOrNull(version)}");
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
                var builds = (string.IsNullOrEmpty(b) ? Builds.Win32 : Range(b)).ToArray();
                var arches = (string.IsNullOrEmpty(a) ? Architectures.Win32 : Range(a)).ToArray();

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
                var builds = (string.IsNullOrEmpty(b) ? Builds.Linux : Range(b)).ToArray();
                var arches = (string.IsNullOrEmpty(a) ? Architectures.Linux : Range(a)).ToArray();

                if (t == Targets.linux)
                {
                    foreach (var arch in arches)
                    {
                        foreach (var build in builds)
                        {
                            ProcessSingle(version, t, build, arch);
                        }
                    }

                    ProcessSingle(version, t, Builds.snap);
                }
            }

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

                ProcessWin32(t ?? Targets.win32, b, a);
                ProcessLinux(t ?? Targets.linux, b, a);
                ProcessMacOS(t ?? Targets.darwin);
            }
        }

        public static void Main(string[] args)
        {
            if (!TryParseArguments(() => Assets.areDiscovered, args))
            {
                return;
            }

            void ReportNameValuePair<T>(string name, T value) => Console.WriteLine($"{name}: {value}".ToLower());

            if (dry)
            {
                ReportNameValuePair(nameof(dry), dry);
                ReportNameValuePair(nameof(all), all);
                ReportNameValuePair(nameof(nopause), nopause);
                ReportNameValuePair(nameof(target), target ?? string.Empty);
                ReportNameValuePair(nameof(arch), arch ?? string.Empty);
                ReportNameValuePair(nameof(build), build ?? string.Empty);
                ReportNameValuePair(nameof(insider), insider);
                ReportNameValuePair(nameof(stable), stable);
            }

            ProcessConfiguration(Versions.version);
        }
    }
}
