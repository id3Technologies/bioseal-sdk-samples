using System;
using System.Collections.Generic;
using System.Linq;

namespace id3.BioSeal.Samples
{
    using id3.Bioseal;
    using System.IO;
    using System.Net;

    public static class BioSealSDKTools
    {
        // cache variables
        private static bool localCache_ = true;
        private static string localCacheDir_ = @"C:\temp\cache_bioseal\";
        private static bool internalCache_ = true;

        // BioSeal instance
        private static Bioseal bioseal_;

        // initialize BioSeal instance and callback
        public static void Initialize()
        {
            bioseal_ = new Bioseal();

            // optionnal, use cache
            bioseal_.ExternalResourceCallback = new ResourceCallbackHandler(GetExternalResourceWithCache);
            bioseal_.EnableDownloadCache = internalCache_;
        }

        public static void Decode(byte[] data)
        {
            try
            {
                bioseal_.Decode(data);
                bioseal_.Verify();
            }
            catch (BiosealException ex)
            {
                throw ex;
            }
        }

        public static void DecodeBase32String(string text)
        {
            bioseal_.DecodeFromString(text);
        }

        public static void Verify()
        {
            bioseal_.Verify();
        }

        public static BiosealFormat GetFormat()
        {
            return bioseal_.GetFormat();
        }

        #region biographics

        /// <summary>
        /// Gets the dictionary of biographics data from the BioSeal instance.
        /// </summary>
        public static Dictionary<string, object> GetBiographics()
        {
            Dictionary<string, object> biographics = new Dictionary<string, object>();

            // Payload
            var keys = bioseal_.Payload.Keys;
            foreach (string key in keys)
            {
                var field = bioseal_.Payload[key];
                string type = field.FieldType.ToString();
                string data = "?"; // default
                switch (field.FieldType)
                {
                    case FieldType.Integer:
                        data = field.ValueAsInteger.ToString();
                        break;

                    case FieldType.Boolean:
                        data = field.ValueAsBoolean.ToString();
                        break;

                    case FieldType.Float:
                        data = field.ValueAsFloat.ToString();
                        break;

                    case FieldType.String:
                        data = field.ValueAsString;
                        break;

                    case FieldType.Binary:
                        data = HexTools.GetString(field.ValueAsBinary);
                        break;

                    case FieldType.Date:
                        {
                            var datetime = field.ValueAsDateTime;
                            System.DateTime date_time;
                            if (datetime.Hour > 0)
                            {
                                date_time = new System.DateTime(datetime.Year, datetime.Month, datetime.Day, datetime.Hour, datetime.Minute, 0);
                                data = date_time.ToLongDateString() + " " + date_time.ToShortTimeString();
                            }
                            else
                            {
                                date_time = new System.DateTime(datetime.Year, datetime.Month, datetime.Day);
                                data = date_time.ToLongDateString();
                            }
                            break;
                        }

                    case FieldType.Time:
                        {
                            var time = field.ValueAsDateTime;
                            var now = System.DateTime.Now;
                            var date_time = new System.DateTime(now.Year, now.Month, now.Day, time.Hour, time.Minute, time.Second);
                            data = date_time.ToShortTimeString();
                            break;
                        }

                    case FieldType.Timestamp:
                        {
                            var time = field.ValueAsDateTime;
                            var now = System.DateTime.Now;
                            var date_time = new System.DateTime(now.Year, now.Month, now.Day, time.Hour, time.Minute, time.Second);
                            data = date_time.ToString();
                            break;
                        }
                }

                if (key != "face_image" && key != "face_template" && key != "BiometricDataGroup")
                    biographics.Add(key, data);
            }

            return biographics;
        }

        #endregion

        #region face

        /// <summary>
        /// Returns true iff. the BioSeal instance contains a face template.
        /// </summary>
        public static bool ContainsFaceTemplate()
        {
            if (bioseal_ == null)
                return false;

            FieldList faceTemplates = bioseal_.FindBiometrics(BiometricDataType.FaceTemplate, BiometricFormat.Undefined);
            return (faceTemplates.Count > 0);
        }

