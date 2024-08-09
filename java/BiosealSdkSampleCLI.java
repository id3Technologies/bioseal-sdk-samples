import eu.id3.bioseal.*;

import java.io.File;
import java.nio.file.Files;
import java.nio.file.Paths;
import java.util.Base64;

public class BiosealSdkSampleCLI {

    static boolean localCache = true;
    static String localCacheDir = "cache_bioseal";

    static void displayBioSealInfo(Bioseal bioseal, String biosealPath) {
        try {
            System.out.println("Decoding and verification BioSeal file " + biosealPath);
            byte[] bFile = Files.readAllBytes(Paths.get(biosealPath));
            VerificationResult verifyResult =bioseal.verifyFromBuffer(bFile);
            System.out.println("governanceValid = " + verifyResult.governanceValid);
            System.out.println("caCertificateVerified = " + verifyResult.caCertificateVerified);
            System.out.println("certificationChainVerified = " + verifyResult.certificationChainVerified);
            System.out.println("signingCertificateUsageAuthorized = " + verifyResult.signingCertificateUsageAuthorized);
            System.out.println("vdsNotExpired = " + verifyResult.vdsNotExpired);
            System.out.println("vdsSignatureVerified = " + verifyResult.vdsSignatureVerified);

            System.out.println("  BioSeal format : " + bioseal.getFormat());
            // display manifest information
            System.out.println("   Use case: " + String.format("%06x", bioseal.getManifestId()));
            var supportedLanguages = bioseal.getSupportedLanguages();
            for (var language : supportedLanguages) {
                System.out.println("   Document name: '"+ bioseal.getDocumentName(language)+ "'");
            }

            System.out.println("   Payload:");
            var payload = bioseal.getPayload();
            for (var field : payload) {
                String info = "null";
                if (!field.getIsNull() ) {
                    switch (field.getFieldType()) {
                        case INTEGER -> info = String.valueOf(field.getValueAsInteger());
                        case STRING -> info = field.getValueAsString();
                        case FLOAT -> info = String.valueOf(field.getValueAsFloat());
                        case BOOLEAN -> info = String.valueOf(field.getValueAsBoolean());
                        case BINARY -> info = Base64.getEncoder().encodeToString(field.getValueAsBinary());
                        case DATE -> info = field.getValueAsDate().toString();
                        case TIME -> info = field.getValueAsTime().toString();
                        case TIMESTAMP -> info = field.getValueAsDateTime().toString();
                        default -> info = "?";
                    }
                }
                System.out.println("      " + field.getName() + ": " + field.getFieldType() + " = '" + info + "'");
            }

            // display face template if existing
            boolean hasFaceTemplate = bioseal.getContainsFaceTemplates();
            System.out.println("   Face template: " + hasFaceTemplate);
            if (hasFaceTemplate) {
                var fieldFaceList = bioseal.findBiometrics(BiometricDataType.FACIAL_FEATURES, null);
                if (fieldFaceList.getCount() > 0) {
                    var fieldFace = fieldFaceList.get(0);
                    System.out.println("   Face template saved in data folder");
                    Files.write(Paths.get(biosealPath.replace(".dat", ".template")), fieldFace.getValueAsBinary());
                }
            }

            // display and save face image if existing
            boolean hasFaceImage = bioseal.getContainsPortraits();
            System.out.println("   Face image: " + hasFaceImage);
            if (hasFaceImage) {
                var fieldFaceList = bioseal.findFieldsByExtension(FieldExtensionType.PORTRAIT);
                if (fieldFaceList.getCount() > 0) {
                    var fieldFace = fieldFaceList.get(0);
                    System.out.println("   Face image saved in data folder");
                    Files.write(Paths.get(biosealPath.replace(".dat", ".webp")), fieldFace.getValueAsBinary());
                }
            }

            // fetch and save presentation view
            System.out.println("   Presentation view supported languages:");
            supportedLanguages = bioseal.getSupportedLanguages();
            for (var supportedLanguage : supportedLanguages) {
                System.out.println("      " + supportedLanguage);
            }

            // Get default language
            bioseal.buildHtmlView(null, true);
            Files.writeString(Paths.get(biosealPath.replace(".dat", ".html")), bioseal.getHtmlView());
            System.out.println("   Presentation view saved in data folder");

            // build and save JSON file
            var jsonPayload = bioseal.buildVdsAsJson("  ");
            Files.writeString(Paths.get(biosealPath.replace(".dat", ".json")), jsonPayload);
            System.out.println("   JSON representation saved in data folder");

            // display signature information
            var verificationResult = bioseal.getVerificationResult();
            System.out.println("   Signature:");
            System.out.println("      Signature verified status: " + verificationResult.vdsSignatureVerified);
            System.out.println("      Certification chain verified status: " + verificationResult.certificationChainVerified);
            System.out.println("      Certificate usage authorized status: " + verificationResult.signingCertificateUsageAuthorized);

            // display governance information
            System.out.println("   Governance:");
            System.out.println("      LoTL: " + bioseal.getLotlUrl());
            System.out.println("      TSL: " + bioseal.getTslUrl());
            System.out.println("      Manifest: " + bioseal.getManifestUrl());
            System.out.println("      LoTL valid status: " + verificationResult.lotlGovernanceValid);
            System.out.println("      TSL valid status: " + verificationResult.tslGovernanceValid);
            System.out.println("      Manifest valid status: " + verificationResult.manifestGovernanceValid);
            System.out.println("      Authority verified status: " + verificationResult.caCertificateVerified);

            // display certificate information
            var certificateInformation = bioseal.getCertificateInformation();
            System.out.println("   Certificate:");
            System.out.println("      Authority AC: " + bioseal.getCertificateAuthorityReference());
            System.out.println("      Authority ID: " + bioseal.getCertificateIdentifier());
            System.out.println("      Issuer: " + certificateInformation.getIssuerCommonName());
            System.out.println("      Subject: " + certificateInformation.getSubjectCommonName());
            System.out.println("      Organization: " + certificateInformation.getSubjectOrganization());
            System.out.println("      Organization unit: " + certificateInformation.getSubjectOrganizationalUnit());
            System.out.println("      Date of creation: " + certificateInformation.getNotBefore().toString());
            System.out.println("      Date of expiration: " + certificateInformation.getNotAfter().toString());

        }
        catch (Exception ex) {
            System.out.println(ex.getMessage());
        }
    }

