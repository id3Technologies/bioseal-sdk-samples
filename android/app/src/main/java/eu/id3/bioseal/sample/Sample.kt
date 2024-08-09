package eu.id3.bioseal.sample

import android.content.Context
import eu.id3.bioseal.BiometricDataType
import eu.id3.bioseal.Bioseal
import eu.id3.bioseal.BiosealError
import eu.id3.bioseal.BiosealException
import eu.id3.bioseal.FieldExtensionType
import eu.id3.bioseal.FieldType
import eu.id3.bioseal.ResourceCallbackArgs
import java.io.File
import java.text.DateFormat
import java.text.SimpleDateFormat
import java.util.Calendar
import java.util.Date

class Sample(val context: Context) {
    val bioseal: Bioseal by lazy { Bioseal() }
    val localCache = true
    val dateFormat: DateFormat = SimpleDateFormat("EEEE, MMMM d, yyyy")
    val timeFormat: DateFormat = SimpleDateFormat("h:mm a")
    val localCacheDir = "/storage/emulated/0/Download/cache_bioseal"

    fun run() {
        println("-------------------")
        println("id3.BioSeal.Samples")
        println("-------------------")

        // The bioseal instance must first be initialized
        bioseal.setExternalResourceCallback(::getExternalResourceWithCache)

        // This basic sample shows how to read BioSeal biographics only contents
        displayBioSealInfo("ExBioSealBiographics.dat")

        // This sample shows how to read BioSeal face image and template contents
        displayBioSealInfo("ExBioSealFace.dat")

        // This accreditation sample shows how to read BioSeal face template contents
        displayBioSealInfo("ExBioSealAccreditation.dat")
    }

    private fun displayBioSealInfo(path: String) {
        println("Decoding BioSeal file $path")
        val dataBioseal = context.assets.open(path).readBytes()
        try {
            bioseal.verifyFromBuffer(dataBioseal)
        } catch (ex: BiosealException) {
            println(ex.message)
            throw ex
        }

        println("  BioSeal format : ${bioseal.format}")
        // display manifest information
        println("   Use case: ${bioseal.manifestId.toString(16)}")
        val supportedLanguages = bioseal.supportedLanguages
        for (i in 0 until supportedLanguages.count) {
            println("   Document name: '${bioseal.getDocumentName(supportedLanguages[i])}'")
        }

        println( "   Payload:")
        val payload = getPayload()
        for ((key, value) in payload.entries) {
            println("      $key: $value")
        }

        // display face template if existing
        val hasFaceTemplate = bioseal.containsFaceTemplates
        println("   Face template: $hasFaceTemplate")
        if (hasFaceTemplate) {
            val fieldFaceList =
                bioseal.findBiometrics(BiometricDataType.FACIAL_FEATURES, null)
            if (fieldFaceList.count > 0) {
                val fieldFace = fieldFaceList.get(0)
                println("   Face template saved in data folder")
                File(
                    localCacheDir,
                    path.replace(".dat", ".template")
                ).writeBytes(fieldFace.valueAsBinary)
            }
        }

        // display and save face image if existing
        val hasFaceImage = bioseal.containsPortraits
        println("   Face image: $hasFaceImage")
        if (hasFaceImage) {
            val fieldFaceList =
                bioseal.findFieldsByExtension(FieldExtensionType.PORTRAIT)
            if (fieldFaceList.count > 0) {
                val fieldFace = fieldFaceList.get(0)
                println("   Face image saved in data folder")
                File(
                    localCacheDir,
                    path.replace(".dat", ".webp")
                ).writeBytes(fieldFace.valueAsBinary)
            }
        }

        // fetch and save presentation view
        println("   Presentation view supported languages:")
        for (i in 0 until bioseal.supportedLanguages.count) {
            println("      ${bioseal.supportedLanguages[i]}")
        }

        // Get default language
        bioseal.buildHtmlView(null, true)
        File(localCacheDir, path.replace(".dat", ".html")).writeText(bioseal.htmlView)
        println("   Presentation view saved in data folder")

        // build and save JSON file
        val jsonPayload = bioseal.buildVdsAsJson("  ")
        File(localCacheDir, path.replace(".dat", ".json")).writeText(jsonPayload)
        println("   JSON representation saved in data folder")

        // display signature information
        println("   Signature:")
        println("      Signature verified status: ${bioseal.verificationResult.vdsSignatureVerified}")
        println("      Certification chain verified status: ${bioseal.verificationResult.certificationChainVerified}")
        println("      Certificate usage authorized status: ${bioseal.verificationResult.signingCertificateUsageAuthorized}")

        // display governance information
        println("   Governance:")
        println("      LoTL: ${bioseal.lotlUrl}")
        println("      TSL: ${bioseal.tslUrl}")
        println("      Manifest: ${bioseal.manifestUrl}")
        println("      LoTL valid status: ${bioseal.verificationResult.lotlGovernanceValid}")
        println("      TSL valid status: ${bioseal.verificationResult.tslGovernanceValid}")
        println("      Manifest valid status: ${bioseal.verificationResult.manifestGovernanceValid}")
        println("      Authority verified status: ${bioseal.verificationResult.caCertificateVerified}")

        // display certificate information
        println("   Certificate:")
        println("      Authority AC: ${bioseal.certificateAuthorityReference}")
        println("      Authority ID: ${bioseal.certificateIdentifier}")
        println("      Issuer: ${bioseal.certificateInformation.issuerCommonName}")
        println("      Subject: ${bioseal.certificateInformation.subjectCommonName}")
        println("      Organization: ${bioseal.certificateInformation.subjectOrganization}")
        println("      Organization unit: ${bioseal.certificateInformation.subjectOrganizationalUnit}")
        println("      Date of creation: ${bioseal.certificateInformation.notBefore.toString()}")
        println("      Date of expiration: ${bioseal.certificateInformation.notAfter.toString()}")
    }

