import Foundation
import UIKit
import id3Bioseal

class Sample {
    let bioseal: Bioseal = try! Bioseal()
    let localCache = true
    let dateFormat: DateFormatter = DateFormatter()
    let timeFormat: DateFormatter = DateFormatter()
    let downloadsDirectory: URL
    let localCacheDir: URL

    init() {
        self.dateFormat.dateFormat = "EEEE, MMMM d, yyyy"
        self.timeFormat.dateFormat = "h:mm a"
        downloadsDirectory = FileManager.default.urls(for: .downloadsDirectory, in: .userDomainMask).first!
        localCacheDir = downloadsDirectory.appendingPathComponent("cache_bioseal")
    }

    func run() {
        print("-------------------")
        print("id3.BioSeal.Samples")
        print("-------------------")

        // This basic sample shows how to read BioSeal biographics only contents
        try! displayBioSealInfo(name: "ExBioSealBiographics")

        // This sample shows how to read BioSeal face image and template contents
        try! displayBioSealInfo(name: "ExBioSealFace")

        // This accreditation sample shows how to read BioSeal face template contents
        try! displayBioSealInfo(name: "ExBioSealAccreditation")
    }

    private func displayBioSealInfo(name: String) throws {
        print("Decoding BioSeal file \(name).dat")
        let dataBioseal = [UInt8](NSDataAsset(name: name)!.data)

        do {
            try bioseal.verifyFromBuffer(data: dataBioseal)
            try bioseal.verify()
        } catch let ex as BiosealException {
            print(ex.getMessage())
        }

        print("  BioSeal format : \(try bioseal.getFormat())")
        // display manifest information
        print("   Use case: \(String(format: "%X", try bioseal.getManifestId()))")
        let supportedLanguages = try bioseal.getSupportedLanguages()
        let supportedLanguagesCount = try supportedLanguages.getCount()
        for i in 0..<supportedLanguagesCount {
            print("   Document name: '\(try bioseal.getDocumentName(language: try supportedLanguages.get(index: i)))'")
        }

        print("   Payload:")
        let payload = try getPayload()
        for (key, value) in payload {
            print("      \(key): \(value)")
        }

        // display face template if existing
        let hasFaceTemplate = try bioseal.getContainsFaceTemplates()
        print("   Face template: \(hasFaceTemplate)")
        if hasFaceTemplate {
            let fieldFaceList = try bioseal.findBiometrics(biometricDataType: BiometricDataType.facialFeatures, biometricFormat: "")
            if(try fieldFaceList.getCount() > 0) {
                let fieldFace = try fieldFaceList.get(index: 0)
                print("   Face template saved in data folder")
                let templateFilePath = localCacheDir.appendingPathComponent(name + ".template").path
                FileManager.default.createFile(atPath: templateFilePath, contents: Data(try fieldFace.getValueAsBinary()), attributes: nil)
            }
        }

        // display and save face image if existing
        let hasFaceImage = try bioseal.getContainsPortraits()
        print("   Face image: \(hasFaceImage)")
        if hasFaceImage {
            let fieldFaceList = try bioseal.findBiometrics(biometricDataType: BiometricDataType.facialFeatures, biometricFormat: "")
            if (try fieldFaceList.getCount() > 0) {
                let fieldFace = try fieldFaceList.get(index: 0)
                print("   Face image saved in data folder")
                let imageFilePath = localCacheDir.appendingPathComponent(name + ".webp").path
                FileManager.default.createFile(atPath: imageFilePath, contents: Data(try fieldFace.getValueAsBinary()), attributes: nil)
            }
        }

        // fetch and save presentation view
        print("   Presentation view supported languages:")
        let supportedHtmlViewLanguages = try bioseal.getSupportedHtmlViewLanguages()
        let supportedHtmlViewLanguagesCount = try supportedHtmlViewLanguages.getCount()
        for i in 0..<supportedHtmlViewLanguagesCount {
            print("      \(try supportedHtmlViewLanguages.get(index: i))")
        }

        // Get default language
        try bioseal.buildHtmlView(language: "en", userAuthenticated: true)
        let htmlFilePath = localCacheDir.appendingPathComponent(name + ".html").path
        try? bioseal.getHtmlView().write(toFile: htmlFilePath, atomically: true, encoding: .utf8)

        print("   Presentation view saved in data folder")

        // build and save JSON file
        let jsonPayload = try bioseal.buildVdsAsJson(indentation: "  ")
        let jsonFilePath = localCacheDir.appendingPathComponent(name +  ".json").path
        try? jsonPayload.write(toFile: jsonFilePath, atomically: true, encoding: .utf8)
        print("   JSON representation saved in data folder")

        // display signature information
        print("   Signature:")
        print("      Signature verified status: \(try bioseal.getVerificationResult().VdsSignatureVerified)")
        print("      Certification chain verified status: \(try bioseal.getVerificationResult().CertificationChainVerified)")
        print("      Certificate usage authorized status: \(try bioseal.getVerificationResult().SigningCertificateUsageAuthorized)")

        // display governance information
        print("   Governance:")
        print("      LoTL: \(try bioseal.getLotlUrl())")
        print("      TSL: \(try bioseal.getTslUrl())")
        print("      Manifest: \(try bioseal.getManifestUrl())")
        print("      LoTL valid status: \(try bioseal.getVerificationResult().LotlGovernanceValid)")
        print("      TSL valid status: \(try bioseal.getVerificationResult().TslGovernanceValid)")
        print("      Manifest valid status: \(try bioseal.getVerificationResult().ManifestGovernanceValid)")
        print("      Authority verified status: \(try bioseal.getVerificationResult().CaCertificateVerified)")

        // display certificate information
        print("   Certificate:")
        print("      Authority reference: \(try bioseal.getCertificateAuthorityReference())")
        print("      Issuer: \(try bioseal.getCertificateInformation().getIssuerCommonName())")
        print("      Subject: \(try bioseal.getCertificateInformation().getSubjectCommonName())")
        print("      Organization: \(try bioseal.getCertificateInformation().getSubjectOrganization())")
        print("      Organization unit: \(try bioseal.getCertificateInformation().getSubjectOrganizationalUnit())")
        print("      Date of creation: \(try bioseal.getCertificateInformation().getNotBefore())")
        print("      Date of expiration: \(try bioseal.getCertificateInformation().getNotAfter())")
    }

