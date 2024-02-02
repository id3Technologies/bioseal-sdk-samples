rem Build the sample with the jar from the sdk
javac -cp ".;../sdk/java/eu.id3.bioseal.jar" BiosealSdkSampleCLI.java

rem Copy native library to current directory
copy ..\\sdk\\bin\\windows\\x64\\id3Bioseal.dll id3Bioseal.dll

rem Run the sample
java -cp ".;../sdk/java/eu.id3.bioseal.jar" BiosealSdkSampleCLI