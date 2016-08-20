#region

using System.Collections.Generic;

#endregion

namespace UlteriusServer.Utilities.Drive
{
    public class Disk
    {
        public Dictionary<int, Smart> Attributes = new Dictionary<int, Smart>
        {
            {0x00, new Smart("Invalid")},
            {0x01, new Smart("Raw read error rate")},
            {0x02, new Smart("Throughput performance")},
            {0x03, new Smart("Spinup time")},
            {0x04, new Smart("Start/Stop count")},
            {0x05, new Smart("Reallocated sector count")},
            {0x06, new Smart("Read channel margin")},
            {0x07, new Smart("Seek error rate")},
            {0x08, new Smart("Seek timer performance")},
            {0x09, new Smart("Power-on hours count")},
            {0x0A, new Smart("Spinup retry count")},
            {0x0B, new Smart("Calibration retry count")},
            {0x0C, new Smart("Power cycle count")},
            {0x0D, new Smart("Soft read error rate")},
            {0xB8, new Smart("End-to-End error")},
            {0xBE, new Smart("Airflow Temperature")},
            {0xBF, new Smart("G-sense error rate")},
            {0xC0, new Smart("Power-off retract count")},
            {0xC1, new Smart("Load/Unload cycle count")},
            {0xC2, new Smart("HDD temperature")},
            {0xC3, new Smart("Hardware ECC recovered")},
            {0xC4, new Smart("Reallocation count")},
            {0xC5, new Smart("Current pending sector count")},
            {0xC6, new Smart("Offline scan uncorrectable count")},
            {0xC7, new Smart("UDMA CRC error rate")},
            {0xC8, new Smart("Write error rate")},
            {0xC9, new Smart("Soft read error rate")},
            {0xCA, new Smart("Data Address Mark errors")},
            {0xCB, new Smart("Run out cancel")},
            {0xCC, new Smart("Soft ECC correction")},
            {0xCD, new Smart("Thermal asperity rate (TAR)")},
            {0xCE, new Smart("Flying height")},
            {0xCF, new Smart("Spin high current")},
            {0xD0, new Smart("Spin buzz")},
            {0xD1, new Smart("Offline seek performance")},
            {0xDC, new Smart("Disk shift")},
            {0xDD, new Smart("G-sense error rate")},
            {0xDE, new Smart("Loaded hours")},
            {0xDF, new Smart("Load/unload retry count")},
            {0xE0, new Smart("Load friction")},
            {0xE1, new Smart("Load/Unload cycle count")},
            {0xE2, new Smart("Load-in time")},
            {0xE3, new Smart("Torque amplification count")},
            {0xE4, new Smart("Power-off retract count")},
            {0xE6, new Smart("GMR head amplitude")},
            {0xE7, new Smart("Temperature")},
            {0xF0, new Smart("Head flying hours")},
            {0xFA, new Smart("Read error retry rate")}
            /* slot in any new codes you find in here */
        };

        public int Index { get; set; }
        public bool IsOk { get; set; }
        public string Model { get; set; }
        public string Type { get; set; }
        public string Serial { get; set; }
    }
}