    public static void main(String[] args) {
        System.out.println("---------------------------------------");
        System.out.println("id3.Samples.Bioseal.BiosealSdkSampleCLI");
        System.out.println("---------------------------------------");

        System.out.println(BiosealLibrary.getVersion());

        try {
            try (Bioseal bioseal = new Bioseal()) {

                ResourceCallbackHandler callback1 = (bioseal1, args1) -> {
                    try {
                        if (!localCache) {
                            args1.download();
                        }
                        else {
                            File dir = new File(localCacheDir);
                            if(!dir.exists())  {
                                dir.mkdir();
                            }
                            File cacheFile = new File(localCacheDir + File.separator + args1.getResourceName());
                            if (!cacheFile.exists() || args1.getRequiresUpdate()) {
                                args1.download();
                                Files.write(Paths.get(localCacheDir + File.separator + args1.getResourceName()), args1.getOutputData());
                            }
                            else {
                                args1.setOutputData(Files.readAllBytes(Paths.get(localCacheDir + File.separator + args1.getResourceName())));
                            }
                        }
                    }
                    catch (Exception ex) {
                        return BiosealError.EXCEPTION_IN_CALLBACK.getValue();
                    }
                    return 0;
                };
                bioseal.setExternalResourceCallback(callback1);
                displayBioSealInfo(bioseal, "../data/ExBioSealBiographics.dat");
                displayBioSealInfo(bioseal, "../data/ExBioSealFace.dat");
                displayBioSealInfo(bioseal, "../data/ExBioSealAccreditation.dat");
            }
            catch (BiosealException ex) {
                System.out.println(ex.getMessage());
            }
        }
        catch (Exception ex) {
            System.out.println(ex.getMessage());
        }

        System.out.println("Sample terminated successfully.");
    }
}