        /// <summary>
        /// Returns true iff. the BioSeal instance contains a face image.
        /// </summary>
        public static bool ContainsFaceImage()
        {
            return (bioseal_ != null && bioseal_.ContainsFaceImages);
        }

        /// <summary>
        /// Gets the face template data from the BioSeal instance.
        /// </summary>
        public static byte[] GetFaceTemplate()
        {
            if (bioseal_.Format == BiosealFormat.Undefined || !bioseal_.ContainsFaceTemplates)
                return null;

            using (var field_face_list = bioseal_.FindBiometrics(BiometricDataType.FaceTemplate, BiometricFormat.Undefined))
            {
                foreach (Field field_face in field_face_list)
                    return field_face.ValueAsBinary;
            }

            return null;
        }

        /// <summary>
        /// Gets the face image data from the BioSeal instance.
        /// </summary>
        /// <returns>Face image data in webp BGR 24bits format</returns>returns>
        public static byte[] GetFaceImageData()
        {
            if (bioseal_.Format == BiosealFormat.Undefined || !bioseal_.ContainsFaceImages)
                return null;
        
            byte[] imageData = null;
            using (var field_face_list = bioseal_.FindBiometrics(BiometricDataType.FaceImage, BiometricFormat.Undefined))
            {
                foreach (Field field_face in field_face_list)
                    imageData = field_face.GetValueAsBinary();
            }
        
            return imageData;
        }

        #endregion

        #region fingers

        /// <summary>
        /// Returns true iff. the BioSeal instance contains finger templates.
        /// </summary>
        public static bool ContainsFingerTemplates()
        {
            return (bioseal_ != null && bioseal_.ContainsFingerTemplates);
        }

        /// <summary>
        /// Gets the enrolled fingers' positions from the BioSeal instance.
        /// </summary>
        public static List<int> GetEnrolledFingerPositions()
        {
            if (bioseal_.Format == BiosealFormat.Undefined || !bioseal_.ContainsFingerTemplates)
                return null;
        
            // get fingerprints positions
            List<int> fingerPositions = new List<int>();
            using (var field_fingers_list = bioseal_.FindBiometrics(BiometricDataType.FingerTemplate, BiometricFormat.Undefined))
            {
                foreach (Field field_fingers in field_fingers_list)
                {
                    for (int i = 0; i < field_fingers.Count; i++)
                    {
                        using (var finger = field_fingers.GetObjectAtIndex(i))
                        {
                            var position = finger["position"];
                            int pos = (int)position.ValueAsInteger;
                            fingerPositions.Add(pos);
                        }
                    }
                }
            }
        
            return fingerPositions;
        }

        /// <summary>
        /// Gets the enrolled fingers' templates from the BioSeal instance.
        /// </summary>
        public static Dictionary<int, byte[]> GetFingerTemplates()
        {
            if (bioseal_.Format == BiosealFormat.Undefined || !bioseal_.ContainsFingerTemplates)
                return null;

            // get fingerprints positions and templates
            Dictionary<int, byte[]> positionsTemplates = new Dictionary<int, byte[]>();
            using (var field_fingers_list = bioseal_.FindBiometrics(BiometricDataType.FingerTemplate, BiometricFormat.Undefined))
            {
                foreach (Field field_fingers in field_fingers_list)
                {
                    for (int i = 0; i < field_fingers.Count; i++)
                    {
                        using (var finger = field_fingers.GetObjectAtIndex(i))
                        {
                            var position = finger["position"];
                            var template_data = finger["template"];

                            int pos = (int)position.ValueAsInteger;
                            byte[] template = template_data.ValueAsBinary;

                            positionsTemplates.Add(pos, template);
                        }
                    }
                }
            }

            return positionsTemplates;
        }

        #endregion

        #region signature

        /// <summary>
        /// Gets the signature verification status (1 = true, 0 = false, -1 = not verificated or not verificable).
        /// </summary>
        public static int GetSignatureVerifiedStatus()
        {
            return bioseal_.VerificationResult.VdsSignatureVerified;
        }

        /// <summary>
        /// Gets the certification chain verified status (1 = true, 0 = false, -1 = not verificated or not verificable).
        /// </summary>
        public static int GetCertificationChainVerifiedStatus()
        {
            return bioseal_.VerificationResult.CertificationChainVerified;
        }

