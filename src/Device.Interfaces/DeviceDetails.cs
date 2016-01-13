using System.Runtime.Serialization;

namespace Device.Interfaces
{
    [DataContract]
    public class DeviceDetails
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Description { get; set; }
    }
}