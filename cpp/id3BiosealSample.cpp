#include <id3BiosealLib.h>
#include <cstdio>
#include <cstdlib>
#include <string>
#include <vector>
#include <fstream>
#include <experimental/filesystem>
#include <map>
#include <cstdarg>

namespace filesystem = std::experimental::filesystem;

const char *BIOSEAL_FORMAT[] ={
    "Undefined",
    "VdsAfnorXpZ42_101",
    "VdsAfnorXpZ42_105"
};

#define BOOLEAN_STRING(_b_) (_b_) ? "True":"False"

void check(int err, const char *functionName) {
    if (err != 0) {
        printf("Bioseal error in %s: %d\n", functionName, err);
        exit(err);
    }
}

std::string format(const char *format, ...)
{
    va_list	list;
    std::string str(256, '\0');
    va_start(list, format);
    int result = vsnprintf((char *)str.data(), str.capacity(), format, list);
    if (result < (int)str.capacity())
        str.resize(result);
    va_end(list);
    return str;
}

void aformat(std::string &dst, const char *format, ...)
{
    va_list	list;
    std::string str(256, '\0');
    va_start(list, format);
    int result = vsnprintf((char *)str.data(), str.capacity(), format, list);
    if (result < (int)str.capacity())
        str.resize(result);
    va_end(list);
    dst.append(str);
}

bool readBinaryFile(const std::string &path, std::vector<uint8_t> &data) {
    try {
        std::ifstream infile(path, std::ifstream::in|std::ios::binary);
        if (infile.is_open()) {
            data.assign(std::istreambuf_iterator<char>(infile), std::istreambuf_iterator<char>());
            return true;
        }
    }
    catch (std::ifstream::failure &e) {
        printf("Exception fire when reading file '%s' : %s\n", path.c_str(), e.what());
    }
    return false;
}

bool writeBinaryFile(const std::string &path, std::vector<uint8_t> &data) {
    try {
        std::ofstream outfile(path, std::ifstream::out|std::ios::binary|std::ios::trunc);
        if (outfile.is_open()) {
            outfile.write((char*)data.data(), data.size());
            return true;
        }
    }
    catch (std::ifstream::failure &e) {
        printf("Exception fire when reading file '%s' : %s\n", path.c_str(), e.what());
    }
    return false;
}

bool writeBinaryFile(const std::string &path, std::string &data) {
    try {
        std::ofstream outfile(path, std::ifstream::out|std::ios::binary|std::ios::trunc);
        if (outfile.is_open()) {
            outfile.write((char*)data.data(), data.size());
            return true;
        }
    }
    catch (std::ifstream::failure &e) {
        printf("Exception fire when reading file '%s' : %s\n", path.c_str(), e.what());
    }
    return false;
}

// Generic function for id3 string API
// For short string
template <class T, typename F> std::string getString(T handle, F fct) {
    int str_size = -1;
    fct(handle, nullptr, &str_size);
    std::string str(str_size, '\0');
    fct(handle, (char *)str.data(), &str_size);
    str.resize(str_size);
    return str;
}

// Generic function for id3 string API
// For long string
template <class T, typename F> void getString(T handle, F fct, std::string &str) {
    int str_size = -1;
    fct(handle, nullptr, &str_size);
    str.resize(str_size);
    fct(handle, (char *)str.data(), &str_size);
    str.resize(str_size);
}

// Generic function for id3 binary API
template <class T, typename F> void getBinary(T handle, F fct, std::vector<uint8_t> &data) {
    int data_size = -1;
    fct(handle, nullptr, &data_size);
    data.resize(data_size);
    fct(handle, data.data(), &data_size);
}

std::string getString(ID3_BIOSEAL_STRING_ARRAY keys, int index) {
    int str_size = -1;
    id3BiosealStringArray_Get(keys, index, nullptr, &str_size);
    std::string str(str_size, '\0');
    id3BiosealStringArray_Get(keys, index, (char *)str.data(), &str_size);
    str.resize(str_size);
    return str;
}

