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
# 	echo "super_powers_plugin_${version}.zip"
	mkdir -p addons/counterstrikesharp/plugins/super_powers_plugin
	cp ${cur_folder_name}.* addons/counterstrikesharp/plugins/super_powers_plugin/
	zip -r "super_powers_plugin_${version}.zip" addons/*
