namespace KnxMqttBridge.Infrastructure
{
    // NOTE: Generated code may require at least .NET Framework 4.5 or .NET Core/Standard 2.0.
    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://knx.org/xml/ga-export/01")]
    [System.Xml.Serialization.XmlRootAttribute("GroupAddress-Export", Namespace = "http://knx.org/xml/ga-export/01", IsNullable = false)]
    public partial class GroupAddressExport
    {

        private GroupAddressExportGroupRange[] _groupRange;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("GroupRange")]
        public GroupAddressExportGroupRange[] GroupRange
        {
            get => this._groupRange;
            set => this._groupRange = value;
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://knx.org/xml/ga-export/01")]
    public partial class GroupAddressExportGroupRange
    {

        private GroupAddressExportGroupRangeGroupRange[] _groupRange;

        private string _name;

        private ushort _rangeStart;

        private ushort _rangeEnd;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("GroupRange")]
        public GroupAddressExportGroupRangeGroupRange[] GroupRange
        {
            get => this._groupRange;
            set => this._groupRange = value;
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Name
        {
            get => this._name;
            set => this._name = value;
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public ushort RangeStart
        {
            get => this._rangeStart;
            set => this._rangeStart = value;
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public ushort RangeEnd
        {
            get => this._rangeEnd;
            set => this._rangeEnd = value;
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://knx.org/xml/ga-export/01")]
    public partial class GroupAddressExportGroupRangeGroupRange
    {

        private GroupAddressExportGroupRangeGroupRangeGroupAddress[] _groupAddress;

        private string _name;

        private ushort _rangeStart;

        private ushort _rangeEnd;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("GroupAddress")]
        public GroupAddressExportGroupRangeGroupRangeGroupAddress[] GroupAddress
        {
            get => this._groupAddress;
            set => this._groupAddress = value;
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Name
        {
            get => this._name;
            set => this._name = value;
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public ushort RangeStart
        {
            get => this._rangeStart;
            set => this._rangeStart = value;
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public ushort RangeEnd
        {
            get => this._rangeEnd;
            set => this._rangeEnd = value;
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://knx.org/xml/ga-export/01")]
    public partial class GroupAddressExportGroupRangeGroupRangeGroupAddress
    {

        private string _name;

        private string _address;

        private string _dPTs;

        private string _security;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Name
        {
            get => this._name;
            set => this._name = value;
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Address
        {
            get => this._address;
            set => this._address = value;
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string DPTs
        {
            get => this._dPTs;
            set => this._dPTs = value;
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Security
        {
            get => this._security;
            set => this._security = value;
        }
    }
}
