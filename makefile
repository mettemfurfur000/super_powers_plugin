cur = ${shell pwd | awk -F'/' '{print $$NF}'}
files = bin/Debug/net8.0/${cur}.*
version = ${shell grep Version src/main.cs | awk -F'=>' '{print $$2}' | tr -d '\ \";'}

mysqldllpath = ${shell grep -r mysqlconnector obj/project.nuget.cache | tr -d ', ' | sed -e 's/mysqlconnector.2.5.0.nupkg.sha512/lib\\net8.0\\MySqlConnector.dll/g'}

all:
	rm -f *.zip
	rm -f MySqlConnector.dll
	rm -f ${cur}.dll
	rm -f ${cur}.pdb
	rm -f ${cur}.deps.json
	dotnet build
	cp ${files} .
	if grep -q MySqlConnector super_powers_plugin.csproj; then cp ${mysqldllpath} . ; fi

.PHONY: release_full
release_full:
	mkdir -p addons/counterstrikesharp/plugins/${cur}
	mkdir -p addons/counterstrikesharp/configs/plugins/${cur}
	mkdir -p addons/counterstrikesharp/shared/${cur}_api
	if grep -q MySqlConnector super_powers_plugin.csproj; then cp MySqlConnector.dll addons/counterstrikesharp/plugins/${cur}/ ; fi
	cp ${cur}.* addons/counterstrikesharp/plugins/${cur}/
	cp ../../configs/plugins/${cur}/${cur}.json addons/counterstrikesharp/configs/plugins/${cur}/
	cp ../../shared/${cur}_api/${cur}_api.* addons/counterstrikesharp/shared/${cur}_api/
	zip -r "${cur}_${version}.zip" addons/*
	rm -rf addons