template <class T, typename F> std::vector<std::string> getStringList(T handle, F fct) {
    ID3_BIOSEAL_STRING_ARRAY hKeys{};
    id3BiosealStringArray_Initialize(&hKeys);
    fct(handle, hKeys);
    int keys_count = 0;
    id3BiosealStringArray_GetCount(hKeys, &keys_count);
    std::vector<std::string> keys;
    for (int k=0; k<keys_count; k++) {
        keys.push_back(getString(hKeys, k));
    }
    id3BiosealStringArray_Dispose(&hKeys);
    return keys;
}

int getExternalResourceWithCache(void *context, ID3_BIOSEAL_RESOURCE_CALLBACK_ARGS hArgs) {
    int err = id3BiosealResourceCallbackArgs_CheckHandle(hArgs);
    if (err == 0) {
        // load file from 'data' folder
        filesystem::path data_path = "C:/temp/cache_bioseal";
        if (!exists(data_path)) {
            create_directory(data_path);
        }
#ifdef DEBUG
        //std::string uri = getUri(hArgs);
        std::string uri = getString(hArgs, id3BiosealResourceCallbackArgs_GetUri);
        printf("URI = '%s'\n", uri.c_str());
#endif
        //std::string res_name = getResourceName(hArgs);
        bool requiresUpdate = false;
        id3BiosealResourceCallbackArgs_GetRequiresUpdate(hArgs, &requiresUpdate);
        std::string res_name = getString(hArgs, id3BiosealResourceCallbackArgs_GetResourceName);
        filesystem::path data_file = data_path / res_name;
        if (!requiresUpdate && exists(data_file)) {
            std::vector<uint8_t> data;
            if (readBinaryFile(data_file.string(), data)) {
                err = id3BiosealResourceCallbackArgs_SetOutputData(hArgs, data.data(), (int)data.size());
            }
            else {
                err = id3BiosealError_ExceptionInCallback;
            }
        }
        else {
            err = id3BiosealResourceCallbackArgs_Download(hArgs);
            if (err == 0) {
                std::vector<uint8_t> data;
                getBinary(hArgs, id3BiosealResourceCallbackArgs_GetOutputData, data);
                writeBinaryFile(data_file.string(), data);
            }
        }
    }
    return err;
}

std::string getValueAsBinaryString(ID3_BIOSEAL_FIELD field) {
    int data_size = -1;
    id3BiosealField_GetValueAsBinary(field, nullptr, &data_size);
    std::vector<uint8_t> data(data_size);
    id3BiosealField_GetValueAsBinary(field, data.data(), &data_size);
    std::string str(data_size*2, '\0');
    char *p = (char *)str.data();
    for (auto b : data) {
        sprintf(p, "%02X", b);
        p += 2;
    }
    return str;
}

struct DateTime {
    int year;
    int month;
    int day;
    int hour;
    int minute;
    int second;
};

// Generic function for id3 DateTime API
// YYYY-MM-DD
// YYYY-MM-DDTHH:MM:SS
// HH:MM:SS
template <class T, typename F> std::string getDateTime(T handle, F fct) {
    std::string value_str;
    ID3_BIOSEAL_DATE_TIME hDateTime{};
    id3BiosealDateTime_Initialize(&hDateTime);
    int sdk_err = fct(handle, hDateTime);
    DateTime dt{};
    bool valid = false;
    id3BiosealDateTime_IsDateTimeValid(hDateTime, &valid);
    if (valid) {
        id3BiosealDateTime_GetYear(hDateTime, &dt.year);
        id3BiosealDateTime_GetMonth(hDateTime, &dt.month);
        id3BiosealDateTime_GetDay(hDateTime, &dt.day);
        id3BiosealDateTime_GetHour(hDateTime, &dt.hour);
        id3BiosealDateTime_GetMinute(hDateTime, &dt.minute);
        id3BiosealDateTime_GetSecond(hDateTime, &dt.second);
        value_str = format("%04d-%02d-%02dT%02d:%02d:%02d", dt.year, dt.month, dt.day, dt.hour, dt.minute, dt.second);
    }
    else {
        id3BiosealDateTime_IsDateValid(hDateTime, &valid);
        if (valid) {
            id3BiosealDateTime_GetYear(hDateTime, &dt.year);
            id3BiosealDateTime_GetMonth(hDateTime, &dt.month);
            id3BiosealDateTime_GetDay(hDateTime, &dt.day);
            value_str = format("%04d-%02d-%02d", dt.year, dt.month, dt.day);
        }
        id3BiosealDateTime_IsTimeValid(hDateTime, &valid);
        if (valid) {
            id3BiosealDateTime_GetHour(hDateTime, &dt.hour);
            id3BiosealDateTime_GetMinute(hDateTime, &dt.minute);
            id3BiosealDateTime_GetSecond(hDateTime, &dt.second);
            value_str = format("%02d:%02d:%02d", dt.hour, dt.minute, dt.second);
        }
    }
    id3BiosealDateTime_Dispose(&hDateTime);
    return value_str;
}

