import 'dart:io';

import 'package:id3_bioseal/id3_bioseal.dart' hide DateTime;
import 'package:intl/intl.dart';
import 'package:path/path.dart';

late Bioseal bioseal;
final localCache = true;

final dateFormat = DateFormat("dddd, MMMM d, yyyy");
final timeFormat = DateFormat("h:mm tt");
final localCacheDir = "cache_bioseal";

void main(List<String> arguments) {
  print('-------------------');
  print('id3.BioSeal.Samples');
  print('-------------------');

  // The bioseal instance must first be initialized
  bioseal = Bioseal();

  // optionnal, use cache
  bioseal.setExternalResourceCallback(getExternalResourceWithCache);

  // This basic sample shows how to read BioSeal biographics only contents
  displayBioSealInfo("../../data/ExBioSealBiographics.dat");

  // This sample shows how to read BioSeal face image and template contents
  displayBioSealInfo("../../data/ExBioSealFace.dat");

  // This accreditation sample shows how to read BioSeal face template contents
  displayBioSealInfo("../../data/ExBioSealAccreditation.dat");
}

void displayBioSealInfo(String path) {
  print("Decoding BioSeal file $path");
  final dataBioseal = File(path).readAsBytesSync();
  try {
    bioseal.verifyFromBuffer(dataBioseal);
  } on BiosealException catch (ex) {
    print(ex.message);
    rethrow;
  }

  print("  BioSeal format : ${bioseal.format}");
  // display manifest information
  print("   Use case: ${bioseal.manifestId.toRadixString(16)}");
  final supportedLanguages = bioseal.supportedLanguages;
  for (final language in supportedLanguages) {
    print("   Document name: '${bioseal.getDocumentName(language)}'");
  }

  print("   Payload:");
  final payload = getPayload();
  for (final keyValue in payload.entries) {
    print("      ${keyValue.key}: ${keyValue.value}");
  }

  // display face template if existing
  bool hasFaceTemplate = bioseal.containsFaceTemplates;
  print("   Face template: $hasFaceTemplate");
  if (hasFaceTemplate) {
    final fieldFaceList = bioseal.findBiometrics(BiometricDataType.facialFeatures, null);

    if (fieldFaceList.count > 0) {
      final fieldFace = fieldFaceList.get(0);
      print("   Face template saved in data folder");
      File(path.replaceAll(".dat", ".template")).writeAsBytesSync(fieldFace.valueAsBinary);
    }
  }

  // display and save face image if existing
  bool hasFaceImage = bioseal.containsPortraits;
  print("   Face image: $hasFaceImage");
  if (hasFaceImage) {
    final fieldFaceList = bioseal.findFieldsByExtension(FieldExtensionType.portrait);

    if (fieldFaceList.count > 0) {
      final fieldFace = fieldFaceList.get(0);
      print("   Face image saved in data folder");
      File(path.replaceAll(".dat", ".webp")).writeAsBytesSync(fieldFace.valueAsBinary);
    }
  }

  // fetch and save presentation view
  print("   Presentation view supported languages:");
  for (final supportedLanguage in bioseal.supportedLanguages) {
    print("      $supportedLanguage");
  }

  // Get default language
  bioseal.buildHtmlView(null, true);
  File(path.replaceAll(".dat", ".html")).writeAsStringSync(bioseal.htmlView);
  print("   Presentation view saved in data folder");

  // build and save JSON file
  final jsonPayload = bioseal.buildVdsAsJson("  ");
  File(path.replaceAll(".dat", ".json")).writeAsStringSync(jsonPayload);
  print("   JSON representation saved in data folder");

  // display signature information
  print("   Signature:");
  print("      Signature verified status: ${bioseal.verificationResult.vdsSignatureVerified}");
  print("      Certification chain verified status: ${bioseal.verificationResult.certificationChainVerified}");
  print("      Certificate usage authorized status: ${bioseal.verificationResult.signingCertificateUsageAuthorized}");

  // display governance information
  print("   Governance:");
  print("      LoTL: ${bioseal.lotlUrl}");
  print("      TSL: ${bioseal.tslUrl}");
  print("      Manifest: ${bioseal.manifestUrl}");
  print("      LoTL valid status: ${bioseal.verificationResult.lotlGovernanceValid}");
  print("      TSL valid status: ${bioseal.verificationResult.tslGovernanceValid}");
  print("      Manifest valid status: ${bioseal.verificationResult.manifestGovernanceValid}");
  print("      Authority verified status: ${bioseal.verificationResult.caCertificateVerified}");

  // display certificate information
  print("   Certificate:");
  print("      Authority AC: ${bioseal.certificateAuthorityReference}");
  print("      Authority ID: ${bioseal.certificateIdentifier}");
  print("      Issuer: ${bioseal.certificateInformation.issuerCommonName}");
  print("      Subject: ${bioseal.certificateInformation.subjectCommonName}");
  print("      Organization: ${bioseal.certificateInformation.subjectOrganization}");
  print("      Organization unit: ${bioseal.certificateInformation.subjectOrganizationalUnit}");
  print("      Date of creation: ${bioseal.certificateInformation.notBefore.toString()}");
  print("      Date of expiration: ${bioseal.certificateInformation.notAfter.toString()}");
}

