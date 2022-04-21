import argparse
import json
import os
import glob
import pprint
import subprocess
import sys
import re

parser = argparse.ArgumentParser(description='Sign binaries for Windows, macOS, and Linux')
parser.add_argument('path', help='Path to file for signing')
parser.add_argument('keycode', help='Platform-specific key code for signing')
parser.add_argument('opcode', help='Platform-specific operation code for signing')
# Setting nargs=argparse.REMAINDER allows us to pass in params that begin with `--`
parser.add_argument('--params', nargs=argparse.REMAINDER, help='Parameters for signing')
args = parser.parse_args()

esrp_tool = os.path.join("esrp", "tools", "EsrpClient.exe")

aad_id = os.environ['AZURE_AAD_ID'].strip()
# We temporarily need two AAD IDs, as we're using an SSL certificate associated
# with an older App Registration until we have the required hardware to approve
# the new certificate in SSL Admin.
aad_id_ssl = os.environ['AZURE_AAD_ID_SSL'].strip()
workspace = os.environ['GITHUB_WORKSPACE'].strip()

source_location = args.path
files = glob.glob(os.path.join(source_location, "*"))

print("Found files:")
pprint.pp(files)

auth_json = {
    "Version": "1.0.0",
    "AuthenticationType": "AAD_CERT",
    "TenantId": "72f988bf-86f1-41af-91ab-2d7cd011db47",
    "ClientId": f"{aad_id}",
    "AuthCert": {
            "SubjectName": f"CN={aad_id_ssl}.microsoft.com",
            "StoreLocation": "LocalMachine",
            "StoreName": "My"
    },
    "RequestSigningCert": {
            "SubjectName": f"CN={aad_id}",
            "StoreLocation": "LocalMachine",
            "StoreName": "My"
    }
}

input_json = {
	"Version": "1.0.0",
	"SignBatches": [
		{
			"SourceLocationType": "UNC",
			"SourceRootDirectory": source_location,
			"DestinationLocationType": "UNC",
			"DestinationRootDirectory": workspace,
			"SignRequestFiles": [],
			"SigningInfo": {
				"Operations": [
					{
						"KeyCode": f"{args.keycode}",
						"OperationCode": f"{args.opcode}",
						"Parameters": {},
						"ToolName": "sign",
						"ToolVersion": "1.0",
					}
				]
			}
		}
	]
}

# add files to sign
for f in files:
	name = os.path.basename(f)
	input_json["SignBatches"][0]["SignRequestFiles"].append(
		{
			"SourceLocation": name,
			"DestinationLocation": os.path.join("signed", name),
		}
	)

# add parameters to input.json (e.g. enabling the hardened runtime for macOS)
if args.params is not None:
	i = 0
	while i < len(args.params):
		input_json["SignBatches"][0]["SigningInfo"]["Operations"][0]["Parameters"][args.params[i]] = args.params[i + 1]
		i += 2

policy_json = {
	"Version": "1.0.0",
	"Intent": "production release",
	"ContentType": "binary",
}

configs = [
	("auth.json", auth_json),
	("input.json", input_json),
	("policy.json", policy_json),
]

for filename, data in configs:
	with open(filename, 'w') as fp:
		json.dump(data, fp)

# Run ESRP Client
esrp_out = "esrp_out.json"
result = subprocess.run(
	[esrp_tool, "sign",
	"-a", "auth.json",
	"-i", "input.json",
	"-p", "policy.json",
	"-o", esrp_out,
	"-l", "Verbose"],
	capture_output=True,
	text=True,
	cwd=workspace)

# Scrub log before printing
log = re.sub(r'^.+Uploading.*to\s*destinationUrl\s*(.+?),.+$',
    '***', 
    result.stdout,
    flags=re.IGNORECASE|re.MULTILINE)
print(log)

if result.returncode != 0:
	print("Failed to run ESRPClient.exe")
	sys.exit(1)

if os.path.isfile(esrp_out):
	print("ESRP output json:")
	with open(esrp_out, 'r') as fp:
		pprint.pp(json.load(fp))

for file in files:
	if os.path.isfile(os.path.join("signed", file)):
		print(f"Success!\nSigned {file}")