﻿using FreePIE.Core.Plugins.Dx;
using FreePIE.Core.Plugins.VJoy.PacketData;
using System;
using System.Collections.Generic;

namespace FreePIE.Core.Plugins.VJoy
{
    public interface IAction<T>
    {
        void Call(T param);
    }

    public abstract class PacketAction
    {
        public abstract IAction<IList<ICollection<Device>>> Convert(FfbPacket packet);
    }

    /// <summary>
    /// Wrapper class for extracting <typeparamref name="T"/> from an <see cref="FfbPacket"/>, and apply it to devices using the given <see cref="action"/>
    /// </summary>
    /// <typeparam name="T"><see cref="IFfbPacketData"/> type to convert to.</typeparam>
    public class PacketAction<T> : PacketAction
        where T : IFfbPacketData
    {
        public readonly Action<Device, T> action;
        public PacketAction(Action<Device, T> act)
        {
            action = act;
        }

        public override IAction<IList<ICollection<Device>>> Convert(FfbPacket packet)
        {
            return new AsyncPacketData<T>(action, packet);
        }
    }

    public class AsyncPacketData<T> : PacketAction<T>, IAction<IList<ICollection<Device>>>
            where T : IFfbPacketData
    {
        public readonly FfbPacket packet;
        public readonly T convertedPacket;
        private readonly DateTime timestamp;

        public AsyncPacketData(Action<Device, T> a, FfbPacket p) : base(a)
        {
            timestamp = DateTime.Now;
            packet = p;
            convertedPacket = packet.GetPacketData<T>();
        }

        public void Call(IList<ICollection<Device>> registeredDevices)
        {
            //DEBUG: print useful information
            Console.WriteLine("----------------------");
            Console.WriteLine(packet);
            Console.WriteLine(convertedPacket);
            Console.WriteLine("Receive->process delay: {0:N3}ms", (DateTime.Now - timestamp).TotalMilliseconds);
            if (action != null)
            {
                var rdevs = registeredDevices[packet.DeviceId - 1];
                Console.WriteLine("Forwarding to {0} device{1}", rdevs.Count, rdevs.Count > 1 ? "s" : "");
                try
                {
                    foreach (var dev in rdevs)
                        action(dev, convertedPacket);
                } catch (Exception e)
                {
                    Console.WriteLine("Excecption when trying to forward:{0}\t{1}{0}\t{2}", Environment.NewLine, e.Message, e.StackTrace);
                    //throw;
                }
            } else
                Console.WriteLine("No forwarding action defined");
        }
    }
}