int getPayload(ID3_BIOSEAL hBioseal, std::map<std::string, std::string> &payload) {
    ID3_BIOSEAL_FIELD hPayload{};
    id3BiosealField_Initialize(&hPayload);
    int sdk_err = id3Bioseal_GetPayload(hBioseal, hPayload);
    if (sdk_err == 0) {
        auto keys = getStringList(hPayload, id3BiosealField_GetKeys);
        for (auto & key : keys) {
            ID3_BIOSEAL_FIELD hField{};
            id3BiosealField_Initialize(&hField);
            sdk_err = id3BiosealField_Get(hPayload, key.c_str(), hField);
            if (sdk_err == 0) {
                bool isNull{};
                id3BiosealField_GetIsNull(hField, &isNull);
                if (isNull) {
                    payload[key] = "null";
                }
                else {
                    id3BiosealFieldType fieldType{};
                    id3BiosealField_GetFieldType(hField, &fieldType);
                    switch (fieldType)
                    {
                    case id3BiosealFieldType_Integer: {
                        long long value{};
                        id3BiosealField_GetValueAsInteger(hField, &value);
                        payload[key] = format("%lld", value);
                        break;
                    }
                    case id3BiosealFieldType_Boolean: {
                        bool value{};
                        id3BiosealField_GetValueAsBoolean(hField, &value);
                        payload[key] = format("%d", value);
                        break;
                    }
                    case id3BiosealFieldType_Float: {
                        float value{};
                        id3BiosealField_GetValueAsFloat(hField, &value);
                        payload[key] = format("%.6g", value);
                        break;
                    }
                    case id3BiosealFieldType_String: {
                        payload[key] = getString(hField, id3BiosealField_GetValueAsString);
                        break;
                    }
                    case id3BiosealFieldType_Binary: {
                        payload[key] = getValueAsBinaryString(hField);
                        break;
                    }
                    case id3BiosealFieldType_Date: {
                        payload[key] = getDateTime(hField, id3BiosealField_GetValueAsDate);
                        break;
                    }
                    case id3BiosealFieldType_Timestamp: {
                        payload[key] = getDateTime(hField, id3BiosealField_GetValueAsDateTime);
                        break;
                    }
                    case id3BiosealFieldType_Time: {
                        payload[key] = getDateTime(hField, id3BiosealField_GetValueAsTime);
                        break;
                    }
                    case id3BiosealFieldType_ObjectArray: break;
                    default:
                        printf(" >> Field type %d is not supported here\n", fieldType);
                        break;
                    }
                }
            }
            id3BiosealField_Dispose(&hField);
        }
    }
    id3BiosealField_Dispose(&hPayload);
    return sdk_err;
}

bool extractBiometrics(ID3_BIOSEAL hBioseal, id3BiosealBiometricDataType eBiometricDataType, const char *path, const char *ext) {
    bool ret = false;
    ID3_BIOSEAL_FIELD_LIST hResultFieldList{};
    id3BiosealFieldList_Initialize(&hResultFieldList);
    id3Bioseal_FindBiometrics(hBioseal, eBiometricDataType, id3BiosealBiometricFormat_Undefined, hResultFieldList);
    int count{};
    id3BiosealFieldList_GetCount(hResultFieldList, &count);
    if (count >= 1) {

        ID3_BIOSEAL_FIELD hField{};
        id3BiosealField_Initialize(&hField);
        int sdk_err = id3BiosealFieldList_Get(hResultFieldList, 0, hField);
        if (sdk_err == 0) {
            std::vector<uint8_t> data;
            getBinary(hField, id3BiosealField_GetValueAsBinary, data);
            filesystem::path fs_path = path;
            fs_path.replace_extension(ext);
            writeBinaryFile(fs_path.string(), data);
            ret = true;
        }
        id3BiosealField_Dispose(&hField);
    }
    id3BiosealFieldList_Dispose(&hResultFieldList);
    return ret;
}

