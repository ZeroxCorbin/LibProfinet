using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ProfinetTools.Interfaces.Extensions;
using ProfinetTools.Interfaces.Models;
using ProfinetTools.Interfaces.Services;
using ProfinetTools.Logic.Protocols;
using ProfinetTools.Logic.Transport;
using SharpPcap;

namespace ProfinetTools.Logic.Services
{
    public class DeviceService : IDeviceService
    {
        //CompositeDisposable disposables = new CompositeDisposable();
        ProfinetEthernetTransport transport;
        public async Task<List<Device>> GetDevices(ICaptureDevice adapter, TimeSpan timeout, CancellationToken token)
        {
            using var transport = new ProfinetEthernetTransport(adapter);
            transport.Open();

            var devices = new List<Device>();

            Observable.FromEventPattern<ProfinetEthernetTransport.OnDcpMessageHandler, ConnectionInfoEthernet, DcpMessageArgs>(h => transport.OnDcpMessage += h, h => transport.OnDcpMessage -= h)
                .Select(x => ConvertEventToDevice(x.Sender, x.EventArgs))
                .Where(device => devices != null)
                .Do(device => devices.Add(device))
                .Subscribe()
                ;

            transport.SendIdentifyBroadcast();

            try
            {
                await Task.Delay((int)timeout.TotalMilliseconds, token);
            }
            catch (TaskCanceledException)
            {
              
            }

            return devices;
        }

        private readonly BehaviorSubject<Device> selectedDeviceSubject = new BehaviorSubject<Device>(null);

        public void SelectDevice(Device device)
        {
            selectedDeviceSubject.OnNext(device);
        }

        public IObservable<Device> SelectedDevice => selectedDeviceSubject.AsObservable();

        private Device ConvertEventToDevice(ConnectionInfoEthernet sender, DcpMessageArgs args)
        {
            try
            {
                var device = new Device()
                {
                    MAC = sender.Source.ToString(),
                    Name = (string)args.Blocks[DCP.BlockOptions.DeviceProperties_NameOfStation],
                    IP = ((DCP.IpInfo)args.Blocks[DCP.BlockOptions.IP_IPParameter]).Ip.ToString(),
                    SubnetMask = ((DCP.IpInfo)args.Blocks[DCP.BlockOptions.IP_IPParameter]).SubnetMask.ToString(),
                    Gateway = ((DCP.IpInfo)args.Blocks[DCP.BlockOptions.IP_IPParameter]).Gateway.ToString(),
                    VendorID = ((DCP.DeviceIdInfo)args.Blocks[DCP.BlockOptions.DeviceProperties_DeviceID]).VendorId.ToString(),
                    DeviceID = ((DCP.DeviceIdInfo)args.Blocks[DCP.BlockOptions.DeviceProperties_DeviceID]).DeviceId.ToString(),
                    Role = ((DCP.DeviceRoleInfo)args.Blocks[DCP.BlockOptions.DeviceProperties_DeviceRole]).ToString(),
                    Type = (string)args.Blocks[DCP.BlockOptions.DeviceProperties_DeviceVendor],
                };

                device.DeviceOptions = new List<ushort>();
                foreach(var op in (DCP.BlockOptions[])args.Blocks[DCP.BlockOptions.DeviceProperties_DeviceOptions])
                    device.DeviceOptions.Add((ushort)op);

                return device;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }
    }
}