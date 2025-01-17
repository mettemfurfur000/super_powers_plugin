cur_folder_name = ${shell pwd | awk -F'/' '{print $$NF}'}

all:
	rm -f *.zip
	rm -f ${cur_folder_name}.dll
	rm -f ${cur_folder_name}.pdb
	rm -f ${cur_folder_name}.deps.json
	dotnet build
	cp bin/Debug/net8.0/${cur_folder_name}.* .
	zip package.zip ${cur_folder_name}.*
	