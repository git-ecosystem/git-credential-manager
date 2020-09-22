import sys
import os
import hashlib
import base64
import subprocess


if len(sys.argv) != 4:
    print("Usage Error: Unexpected number of arguments.")
    print(f"Usage: {sys.argv[0]} PRIVATE_KEY_ENV_VAR  PASSWORD_ENV_VAR KEY_MD5_ENV_VAR")
    sys.exit(1)

key_env_var = sys.argv[1]
password_env_var = sys.argv[2]
key_md5_env_var = sys.argv[3]

print(f"""Using Env Vars:
key_env_var: {key_env_var}
password_env_var: {password_env_var}
key_md5_env_var: {key_md5_env_var}
""")

# Get env var values
key_b64 = os.environ[key_env_var].strip()
password = os.environ[password_env_var].strip()
key_md5_expected = os.environ[key_md5_env_var].strip()

# Decode private key
key = base64.b64decode(key_b64)
key_md5 = hashlib.md5()
key_md5.update(key)
key_md5_actual = key_md5.hexdigest()

# Check md5 with expected
if key_md5_actual != key_md5_expected:
    print(f"""Actual key md5 did not match expected:
actual   : {key_md5_actual}
expected : {key_md5_expected}
""")
    sys.exit(1)

print(f"Key MD5 Check: passed ✅")
