//
// Copyright (c) 2003-2006 Jaroslaw Kowalski <jaak@jkowalski.net>
// Copyright (c) 2006-2014 Piotr Fusik <piotr@fusik.info>
//
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions
// are met:
//
// * Redistributions of source code must retain the above copyright notice,
//   this list of conditions and the following disclaimer.
//
// * Redistributions in binary form must reproduce the above copyright notice,
//   this list of conditions and the following disclaimer in the documentation
//   and/or other materials provided with the distribution.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
// ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
// LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
// SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
// INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF
// THE POSSIBILITY OF SUCH DAMAGE.
//

using System;
using System.IO;
using System.Xml;

namespace Sooda.CodeGen
{
    public abstract class VS2005ProjectFileBase : IProjectFile
    {
        protected XmlDocument doc = new XmlDocument();
        protected readonly string projectExtension;
        protected readonly string templateName;
        protected bool modified = false;
        XmlNamespaceManager namespaceManager;
        const string msbuildNamespace = "http://schemas.microsoft.com/developer/msbuild/2003";

        protected VS2005ProjectFileBase(string projectExtension, string templateName)
        {
            this.projectExtension = projectExtension;
            this.templateName = templateName;
        }

        protected XmlElement SelectElement(XmlNode parent, string xpath)
        {
            if (namespaceManager == null)
            {
                namespaceManager = new XmlNamespaceManager(doc.NameTable);
                namespaceManager.AddNamespace("msbuild", msbuildNamespace);
            }
            return (XmlElement) parent.SelectSingleNode(xpath, namespaceManager);
        }

        protected static bool IsEmpty(XmlElement element)
        {
            return element != null && element.InnerText.Trim() == "";
        }

        public virtual void CreateNew(string outputNamespace, string assemblyName)
        {
            doc = new XmlDocument();
            using (Stream ins = typeof(CodeGenerator).Assembly.GetManifestResourceStream(templateName))
            {
                doc.Load(ins);
            }
            modified = true;
            XmlElement assemblyNameElement = SelectElement(doc, "msbuild:Project/msbuild:PropertyGroup[msbuild:OutputType]/msbuild:AssemblyName");
            if (IsEmpty(assemblyNameElement))
            {
                assemblyNameElement.InnerText = assemblyName;
            }
        }

        void IProjectFile.LoadFrom(string fileName)
        {
            doc = new XmlDocument();
            doc.Load(fileName);
            modified = false;
        }

        void IProjectFile.SaveTo(string fileName)
        {
            XmlElement projectGuid = SelectElement(doc, "msbuild:Project/msbuild:PropertyGroup/msbuild:ProjectGuid");
            if (IsEmpty(projectGuid))
            {
                Guid g = Guid.NewGuid();
                projectGuid.InnerText = "{" + g.ToString().ToUpper() + "}";
                modified = true;
            }

            if (modified)
            {
                doc.Save(fileName);
            }
        }

        void AddItem(string type, string relativeFileName)
        {
            XmlElement itemGroup = SelectElement(doc, "msbuild:Project/msbuild:ItemGroup[msbuild:Compile]");
            if (itemGroup == null)
            {
                itemGroup = doc.CreateElement("", "ItemGroup", msbuildNamespace);
                doc.DocumentElement.AppendChild(itemGroup);
            }

            XmlElement file = SelectElement(itemGroup, "msbuild:" + type + "[@Include='" + relativeFileName + "']");
            if (file == null)
            {
                XmlElement el = doc.CreateElement("", type, msbuildNamespace);
                el.SetAttribute("Include", relativeFileName);
                itemGroup.AppendChild(el);
                modified = true;
            }
        }

        void IProjectFile.AddCompileUnit(string relativeFileName)
        {
            AddItem("Compile", relativeFileName);
        }

        void IProjectFile.AddResource(string relativeFileName)
        {
            AddItem("EmbeddedResource", relativeFileName);
        }

        string IProjectFile.GetProjectFileName(string outNamespace)
        {
            return outNamespace + projectExtension;
        }
    }
}
