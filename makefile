cur = ${shell pwd | awk -F'/' '{print $$NF}'}
files = bin/Debug/net8.0/${cur}.*
version = ${shell grep Version src/main.cs | awk -F'=>' '{print $$2}' | tr -d '\ \";'}

path = ${shell grep -r mysqlconnector obj/project.nuget.cache | tr -d ' ' | sed -e 's/mysqlconnector.2.5.0.nupkg.sha512/lib\\net8.0\\MySqlConnector.dll/g'}

all:
	rm -f *.zip
	rm -f MySqlConnector.dll
	rm -f ${cur}.dll
	rm -f ${cur}.pdb
	rm -f ${cur}.deps.json
	dotnet build
	cp ${files} .
	if grep -q MySqlConnector super_powers_plugin.csproj; then cp ${path} . ; fi

.PHONY: release
release:
# make the whole folder tree if it doesn't exist
	mkdir -p addons/counterstrikesharp/plugins/${cur}
	cp ${cur}.* addons/counterstrikesharp/plugins/${cur}/
	zip -r "${cur}_${version}.zip" addons/*

.PHONY: release_config
release_config:
	mkdir -p addons/counterstrikesharp/plugins/${cur}
	mkdir -p addons/counterstrikesharp/configs/plugins/${cur}
# copy the config if its here
	cp ../../configs/plugins/${cur}/${cur}.json addons/counterstrikesharp/configs/plugins/${cur}/
	cp ${cur}.* addons/counterstrikesharp/plugins/${cur}/
	zip -r "${cur}_${version}.zip" addons/*