        /// <summary>
        /// Gets the certificate usage authorized status (1 = true, 0 = false, -1 = not verificated or not verificable).
        /// </summary>
        public static int GetSigningCertificateUsageAuthorized()
        {
            return bioseal_.VerificationResult.SigningCertificateUsageAuthorized;
        }

        #endregion

        #region governance

        /// <summary>
        /// Gets the LOTL url.
        /// </summary>
        public static string GetLOTLUrl()
        {
            return bioseal_.GetLotlUrl();
        }

        /// <summary>
        /// Gets the TSL url.
        /// </summary>
        public static string GetTSLUrl()
        {
            return bioseal_.GetTslUrl();
        }

        /// <summary>
        /// Gets the manifest url.
        /// </summary>
        public static string GetManifestUrl()
        {
            return bioseal_.GetManifestUrl();
        }

        /// <summary>
        /// Gets the LOTL validity status.
        /// </summary>
        public static int GetLOTLValidityStatus()
        {
            return bioseal_.VerificationResult.LotlGovernanceValid;
        }

        /// <summary>
        /// Gets the TSL validity status.
        /// </summary>
        public static int GetTSLValidityStatus()
        {
            return bioseal_.VerificationResult.TslGovernanceValid;
        }

        /// <summary>
        /// Gets the manifest validity status.
        /// </summary>
        public static int GetManifestValidityStatus()
        {
            return bioseal_.VerificationResult.ManifestGovernanceValid;
        }

        /// <summary>
        /// Gets the authority verified status (1 = true, 0 = false, -1 = not verificated or not verificable).
        /// </summary>
        public static int GetAuthorityVerifiedStatus()
        {
            return bioseal_.VerificationResult.CaCertificateVerified;
        }

        #endregion

        #region manifest

        /// <summary>
        /// Gets the use case identifier and version from the manifest of the BioSeal instance.
        /// </summary>
        public static string GetUseCaseID()
        {
            string hexStr = HexTools.GetString(bioseal_.GetManifestId());
            hexStr = hexStr.Replace(" ", "");
            hexStr = hexStr.Insert(4, "-");

            return hexStr;
        }

        /// <summary>
        /// Gets the document name from the manifest of the BioSeal instance.
        /// </summary>
        /// <param name="language">language</param>
        public static string GetDocumentName(string language = "en")
        {
            return bioseal_.GetDocumentName(language);
        }

        #endregion

        #region certificate

        /// <summary>
        /// Gets the certificate authority identifier from the BioSeal instance.
        /// </summary>
        public static string GetCertificateAuthorityId()
        {
            return bioseal_.CertificateAuthorityId;
        }

        /// <summary>
        /// Gets the certificate authority issuing country from the BioSeal instance.
        /// </summary>
        public static string GetCertificateAuthorityIssuingCountry()
        {
            return bioseal_.CertificateAuthorityIssuingCountry;
        }

        /// <summary>
        /// Gets the certificate issuer common name from the BioSeal instance.
        /// </summary>
        public static string GetCertificateIssuer()
        {
            return bioseal_.CertificateInformation.GetIssuerCommonName();
        }

        /// <summary>
        /// Gets the certificate subject common name from the BioSeal instance.
        /// </summary>
        public static string GetCertificateSubject()
        {
            return bioseal_.CertificateInformation.GetSubjectCommonName();
        }

        /// <summary>
        /// Gets the certificate issuer organization from the BioSeal instance.
        /// </summary>
        public static string GetCertificateSubjectOrganization()
        {
            return bioseal_.CertificateInformation.GetSubjectOrganization();
        }

        /// <summary>
        /// Gets the certificate issuer organization unit from the BioSeal instance.
        /// </summary>
        public static string GetCertificateSubjectOrganizationUnit()
        {
            return bioseal_.CertificateInformation.GetSubjectOrganizationalUnit();
        }

        /// <summary>
        /// Gets the certificate issuer organization from the BioSeal instance.
        /// </summary>
        public static string GetCertificateIssuerOrganization()
        {
            return bioseal_.CertificateInformation.GetIssuerOrganization();
        }

