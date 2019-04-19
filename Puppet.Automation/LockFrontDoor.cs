﻿using Puppet.Common.Automation;
using Puppet.Common.Devices;
using Puppet.Common.Events;
using Puppet.Common.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Puppet.Automation
{
    [TriggerDevice("Contact.FrontDoor", Capability.Contact)]
    [TriggerDevice("Lock.FrontDoorDeadbolt", Capability.Lock)]
    public class LockFrontDoor : AutomationBase
    {
        ContactSensor _frontDoor;
        LockDevice _frontDoorLock;
        List<Speaker> _notificationDevices;

        public LockFrontDoor(HomeAutomationPlatform hub, HubEvent evt) : base(hub, evt)
        {
            _frontDoor =
                _hub.GetDeviceByMappedName<ContactSensor>("Contact.FrontDoor") as ContactSensor;

            _frontDoorLock =
                _hub.GetDeviceByMappedName<LockDevice>("Lock.FrontDoorDeadbolt") as LockDevice;

            _notificationDevices =
                new List<Speaker>() {
                    _hub.GetDeviceByMappedName<Speaker>("Speaker.WebhookNotifier") as Speaker,
                    _hub.GetDeviceByMappedName<Speaker>("Speaker.KitchenSpeaker") as Speaker
                };
        }

        protected async override Task Handle()
        {
            if(_evt.Value == "unknown" && _evt.DeviceId == _hub.LookupDeviceId("Lock.FrontDoorDeadbolt"))
            {
                _notificationDevices.Speak("Front door deadbolt is in an unknown state! Please check it.");
                return;
            }

            await WaitForCancellationAsync(TimeSpan.FromSeconds(10));

            int attempts = 0;
            while (_frontDoor.Status == ContactStatus.Closed && 
                    _frontDoorLock.Status != LockStatus.Locked &&
                    attempts < 3)
            {
                attempts++;
                _frontDoorLock.Lock();
                await WaitForCancellationAsync(TimeSpan.FromSeconds(20));
            }

            if (_frontDoorLock.Status != LockStatus.Locked)
            {
                _notificationDevices.Speak("Front door deadbolt failed to lock! Please check it.");
            }
        }
    }
}