//------------------------------------------------------------------------------
// <copyright file="DacMerge.cs" company="SQLProj.com">
//         Copyright © 2012 SQLProj.com - All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace SqlProj.Utils.Dac.Merge
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Xml;
    using System.Xml.Linq;
    using SqlProj.Common;
    using SqlProj.Dac;

    internal class DacMerge
    {
        private readonly AppArgs _args;

        public DacMerge(AppArgs args)
        {
            this._args = args;
        }

        public void Run()
        {
            // check arguments
            if (string.IsNullOrEmpty(this._args.InputFilename))
            {
                throw new ArgumentException(DacMergeResource.ErrorNoInputFile);
            }

            var inputFile = new FileInfo(this._args.InputFilename);
            if (!inputFile.Exists)
            {
                throw new FileNotFoundException(this._args.InputFilename);
            }

            if (!DacPackage.IsDacPac(inputFile))
            {
                throw new FileFormatException(DacMergeResource.ErrorNotValidDacPac);
            }

            FileInfo outputFile = null;
            if (!string.IsNullOrEmpty(this._args.OutputFilename))
            {
                outputFile = new FileInfo(this._args.OutputFilename);
                if (outputFile.Exists && this._args.Overwrite == false)
                {
                    throw new InvalidOperationException(string.Format(DacMergeResource.ErrorOutputFileExists, this._args.OutputFilename));
                }

                if (string.Compare(inputFile.FullName, outputFile.FullName, StringComparison.InvariantCulture) == 0)
                {
                    throw new ApplicationException(DacMergeResource.ErrorInputEqualToOutput);
                }
            }

            DirectoryInfo loadPath = null;
            loadPath = String.IsNullOrEmpty(this._args.ReferenceLoadPath) 
                ? new DirectoryInfo(Directory.GetCurrentDirectory()) 
                : new DirectoryInfo(this._args.ReferenceLoadPath);
            if (loadPath != null && !loadPath.Exists)
            {
                throw new DirectoryNotFoundException(this._args.ReferenceLoadPath);
            }

            LogWriter.WriteMessage(DacMergeResource.Starting);
            LogWriter.WriteMessage(String.Format(DacMergeResource.InputFileNameArg, inputFile.Name));
            LogWriter.WriteMessage(outputFile == null
                ? String.Format(DacMergeResource.UpdateInplace, inputFile.Name)
                : String.Format(DacMergeResource.OutputFileNameArg, outputFile.Name));
            LogWriter.WriteMessage(String.Format(DacMergeResource.OverwriteArg, this._args.Overwrite.ToString(CultureInfo.InvariantCulture)));
            LogWriter.WriteMessage(String.Format(DacMergeResource.BackupArg, this._args.Backup.ToString(CultureInfo.InvariantCulture)));
            LogWriter.WriteMessage(String.Format(DacMergeResource.RelativeLoadPathArg, loadPath.FullName));

            if (_args.Backup)
            {
                var backupFilePath = Path.Combine(inputFile.DirectoryName, inputFile.Name + ".bak");
                inputFile.CopyTo(backupFilePath, true);
            }

            Merge(inputFile, outputFile, loadPath);

            LogWriter.WriteMessage(DacMergeResource.Finished);
        }

        internal static bool Merge(FileInfo inputFile, FileInfo outputFile, DirectoryInfo referenceLoadPath)
        {
            bool singleFileMode = (outputFile == null);

            using (var inputPackage = new DacPackage(inputFile))
            {
                inputPackage.Open(singleFileMode ? FileAccess.ReadWrite : FileAccess.Read);

                var inputModelDoc = inputPackage.Model;
                if (inputModelDoc.Root == null)
                {
                    throw new NullReferenceException("inputModelDoc");
                }

                LogWriter.WriteMessage(String.Format(DacMergeResource.LoadingFile, inputFile.Name));

                // TODO:<GertD> check if this is needed
                // set to last model element in input document
                var inputModelElement = inputModelDoc.Root.Elements().LastOrDefault(e => e.Name.LocalName == Constants.ModelElement);
                if (inputModelElement == null)
                {
                    throw new NullReferenceException(Constants.ModelElement);
                }

                // Event handder to record all the changes to the model
                inputModelElement.Changed += new EventHandler<XObjectChangeEventArgs>
                    ((sender, e) =>
                        {
                            var element = sender as XElement;
                            if (element != null)
                            {
                                var elementType = Constants.Unknown;
                                var attribute = element.Attribute(Constants.TypeAttribute);
                                if (attribute != null)
                                    elementType = element.Attribute(Constants.TypeAttribute).Value;
                                
                                var elementName = Constants.Unnamed;
                                attribute = element.Attribute(Constants.NameAttribute);
                                if (attribute != null)
                                    elementName = element.Attribute(Constants.NameAttribute).Value;

                                LogWriter.WriteMessage(
                                    String.Format("{0} - {1} - {2}",
                                    e.ObjectChange,
                                    elementType,
                                    elementName));
                            }
                        }
                    );

                // get child packages, using the CustomData Category="Reference" and Type="SqlSchema"
                //
                // <CustomData Category="Reference" Type="SqlSchema"> 
                //    <Metadata Value="D:\USERS\GERTD\PROJECTS\DACMERGE\CHILDDB1\BIN\DEBUG\CHILDDB1.DACPAC" Name="FileName"/> 
                //    <Metadata Value="ChildDB1.dacpac" Name="LogicalName"/> 
                //    <Metadata Value="False" Name="SuppressMissingDependenciesErrors"/>
                //    <Metadata Value="[$(OtherServer)].[$(ChildDB1)]" Name="ExternalParts"/>
                // </CustomData>
                var header = inputModelDoc.Root.Elements()
                    .Where(e => e.Name.LocalName == Constants.HeaderElement);

                var customDataElements = header.Elements()
                    .Where(e => e.Name.LocalName == Constants.CustomData)
                    .Where(a => a.Attribute(Constants.CategoryAttribute).Value == Constants.Reference)
                    .Where(a => a.Attribute(Constants.TypeAttribute).Value == Constants.SqlSchema);

                Int32 maxDisambiguator = 0;
                var x = inputModelElement.Descendants()
                    .Where(e => e.Name.LocalName == Constants.AnnotationElement)
                    .Where(a =>
                    {
                        var attribute = a.Attribute(Constants.DisambiguatorAttribute);
                        return attribute != null;
                    })
                    .LastOrDefault().Attribute(Constants.DisambiguatorAttribute).Value;

                Int32.TryParse((String.IsNullOrEmpty(x) ? "3" : x), out maxDisambiguator);
                
                Int32 maxCummulativeDisambiguator = maxDisambiguator;
                
                foreach (var customDataElement in customDataElements.ToList())
                {
                    var inSameDb = String.IsNullOrEmpty(customDataElement.Elements()
                        .Where(e => e.Name.LocalName == Constants.Metadata)
                        .Where(a => a.Attribute(Constants.NameAttribute).Value == Constants.ExternalParts)
                        .Select(a => a.Attribute(Constants.ValueAttribute).Value).FirstOrDefault());

                    if (inSameDb)
                    {
                        // get dacpac filename
                        var childDacPac = customDataElement.Elements()
                            .Where(e => e.Name.LocalName == Constants.Metadata)
                            .Where(a => a.Attribute(Constants.NameAttribute).Value == Constants.FileName)
                            .Select(a => a.Attribute(Constants.ValueAttribute).Value).FirstOrDefault();

                        if (childDacPac == null || String.IsNullOrEmpty(childDacPac))
                        {
                            throw new ArgumentNullException("childDacPac");
                        }

                        var childFile = ResolveFileName(childDacPac, inputFile, referenceLoadPath);
                        if (childFile == null)
                        {
                            throw new ArgumentNullException("childFile");
                        }

                        if (!childFile.Exists)
                        {
                            throw new FileNotFoundException(childDacPac);
                        }

                        LogWriter.WriteMessage(String.Format(DacMergeResource.MergeFile, childFile.Name));

                        // import child package
                        using (var childPackage = new DacPackage(childFile))
                        {
                            childPackage.Open(FileAccess.Read);

                            // var childModelPart = childPackage.GetPart(modelPartUri);

                            var childModelDoc = childPackage.Model; // XDocument.Load(XmlReader.Create(childModelPart.GetStream()));
                            if (childModelDoc.Root == null)
                            {
                                throw new NullReferenceException("childModelDoc");
                            }

                            var childModelElement = childModelDoc.Root.Elements().FirstOrDefault(e => e.Name.LocalName == Constants.ModelElement);
                            if (childModelElement == null)
                            {
                                throw new NullReferenceException(Constants.ModelElement);
                            }
                            
                            // remove DatabaseOptions from child model
                            RemoveElements(childModelElement, Constants.TypeAttribute, SqlElementType.SqlDatabaseOptions);

                            // update Disambiguator for Annotations
                            var annotations = childModelElement.Descendants()
                                .Where(e => e.Name.LocalName == Constants.AnnotationElement)
                                .Where(a =>
                                {
                                    var attribute = a.Attribute(Constants.DisambiguatorAttribute);
                                    return attribute != null;
                                });

                            foreach (var annotation in annotations)
                            {
                                Int32 disambiguator = 0;

                                if (Int32.TryParse(annotation.Attribute(Constants.DisambiguatorAttribute).Value, out disambiguator))
                                {
                                    Int32 newdisambiguator = disambiguator + maxDisambiguator;
                                    annotation.Attribute(Constants.DisambiguatorAttribute).Value = newdisambiguator.ToString(CultureInfo.InvariantCulture);
                                    maxCummulativeDisambiguator = Math.Max(newdisambiguator, maxCummulativeDisambiguator);
                                }
                                else
                                {
                                    System.Diagnostics.Debug.Assert(false);
                                }
                            }

                            // update Disambiguator for AttachedAnnotations
                            var attachedAnnotations = childModelElement.Descendants()
                                .Where(e => e.Name.LocalName == Constants.AttachedAnnotationElement)
                                .Where(a =>
                                {
                                    var attribute = a.Attribute(Constants.DisambiguatorAttribute);
                                    return attribute != null;
                                });

                            foreach (var attachedAnnotation in attachedAnnotations)
                            {
                                Int32 disambiguator = 0;
                                if (Int32.TryParse(attachedAnnotation.Attribute(Constants.DisambiguatorAttribute).Value, out disambiguator))
                                {
                                    Int32 newdisambiguator = disambiguator + maxDisambiguator;
                                    attachedAnnotation.Attribute(Constants.DisambiguatorAttribute).Value = newdisambiguator.ToString(CultureInfo.InvariantCulture);
                                }
                                else
                                {
                                    System.Diagnostics.Debug.Assert(false);
                                }
                            }

                            var childElements = childModelElement.Elements();

                            LogWriter.WriteMessage(String.Format(DacMergeResource.MergeModelElements, childElements.Count()));

                            inputModelElement.Add(childElements);

                            maxDisambiguator = maxCummulativeDisambiguator;
 
                            customDataElement.Remove();

                        }
                    }
                }

                DacPackage outputPackage = null;
                if (singleFileMode)
                {
                    outputPackage = inputPackage;
                }
                else
                {
                    outputPackage = new DacPackage(outputFile);
                    outputPackage.Open(FileAccess.ReadWrite);
                }

                if (singleFileMode)
                {
                    LogWriter.WriteMessage(String.Format(DacMergeResource.Stream, DacPackage.ModelUri));
                    inputModelDoc.Save(outputPackage.GetStream(DacPackage.ModelUri));
                }
                else
                {
                    LogWriter.WriteMessage(DacMergeResource.CopyingStreams);

                    // copy all parts from input to output package,
                    // except for the model.xml stream which will is replace with the merged XML document content
                    foreach (var part in inputPackage.GetParts())
                    {
                        LogWriter.WriteMessage(String.Format(DacMergeResource.Stream, part.Uri));

                        var outputPart = outputPackage.CreatePartAs(part);

                        if (part.Uri == DacPackage.ModelUri)
                        {
                            inputModelDoc.Save(outputPart.GetStream());
                        }
                        else
                        {
                            if (outputPart == null)
                                throw new NullReferenceException("outputPart");

                            part.GetStream().CopyTo(outputPart.GetStream());
                        }
                    }
                }

                var outputOriginDoc = XDocument.Load(XmlReader.Create(outputPackage.GetStream(DacPackage.OriginUri)));
                if (outputOriginDoc.Root == null)
                {
                    throw new NullReferenceException("outputOriginDoc");
                }

                LogWriter.WriteMessage(DacMergeResource.CalculatingChecksum);
                var newChecksum = outputPackage.CalculateChecksum();
                LogWriter.WriteMessage(DacPackage.ByteArrayToString(newChecksum));

                LogWriter.WriteMessage(DacMergeResource.UpdatingChecksum);
                outputPackage.UpdateChecksum(newChecksum);

                outputOriginDoc.Save(outputPackage.GetStream(DacPackage.OriginUri));

                outputPackage.Close();
                inputPackage.Close();
            }

            return true;
        }

        private static int RemoveElements(XElement modelElement, string atributeName, string attributeValue)
        {
            var count = modelElement.Elements().Where(
                e =>
                {
                    var attribute = e.Attribute(atributeName);
                    return attribute != null && attribute.Value == attributeValue;
                }).Count();

            if (count > 0)
            {
                LogWriter.WriteMessage(string.Format(DacMergeResource.RemovingElements, attributeValue, count));

                modelElement.Elements().Where(
                    e =>
                        {
                            var attribute = e.Attribute(atributeName);
                            return attribute != null && attribute.Value == attributeValue;
                        }).Remove();
            }

            return count;
        }

        private static int RemoveDescendents(XElement modelElement, string attributeName, string attributeValue, string sqlObjectType)
        {
            var count = modelElement.Descendants().Where(e => e.Name.LocalName == Constants.RelationshipElement).Where(
                a =>
                {
                    var attribute = a.Attribute(attributeName);
                    return attribute != null && attribute.Value == attributeValue;
                }).Where(
                        p =>
                        {
                            if (p.Parent == null)
                            {
                                return false;
                            }

                            var attribute = p.Parent.Attribute(Constants.TypeAttribute);
                            return attribute != null && (p.Parent != null && attribute.Value == sqlObjectType);
                        }).Count();

            if (count > 0)
            {
                LogWriter.WriteMessage(string.Format(DacMergeResource.RemoveDescendents, attributeValue, sqlObjectType, count));

                modelElement.Descendants().Where(e => e.Name.LocalName == Constants.RelationshipElement).Where(
                    a =>
                        {
                            var attribute = a.Attribute(attributeName);
                            return attribute != null && attribute.Value == attributeValue;
                        }).Where(
                            p =>
                                {
                                    if (p.Parent == null)
                                    {
                                        return false;
                                    }

                                    var attribute = p.Parent.Attribute(Constants.TypeAttribute);
                                    return attribute != null && (p.Parent != null && attribute.Value == sqlObjectType);
                                }).Remove();
            }
            return count;
        }

        private static FileInfo ResolveFileName(string childFileName, FileInfo parentFile, DirectoryInfo loadPath)
        {
            /* File resolution order:
             * 1) same directory as the input package
             * 2) absolute path as specified in the input package
             * 3) using the reference load path parameter /p
             */
            try
            {
                if (File.Exists(Path.Combine(Path.GetDirectoryName(parentFile.FullName), Path.GetFileName(childFileName))))
                {
                    return new FileInfo(Path.Combine(Path.GetDirectoryName(parentFile.FullName),Path.GetFileName(childFileName)));
                }
                else if (File.Exists(childFileName))
                {
                    return new FileInfo(childFileName);
                }
                else if (loadPath != null && loadPath.Exists)
                {
                    if (File.Exists(Path.Combine(loadPath.FullName, Path.GetFileName(childFileName))))
                    {
                        return new FileInfo(Path.Combine(loadPath.FullName, Path.GetFileName(childFileName)));
                    }
                    else
                    {
                        List<string> files = Directory.GetFiles(
                            loadPath.FullName, 
                            Path.GetFileName(childFileName),
                            SearchOption.AllDirectories).ToList<string>();

                        return null;
                    }
                }

                return null;
            }
            catch (IOException)
            {
                return null;    
            }
        }
    }
}