        /// <summary>
        /// Gets the certificate issuer organization unit from the BioSeal instance.
        /// </summary>
        public static string GetCertificateIssuerOrganizationUnit()
        {
            return bioseal_.CertificateInformation.GetIssuerOrganizationalUnit();
        }

        /// <summary>
        /// Gets the certificate creation date from the BioSeal instance.
        /// </summary>
        public static DateTime GetCertificateCreationDate()
        {
            return bioseal_.CertificateInformation.NotBefore;
        }

        /// <summary>
        /// Gets the certificate expiration date from the BioSeal instance.
        /// </summary>
        public static DateTime GetCertificateExpirationDate()
        {
            return bioseal_.CertificateInformation.NotAfter;
        }

        #endregion

        #region representations

        /// <summary>
        /// Gets the presentation view from the BioSeal instance.
        /// </summary>
        /// <param name="language">language</param>
        /// <returns>Presentation view in html</returns>
        public static string GetPresentationView(string language = "en")
        {
            if (bioseal_ == null || bioseal_.Format == BiosealFormat.Undefined)
                return String.Empty;

            // check if language is supported
            bool isLanguageSupported = false;
            List<string> supportedLanguages = new List<string>();
            foreach (string supportedLanguage in bioseal_.SupportedHtmlViewLanguages)
            {
                supportedLanguages.Add(supportedLanguage);
                if (supportedLanguage == language)
                {
                    isLanguageSupported = true;
                    break;
                }
            }
            if (!isLanguageSupported)
            {
                if (supportedLanguages.Contains("en") || supportedLanguages.Count == 0)
                    language = "en";
                else
                    language = supportedLanguages.First();
            }

            // ok, build corresponding presentation view
            bioseal_.BuildHtmlView(language, true);
            return bioseal_.HtmlView;
        }

        /// <summary>
        /// Gets the JSON representation of the BioSeal payload.
        /// </summary>
        public static string GetJSONRepresentation()
        {
            return bioseal_.BuildPayloadAsJson(String.Empty);
        }

        #endregion

        #region callbacks

        /// <summary>
        /// Implements the GetExternalResource callback.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="argsPtr"></param>
        /// <returns>Error code</returns>
        private static int GetExternalResource(IntPtr context, IntPtr argsPtr)
        {
            int err = 0;
            try
            {
                using (ResourceCallbackArgs args = new ResourceCallbackArgs(argsPtr))
                {
#if DEBUG
                    Console.WriteLine("   BioSeal decode URI: " + args.Uri);
#endif
                    args.Download();
                }
            }
            catch
            {
                err = (int)BiosealError.ExceptionInCallback;
            }
            return err;
        }

        /// <summary>
        /// Implements the GetExternalResource with cache callback.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="argsPtr"></param>
        /// <returns>Error code</returns>
        private static int GetExternalResourceWithCache(IntPtr context, IntPtr argsPtr)
        {
            int err = 0;
            try
            {
                using (ResourceCallbackArgs args = new ResourceCallbackArgs(argsPtr))
                {
                    if (!localCache_)
                    {
                        args.Download();
                    }
                    else
                    {
#if DEBUG
                        Console.WriteLine("URI = " + args.Uri);
#endif

                        // Cache disque
                        if (!Directory.Exists(localCacheDir_))
                            Directory.CreateDirectory(localCacheDir_);

                        string[] cache_files = Directory.GetFiles(localCacheDir_, args.ResourceName);
                        if (cache_files.Length == 0 || args.RequiresUpdate)
                        {
                            try
                            {
                                args.Download();
                                File.WriteAllBytes(localCacheDir_ + args.ResourceName, args.OutputData);
                            }
                            catch (BiosealException)
                            {
                                err = (int)BiosealError.ResourceNotFound;
                            }
                        }
                        else
                        {
                            args.OutputData = File.ReadAllBytes(cache_files[0]);
                        }
                    }
                }
            }
            catch (BiosealException /*ex*/)
            {
                err = (int)BiosealError.ExceptionInCallback;
            }
            return err;
        }

        #endregion
    }
}
