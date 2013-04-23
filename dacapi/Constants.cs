//------------------------------------------------------------------------------
// <copyright file="Constants.cs" company="SQLProj.com">
//         Copyright © 2012 SQLProj.com - All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace SqlProj.Dac
{
    public static class Constants
    {
        public const string DacXsdUri = @"http://schemas.microsoft.com/sqlserver/dac/Serialization/2012/02";
        public const string ContentTypesXmllUri = @"/[Content_Types].xml";
        public const string DacMetadataXmlUri = @"/DacMetadata.xml";
        public const string ModelXmlUri = @"/model.xml";
        public const string OriginXmlUri = @"/Origin.xml";
        public const string DacOriginElement = "DacOrigin";
        public const string VersionElement = "Version";
        public const string ProductVersionElement = "ProductVersion";
        public const string ProductVersionValue10 = "10.3.0.0";
        public const string ProductVersionValue11 = "11.1.0.0";
        public const string ProductSchemaElement = "ProductSchema";
        public const string ChecksumsElement = "Checksums";
        public const string ChecksumElement = "Checksum";
        public const string UriAttribute = "Uri";
        public const string HeaderElement = "Header";
        public const string ModelElement = "Model";
        public const string AnnotationElement = "Annotation";
        public const string AttachedAnnotationElement = "AttachedAnnotation";
        public const string TypeAttribute = "Type";
        public const string CategoryAttribute = "Category";
        public const string NameAttribute = "Name";
        public const string PropertyElement = "Property";
        public const string ValueAttribute = "Value";
        public const string RelationshipElement = "Relationship";
        public const string BodyScriptElement = "BodyScript";
        public const string BodyDependenciesElement = "BodyDependencies";
        public const string Unknown = "Unknown";
        public const string Unnamed = "Unnamed";
        public const string CustomData = "CustomData";
        public const string Reference = "Reference";
        public const string SqlSchema = "SqlSchema";
        public const string Metadata = "Metadata";
        public const string ExternalParts = "ExternalParts";
        public const string DisambiguatorAttribute = "Disambiguator";
        public const string FileName = "FileName";
    }
}
