#nullable enable
using System;
using VL.Core;
using VL.Core.Import;
using VL.Lib.Secrets;

[assembly: ImportType(typeof(SecretStore), Category = "System")]

namespace VL.Lib.Secrets
{
    /// <summary>
    /// Encrypts secrets a patch keeps in a file (an access token) so that only the user who runs it, on this computer, can read them again (<see cref="IPlatformServices"/>:
    /// DPAPI on Windows). Where the app has no secret store (Linux, macOS) nothing is encrypted and Success is false: keep no secret then.
    /// </summary>
    public static class SecretStore
    {
        /// <summary>Encrypts the bytes for the current user of this computer.</summary>
        /// <param name="data">The secret.</param>
        /// <param name="success">False when the app has no secret store: the output is then empty.</param>
        /// <returns>The encrypted bytes.</returns>
        public static byte[] Protect(byte[] data, out bool success)
        {
            var result = PlatformServices.Default?.ProtectForCurrentUser(data ?? Array.Empty<byte>());
            success = result is not null;
            return result ?? Array.Empty<byte>();
        }

        /// <summary>Decrypts what Protect gave.</summary>
        /// <param name="data">The encrypted bytes.</param>
        /// <param name="success">False when the app has no secret store or the bytes cannot be decrypted (another user or computer, damaged): the output is then empty.</param>
        /// <returns>The secret.</returns>
        public static byte[] Unprotect(byte[] data, out bool success)
        {
            var result = PlatformServices.Default?.UnprotectForCurrentUser(data ?? Array.Empty<byte>());
            success = result is not null;
            return result ?? Array.Empty<byte>();
        }
    }
}
