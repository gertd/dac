//------------------------------------------------------------------------------
// <copyright file="DacPackage.cs" company="SQLProj.com">
//         Copyright © 2013 SQLProj.com - All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace SqlProj.Dac
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.IO;
    using System.IO.Packaging;
    using System.Xml.Linq;
    using System.Xml;
    using System.Security.Cryptography;
    using System.Globalization;

    public class DacPackage : IDisposable
    {
        private bool _disposed;

        private Package _package;
        
        private readonly Dictionary<Uri, PackagePart> _parts;

        public static Uri OriginUri = PackUriHelper.CreatePartUri(new Uri(Constants.OriginXmlUri, UriKind.Relative));
        public static Uri ModelUri = PackUriHelper.CreatePartUri(new Uri(Constants.ModelXmlUri, UriKind.Relative));
        public static Uri DacMetaDataUri = PackUriHelper.CreatePartUri(new Uri(Constants.DacMetadataXmlUri, UriKind.Relative));

        readonly XmlWriterSettings _xmlWriterSettings = new XmlWriterSettings()
        {
            Encoding = Encoding.UTF8
        };

        public FileInfo FileInfo { get; private set; }

        public DacPackage(FileInfo package)
        {
            this._parts = new Dictionary<Uri, PackagePart>();
            this.FileInfo = package;
            this.IsOriginUpdated = false;
            this.IsModelUpdated = false;
        }

        public void Open()
        {
            this.Open(FileAccess.Read);
        }

        public void Open(FileAccess fileAccess)
        {
            if (this.FileInfo == null)
                throw new NullReferenceException("Package FileInfo is null");

            if (!this.FileInfo.Exists)
                throw new FileNotFoundException(this.FileInfo.FullName);

            if (_package != null)
            {
                _package.Close();
                _package = null;
            }

            _package = Package.Open(this.FileInfo.FullName, FileMode.Open, fileAccess);

            if (_package == null)
                throw new NullReferenceException("_package");
        }

        public void Close()
        {
            if (_package != null)
            {
                _package.Close();
                _package = null;
            }
        }

        public void Save()
        {
            if (_package.FileOpenAccess != FileAccess.ReadWrite)
            {
                Open(FileAccess.ReadWrite);
            }

            _package.Flush();
        }

        public void SaveTo(FileInfo package)
        { 

        }

        public IEnumerable<Uri> GetPartUris()
        {
            return _package.GetParts().Select(part => part.Uri).ToList();
        }

        public IEnumerable<PackagePart> GetParts()
        {
            return _package.GetParts().ToList();
        }

        public PackagePart GetPart(Uri uri)
        {
            PackagePart pp;
            if (_parts.TryGetValue(uri, out pp))
            {
                return pp;
            }
            else
            {
                try
                {
                    pp = _package.GetPart(uri);
                    _parts.Add(uri, pp);
                }
                catch (ArgumentNullException) { }
                catch (ArgumentException) { }
                catch (ObjectDisposedException) { }
                catch (InvalidOperationException) {}
                catch (IOException) { }
            }
            return pp;
        }

        public PackagePart CreatePartAs(PackagePart packagePart)
        {
            return _package.CreatePart(packagePart.Uri, packagePart.ContentType, packagePart.CompressionOption);
        }

        public XDocument GetDocument(Uri uri)
        {
            var xdoc = XDocument.Load(XmlReader.Create(GetPart(uri).GetStream()));
            if (xdoc == null)
                throw new NullReferenceException("xdoc");

            return xdoc;
        }

        public Stream GetStream(Uri uri)
        {
            var strm = GetPart(uri).GetStream();
            if (strm == null)
                throw new NullReferenceException("strm");

            return strm;
        }

        public XDocument DacMetadata
        {
            get
            {
                return GetDocument(DacPackage.DacMetaDataUri);
            }
        }

        public XDocument Origin
        {
            get
            {
                return GetDocument(DacPackage.OriginUri);
            }
        }

        public bool IsOriginUpdated { get; private set; }

        public XDocument Model
        {
            get
            {
                return GetDocument((DacPackage.ModelUri));
            }
        }

        public bool IsModelUpdated { get; private set; }

        public byte[] ReadChecksum()
        {
            var checksumElement = Origin.Root.Descendants().Elements()
                .Where(e => e.Name.LocalName == Constants.ChecksumElement).FirstOrDefault(a =>
                {
                    var attribute = a.Attribute(Constants.UriAttribute);
                    return attribute != null && attribute.Value == Constants.ModelXmlUri;
                });

            if (checksumElement == null || String.IsNullOrEmpty(checksumElement.Value))
            {
                throw new NullReferenceException("checksumElement");
            }
            
            return StringToByteArray(checksumElement.Value);
        }

        public void UpdateChecksum(byte[] checksum)
        {
            var checksumElement = Origin.Root.Descendants().Elements()
            .Where(e => e.Name.LocalName == Constants.ChecksumElement).FirstOrDefault(a =>
            {
                var attribute = a.Attribute(Constants.UriAttribute);
                return attribute != null && attribute.Value == Constants.ModelXmlUri;
            });

            if (checksumElement == null)
            {
                throw new NullReferenceException("checksumElement");
            }
            
            checksumElement.Value = ByteArrayToString(checksum);
        }

        public static bool IsDacPac(FileInfo package)
        {
            try
            {
                using (Package opc = Package.Open(package.FullName, FileMode.Open, FileAccess.Read))
                {
                    var originPartUri = PackUriHelper.CreatePartUri(new Uri(Constants.OriginXmlUri, UriKind.Relative));

                    var originPart = opc.GetPart(originPartUri);
                    var originDoc = XDocument.Load(XmlReader.Create(originPart.GetStream()));

                    if (originDoc.Root == null || string.Compare(originDoc.Root.Name.NamespaceName, Constants.DacXsdUri, StringComparison.InvariantCulture) != 0)
                    {
                        return false;
                    }

                    if (string.Compare(originDoc.Root.Name.LocalName, Constants.DacOriginElement, StringComparison.InvariantCulture) != 0)
                    {
                        return false;
                    }

                    var productSchema = originDoc.Root.Descendants().Elements().FirstOrDefault(d => d.Name.LocalName == Constants.ProductSchemaElement);
                    if (productSchema == null || string.Compare(productSchema.Value, Constants.DacXsdUri, StringComparison.InvariantCulture) != 0)
                    {
                        return false;
                    }

                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public byte[] CalculateChecksum()
        {
            var hashGenerator = HashAlgorithm.Create("System.Security.Cryptography.SHA256CryptoServiceProvider");

            return hashGenerator.ComputeHash(GetStream(DacPackage.ModelUri));
        }

        public static string ByteArrayToString(byte[] bytes)
        {
            return string.Concat(bytes.Select(b => b.ToString("X2", CultureInfo.InvariantCulture)));
        }

        public static byte[] StringToByteArray(string hex)
        {
            if (hex.Length % 2 == 1)
            {
                hex = "0" + hex;
            }

            int byteCount = hex.Length / 2;
            byte[] bytes = new byte[byteCount];

            try
            {
                for (int i = 0; i < byteCount; i++)
                {
                    bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
                }
            }
            catch (FormatException)
            {
                bytes = null;
            }

            return bytes;
        }

        #region IDisposable
        protected virtual void Dispose(bool disposing)
        {
            if (disposing && _disposed == false)
            {
                if (_package != null)
                {
                    _package.Close();
                    _package = null;
                }

                _disposed = true;
            }
        }

        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
