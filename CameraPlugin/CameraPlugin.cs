using Dalamud.Game.ClientState;
using Dalamud.Game.Command;
using Dalamud.Game.Internal;
using Dalamud.Plugin;
using ImGuiNET;
using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace CameraPlugin
{
    public sealed partial class CameraPlugin : IDalamudPlugin
    {
        public string Name => "CameraMan";
        private const string CameraCommand = "/camera";

        internal DalamudPluginInterface Interface;
        internal CameraAddressResolver Address;
        internal bool IsImguiSetupOpen = false;
        internal CameraMemory InitialValues;

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.Interface = pluginInterface ?? throw new ArgumentNullException(nameof(pluginInterface), "DalamudPluginInterface cannot be null");
            this.Interface.CommandManager.AddHandler(CameraCommand, new CommandInfo(CommandHandler));
            this.Interface.UiBuilder.OnBuildUi += UiBuilder_OnBuildUi;

            this.Address = new CameraAddressResolver();
            this.Address.Setup(pluginInterface.TargetModuleScanner);

            this.InitialValues = Marshal.PtrToStructure<CameraMemory>(Address.CameraAddress);

            this.Interface.Framework.OnUpdateEvent += Framework_OnUpdateEvent;
        }

        public void Dispose()
        {
            this.Interface.CommandManager.RemoveHandler(CameraCommand);
            this.Interface.UiBuilder.OnBuildUi -= UiBuilder_OnBuildUi;
            this.Interface.Framework.OnUpdateEvent -= Framework_OnUpdateEvent;
        }

        internal void GameLogMessage(string message) => Interface.Framework.Gui.Chat.Print(message);

        internal void GameLogError(string message) => Interface.Framework.Gui.Chat.PrintError(message);

        private float OutOfCombatZoomMax = 20;
        private bool WasInCombatPreviously = false;

        public unsafe void Framework_OnUpdateEvent(Framework framework)
        {
            if (Interface.ClientState.Condition[ConditionFlag.InCombat])
            {
                if (!WasInCombatPreviously)
                {
                    WasInCombatPreviously = true;

                    var mem = (CameraMemory*)Address.CameraAddress;
                    if (mem != null)
                    {
                        OutOfCombatZoomMax = mem->zoomMax;
                        if (mem->zoomCurrent > InitialValues.zoomMax)
                            mem->zoomCurrent = InitialValues.zoomMax;
                        if (mem->zoomMax > InitialValues.zoomMax)
                            mem->zoomMax = InitialValues.zoomMax;
                    }
                }
            }
            else
            {
                if (WasInCombatPreviously)
                {
                    WasInCombatPreviously = false;
                    var mem = (CameraMemory*)Address.CameraAddress;
                    if (mem != null)
                    {
                        mem->zoomMax = OutOfCombatZoomMax;
                    }
                }
            }
        }

        public void CommandHandler(string command, string args) => IsImguiSetupOpen = true;

        public unsafe void UiBuilder_OnBuildUi()
        {
            if (!IsImguiSetupOpen)
                return;

            ImGui.SetNextWindowSize(new Vector2(400, 150), ImGuiCond.Always);
            ImGui.Begin("Zoom Setup", ref IsImguiSetupOpen, ImGuiWindowFlags.NoResize);

            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 5));

            ImGui.Text($"Note: Settings are reset to their initial login values during combat.");

            if (Interface.ClientState.Condition[ConditionFlag.InCombat])
            {
                ImGui.Text("---In Combat---");
            }
            else
            {
                var mem = (CameraMemory*)Address.CameraAddress;
                if (mem != null)
                {
                    ImGui.SliderFloat("Zoom Current", ref mem->zoomCurrent, 0, mem->zoomMax);
                    if (ImGui.SliderFloat("Zoom Max", ref mem->zoomMax, 0, 100))
                    {
                        if (mem->zoomMax < mem->zoomCurrent)
                            mem->zoomCurrent = mem->zoomMax;
                    }
                }
                else
                {
                    ImGui.Text($"---Error---");
                }
            }

            ImGui.PopStyleVar();

            ImGui.End();
        }

        private float ParseFloat(string arg)
        {
            try
            {
                return float.Parse(arg);
            }
            catch (FormatException)
            {
                return float.NaN;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct CameraMemory
        {
            public float zoomCurrent;
            public float zoomInterval;
            public float zoomMax;
            public float fovCurrent;
            public float fovInterval;
            public float fovMax;
            public float unkCurrent;
            public float unkInterval;
            public float unkMax;
        }

        private void SetZoom(float value) => SetValue(value, "zoomCurrent");

        private void SetZoomMax(float value) => SetValue(value, "zoomMax");

        private void SetFov(float value) => SetValue(value, "fovCurrent");

        private void SetFovMax(float value) => SetValue(value, "fovMax");

        private void SetValue(float value, string cameraMemoryField)
        {
            var floatArray = new float[] { value };
            var address = Address.CameraAddress + Marshal.OffsetOf(typeof(CameraMemory), cameraMemoryField).ToInt32();
            Marshal.Copy(floatArray, 0, address, floatArray.Length);
        }
    }
}
