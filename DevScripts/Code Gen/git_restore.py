'''
This script was created in case a Git Discard is done in all staging area by accident (I wonder why we needed one of these.....)

The idea is to go through Git's lost and found blob data, analyzing them, looking for a specific sub-string that you know would be in the changes
To use this script, do the following:

	1 - Navigate to git's directory in Terminal
	2 - Run "git fsck --lost-found > blob_data"
	3 - Run this script as: "pyton git_restore.py <path_to_blob_data_created> <The substring you wanna search>"
	4 - If this script outputs a "found" message towards a blob hash, fetch these and run: "git show <blob_hash>"

Do this for all blob hashes until you find the ones associated to the changes you just discarded

'''

import sys
import subprocess as sp

restore = open(sys.argv[1], "r").read()
restore = restore.replace("\n", " ")
restore = restore.split(" ")[2::3]
i = 0

for dang in restore:
	i += 1
	print(f"Checking file {i}/{len(restore)} -> {dang}")

	skip = False
	byte_result = sp.run(['git', 'show', dang], stdout=sp.PIPE, stderr=sp.PIPE).stdout

	try:
		result = byte_result.decode('utf-8')
	except UnicodeDecodeError:
		skip = True

	if(skip):
		continue

	if(sys.argv[2] in result):
		print(f"Found at: {dang}")