void buildPayloadAsJson(ID3_BIOSEAL hBioseal, const char *indentation, std::string &str) {
    int str_size = -1;
    id3Bioseal_BuildPayloadAsJson(hBioseal, indentation, nullptr, &str_size);
    str.resize(str_size);
    id3Bioseal_BuildPayloadAsJson(hBioseal, indentation, (char *)str.data(), &str_size);
    str.resize(str_size);
}

void displayBioSealInfo(ID3_BIOSEAL hBioseal, const char *path) {
    // read and decode BioSeal contents
    puts("");
    printf("Decoding BioSeal file '%s'\n", path);
    std::vector<uint8_t> dataBioSeal;
    id3BiosealVerificationResult verificationResult{};
    if (readBinaryFile(path, dataBioSeal)) {
        check(id3Bioseal_Decode(hBioseal, dataBioSeal.data(), (int)dataBioSeal.size()), "id3Bioseal_Decode");
        check(id3Bioseal_Verify(hBioseal, &verificationResult), "id3Bioseal_Verify");
    }
    else {
        printf("Error reading file '%s'\n", path);
        exit(1);
    }

    id3BiosealFormat biosealFormat{};
    id3Bioseal_GetFormat(hBioseal, &biosealFormat);
    printf("  BioSeal format : %s\n", BIOSEAL_FORMAT[biosealFormat]);
    // display manifest information
    int manifestId{};
    id3Bioseal_GetManifestId(hBioseal, &manifestId);
    printf("   Use case: %06X\n", manifestId);
    // enumerate available languages
    {
        auto list = getStringList(hBioseal, id3Bioseal_GetSupportedLanguages);
        std::string msg = "   Manifest supported languages : ";
        for (auto & str : list) {
            aformat(msg, "%s, ", str.c_str());
        }
        msg.resize(msg.length()-2); // remove 2 last char
        puts(msg.c_str());
    }
    // display payload information
    puts("   Payload:");
    std::map<std::string, std::string> payload;
    check(getPayload(hBioseal, payload), "getPayload");
    for (auto & key_value : payload) {
        printf("      %s: %s\n", key_value.first.c_str(), key_value.second.c_str());
    }

    // display face template if existing
    bool hasFaceTemplate{};
    id3Bioseal_GetContainsFaceTemplates(hBioseal, &hasFaceTemplate);
    printf("   Face template: %s\n", BOOLEAN_STRING(hasFaceTemplate));
    if (hasFaceTemplate) {
        if (extractBiometrics(hBioseal, id3BiosealBiometricDataType_FaceTemplate, path, ".template")) {
            printf("   Face template saved in data folder\n");
        }
    }

    // display and save face image if existing
    bool hasFaceImage{};
    id3Bioseal_GetContainsFaceImages(hBioseal, &hasFaceImage);
    printf("   Face image: %s\n", BOOLEAN_STRING(hasFaceImage));
    if (hasFaceImage)
    {
        if (extractBiometrics(hBioseal, id3BiosealBiometricDataType_FaceImage, path, ".webp")) {
            printf("   Face image saved in data folder\n");
        }
    }

    // fetch and save presentation view
    {
        auto list = getStringList(hBioseal, id3Bioseal_GetSupportedHtmlViewLanguages);
        std::string msg = "   Presentation view supported languages: ";
        for (auto & str : list) {
            aformat(msg, "%s, ", str.c_str());
        }
        msg.resize(msg.length()-2); // remove 2 last char
        puts(msg.c_str());
    }
    // Get default language
    {
        id3Bioseal_BuildHtmlView(hBioseal, "", true);
        filesystem::path fs_path = path;
        fs_path.replace_extension(".html");
        std::string htlmView;
        getString(hBioseal, id3Bioseal_GetHtmlView, htlmView);
        writeBinaryFile(fs_path.string(), htlmView);
        printf("   Presentation view saved in data folder\n");
    }

    // build and save JSON file
    {
        std::string jsonPayload;
        buildPayloadAsJson(hBioseal, "  ", jsonPayload);
        filesystem::path fs_path = path;
        fs_path.replace_extension(".json");
        writeBinaryFile(fs_path.string(), jsonPayload);
        printf("   JSON representation saved in data folder\n");
    }

    // display signature information
    printf("   Signature:\n");
    printf("      Signature verified status: %d\n", verificationResult.VdsSignatureVerified);
    printf("      Certification chain verified status: %d\n", verificationResult.CertificationChainVerified);
    printf("      Certificate usage authorized status: %d\n", verificationResult.SigningCertificateUsageAuthorized);

    // display governance information
    printf("   Governance:\n");
    printf("      LoTL: %s\n", getString(hBioseal, id3Bioseal_GetLotlUrl).c_str());
    printf("      TSL: %s\n", getString(hBioseal, id3Bioseal_GetTslUrl).c_str());
    printf("      Manifest: %s\n", getString(hBioseal, id3Bioseal_GetManifestUrl).c_str());
    printf("      LoTL valid status: %d\n", verificationResult.LotlGovernanceValid);
    printf("      TSL valid status: %d\n", verificationResult.TslGovernanceValid);
    printf("      Manifest valid status: %d\n", verificationResult.ManifestGovernanceValid);
    printf("      Authority verified status: %d\n", verificationResult.CaCertificateVerified);

    // display certificate information
    printf("   Certificate:\n");
    ID3_BIOSEAL_CERTIFICATE_INFORMATION hCertificateInformation{};
    id3BiosealCertificateInformation_Initialize(&hCertificateInformation);
    id3Bioseal_GetCertificateInformation(hBioseal, hCertificateInformation);
    printf("      Authority ID: %s\n", getString(hBioseal, id3Bioseal_GetCertificateAuthorityId).c_str());
    printf("      Authority issuing country: %s\n", getString(hBioseal, id3Bioseal_GetCertificateAuthorityIssuingCountry).c_str());
    printf("      Issuer: %s\n", getString(hCertificateInformation, id3BiosealCertificateInformation_GetIssuerCommonName).c_str());
    printf("      Subject: %s\n", getString(hCertificateInformation, id3BiosealCertificateInformation_GetSubjectCommonName).c_str());
    printf("      Organization: %s\n", getString(hCertificateInformation, id3BiosealCertificateInformation_GetSubjectOrganization).c_str());
    printf("      Organization unit: %s\n", getString(hCertificateInformation, id3BiosealCertificateInformation_GetSubjectOrganizationalUnit).c_str());
    printf("      Date of creation: %s\n", getDateTime(hCertificateInformation, id3BiosealCertificateInformation_GetNotBefore).c_str());
    printf("      Date of expiration: %s\n", getDateTime(hCertificateInformation, id3BiosealCertificateInformation_GetNotAfter).c_str());
    id3BiosealCertificateInformation_Dispose(&hCertificateInformation);
    puts("");
}

int main()
{
    puts("----------------------");
    puts("id3BioSeal c++ samples");
    puts("----------------------");

    // The bioseal instance must first be initialized
    ID3_BIOSEAL hBioseal{};
    id3Bioseal_Initialize(&hBioseal);

    // optionnal, use cache
    id3Bioseal_SetExternalResourceCallback(hBioseal, getExternalResourceWithCache, nullptr);
    //id3Bioseal_SetEnableDownloadCache(hBioseal, true);

    // This basic sample shows how to read BioSeal biographics only contents
    displayBioSealInfo(hBioseal, "../../data/ExBioSealBiographics.bin");

    // This sample shows how to read BioSeal face image and template contents
    displayBioSealInfo(hBioseal, "../../data/ExBioSealFace.bin");

    // This accreditation sample shows how to read BioSeal face template contents
    displayBioSealInfo(hBioseal, "../../data/ExBioSealAccreditation.bin");

    id3Bioseal_Dispose(&hBioseal);
}