/// Gets the dictionary of biographics data from the BioSeal instance.
Map<String, Object> getPayload() {
  final payload = <String, Object>{};
  // Scan payload
  String data = "?";
  for (final field in bioseal.payload) {
    if (field.isNull) {
      data = "null";
    } else {
      switch (field.fieldType) {
        case FieldType.integer:
          data = field.valueAsInteger.toString();
          break;
        case FieldType.boolean:
          data = field.valueAsBoolean.toString();
          break;
        case FieldType.float:
          data = field.valueAsFloat.toString();
          break;
        case FieldType.string:
          data = field.valueAsString;
          break;
        case FieldType.binary:
          data = String.fromCharCodes(field.valueAsBinary).replaceAll("-", "");
          break;
        case FieldType.date:
          final date = field.valueAsDate!;
          final dateTime = DateTime(date.year, date.month, date.day);
          data = dateFormat.format(dateTime);
          break;
        case FieldType.time:
          final time = field.valueAsTime!;
          final now = DateTime.now();
          final dateTime = DateTime(
            now.year,
            now.month,
            now.day,
            time.hour,
            time.minute,
            time.second,
          );
          data = timeFormat.format(dateTime);
          break;
        case FieldType.timestamp:
          final timestamp = field.valueAsDateTime!;
          final dateTime = DateTime(
            timestamp.year,
            timestamp.month,
            timestamp.day,
            timestamp.hour,
            timestamp.minute,
            timestamp.second,
          );
          data = dateTime.toString();
          break;
        default:
          break;
      }
    }
    payload.putIfAbsent(field.name, () => data);
  }

  return payload;
}

/// Implements the GetExternalResource with cache callback.
int getExternalResourceWithCache(Object obk, ResourceCallbackArgs args) {
  int err = 0;
  try {
    if (!localCache) {
      args.download();
    } else {
      print("URI = ${args.uri}");
      // Cache disque
      if (!Directory(localCacheDir).existsSync()) {
        Directory(localCacheDir).createSync();
      }

      final cacheFile = File(join(localCacheDir, args.resourceName));
      if (!cacheFile.existsSync() || args.requiresUpdate) {
        try {
          args.download();
          cacheFile
            ..createSync()
            ..writeAsBytesSync(args.outputData);
        } on BiosealException catch (_) {
          err = BiosealError.resourceNotFound as int;
        }
      } else {
        args.outputData = cacheFile.readAsBytesSync();
      }
    }
  } on BiosealException catch (ex) {
    print(ex.message);
    err = BiosealError.exceptionInCallback as int;
  }
  return err;
}
