# BioSeal SDK samples

## Introduction

### Content

This repository contains basic samples for the **id3 Technologies** BioSeal SDK.

Going through those samples before trying to integrate it in your applications is strongly recommended.

### A word on version format

The version of this repository is made of 4 digits:
* The first 3 correspond to the version of the BioSeal SDK that is currently supported in this repository.
* The forth digit contains updates dedicated to the samples (evolutions, bug fixes, doc, etc).

This strategy is employed to ensure version consistency among the various supported languages. When updating the BioSeal SDK version, all the samples are updated as well.

For this release of the samples the version numbers are: 
* Samples version: **1.32.0.0**
* Required id3 BioSeal SDK version: **1.32.0**

## Getting started

Once you have the SDK ZIP archive, you need to unzip it in the *sdk/* subfolder resulting in the following architecture.

    .
    ├── android
    ├── cpp
    ...
    ├── flutter
    ├── sdk
        ├── activation
        ├── bin
        ...
        └── README.md
    ├── data
    └── README.md

The `data` folder containts 3 BioSeal :
- ExBioSealBiographics.dat : BioSeal with biographics only contents.
- ExBioSealFace.dat : BioSeal with a face image and template contents.
- ExBioSealAccreditation.dat : BioSeal with face template contents.

## Play around with the samples

You are now ready to go straight to the directory of your favorite language/platform which will contain a readme file with additional information on how to run the samples.

Sample code is heavily commented in order to give you an overview of the id3 BioSeal SDK usage. We recommend you to read through the code while you run the samples.

### Play with cpp sample

The cpp sample can be build under Windows (x64) or Linux (x64).
Under Windows, execute the PowerShell script `build.ps1` ot `build.bat`.
Under Linux, execute the script `build.sh`.

### Play with dotnet sample

Open `dotnet\id3.BioSeal.Samples.sln` with Microsoft Visual Studio 2017 or higher.

### Play with dart sample

- Go to `\dart\id3_bioseal_sample` folder.`
- Execute theses commands:
  - `dart pub get`
  - `dart analyze --no-fatal-warnings`
  - `dart compile exe .\bin\id3_bioseal_sample.dart -o bin/runme.exe`

## Troubleshooting

If you get stuck at any step of this procedure, please contact us: support@id3.eu.