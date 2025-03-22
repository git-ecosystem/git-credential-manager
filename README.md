# Wacom Ink SDK for Verification - Windows

---

## Introduction

The purpose of the WISDK for verification is to verify handwritten signatures.
It uses and extends the existing verification tools, including the SSV (Static Signature Verification) SDK for processing scanned images, 
and the DSV (Dynamic Signature Verification) SDK which handles signatures captured in Wacom's proprietary FSS (Forensic Signature Store) format.
The SDK supports all the functionality of the previous SDKs but handles both formats in a single component and allows individual signing variability to be measured by enrolling signature samples in a template.


## Overview

The main verification component is the **SignatureEngine** which is a COM component responsible for enrolling and verifying signatures. The SignatureEngine does not store any data but processes signatures and updates a **Template** for each person to record their signing characteristics. It is the responsibility of the calling application to supply the user's template with each signature being verified, and to store the updated version afterwards.

The main features of the SDK's SignatureEngine include the following:-

- A single template can handle either dynamic signatures supplied in FSS format or static signatures supplied as scanned graphical images, or both. In most implementations it is expected than one type or other will be used, but when both types are handled the SignatureEngine checks that the two sets of data are of the same signature. The SignatureEngine will also accept FSS data which is embedded steganographically in a graphical image.

- When a new person is to be verified the calling system must first create a new template which will record their signing behaviour. There are a number of configuration options that must be set at this stage and which cannot be subsequently changed. These control the way in which the enrollment process is handled and subsequently updated.

- Once the template has been created signatures can be verified. This is done using the **VerifySignature** method. The method must be supplied with the user's template and the signature.

- After the signature has been processed the following is returned to the calling application:-
  - A score of between 0 (inconsistent signature) and 1 (consistent signature). Note that for the first signature no comparison is possible and the score is meaningless
  - A flag indicating the type of verification that was used i.e. dynamic or static
  - A flag indicating the nature of the comparison that was used to arrive at the score e.g. whether the signatures differed in their geometry, timing, pressure variability etc.
  - A summary of the status of the template, e.g enrolling, enrolled, updated etc.
  - The updated template which should be saved for the next verification for the user
  
- A user's template becomes fully enrolled when the required number of consistent signatures have been verified. By default the number needed is 6 but different values can be set when the template is created. If when the required number has been received one of the signatures is significantly inconsistent with the others then it is rejected and the enrollment process will continue. Some inconsistent signers may need to verify more than the nominal enrollment number of signatures to become fully enrolled. 

- During enrollment the verification score for each signature is determined using the conventional DSV or SSV engines depending on the data type. These use 1:1 comparisons which are assessed using average variability characteristics. Once enrollment has been completed each signature being verified is compared against the range of characteristics measured in the enrollment set, which generally reduces the false acceptance error rate significantly. 

- After enrollment has been completed the reference data set can be periodically updated to track the drift of a user's signing behaviour with time. The minimum elapsed period between updates is set when the template is created


## SDK Delivery


The verification SDK includes the following:

| Name                      | Description |
| ------------------------- | ----------- |
| SignatureEngine Component | The core verification functionality is provided in the form of a COM component. The component is secured using a machine specific license |
| Documentation             | The SDK is supplied with a detailed doxygen API reference |
| Installer                 | An MSI installer is provided to install and register the SDK COM components. A licenser app is also included to report the machine identifier and install the machine license key |

## SDK Sample Application

The C# .NET sample application is supplied with source code and demonstrates the following features :

| Feature                       | Description |
| ----------------------------- | ----------- |
| Options                        | A menu item opens a dialog which allows the user to modify the ConfigurationOptions, ImageOptions and the template folder. The user options are used whenever a new template is created |
| Templates                      | Every template is given a name when it is created. A list of all the templates is displayed in a list box and one of them highlighted as the current template. Controls are provided to delete and reset templates.  Templates are stored in the folder shown on the options dialog |
|  Verify signature from file    | The app allows the user to drag and drop signature files which are then verified using the currently selected template and the results displayed. Dynamic signatures may be in .FSS or .TXT (Base-64 encoded) form. Signature image files can be in any common format, including .PNG, .TIF, .BMP, .JPG etc.  Images containing stegangraphically embedded data will be processed as FSS data. |
|  Capture and verify signature  |  A button is provided to capture a signature using the Signature SDK and verify it against the currently selected template  |

# Additional resources 

## Sample Code
For further samples check Wacom's Developer additional samples, see [https://github.com/Wacom-Developer](https://github.com/Wacom-Developer)

## Documentation
For further details on using the SDK see [Wacom Ink SDK for verification documentation](http://developer-docs.wacom.com/sdk-for-verification/) 

## Support
If you experience issues with the technology components, please see related [FAQs](https://developer-support.wacom.com/hc/en-us)

For further support file a ticket in our **Developer Support Portal** described here: [Request Support](https://developer-support.wacom.com/hc/en-us/requests/new)

## Developer Community 
Join our developer community:

- [LinkedIn - Wacom for Developers](https://www.linkedin.com/company/wacom-for-developers/)
- [Twitter - Wacom for Developers](https://twitter.com/Wacomdevelopers)

## License 
This sample code is licensed under the [MIT License](https://choosealicense.com/licenses/mit/)

---
