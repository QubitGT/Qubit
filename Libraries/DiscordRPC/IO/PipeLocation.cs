using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Qubit.Libraries.DiscordRPC.IO
{
    


    public static class PipeLocation
    {
        const string DiscordPipePrefix = @"discord-ipc-";
        const int MaximumPipeVariations = 10;

        static readonly string[] LinuxPackageManagers = {
            "app/com.discordapp.Discord/",	
			"snap.discord/",				
			
		};

        








        public static IEnumerable<string> GetPipes(int startPipe = 0)
        {
            IsOSUnix();
            return IsOSUnix()
                ? Enumerable.Range(startPipe, MaximumPipeVariations).SelectMany(GetUnixPipes)
                : Enumerable.Range(startPipe, MaximumPipeVariations).SelectMany(GetWindowsPipes);
        }

        private static IEnumerable<string> GetWindowsPipes(int index)
        {
            yield return $"{DiscordPipePrefix}{index}";
        }

        private static IEnumerable<string> GetUnixPipes(int index)
        {
            foreach (var tempDir in TemporaryDirectories())
            {
                
                
                yield return Path.Combine(tempDir, $"{DiscordPipePrefix}{index}");

                
                foreach (var pmDir in LinuxPackageManagers)
                    yield return Path.Combine(tempDir, pmDir, $"{DiscordPipePrefix}{index}");
            }
        }

        private static IEnumerable<string> TemporaryDirectories()
        {
            string temp;
            temp = Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR");
            if (temp != null)
                yield return temp;

            temp = Environment.GetEnvironmentVariable("TMPDIR");
            if (temp != null)
                yield return temp;

            temp = Environment.GetEnvironmentVariable("TMP");
            if (temp != null)
                yield return temp;

            temp = Environment.GetEnvironmentVariable("TEMP");
            if (temp != null)
                yield return temp;

            yield return "/temp";
        }

        private static bool IsOSUnix()
        {
            if (Environment.OSVersion.Platform == PlatformID.Unix)
                return true;

#if NETFRAMEWORK // MacOS was replaced with Unix in .NET Core
			if (Environment.OSVersion.Platform == PlatformID.MacOSX)
				return true;
#endif

            return false;
        }
    }
}
