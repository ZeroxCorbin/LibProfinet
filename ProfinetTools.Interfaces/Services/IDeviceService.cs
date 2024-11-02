using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ProfinetTools.Interfaces.Models;
using SharpPcap;

namespace ProfinetTools.Interfaces.Services
{
	public interface IDeviceService
	{
		Task<List<Device>> GetDevices(ICaptureDevice adapter, TimeSpan timeout, CancellationToken token);
		void SelectDevice(Device device);
		IObservable<Device> SelectedDevice { get; }
	}
}