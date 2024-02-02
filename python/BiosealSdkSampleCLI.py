import base64
import os
from id3bioseal import *

localCache = True
localCacheDir = "cache_bioseal"

def writeFile(path: str, data):
    with open(path, "wb") as f:
        f.write(data)

def writeFileStr(path: str, data : str):
    with open(path, "w") as f:
        f.write(data)

def resourceCallbackHandler(context: Bioseal, args: ResourceCallbackArgs) -> int:
    try:
        if not localCache:
            args.download()
        else:
            print(f"URI = {args.uri}")
            # Cache disque
        if not os.path.exists(localCacheDir):
            os.mkdir(localCacheDir)
        path = os.path.join(localCacheDir, args.resource_name)
        if not os.path.exists(path) or args.requires_update:
            args.download()
            writeFile(path, args.output_data)
        else:
            try:
                with open(path, "rb") as f:
                    data = f.read(-1)
            except IOError:
                return BiosealError.DOWNLOAD_ERROR
            args.output_data = data
        return 0
    except:
        return BiosealError.EXCEPTION_IN_CALLBACK

def displayBioSealInfo(path: str):
    print(f"Decoding BioSeal file {path}")
    try:
        with open(path) as f:
            data = f.read()
            bioseal.decode_from_string(data)
            bioseal.verify()

        print(f"  BioSeal format : {bioseal.format}")
        # display manifest information
        print(f"   Use case: {bioseal.manifest_id:6x}")
        supportedLanguages = bioseal.supported_languages
        for language in supportedLanguages:
            print(f"   Document name: '{bioseal.get_document_name(language)}'")

        print("   Payload:")
        payload = bioseal.payload
        for field_name in payload:
            info = "null"
            field = payload.get(field_name)
            if not field.is_null:
                if field.field_type == FieldType.INTEGER:
                    info = field.value_as_integer
                elif field.field_type == FieldType.STRING:
                    info = field.value_as_string
                elif field.field_type == FieldType.FLOAT:
                    info = field.value_as_float
                elif field.field_type == FieldType.BOOLEAN:
                    info = field.value_as_boolean
                elif field.field_type == FieldType.BINARY:
                    info = base64.b64encode(field.value_as_binary)
                elif field.field_type == FieldType.DATE:
                    info = field.value_as_date.to_string()
                elif field.field_type == FieldType.TIME:
                    info = field.value_as_date.to_string()
                elif field.field_type == FieldType.TIMESTAMP:
                    info = field.value_as_date.to_string()
            print(f"      {field.name}: {field.field_type} = '{info}'")

        # display face template if existing
        hasFaceTemplate = bioseal.contains_face_templates
        print(f"   Face template: {hasFaceTemplate}")
        if hasFaceTemplate:
            fieldFaceList = bioseal.find_biometrics(BiometricDataType.FACIAL_FEATURES, None)
            if fieldFaceList.count > 0:
                fieldFace = fieldFaceList.get(0)
                print("   Face template saved in data folder")
                writeFile(path.replace(".dat", ".template"), fieldFace.value_as_binary)

        # display and save face image if existing
        hasFaceImage = bioseal.contains_portraits
        print(f"   Face image: {hasFaceImage}")
        if hasFaceImage:
            fieldFaceList = bioseal.find_fields_by_extension(FieldExtensionType.PORTRAIT)
            if fieldFaceList.count > 0:
                fieldFace = fieldFaceList.get(0)
                print("   Face image saved in data folder")
                writeFile(path.replace(".dat", ".webp"), fieldFace.value_as_binary)

        # fetch and save presentation view
        print("   Presentation view supported languages:")
        for supportedLanguage in bioseal.supported_languages:
            print(f"      {supportedLanguage}")

        # Get default language
        bioseal.build_html_view(None, True)
        writeFileStr(path.replace(".dat", ".html"), bioseal.html_view)
        print("   Presentation view saved in data folder")

        # build and save JSON file
        jsonPayload = bioseal.build_payload_as_json("  ")
        writeFileStr(path.replace(".dat", ".json"), jsonPayload)
        print("   JSON representation saved in data folder")

        # display signature information
        print( "   Signature:")
        verificationResult = bioseal.verification_result
        print(f"      Signature verified status: {verificationResult.vds_signature_verified}")
        print(f"      Certification chain verified status: {verificationResult.certification_chain_verified}")
        print(f"      Certificate usage authorized status: {verificationResult.signing_certificate_usage_authorized}")

        # display governance information
        print( "   Governance:")
        print(f"      LoTL: {bioseal.lotl_url}")
        print(f"      TSL: {bioseal.tsl_url}")
        print(f"      Manifest: {bioseal.manifest_url}")
        print(f"      LoTL valid status: {verificationResult.lotl_governance_valid}")
        print(f"      TSL valid status: {verificationResult.tsl_governance_valid}")
        print(f"      Manifest valid status: {verificationResult.manifest_governance_valid}")
        print(f"      Authority verified status: {verificationResult.ca_certificate_verified}")

        # display certificate information
        print( "   Certificate:")
        certificateInformation = bioseal.certificate_information
        print(f"      Authority ID: {bioseal.certificate_authority_id}")
        print(f"      Authority issuing country: {bioseal.certificate_authority_issuing_country}")
        print(f"      Issuer: {certificateInformation.issuer_common_name}")
        print(f"      Subject: {certificateInformation.subject_common_name}")
        print(f"      Organization: {certificateInformation.subject_organization}")
        print(f"      Organization unit: {certificateInformation.subject_organizational_unit}")
        print(f"      Date of creation: {certificateInformation.not_before.to_string()}")
        print(f"      Date of expiration: {certificateInformation.not_after.to_string()}")
    except Exception as ex:
        print(ex)

bioseal = Bioseal()
bioseal.external_resource_callback = resourceCallbackHandler

# This basic sample shows how to read BioSeal biographics only contents
displayBioSealInfo("/data/ExBioSealBiographics.dat")

# This sample shows how to read BioSeal face image and template contents
displayBioSealInfo("data/ExBioSealFace.dat")

# This accreditation sample shows how to read BioSeal face template contents
displayBioSealInfo("data/ExBioSealAccreditation.dat")

os.system("pause")