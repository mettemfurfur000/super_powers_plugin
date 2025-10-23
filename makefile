cur_folder_name = ${shell pwd | awk -F'/' '{print $$NF}'}
files = bin/Debug/net8.0/${cur_folder_name}.*
version = ${shell grep Version src/main.cs | awk -F'=>' '{print $$2}' | tr -d '\ \";'}

all:
	rm -f *.zip
	rm -f ${cur_folder_name}.dll
	rm -f ${cur_folder_name}.pdb
	rm -f ${cur_folder_name}.deps.json
	dotnet build
	cp ${files} .

.PHONY: release
release:
	mkdir -p addons/counterstrikesharp/plugins/${cur_folder_name}
	cp ${cur_folder_name}.* addons/counterstrikesharp/plugins/${cur_folder_name}/
	zip -r "${cur_folder_name}_${version}.zip" addons/*

.PHONY: release_config
release_config:
	mkdir -p addons/counterstrikesharp/plugins/${cur_folder_name}
	mkdir -p addons/counterstrikesharp/configs/plugins/${cur_folder_name}
# copy the config if its here
	cp ../../configs/plugins/${cur_folder_name}/${cur_folder_name}.json addons/counterstrikesharp/configs/plugins/${cur_folder_name}/
	cp ${cur_folder_name}.* addons/counterstrikesharp/plugins/${cur_folder_name}/
	zip -r "${cur_folder_name}_${version}.zip" addons/*
