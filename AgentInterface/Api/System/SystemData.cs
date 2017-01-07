using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using OpenHardwareMonitor.Hardware;

namespace AgentInterface.Api.System
{
   public  class SystemData
    {

        [HandleProcessCorruptedStateExceptions]
        public static float GetGpuTemp(string gpuName)
        {
            try
            {
                var myComputer = new Computer();
                myComputer.Open();
                //possible fix for gpu temps on laptops
                myComputer.GPUEnabled = true;
                float temp = -1;
                foreach (var hardwareItem in myComputer.Hardware)
                {
                    hardwareItem.Update();
                    switch (hardwareItem.HardwareType)
                    {
                        case HardwareType.GpuNvidia:
                            foreach (
                                var sensor in
                                    hardwareItem.Sensors.Where(
                                        sensor =>
                                            sensor.SensorType == SensorType.Temperature &&
                                            hardwareItem.Name.Contains(gpuName)))
                            {
                                if (sensor.Value != null)
                                {
                                    temp = (float)sensor.Value;
                                }
                            }
                            break;
                        case HardwareType.GpuAti:
                            foreach (
                                var sensor in
                                    hardwareItem.Sensors.Where(
                                        sensor =>
                                            sensor.SensorType == SensorType.Temperature &&
                                            hardwareItem.Name.Contains(gpuName)))
                            {
                                if (sensor.Value != null)
                                {
                                    temp = (float)sensor.Value;
                                }
                            }
                            break;
                    }
                }
                myComputer.Close();
                return temp;
            }
            catch (AccessViolationException)
            {
                return -1;
            }
        }

        public static List<float> GetCpuTemps()
        {
            var myComputer = new Computer();
            myComputer.Open();
            myComputer.CPUEnabled = true;
            var tempTemps = new List<float>();
            var procCount = Environment.ProcessorCount;
            for (var i = 0; i < procCount; i++)
            {
                tempTemps.Add(-1);
            }
            try
            {
                var temps = (from hardwareItem in myComputer.Hardware
                             where hardwareItem.HardwareType == HardwareType.CPU
                             from sensor in hardwareItem.Sensors
                             where sensor.SensorType == SensorType.Temperature
                             let value = sensor.Value
                             where value != null
                             where value != null
                             select (float)value).ToList();
                if (temps.Count != 0) return temps;
                myComputer.Close();
                return tempTemps;
            }
            catch (Exception)
            {
                myComputer.Close();
                return tempTemps;
            }
        }

    }
}
