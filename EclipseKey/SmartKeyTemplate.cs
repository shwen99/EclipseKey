// ****************************************************************************
// @Copyright: (C) 2010-2013 广东省电信工程有限公司 版权所有
// @File:  SmartKeyTemplate.cs
// @Create: 2017-03-17
// @Author: sunhaiwen<sunhaiwen99@gmail.com>
// @History:
//  - yyyy-mm-dd by Author[<@email>] : brief
// ****************************************************************************

using System.ComponentModel;
using System.Xml.Serialization;

namespace EclipseKey
{
    public class SmartKeyTemplate
    {
        private string _fileType;

        [DefaultValue(null)]
        [XmlAttribute]
        public string FileType
        {
            get { return _fileType; }
            set { _fileType = value == null ? string.Empty : value.ToLower(); }
        }

        [XmlAttribute]
        public string Key { get; set; }

        [XmlAttribute]
        public string Value { get; set; }
    }
}