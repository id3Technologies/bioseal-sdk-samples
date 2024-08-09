using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace id3.BioSeal.Samples
{
    using id3.Bioseal;

    class Program
    {
        static Bioseal bioseal_;
        static bool localCache_ = true;
        static string localCacheDir_ = @"C:\temp\cache_bioseal\";

        static void Main(string[] args)
        {
            Console.WriteLine("-------------------");
            Console.WriteLine("id3.BioSeal.Samples");
            Console.WriteLine("-------------------");

            // The bioseal instance must first be initialized
            bioseal_ = new Bioseal();

            // optionnal, use cache
            bioseal_.ExternalResourceCallback = new ResourceCallbackHandler(GetExternalResourceWithCache);
            //bioseal_.EnableDownloadCache = true;

            // This basic sample shows how to read BioSeal biographics only contents
            displayBioSealInfo(@"../../../../data/ExBioSealBiographics.dat");

            // This sample shows how to read BioSeal face image and template contents
            displayBioSealInfo(@"../../../../data/ExBioSealFace.dat");

            // This accreditation sample shows how to read BioSeal face template contents
            displayBioSealInfo(@"../../../../data/ExBioSealAccreditation.dat");

            //Console.ReadLine();
        }

        static void displayBioSealInfo(string path)
        {
            // read and decode BioSeal contents
            Console.WriteLine();
            Console.WriteLine("Decoding BioSeal file " + path);
            byte[] dataBioSeal = File.ReadAllBytes(path);
            try
            {
                bioseal_.VerifyFromBuffer(dataBioSeal);
            }
            catch (BiosealException ex)
            {
                Console.WriteLine(ex.Message);
                throw ex;
            }

            Console.WriteLine($"  BioSeal format : {bioseal_.Format}");
            // display manifest information
            Console.WriteLine("   Use case: " + $"{bioseal_.ManifestId:X6}");
            // enumerate available languages
            using (var supportedLanguages = bioseal_.SupportedLanguages)
            {
                foreach (string language in supportedLanguages)
                {
                    Console.WriteLine($"   Document name: '{bioseal_.GetDocumentName(language)}'");
                }
            }

            // display payload information
            Console.WriteLine("   Payload:");
            Dictionary<string, object> payload = GetPayload();
            foreach (KeyValuePair<string, object> keyValue in payload)
                Console.WriteLine($"      {keyValue.Key}: {keyValue.Value}");

            // display face template if existing
            bool hasFaceTemplate = bioseal_.ContainsFaceTemplates;
            Console.WriteLine("   Face template: " + hasFaceTemplate);
            if (hasFaceTemplate)
            {
                using (var field_face_list = bioseal_.FindBiometrics(BiometricDataType.FacialFeatures, null))
                {
                    if (field_face_list.Count > 0)
                    {
                        var field_face = field_face_list.Get(0);
                        Console.WriteLine("   Face template saved in data folder");
                        File.WriteAllBytes(path.Replace(".dat", ".template"), field_face.ValueAsBinary);
                    }
                }
            }

            // display and save face image if existing
            bool hasFaceImage = bioseal_.ContainsPortraits;
            Console.WriteLine("   Face image: " + hasFaceImage);
            if (hasFaceImage)
            {
                using (var field_face_list = bioseal_.FindFieldsByExtension(FieldExtensionType.Portrait))
                {
                    if (field_face_list.Count > 0)
                    {
                        var field_face = field_face_list.Get(0);
                        Console.WriteLine("   Face image saved in data folder");
                        File.WriteAllBytes(path.Replace(".dat", ".webp"), field_face.ValueAsBinary);
                    }
                }
            }

            // fetch and save presentation view
            Console.WriteLine("   Presentation view supported languages:");
            foreach (string supportedLanguage in bioseal_.SupportedLanguages)
            {
                Console.WriteLine("      " + supportedLanguage);
            }
            // Get default language
            bioseal_.BuildHtmlView(null, true);
            File.WriteAllText(path.Replace(".dat", ".html"), bioseal_.HtmlView);
            Console.WriteLine("   Presentation view saved in data folder");

            // build and save JSON file
            string jsonPayload = bioseal_.BuildVdsAsJson("  ");
            File.WriteAllText(path.Replace(".dat", ".json"), jsonPayload);
            Console.WriteLine("   JSON representation saved in data folder");

            // display signature information
            Console.WriteLine("   Signature:");
            Console.WriteLine("      Signature verified status: " + bioseal_.VerificationResult.VdsSignatureVerified);
            Console.WriteLine("      Certification chain verified status: " + bioseal_.VerificationResult.CertificationChainVerified);
            Console.WriteLine("      Certificate usage authorized status: " + bioseal_.VerificationResult.SigningCertificateUsageAuthorized);

            // display governance information
            Console.WriteLine("   Governance:");
            Console.WriteLine("      LoTL: " + bioseal_.LotlUrl);
            Console.WriteLine("      TSL: " + bioseal_.TslUrl);
            Console.WriteLine("      Manifest: " + bioseal_.ManifestUrl);
            Console.WriteLine("      LoTL valid status: " + bioseal_.VerificationResult.LotlGovernanceValid);
            Console.WriteLine("      TSL valid status: " + bioseal_.VerificationResult.TslGovernanceValid);
            Console.WriteLine("      Manifest valid status: " + bioseal_.VerificationResult.ManifestGovernanceValid);
            Console.WriteLine("      Authority verified status: " + bioseal_.VerificationResult.CaCertificateVerified);

            // display certificate information
            Console.WriteLine("   Certificate:");
            Console.WriteLine("      Authority AC: " + bioseal_.CertificateAuthorityReference);
            Console.WriteLine("      Authority ID: " + bioseal_.CertificateIdentifier);
            Console.WriteLine("      Issuer: " + bioseal_.CertificateInformation.IssuerCommonName);
            Console.WriteLine("      Subject: " + bioseal_.CertificateInformation.SubjectCommonName);
            Console.WriteLine("      Organization: " + bioseal_.CertificateInformation.SubjectOrganization);
            Console.WriteLine("      Organization unit: " + bioseal_.CertificateInformation.SubjectOrganizationalUnit);
            Console.WriteLine("      Date of creation: " + bioseal_.CertificateInformation.NotBefore.ToString());
            Console.WriteLine("      Date of expiration: " + bioseal_.CertificateInformation.NotAfter.ToString());

            Console.WriteLine();
        }

        /// <summary>
        /// Gets the dictionary of biographics data from the BioSeal instance.
        /// </summary>
        public static Dictionary<string, object> GetPayload()
        {
            Dictionary<string, object> payload = new Dictionary<string, object>();
            // Scan payload
            string data = "?"; // default
            foreach (Field field in bioseal_.Payload)
            {
                if (field.IsNull)
                {
                    data = "null";
                }
                else
                {
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
                            data = BitConverter.ToString(field.ValueAsBinary).Replace("-", "");
                            break;

                        case FieldType.Date:
                            {
                                var datetime = field.ValueAsDate;
                                System.DateTime date_time;
                                date_time = new System.DateTime(datetime.Year, datetime.Month, datetime.Day);
                                data = date_time.ToLongDateString();
                                break;
                            }

                        case FieldType.Time:
                            {
                                var time = field.ValueAsTime;
                                var now = System.DateTime.Now;
                                var date_time = new System.DateTime(now.Year, now.Month, now.Day, time.Hour, time.Minute, time.Second);
                                data = date_time.ToShortTimeString();
                                break;
                            }

                        case FieldType.Timestamp:
                            {
                                var datetime = field.ValueAsDateTime;
                                var date_time = new System.DateTime(datetime.Year, datetime.Month, datetime.Day, datetime.Hour, datetime.Minute, datetime.Second);
                                data = date_time.ToString();
                                break;
                            }
                    }
                }
                payload.Add(field.Name, data);
            }
            return payload;
        }

        /// <summary>
        /// Implements the GetExternalResource with cache callback.
        /// </summary>
        /// <param name="context">Bioseal handle</param>
        /// <param name="argsPtr">ResourceCallbackArgs handle</param>
        /// <returns>Error code</returns>
        static int GetExternalResourceWithCache(object context, ResourceCallbackArgs args)
        {
            int err = 0;
            try
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
            catch (BiosealException ex)
            {
                Console.WriteLine(ex.Message);
                err = (int)BiosealError.ExceptionInCallback;
            }
            return err;
        }

    }
}