    /// Gets the dictionary of biographics data from the BioSeal instance.
    private fun getPayload(): Map<String, Any> {
        val payload = mutableMapOf<String, Any>()
        // Scan payload
        var data = "?"
        for (i in 0 until bioseal.payload.keys.count) {
            val field = bioseal.payload.get(bioseal.payload.keys[i])
            data = when (field.fieldType) {
                FieldType.INTEGER -> field.valueAsInteger.toString()
                FieldType.BOOLEAN -> field.valueAsBoolean.toString()
                FieldType.FLOAT -> field.valueAsFloat.toString()
                FieldType.STRING -> field.valueAsString
                FieldType.BINARY -> String(field.valueAsBinary).replace("-", "")
                FieldType.DATE -> {
                    val date = field.valueAsDate
                    val dateTime = Date.UTC(date.year, date.month, date.day, 0, 0, 0)
                    dateFormat.format(dateTime)
                }

                FieldType.TIME -> {
                    val time = field.valueAsTime
                    val now = Calendar.getInstance().time
                    val dateTime =
                        Date.UTC(now.year, now.month, now.day, time.hour, time.minute, time.second)
                    timeFormat.format(dateTime)
                }

                FieldType.TIMESTAMP -> {
                    val timestamp = field.valueAsDateTime
                    val dateTime = Date.UTC(
                        timestamp.year,
                        timestamp.month,
                        timestamp.day,
                        timestamp.hour,
                        timestamp.minute,
                        timestamp.second
                    )
                    dateTime.toString()
                }

                else -> "?"
            }
            payload[field.name] = data
        }
        return payload
    }

    /// Implements the GetExternalResource with cache callback.
    private fun getExternalResourceWithCache(obj: Any, args: ResourceCallbackArgs): Int {
        var err = 0
        try {
            if (!localCache) {
                args.download()
            } else {
                println("URI = ${args.uri}")
                // Cache disque
                if (!File(localCacheDir).exists()) {
                    File(localCacheDir).mkdir()
                }

                val cacheFile = File(localCacheDir, args.resourceName)
                if (!cacheFile.exists() || args.requiresUpdate) {
                    try {
                        args.download()
                        cacheFile.createNewFile()
                        cacheFile.writeBytes(args.outputData)
                    } catch (ex: BiosealException) {
                        err = BiosealError.RESOURCE_NOT_FOUND.value
                    }
                } else {
                    args.outputData = cacheFile.readBytes()
                }
            }
        } catch (ex: BiosealException) {
            println(ex.message)
            err = BiosealError.EXCEPTION_IN_CALLBACK.value
        }
        return err
    }

}