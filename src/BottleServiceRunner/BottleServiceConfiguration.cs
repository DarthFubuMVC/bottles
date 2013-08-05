using System.Xml.Serialization;

namespace BottleServiceRunner
{
    [XmlType("service")]
    public class BottleServiceConfiguration
    {
        public const string FILE = "bottle-service.config";

        public BottleServiceConfiguration()
        {
            var defaultValue = typeof (Bottles.Services.BottleServiceRunner).Name + "Service";

            Name = defaultValue;
            DisplayName = defaultValue;
            Description = defaultValue;

			// Hides command line output so only turn this on when running as a service
	        UseEventLog = false;
        }

        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
		public bool UseEventLog { get; set; }
    }
}