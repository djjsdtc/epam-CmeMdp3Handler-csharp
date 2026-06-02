namespace Epam.CmeMdp3Handler.Core.Cfg
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class MulticastBindingOption
    {
        public static MulticastBindingOption Default { get; } = new(MulticastBindingMode.IpAddrAny);
        public MulticastBindingMode BindingMode { get; init; }
        public IEnumerable<string> InterfacesOrAddresses { get; init; }
        public MulticastBindingOption(MulticastBindingMode bindingMode, IEnumerable<string>? interfacesOrAddresses = null)
        {
            this.BindingMode = bindingMode;
            this.InterfacesOrAddresses = interfacesOrAddresses ?? [];
        }
    }

    public enum MulticastBindingMode
    {
        /// <summary>
        /// Bind to IPAddress.Any (0.0.0.0)
        /// </summary>
        IpAddrAny,
        /// <summary>
        /// Bind to all network interfaces in system
        /// </summary>
        AllNI,
        /// <summary>
        /// Bind to specified network interfaces
        /// </summary>
        SpecifiedNI,
        /// <summary>
        /// Bind to specified local IP addresses
        /// </summary>
        SpecifiedLocalAddr
    }
}
