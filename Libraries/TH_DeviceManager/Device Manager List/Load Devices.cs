﻿using System;
using System.Windows;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading;
using System.Data;

using TH_Configuration;
using TH_Global;
using TH_Global.Functions;
using TH_UserManagement.Management;

namespace TH_DeviceManager
{
    public partial class DeviceManagerList
    {
        public delegate void DevicesStatus_Handler();
        public event DevicesStatus_Handler LoadingDevices;

        public delegate void DevicesLoaded_Handler(List<Configuration> configs);
        public event DevicesLoaded_Handler DeviceListUpdated;

        public enum DeviceUpdateEvent
        {
            Added,
            Changed,
            Removed
        }
        public class DeviceUpdateArgs
        {
            public DeviceUpdateEvent Event { get; set; }
        }
        public delegate void DeviceUpdated_Handler(Configuration config, DeviceUpdateArgs args);
        public event DeviceUpdated_Handler DeviceUpdated;

        public bool DevicesLoading
        {
            get { return (bool)GetValue(DevicesLoadingProperty); }
            set { SetValue(DevicesLoadingProperty, value); }
        }

        public static readonly DependencyProperty DevicesLoadingProperty =
            DependencyProperty.Register("DevicesLoading", typeof(bool), typeof(DeviceManager), new PropertyMetadata(false));


        ObservableCollection<DeviceInfo> _devices;
        public ObservableCollection<DeviceInfo> Devices
        {
            get
            {
                if (_devices == null)
                    _devices = new ObservableCollection<DeviceInfo>();
                return _devices;
            }

            set
            {
                _devices = value;
            }
        }

        public List<Configuration> configurations;

        Thread loaddevices_THREAD;

        public void LoadDevices()
        {
            DevicesLoading = true;

            if (loaddevices_THREAD != null) loaddevices_THREAD.Abort();

            loaddevices_THREAD = new Thread(new ThreadStart(LoadDevices_Worker));
            loaddevices_THREAD.Start();

            if (LoadingDevices != null) LoadingDevices();
        }

        void LoadDevices_Worker()
        {
            List<Configuration> devices = null;

            if (currentuser != null)
            {
                // Get Added Configurations
                devices = Configurations.GetConfigurationsListForUser(currentuser);

                devices = devices.OrderBy(x => x.Index).ToList();

                // Reset order to be in intervals of 1000 in order to leave room in between for changes in index
                // This index model allows for devices to change index without having to update every device each time.
                for (var x = 0; x <= devices.Count - 1; x++)
                {
                    devices[x].Index = 1000 + (1000 * x);
                }

                var indexItems = new List<Tuple<string, int>>();

                foreach (var device in devices) indexItems.Add(new Tuple<string, int>(device.TableName, device.Index));

                Configurations.UpdateIndexes(indexItems);


                this.Dispatcher.BeginInvoke(new Action<List<Configuration>>(LoadDevices_GUI), PRIORITY_BACKGROUND, new object[] { devices });
            }
            // If not logged in Read from File in 'C:\TrakHound\'
            else
            {
                devices = Configuration.ReadAll(FileLocations.Devices).ToList();

                devices = devices.OrderBy(x => x.Index).ToList();

                // Reset order to be in intervals of 1000 in order to leave room in between for changes in index
                // This index model allows for devices to change index without having to update every device each time.
                for (var x = 0; x <= devices.Count - 1; x++)
                {
                    devices[x].Index = 1000 + (1000 * x);
                }

                foreach (var device in devices) SaveFileConfiguration(device);

                this.Dispatcher.BeginInvoke(new Action<List<Configuration>>(LoadDevices_GUI), PRIORITY_BACKGROUND, new object[] { devices });
            }

            configurations = devices.ToList();

            this.Dispatcher.BeginInvoke(new Action(LoadDevices_Finished), PRIORITY_BACKGROUND, new object[] { });
        }

        void LoadDevices_GUI(List<Configuration> devices)
        {
            Devices.Clear();

            if (devices != null)
            {
                foreach (var device in devices)
                {
                    AddDevice(device);
                }
            }
        }

        public void AddDevice(Configuration config)
        {
            var info = new DeviceInfo(config);
            Devices.Add(info);

            Devices.Sort();
        }

        public void RemoveDevice(Configuration config)
        {
            int index = Devices.ToList().FindIndex(x => x.UniqueId == config.UniqueId);
            if (index >= 0) Devices.RemoveAt(index);
        }

        void LoadDevices_Finished()
        {
            UpdateDeviceList();
        }

        void UpdateDeviceList()
        {
            // Raise DevicesLoaded event to update devices for rest of TrakHound 
            // (Client is the only one that uses this for now)
            if (DeviceListUpdated != null) DeviceListUpdated(configurations);
        }

        void UpdateDevice(Configuration config, DeviceUpdateArgs args)
        {
            if (DeviceUpdated != null) DeviceUpdated(config, args);
        }

    }
}
