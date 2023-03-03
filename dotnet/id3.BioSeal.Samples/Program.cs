using System;
using System.Collections.Generic;
using System.IO;

namespace id3.BioSeal.Samples
{
    using id3.Bioseal;

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("-------------------");
            Console.WriteLine("id3.BioSeal.Samples");
            Console.WriteLine("-------------------");

            // The bioseal instance must first be initialized
            BioSealSDKTools.Initialize();

            // This basic sample shows how to read BioSeal biographics only contents
            displayBioSealInfo(@"../../../../data/ExBioSealBiographics.bin");

            // This sample shows how to read BioSeal face image and template contents
            displayBioSealInfo(@"../../../../data/ExBioSealFace.bin");

            // This accreditation sample shows how to read BioSeal face template contents
            displayBioSealInfo(@"../../../../data/ExBioSealAccreditation.bin");

            Console.ReadLine();
        }

        static void displayBioSealInfo(string path)
        {
            // read and decode BioSeal contents
            Console.WriteLine();
            Console.WriteLine("Decoding BioSeal file " + path);
            byte[] dataBioSeal = File.ReadAllBytes(path);
            BioSealSDKTools.Decode(dataBioSeal);

            // display manifest information
            Console.WriteLine("   Use case: " + BioSealSDKTools.GetUseCaseID());
            Console.WriteLine("   Document name: " + BioSealSDKTools.GetDocumentName());

            // display biographic information
            Console.WriteLine("   Biographics:");
            Dictionary<string, object> biographics = BioSealSDKTools.GetBiographics();
            foreach (KeyValuePair<string, object> keyValue in biographics)
                Console.WriteLine($"      {keyValue.Key}: {keyValue.Value}");

            // display face template if existing
            bool hasFaceTemplate = BioSealSDKTools.ContainsFaceTemplate();
            Console.WriteLine("   Face template: " + hasFaceTemplate);
            if (hasFaceTemplate)
            {
                byte[] faceTemplateData = BioSealSDKTools.GetFaceTemplate();
                Console.WriteLine("   Face template saved in data folder");
                File.WriteAllBytes(path.Replace(".bin", ".template"), faceTemplateData);
            }

            // display and save face image if existing
            bool hasFaceImage = BioSealSDKTools.ContainsFaceImage();
            Console.WriteLine("   Face image: " + hasFaceImage);
            if (hasFaceImage)
            {
                byte[] faceImageData = BioSealSDKTools.GetFaceImageData();
                Console.WriteLine("   Face image saved in data folder");
                File.WriteAllBytes(path.Replace(".bin", ".webp"), faceImageData);
            }

            // fetch and save presentation view
            string htmlPresentationView = BioSealSDKTools.GetPresentationView();
            File.WriteAllText(path.Replace(".bin", ".html"), htmlPresentationView);
            Console.WriteLine("   Presentation view saved in data folder");

            // build and save JSON file
            string jsonPayload = BioSealSDKTools.GetJSONRepresentation();
            File.WriteAllText(path.Replace(".bin", ".json"), jsonPayload);
            Console.WriteLine("   JSON representation saved in data folder");

            // display signature information
            Console.WriteLine("   Signature:");
            Console.WriteLine("      Signature verified status: " + BioSealSDKTools.GetSignatureVerifiedStatus());
            Console.WriteLine("      Certification chain verified status: " + BioSealSDKTools.GetCertificationChainVerifiedStatus());
            Console.WriteLine("      Certificate usage authorized status: " + BioSealSDKTools.GetSigningCertificateUsageAuthorized());

            // display governance information
            Console.WriteLine("   Governance:");
            Console.WriteLine("      LoTL: " + BioSealSDKTools.GetLOTLUrl());
            Console.WriteLine("      TSL: " + BioSealSDKTools.GetTSLUrl());
            Console.WriteLine("      Manifest: " + BioSealSDKTools.GetManifestUrl());
            Console.WriteLine("      LoTL valid status: " + BioSealSDKTools.GetLOTLValidityStatus());
            Console.WriteLine("      TSL valid status: " + BioSealSDKTools.GetTSLValidityStatus());
            Console.WriteLine("      Manifest valid status: " + BioSealSDKTools.GetManifestValidityStatus());
            Console.WriteLine("      Authority verified status: " + BioSealSDKTools.GetAuthorityVerifiedStatus());

            // display certificate information
            Console.WriteLine("   Certificate:");
            Console.WriteLine("      Authority ID: " + BioSealSDKTools.GetCertificateAuthorityId());
            Console.WriteLine("      Authority issuing country: " + BioSealSDKTools.GetCertificateAuthorityIssuingCountry());
            Console.WriteLine("      Issuer: " + BioSealSDKTools.GetCertificateIssuer());
            Console.WriteLine("      Subject: " + BioSealSDKTools.GetCertificateSubject());
            Console.WriteLine("      Organization: " + BioSealSDKTools.GetCertificateSubjectOrganization());
            Console.WriteLine("      Organization unit: " + BioSealSDKTools.GetCertificateSubjectOrganizationUnit());
            Console.WriteLine("      Date of creation: " + BioSealSDKTools.GetCertificateCreationDate().ToString());
            Console.WriteLine("      Date of expiration: " + BioSealSDKTools.GetCertificateExpirationDate().ToString());

            Console.WriteLine();
        }
    }
}
