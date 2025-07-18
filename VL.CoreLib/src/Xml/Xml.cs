using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.XPath;
using System.Xml.Xsl;
using VL.Core;
using VL.Lib.Basics.Resources;
using VL.Lib.Collections;

namespace VL.Lib.Xml
{
    public enum ValidationType
    {
        None,
        Dtd,
        Schema
    }

    public enum JsonPropertyNamingPolicy
    {
        CamelCase,
        KebabCaseLower,
        KebabCaseUpper,
        SnakeCaseLower,
        SnakeCaseUpper
    }

    public static class XmlNodes
    {
        /// <summary>
        /// Creates an XDocument
        /// </summary>
        /// <param name="root"></param>
        /// <param name="declaration"></param>
        /// <param name="documentType"></param>
        /// <returns></returns>
        public static XDocument JoinXDocument(XElement root, XDeclaration declaration /*= null*/, XDocumentType documentType /*= null*/) // bug in vl doesn't let us set default value null, which would be preferrable here.
            => new XDocument(declaration, documentType, root);

        /// <summary>
        /// Splits an XDocument into its components
        /// </summary>
        /// <param name="input"></param>
        /// <param name="root"></param>
        /// <param name="declaration"></param>
        /// <param name="documentType"></param>
        public static void SplitXDocument(this XDocument input, out XElement root, out XDeclaration declaration, out XDocumentType documentType)
        {
            root = input.Root;
            declaration = input.Declaration;
            documentType = input.DocumentType;
        }

        /// <summary>
        /// Creates an XDeclaration
        /// </summary>
        /// <param name="version"></param>
        /// <param name="encoding"></param>
        /// <param name="standalone"></param>
        /// <returns></returns>
        public static XDeclaration JoinXDeclaration(string version = "1.0", string encoding = "UTF-8", string standalone = "no")
                => new XDeclaration(version, encoding, standalone);

        /// <summary>
        /// Splits an XDeclaration into its components
        /// </summary>
        /// <param name="input"></param>
        /// <param name="version"></param>
        /// <param name="encoding"></param>
        /// <param name="standalone"></param>
        public static void SplitXDeclaration(this XDeclaration input, out string version, out string encoding, out string standalone)
        {
            version = input.Version;
            encoding = input.Encoding;
            standalone = input.Standalone;
        }

        /// <summary>
        /// Creates an XDocumentType
        /// </summary>
        /// <param name="name"></param>
        /// <param name="publicId"></param>
        /// <param name="systemId"></param>
        /// <param name="internalSubset"></param>
        /// <returns></returns>
        public static XDocumentType JoinXDocumentType(string name = "ROOT", string publicId = "", string systemId = "", string internalSubset = "")
            => new XDocumentType(name, publicId, systemId, internalSubset);

        /// <summary>
        /// Splits an XDocumentType into its components
        /// </summary>
        /// <param name="input"></param>
        /// <param name="name"></param>
        /// <param name="publicId"></param>
        /// <param name="systemId"></param>
        /// <param name="internalSubset"></param>
        public static void SplitXDocumentType(this XDocumentType input, out string name, out string publicId, out string systemId, out string internalSubset)
        {
            name = input.Name;
            publicId = input.PublicId;
            systemId = input.SystemId;
            internalSubset = input.InternalSubset;
        }

        /// <summary>
        /// Creates an XElement
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="attributes"></param>
        /// <param name="children"></param>
        /// <returns></returns>
        public static XElement JoinXElement(string name, string value, IEnumerable<XAttribute> attributes, IEnumerable<XElement> children)
        {
            if (string.IsNullOrEmpty(name))
                name = "ElementName";
            //also clean the name of stupid characters
            var element = new XElement(name, value);

            //clone attributes on the fly if they are already rooted somewhere
            attributes = attributes
                .Where(a => a != null)
                .Select(a => a.Parent != null ? new XAttribute(a) : a)
                .ToArray();
            element.Add(attributes);

            //clone children on the fly if they are already rooted somewhere else
            children = children
                .Where(c => c != null)
                .Select(c => c.Parent != null ? new XElement(c) : c)
                .ToArray();
            element.Add(children);

            return element;
        }

        /// <summary>
        /// Splits an XElement into its components
        /// </summary>
        /// <param name="input"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="children"></param>
        /// <param name="attributes"></param>
        public static void SplitXElement(this XElement input, out string name, out string value, out Spread<XElement> children,
           out Spread<XAttribute> attributes)
        {
            name = input.Name.LocalName;

            //deepvalue is not used anymore.
            //deepvalue now gets you the legacy value out implementation: all the text contents of all text nodes of this element and its children get concatenated
            //it is a strange default implementation for value, since it blurrs every NESTED text value into one big string, which is not related to how the value is set.
            //deepvalue = element.Value;
            // anyway: we output this thing as deepvalue to get legacy patches working.

            //now here comes the new implementation:                
            //http://stackoverflow.com/questions/4251215/how-to-get-xelements-value-and-not-value-of-all-child-nodes
            //value = string.Concat(element.Nodes().OfType<XText>().Select(t => t.Value));

            //still not satisfied. trim spaces in front and at the back.
            //value = string.Concat(element.Nodes().OfType<XText>().Select(t => t.Value)).Trim();

            //still not satisfied. actually we don't want to trim blindly. 
            //we just want to pick that single text node (if existant), that has more than just whitespace in it and output it without modification.
            //this covers the standard case in which text value is NOT spread all over the element, separated by child nodes.
            //when found it is returned without modification. we shouldn't delete information for no reason.
            var textNode = input.Nodes().OfType<XText>().FirstOrDefault(t => !string.IsNullOrWhiteSpace(t.Value));
            // if there is just none, let's output the first text nodes' value in all its glory.
            textNode = textNode ?? input.Nodes().OfType<XText>().FirstOrDefault();
            //in summary: out value now just supports the std. case, where only ONE text value is placed into the xelement. 
            //(join nodes shouldn't construct anything else anyway)
            value = textNode != null ? textNode.Value : "";

            children = input.Elements().ToSpread();
            attributes = input.Attributes().ToSpread();
            //changed = element.ch;
        }

