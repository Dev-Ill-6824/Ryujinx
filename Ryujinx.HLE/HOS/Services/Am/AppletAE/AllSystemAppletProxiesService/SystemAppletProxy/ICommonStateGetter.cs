using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Ipc;
using Ryujinx.HLE.HOS.Kernel.Common;
using Ryujinx.HLE.HOS.Kernel.Threading;
using System;

namespace Ryujinx.HLE.HOS.Services.Am.AppletAE.AllSystemAppletProxiesService.SystemAppletProxy
{
    class ICommonStateGetter : IpcService
    {
        private Apm.ManagerServer       apmManagerServer;
        private Apm.SystemManagerServer apmSystemManagerServer;

        private bool _vrModeEnabled = false;

        public ICommonStateGetter(ServiceCtx context)
        {
            apmManagerServer       = new Apm.ManagerServer(context);
            apmSystemManagerServer = new Apm.SystemManagerServer(context);
        }

        [Command(0)]
        // GetEventHandle() -> handle<copy>
        public ResultCode GetEventHandle(ServiceCtx context)
        {
            KEvent Event = context.Device.System.AppletState.MessageEvent;

            if (context.Process.HandleTable.GenerateHandle(Event.ReadableEvent, out int handle) != KernelResult.Success)
            {
                throw new InvalidOperationException("Out of handles!");
            }

            context.Response.HandleDesc = IpcHandleDesc.MakeCopy(handle);

            return ResultCode.Success;
        }

        [Command(1)]
        // ReceiveMessage() -> nn::am::AppletMessage
        public ResultCode ReceiveMessage(ServiceCtx context)
        {
            if (!context.Device.System.AppletState.TryDequeueMessage(out MessageInfo message))
            {
                return ResultCode.NoMessages;
            }

            context.ResponseData.Write((int)message);

            return ResultCode.Success;
        }

        [Command(5)]
        // GetOperationMode() -> u8
        public ResultCode GetOperationMode(ServiceCtx context)
        {
            OperationMode mode = context.Device.System.State.DockedMode
                ? OperationMode.Docked
                : OperationMode.Handheld;

            context.ResponseData.Write((byte)mode);

            return ResultCode.Success;
        }

        [Command(6)]
        // GetPerformanceMode() -> nn::apm::PerformanceMode
        public ResultCode GetPerformanceMode(ServiceCtx context)
        {
            return (ResultCode)apmManagerServer.GetPerformanceMode(context);
        }

        [Command(8)]
        // GetBootMode() -> u8
        public ResultCode GetBootMode(ServiceCtx context)
        {
            context.ResponseData.Write((byte)0); //Unknown value.

            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return ResultCode.Success;
        }

        [Command(9)]
        // GetCurrentFocusState() -> u8
        public ResultCode GetCurrentFocusState(ServiceCtx context)
        {
            context.ResponseData.Write((byte)context.Device.System.AppletState.FocusState);

            return ResultCode.Success;
        }

        [Command(50)] // 3.0.0+
        // IsVrModeEnabled() -> b8
        public ResultCode IsVrModeEnabled(ServiceCtx context)
        {
            context.ResponseData.Write(_vrModeEnabled);

            return ResultCode.Success;
        }

        [Command(60)] // 3.0.0+
        // GetDefaultDisplayResolution() -> (u32, u32)
        public ResultCode GetDefaultDisplayResolution(ServiceCtx context)
        {
            context.ResponseData.Write(1280);
            context.ResponseData.Write(720);

            return ResultCode.Success;
        }

        [Command(61)] // 3.0.0+
        // GetDefaultDisplayResolutionChangeEvent() -> handle<copy>
        public ResultCode GetDefaultDisplayResolutionChangeEvent(ServiceCtx context)
        {
            if (context.Process.HandleTable.GenerateHandle(context.Device.System.DisplayResolutionChangeEvent.ReadableEvent, out int handle) != KernelResult.Success)
            {
                throw new InvalidOperationException("Out of handles!");
            }

            context.Response.HandleDesc = IpcHandleDesc.MakeCopy(handle);

            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return ResultCode.Success;
        }

        [Command(66)] // 6.0.0+
        // SetCpuBoostMode(u32 cpu_boost_mode)
        public ResultCode SetCpuBoostMode(ServiceCtx context)
        {
            uint cpuBoostMode = context.RequestData.ReadUInt32();

            if (cpuBoostMode > 1)
            {
                return ResultCode.InvalidParameters;
            }

            apmSystemManagerServer.SetCpuBoostMode((Apm.CpuBoostMode)cpuBoostMode);

            // TODO: It signals an internal event of ICommonStateGetter. We have to determine where this event is used. 

            return ResultCode.Success;
        }

        [Command(91)] // 7.0.0+
        // GetCurrentPerformanceConfiguration() -> nn::apm::PerformanceConfiguration
        public ResultCode GetCurrentPerformanceConfiguration(ServiceCtx context)
        {
            return (ResultCode)apmSystemManagerServer.GetCurrentPerformanceConfiguration(context);
        }
    }
}