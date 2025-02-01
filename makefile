cur_folder_name = ${shell pwd | awk -F'/' '{print $$NF}'}
files = bin/Debug/net8.0/${cur_folder_name}.*

all:
	rm -f *.zip
	rm -f ${cur_folder_name}.dll
	rm -f ${cur_folder_name}.pdb
	rm -f ${cur_folder_name}.deps.json
	dotnet build
	cp ${files} .
	zip package.zip ${cur_folder_name}.*
