// ****************************************************************************
// @Copyright: (C) 2010-2013 广东省电信工程有限公司 版权所有
// @File:  SmartKeyTemplate.cs
// @Create: 2017-03-17
// @Author: sunhaiwen<sunhaiwen99@gmail.com>
// @History:
//  - yyyy-mm-dd by Author[<@email>] : brief
// ****************************************************************************

using System.Xml.Serialization;

namespace EclipseKey
{
    public class SmartKeyTemplate
    {
        [XmlAttribute]
        public string Key { get; set; }

        [XmlAttribute]
        public string Value { get; set; }
    }
}