        /// <summary>
        /// Creates an XAttribute
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static XAttribute JoinXAttribute(string name, string value)
        {
            if (string.IsNullOrEmpty(name)) name = "AttributeName";
            return new XAttribute(name, value);
        }

        /// <summary>
        /// Splits an XAttribute into its components
        /// </summary>
        /// <param name="input"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public static void SplitXAttribute(this XAttribute input, out string name, out string value)
        {
            name = input.Name.LocalName;
            value = input.Value;
        }

        /// <summary>
        /// Returns a spread of XElements with the given name
        /// </summary>
        /// <param name="input"></param>
        /// <param name="name"></param>
        /// <param name="allDescendants"></param>
        /// <returns></returns>
        public static Spread<XElement> XElementsByName(this XElement input, string name = "Name", bool allDescendants = false)
        {
            XName nsAndName = NsAndName(input, name);

            if (allDescendants)
                return input.Descendants(nsAndName).ToSpread();
            else
                return input.Elements(nsAndName).ToSpread();
        }

        /// <summary>
        /// Returns the XAttribute with the given name
        /// </summary>
        /// <param name="input"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static XAttribute XAttributeByName(this XElement input, string name = "Name")
            =>  input.Attribute(NsAndName(input, name, false));

        /// <summary>
        /// Validates an XDocument or XElement against an XML Schema Definition and returns whether it is valid or not
        /// </summary>
        /// <param name="node"></param>
        /// <param name="validationFile"></param>
        /// <param name="isValid"></param>
        /// <param name="errorMessage"></param>
        public static void ValidateXDocumentSchema(this XNode node, VL.Lib.IO.Path validationFile, out bool isValid, out string errorMessage)
        {
            XDocument document = node as XDocument;
            if(document == null)
            {
                document = new XDocument(node);
            }
        
            if (!validationFile.IsFile)
            {
                isValid = false;
                errorMessage = "Validation File not found.";
                return;
            }

            var _isValid = true;
            var _errorMessage = "";

            var schemas = new XmlSchemaSet();
            schemas.Add(null, validationFile);
            schemas.Compile();

            document.Validate(schemas, (sender, validationEventArgs) =>
            {
                _isValid = false;
                _errorMessage = validationEventArgs.Message;
            });

            isValid = _isValid;
            errorMessage = _errorMessage;
        }

        private static XName NsAndName(XElement element, string name, bool fallbackToDefaultNS = true)
        {
            string prefix = null;
            var s = name.Split(':');
            if (s.Length > 1)
            {
                prefix = s[0];
                name = s[1];
            }
            XNamespace ns = null;
            if (prefix != null)
                ns = element.GetNamespaceOfPrefix(prefix);

            if (ns == null && fallbackToDefaultNS)
                ns = element.GetDefaultNamespace();

            if (ns != null)
                return ns + name;
            else
                return name;
        }

        /// <summary>
        /// Deserializes an XDocument from a JSON string
        /// </summary>
        /// <param name="json"></param>
        /// <param name="deserializeRootElementName"></param>
        /// <param name="writeArrayAttribute"></param>
        /// <returns></returns>
        public static XDocument DeserializeXNode(this string json, string deserializeRootElementName, bool writeArrayAttribute)
             => JsonConvert.DeserializeXNode(json, deserializeRootElementName, writeArrayAttribute);

        /// <summary>
        /// Serializes an XDocument or XElement to a JSON string
        /// </summary>
        /// <param name="input"></param>
        /// <param name="indent"></param>
        /// <param name="omitRootObject"></param>
        /// <returns></returns>
        public static string SerializeXNode(this XNode input, bool indent, bool omitRootObject)
        {
            Newtonsoft.Json.Formatting formatting;
            if (indent)
                formatting = Newtonsoft.Json.Formatting.Indented;
            else formatting = Newtonsoft.Json.Formatting.None;

            return JsonConvert.SerializeXNode(input, formatting, omitRootObject);
        }

        public static void Clone(XDocument input, out XDocument original, out XDocument clone)
        {
            clone = new XDocument(input);
            original = input;
        }

        static public string TransformXDocument(XDocument input, string xsl)
        {
            using (var textReader = new StringReader(xsl))
            using (var reader = XmlReader.Create(textReader))
            using (var writer = new StringWriter())
            {
                var xslt = new XslCompiledTransform();
                xslt.Load(reader);
                xslt.Transform(input.CreateReader(ReaderOptions.None), null, writer);
                return writer.ToString();
            }
        }
    }
}