# Signature Retrieval

---

## Introduction

Signatures captured using the Signature SDK are saved in one of the formats:
- binary signature object (FSS - Forensic Signature Stream)
- text based signature object (base64 encoded signature object)
- embedded signature image (binary signature object embedded steganographically in signature image)
- ISO signature (text based open format signature data, can be created using the Signature SDK or from an external third party source)

The Signature SDK includes API to manage the different formats.  
For example a text based signature:

```
txtSignature = "RlP27RoBGwECGEVDFxYZVCQFBwkLAwYIBA0MFB0cGhsYFQIiBSAQoPNlEBMW0h7fbH6RgfRlKBCF
DSwsynPGSxULGKo32EUCyypDAQEXBAN3aG8WBAN3aHkZIgQgY71Q8SqRqa6bUqt3ogpNbkktzG6k
yOJQqPvw9fYxquVUBAMxNzYkAQEFCgEFAL9PoI0GAgAHCgEFAM87oI0GAgEJCAEFAACQTgQACwfI
AQABAQEAAxkClQEBBwMSLAASQAMiABLAA1gAEEADED+QBtQBApUBqB0IC8sBtv8gDAYAACgCwGg4
AIAUAHevmZ4835jyvm/Oel9Z73vwHy3huTMHNKQORcgUBaA0GwCQQAEAeAMDP9fB/S+R8f3vvNoe
yz4B8roWzQgB5L62//mMyADGSD0fgKyLAgCYAP+YAAKgPBcBgMvAMgLBMAD/38/yf2BIAwIQBwIg
GBOAkVwGwpgA/8AELAHBDgXECBkXwGga/9+d/b/gDAmASAmBYDIHgRjtBgHEAvm/4APAgD7+3Hva
bE8flP9JFA+OxEwhsC/6v59R+OAI1gEClQGIFQkLzQG2/k9R/QAACgJAeL6IFiYnV9hBFT/7+P4P
n+59P4/v8F+24d+n9sCo7AQiMAuD4Bv1/5eo/V3f8PEPsdG/D+X9n7/9AtA+uIOKPgzo9CIS8FoS
/yUf6rPnzvC/4hABAOMGg3wPhcAD9T+P7f1fo+b+XtP8vv/z/aAIRoNEIBtOODwLf4I+eR3iAAtg
akxBCD3/DMvh6D+v9CCBFAjFphFDP+H6HvvU+W8LRvw3BAPo+Eal0JgUvoZJ992r73bvs8m+v84D
A2gPhiAMA8DABBcClQEBBwIQLADIgBJADKgBWgDAgBEn/A2GAQKVAZ8CBgp+/BsTwQFAAQEA9wOA
IDQDBUAgA/7+/8B/vz+P1+6kvURACwvgjB0GQYBQNgHCoAQEAECwCAcAQDADo+/tZUAMFPBwUwHB
oAwLAEAgA+7/wDAIDwDAsIAOv5/z8QAAsAwMJAD7/v/AMQAPvp9CSAQA8J5FAQDoAwwA97DsagAA
DAgBBQD/AwAAABQIAQQAv08AzzsdCAcGAAwpSTPjHCcmTWljcm9zb2Z0OydXaW5kb3dzIDcnOzs7
Ni4xLjc2MDEuMjM0MTgaFhVTVFU7J1NUVS01MDAnOzEuMC4yOzAbCQgwMDY0O1NUVRgH1fv4wwUA
ABUKAQT3CcdF3yffJw=="
```
A signature object can be created using the API:
```
sig = new ActiveXObject("Florentis.SigObj");
sig.SigText = txtSignature;
```

To create a signature object from an image file containing an embedded signature:
```
sig.ReadEncodedBitmap(filename);
```

To create a signature Object from ISO format import the data, for example:
```
sigObj.ImportIso(SignatureISO, XML, additionalImportIsoData);
```

Once the signature object has been created it can be used in the Wacom Ink SDK for verification API.

To help with development, the MiniScope utility which uses these methods will display signature information.  
MiniScope is available here: [MiniScope Installer](https://developer-docs.wacom.com/sdk-for-signature/docs/en/mini-scope)

The following sections describe the retrieval of signature data from different applications.


## sign pro PDF
Different methods are needed depending on how the PDF document was created:

### Sign pro PDF V3 and later, encrypted PDF

Encrypted documents are best handled using a PDF library. The steps are:

- Open the document using the PDF library
- Iterate through all the form fields in the document
- Identify any fields that are Signature fields
- Extract the metadata stream of each signature field (XML)
- The FSS data is stored in the tag wgssSignpro:SigData

Unencrypted documents can be handled using an XMP library:

- Parse the PDF document looking for any XMP blocks which start with the tag **<?xpacket begin** and ending with **<?xpacket end**
- Use the XMP library to parse the XMP block, looking for the property "http://com.wacomgss.xmp/signpro/1.0/"
- Read the FSS text from the block. 

### sign pro PDF V2 documents

sign pro PDF V2 stores the FSS data by steganographically embedding it in the signature image. To extract the signature data:

- Use a PDF library to parse the document looking for images
- Extract the image
- Attempt to read the steganographic data using the signature SDK. (Note: there will be images without signature data) 

## CLB Paper

PDF documents created using CLB Paper use the Wacom Ink SDK for Documents (BaXter library). 

- Parse the PDF document looking for any XMP blocks which start with the tag <?xpacket begin and ending with <?xpacket end
- Use the XMP library to parse the XMP block, looking for the property "<wgss:PacketType wgss:level=\"field\"/>"
- For each block use XMP library to find the property "http://wacomgss.com/barbera/1.0/" and with the field type "Signature"
- For each Signature  block use XMP library to find the property "http://wacomgss.com/barbera/1.0/" with the field type "Data"
- Read the FSS text from the block


## PDF Plugin

- Use a PDF library to iterate through the COS objects in the PDF document
- Find objects with the **"WGSS_Prop_SigData"** key
- Extract the text between the "(" and ")", and remove all "\r\n" character sequences


---
