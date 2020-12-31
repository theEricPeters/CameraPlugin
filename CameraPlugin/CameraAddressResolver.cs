using Dalamud.Game;
using Dalamud.Game.Internal;
using Dalamud.Plugin;
using System;
using System.Runtime.InteropServices;

namespace CameraPlugin
{
    internal class CameraAddressResolver : BaseAddressResolver
    {
        private const string CameraSignature = "48 8D 0D ?? ?? ?? ?? 45 33 C0 33 D2 C6 40 09 01";
        internal IntPtr CameraAddress { get; private set; }

        protected override void Setup64Bit(SigScanner scanner)
        {
            PluginLog.Verbose("===== Camera =====");
            CameraAddress = scanner.GetStaticAddressFromSig(CameraSignature);
            CameraAddress = Marshal.ReadIntPtr(CameraAddress) + 0x114;
            PluginLog.Verbose($"{nameof(CameraAddress)} {CameraAddress.ToInt64():X}");
        }
    }
}