    /// Gets the dictionary of biographics data from the BioSeal instance.
    private func getPayload() throws -> [String: Any] {
        var payload: [String: Any] = [:]
        // Scan payload
        var data: Any = "?"
        let payloadKeys = try bioseal.getPayload().getKeys()
        let payloadKeysCount = try payloadKeys.getCount()
        for i in 0..<payloadKeysCount {
            let field = try bioseal.getPayload().get(key: try payloadKeys.get(index: i))
            switch try field.getFieldType() {
            case .integer:
                data = try field.getValueAsInteger()
            case .boolean:
                data = try field.getValueAsBoolean()
            case .float:
                data = try field.getValueAsFloat()
            case .string:
                data = try field.getValueAsString()
            case .binary:
                data = String(bytes: try field.getValueAsBinary(), encoding: .utf8)?.replacingOccurrences(of: "-", with: "")
            case .date:
                let date = try field.getValueAsDate()
                let dateTime = Calendar.current.date(from: DateComponents(year: Int(try date.getYear()), month: Int(try date.getMonth()), day: Int(try date.getDay())))
                data = dateFormat.string(from: dateTime!)
            case .time:
                let time = try field.getValueAsTime()
                let now = Calendar.current.dateComponents(in: .current, from: Date())
                let dateTime = Calendar.current.date(from: DateComponents(year: now.year, month: now.month, day: now.day, hour: Int(try time.getHour()), minute: Int(try time.getMinute()), second: Int(try time.getSecond())))
                data = timeFormat.string(from: dateTime!)
            case .timestamp:
                let timestamp = try field.getValueAsDateTime()
                let dateTime = Calendar.current.date(from: DateComponents(year: Int(try timestamp.getYear()), month: Int(try timestamp.getMonth()), day: Int(try timestamp.getDay()), hour: Int(try timestamp.getHour()), minute: Int(try timestamp.getMinute()), second: Int(try timestamp.getSecond())))
                data = dateTime?.description ?? "?"
            default:
                data = "?"
            }
            payload[try field.getName()] = data
        }
        return payload
    }
}
