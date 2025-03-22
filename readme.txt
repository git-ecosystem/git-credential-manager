
Wacom Ink SDK for Verification - Windows

Version 1.1

----------------------------------------------------------------------------------------------------------------
About

Wacom Ink SDK for verification is a software-based method of digitally verifying handwritten eSignatures 
in real time, helping to prevent fraud of signature-centric workflows. 


----------------------------------------------------------------------------------------------------------------
Instructions

To install, run the x64 or x86 installer.
The PATHs in the build.bat and run.bat will require adjustment. The JARs and DLLs are installed to C:\Program Files (x86)\Common Files\WacomGSS. 
javac.exe and java.exe must be already be on your PATH within system environment varialbes.

For further details on using the SDK, see the Java documentation installed by the .msi in %ProgramFiles%\Wacom\Signature Verification\Documentation\Java\index.html

Additionally, see https://developer-docs.wacom.com
Navigate to: Wacom Ink SDK for verification
References are included to the SDK sample code on GitHub: https://github.com/Wacom-Developer/gsv-sdk-windows

-------------------------------------------------------------------------------
File Contents
dependabot/nuget/wisdk-verification-client-server-sample/GsvServer/Swashbuckle.AspNetCore.SwaggerUI-6.4.0
├───readme.txt  - This readme file.
├───Wacom-Ink-SDK-Verification-x64-1.1.0.msi    - MSI for 64-bit Windows.
└───Wacom-Ink-SDK-Verification-x86-1.1.0.msi    - MSI for 32-bit Windows.


-------------------------------------------------------------------------------
Version History

1.1 15 January 2024
  - Added normalization support

1.0.10 3 February 2022
  - Added Java wrapper for existing SDK
  - Fixed issues with Java Doxygen

1.0.9   8 December 2021
  - Improved the classifier to give better error rates

1.0.8   19 October 2021
  - Improved the classifier to give better results for short signatures

1.0.7   08 July 2021
  - Production release
  - Improved the processing of input data to give greater flexibility of signature types. 
    Now handles FSS (binary and base64-encoded text), ISO 19794, static images and FSS data stored steganographically within static images
  
1.0.6   15 June 2021
  -	revised licensing to use a JWT text string - now non machine dependent
  
1.0.5   16 March 2021
  -	improvements to the classifier to give better accuracy

1.0.4   08 March 2021
  -	option added for classifier to optimize for Kanji signatures
  - improved enrollment procedure

1.0.3   17 February 2021
  - Enhancements

1.0.0   30 April 2020
  - Initial beta release.


Copyright © 2024 Wacom, Co., Ltd. All Rights Reserved.
