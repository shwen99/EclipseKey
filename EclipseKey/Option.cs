using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;
using EnvDTE80;

namespace EclipseKey
{
    public class Option
    {
        public bool SmartSemicolon { get; set; }

        public bool 圆括号自动补全 { get; set; }

        public bool 方括号自动补全 { get; set; }

        public bool 单引号自动补全 { get; set; }

        public bool 双引号自动补全 { get; set; }

        [XmlArrayItem("Template")]
        public List<SurroundTemplate> SurroundTemplates { get; set; }

        [XmlArrayItem("SmartKey")]
        public List<SmartKeyTemplate> SmartKeyTemplates { get; set; } 
    }
}
